﻿using System;
using System.Diagnostics;
using MyShogi.App;
using MyShogi.Model.Common.ObjectModel;
using MyShogi.Model.Common.Utility;
using MyShogi.Model.Shogi.Core;
using MyShogi.Model.Shogi.Data;
using MyShogi.Model.Shogi.Player;

namespace MyShogi.Model.Shogi.LocalServer
{
    public partial class LocalGameServer
    {
        #region UI側からのコマンド

        /*
         * UI側からのコマンドは、 ～Command()というメソッドが呼び出される。
         * これはdelegateでWorker Thread(対局監視スレッド)に渡され、実行される。
         * delegateのなかのkifuManager.PositionやkifuManager.KifuListは、無名関数の束縛の性質から、
         * 現在のものであって過去のPositionやKifuListのへ参照ではない。
         *
         * また、処理するのは対局監視スレッドであるから(対局監視スレッドはシングルスレッドでかつ、対局監視スレッド側でしか
         * Position.DoMove()は行わないので)、これが処理されるタイミングでは、kifuManager.Positionは最新のPositionであり、
         * これを調べているときに他のスレッドが勝手にPosition.DoMove()を行ったり、他のコマンドを受け付けたり、持ち時間切れに
         * なったりすることはない。
         */

        /// <summary>
        /// 対局スタート
        /// </summary>
        public void GameStartCommand(GameSetting gameSetting)
        {
            AddCommand(
            () =>
            {
                if (!InTheGame)
                {
                    // 現局面から開始するとき、局面が非合法局面であれば受理しない。
                    if (gameSetting.BoardSetting.BoardTypeCurrent)
                    {
                        var error = Position.IsValid();
                        if (error != null)
                        {
                            TheApp.app.MessageShow(error , MessageShowType.Error);
                            return;
                        }
                    }

                    // いったんリセット
                    Disconnect();
                    GameStart(gameSetting);

                    // エンジンの初期化が終わったタイミングで自動的にNotifyTurnChanged()が呼び出される。
                }
            });
        }

        /// <summary>
        /// ユーザーから指し手が指されたときにUI側から呼び出す。
        ///
        /// ユーザーがマウス操作によってmの指し手を入力した。
        /// ユーザーはこれを合法手だと思っているが、これが受理されるかどうかは別の話。
        /// (時間切れなどがあるので)
        ///
        /// 注意)
        /// GameScreenControlを用いる場合は、このメソッドを使わずに
        /// GameScreenControlの同名のメソッドを用いること。
        /// </summary>
        /// <param name="m"></param>
        public void DoMoveCommand(Move m , Action OnCompleted = null)
        {
            // →　駒の移動が確定しても、DoMoveCommand()が受理されるまで新しい場所に駒が描画されないのに
            // StateReset()でDirtyになるから描画が先に行われることがあり、
            // そのときの駒の描画される升がおかしくて(移動元の升に描画される)、すごく違和感がある。
            // ゆえにこのとき描画を抑制しなければならない。
            // 描画自体は、DoMoveCommand()でPositionの変更が発生すれば、そのハンドラにて描画が促されるわけで…。

            // →　駒がドラッグ移動されて、移動をLocalGameServerにCommandとして送信したときに、それが受理されるまで
            // 画面更新を抑制することでこの問題を回避するのは間違っている。いつ描画されても正しく描画されるようにすべき。

            // つまり、DoMoveCommand()が受理されてからStateReset()が呼び出されるべきであり、
            // DoMoveCommand()は、lambdaをとれるようにする必要がある。

            // 本当は、Completedイベントと、画面の更新通知(PositionUpdate)とはatomicに行う必要がある。
            // Completedイベントをあとから送っているので、
            // 内部状態変更→駒移動→PositionUpdate→描画→Completeイベント
            // の順番になる可能性があることに注意。

            // 内部状態変更をCompleteイベントで行うのは気持ち悪いので(前の状態のままなので別の操作などを
            // 受け付けてしまう可能性がある)、surpressDraw = trueにして、更新が終わるまで画面描画を行わない。
            // CompleteイベントでsurpressDraw = falseにすべきである。

            AddCommand(
            () =>
            {
                try
                {
                    var stm = kifuManager.Position.sideToMove;
                    var stmPlayer = Player(stm);
                    var config = TheApp.app.Config;

                    if (InTheGame)
                    {
                        // 対局中は、Human以外であれば受理しない。
                        if (stmPlayer.PlayerType == PlayerTypeEnum.Human)
                        {
                            // これを積んでおけばworker_threadのほうでいずれ処理される。(かも)
                            // 仮に、すでに次の局面になっていたとしても、次にこのユーザーの手番になったときに
                            // BestMove = Move.NONEとされるのでその時に破棄される。
                            stmPlayer.BestMove = m;
                        }
                    }
                    else if (GameMode.IsConsideration())
                    {

                        // 対局中でなく、盤面編集中でなければ自由に動かせる。
                        // 受理して、必要ならば分岐棋譜を生成して…。

                        var misc = config.GameSetting.MiscSettings;
                        if (!kifuManager.Tree.DoMoveUI(m, misc))
                            return;

                        // DoMoveに成功したので駒音を再生する。
                        PlayPieceSound(GameMode, m.To(), stm);

                        // 動かした結果、棋譜の選択行と異なる可能性があるので、棋譜ウィンドウの当該行をSelectしなおす。
                        UpdateKifuSelectedIndex();

                        // 再度、Thinkコマンドを叩く。
                        if (GameMode.IsConsiderationWithEngine())
                            NotifyTurnChanged();
                    }
                }
                finally
                {
                    // 完了時のイベント
                    OnCompleted?.Invoke();
                }
            }
            );
        }

        /// <summary>
        /// エンジンに対して、いますぐに指させる。
        /// 受理されるかどうかは別。
        /// </summary>
        public void MoveNowCommand()
        {
            AddCommand(
            () =>
            {
                if (InTheGame)
                {
                    var stm = kifuManager.Position.sideToMove;
                    var stmPlayer = Player(stm);

                    // 手番側がエンジン以外であれば受理しない。
                    if (stmPlayer.PlayerType == PlayerTypeEnum.UsiEngine)
                    {
                        var enginePlayer = stmPlayer as UsiEnginePlayer;
                        enginePlayer.MoveNow();
                    }
                }
            });
        }

        /// <summary>
        /// ユーザーによる対局中の2手戻し
        /// 受理できるかどうかは別
        /// </summary>
        public void UndoCommand()
        {
            AddCommand(
            () =>
            {
                if (InTheGame)
                {
                    var stm = kifuManager.Position.sideToMove;
                    var stmPlayer = Player(stm);

                    // 人間の手番でなければ受理しない
                    if (stmPlayer.PlayerType == PlayerTypeEnum.Human)
                    {
                        // 棋譜を消すUndo()
                        kifuManager.UndoMoveAndRemoveKifu();
                        kifuManager.UndoMoveAndRemoveKifu();

                        // 残り持ち時間などを巻き戻さないといけない。
                        // ただし、再開局で開始局面以上に巻き戻すと、いまより持ち時間が減ってしまう可能性がある。
                        // 開始局面以上に巻き戻す場合は、再開時の時間にしてやる必要がある。
                        // しかし、中断局であるなら、遡れなければならない。難しい。

                        var nextTime = kifuManager.Tree.GetKifuMoveTimes();
                        PlayTimers.SetKifuMoveTimes(nextTime);

                        // これにより、2手目の局面などであれば1手しかundoできずに手番が変わりうるので手番の更新を通知。
                        NotifyTurnChanged();

                        // TreeのUndoにより、KifuListの更新がかかり、選択行が変化して、
                        // 更新通知が来て、そのハンドラのなかでSetKifuMoveTimes()とNotifyTurnChanged()を
                        // 行っているので、本来、上の文は不要なのだが、そのハンドラでは、Consideration中しか
                        // その処理を行わない(ユーザーの棋譜選択でそこが変更されると困る)ので上記処理は必要。
                    }
                }
            });
        }

        /// <summary>
        /// UI側からの中断要求。
        /// </summary>
        public void GameInterruptCommand()
        {
            AddCommand(
            () =>
            {
                if (InTheGame)
                {
                    // コンピューター同士の対局中であっても人間判断で中断できなければならないので常に受理する。
                    var stm = kifuManager.Position.sideToMove;
                    var stmPlayer = Player(stm);

                    // 連続対局の中止のためにカウンターをリセットしておく。
                    continuousGame.ResetCounter();

                    // 中断の指し手
                    stmPlayer.SpecialMove = Move.INTERRUPT;
                }
            });
        }

        /// <summary>
        /// 棋譜の選択行が変更になった。
        /// 対局中でなければ、現在局面をその棋譜の局面に変更する。
        ///
        /// このメソッドを直接呼び出さずに、this.KifuListSelectedIndexのsetterか、UpdateKifuSelectedIndex()を用いること。
        /// </summary>
        private void KifuListSelectedIndexChangedCommand(PropertyChangedEventArgs args)
        {
            AddCommand(
            () =>
            {
                if (GameMode.IsConsideration())
                {
                    // 現在の局面と違う行であるかを判定して、同じ行である場合は、
                    // このイベントを処理してはならない。

                    // 無理やりではあるが棋譜のN行目に移動出来るのであった…。
                    int selectedIndex = (int)args.value;
                    kifuManager.Tree.GotoSelectedIndex(selectedIndex);
                    PlayTimers.SetKifuMoveTimes(kifuManager.Tree.GetKifuMoveTimes());

                    // 検討モードのとき)
                    //   思考エンジンでの検討中       → 局面が変わったので思考しなおすためにNotifyTurnChanged()が必要。
                    //   思考エンジンで検討していない → 掴んでいる駒を離す必要があるためにNotifyTurnChanged()が必要。
                    if (GameMode.IsConsideration())
                    {
                        NotifyTurnChanged();
                    }
                }
            });
        }

        /// <summary>
        /// 棋譜(string型)の読み込みコマンド
        /// ファイルから読み込むには、MainMenu.ReadKifuFile()のようなメソッドを用いること。
        ///
        /// merge == trueなら、棋譜を分岐棋譜としてマージする。
        /// dirtyは、このあと、KifuDirty(棋譜が汚れているかのフラグ)に設定される値。
        /// </summary>
        /// <param name="kifuText"></param>
        public void KifuReadCommand(string kifuText , bool merge = false , bool dirty = false)
        {
            AddCommand(
            () =>
            {
                if (GameMode.CanUserMove())
                {
                    // 対局中ではないので、EnableKifuList == falseになっているが、
                    // 一時的にこれをtrueにしないと、読み込んだ棋譜に対して、Tree.KifuListが同期しない。
                    // ゆえに、読み込みの瞬間だけtrueにして、そのあとfalseに戻す。
                    kifuManager.EnableKifuList = true;

                    var oldSelectedIndex = kifuManager.Tree.pliesFromRoot;

                    var error = merge ? kifuManager.MergeFromString(kifuText) : kifuManager.FromString(kifuText);
                    // マージした場合、その棋譜の末尾に行って欲しい気もするが…。

                    kifuManager.EnableKifuList = false;

                    if (error != null)
                    {
                        TheApp.app.MessageShow((merge ? "棋譜のマージに失敗しました。\n" : "棋譜の読み込みに失敗しました。\n") + error,  MessageShowType.Error);

                        kifuManager.Init(); // 不正な局面のままになるとまずいので初期化。

                    } else
                    {

                        // 読み込みが完了すれば自動的に末尾の局面に行っているはずだが、
                        // 棋譜ウィンドウを更新した結果、分岐局面などに戻ってしまうといけない。

                        // 棋譜に書かれていた持ち時間設定・残り時間を画面に反映させる。(GameSettingには反映させない)
                        PlayTimers.SetKifuTimeSettings( kifuManager.Tree.KifuTimeSettings );
                        PlayTimers.SetKifuMoveTimes(kifuManager.Tree.GetKifuMoveTimes());
                        UpdateTimeString();

                        // 末尾の局面に移動するコマンドを叩いておく。
                        // マージ時は、マージ起点に移動してやる。たぶん現在の選択していた行がそれ。
                        if (!merge)
                        {
                            // 通常の棋譜のオープンであるなら、
                            // config.MovesWhenKifuOpenの値を反映させる。
                            // これが0なら開始局面、1なら終局図
                            switch (TheApp.app.Config.MovesWhenKifuOpen)
                            {
                                case 0:
                                    UpdateKifuSelectedIndex(0);
                                    break;

                                case 1:
                                    UpdateKifuSelectedIndex(int.MaxValue);
                                    break;
                            }
                        }
                        else
                            UpdateKifuSelectedIndex(oldSelectedIndex + 1);

                        // -- 棋譜上の名前を表示名に反映させる。

                        foreach (var c in All.Colors())
                        {
                            var name = kifuManager.KifuHeader.GetPlayerName(c);
                            SetPlayerName(c, name);
                        }
                    }

                    // 棋譜が綺麗になった扱いにする。(この棋譜はファイルなどに丸ごと保存されているはずであるから)
                    // ただし、mergeのときは汚れたという扱いにする。(ファイルに残っていないので)
                    KifuDirty = dirty;

                    // 駒を持ち上げていたりしたらそれをリセットする必要があるので。
                    RaisePropertyChanged("TurnChanged");
                }
            });
        }

        /// <summary>
        /// 棋譜のファイルへの書き出しコマンド
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        public void KifuWriteCommand(string path , Kifu.KifuFileType type)
        {
            AddCommand(
            () =>
            {
                // ゲーム中でも書き出せる
                // (メニュー上、オフにはなっているが..)

                try
                {
                    var content = kifuManager.ToString(type);
                    FileIO.WriteFile(path, content);

                    // 棋譜が綺麗になった扱いにする。
                    KifuDirty = false;
                }
                catch (Exception ex)
                {
                    TheApp.app.MessageShow($"棋譜ファイルの書き出しに失敗しました。\r\n{ex.Pretty()}" , MessageShowType.Error);
                }
            });
        }

        /// <summary>
        /// 棋譜のクリップボードへの書き出しコマンド
        /// </summary>
        /// <param name="type"></param>
        public void KifuWriteClipboardCommand(Kifu.KifuFileType type)
        {
            AddCommand(() =>
            {
                try
                {
                    var content = kifuManager.ToString(type);
                    // Clipboard.SetText() を実行するスレッドは Single Thread Apartment モードに設定されていなければならない
                    TheApp.app.UIThread(() => SafeClipboard.SetText(content));
                }
                catch (Exception ex)
                {
                    TheApp.app.MessageShow($"棋譜のクリップボードへの書き出しに失敗しました。\r\n{ex.Pretty()}", MessageShowType.Error);
                }
            });
        }

        /// <summary>
        /// 現在の局面のファイルへの書き出しコマンド
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        public void PositionWriteCommand(string path , Kifu.KifuFileType type)
        {
            AddCommand(
            () =>
            {
                try
                {
                    var content = kifuManager.ToPositionString(type);
                    FileIO.WriteFile(path, content);
                }
                catch (Exception ex)
                {
                    TheApp.app.MessageShow($"棋譜ファイルの書き出しに失敗しました。\r\n{ex.Pretty()}" , MessageShowType.Error);
                }
            });
        }

        /// <summary>
        /// 現在の局面のクリップボードへの書き出しコマンド
        /// </summary>
        /// <param name="type"></param>
        public void PositionWriteClipboardCommand(Kifu.KifuFileType type)
        {
            AddCommand(() =>
            {
                try
                {
                    var content = kifuManager.ToPositionString(type);
                    TheApp.app.UIThread(() => SafeClipboard.SetText(content));
                }
                catch (Exception ex)
                {
                    TheApp.app.MessageShow($"局面のクリップボードへの書き出しに失敗しました。\r\n{ex.Pretty()}", MessageShowType.Error);
                }
            });
        }

        /// <summary>
        /// 本譜以外の分岐をクリアするコマンド
        /// </summary>
        public void ClearSubKifuTreeCommand()
        {
            AddCommand(() =>
            {
                // 対局中は使用不可
                if (GameMode.IsConsideration())
                {
                    kifuManager.Tree.ClearSubKifuTree();
                    UpdateKifuSelectedIndex();
                }
            });
        }

        /// <summary>
        /// 本譜の手順に戻るボタン
        /// </summary>
            public void MainBranchButtonCommand(PropertyChangedEventArgs args)
        {
            AddCommand(
            () =>
            {
                // 対局中は使用不可
                if (GameMode.IsConsideration())
                {
                    // 本譜の手順に戻るので本譜に移動したあと最初の分岐の起点まで局面を移動する。
                    int branch = kifuManager.Tree.KifuBranch;

                    kifuManager.Tree.MainBranch();

                    // ここが分岐の起点だったのでここのnode選択する。
                    // 1) branch == -1のときは、plyの局面に移動したい　   → UpdateKifuSelectedIndex()の引数は-1 == branch。
                    // 2) branch != -1のときは、branchの局面に移動したい　→ UpdateKifuSelectedIndex()の引数はbranch。
                    // つまりは以下のように書くだけで良い。
                    UpdateKifuSelectedIndex(branch);
                }
            });
        }

        /// <summary>
        /// 棋譜の次分岐に移動するボタン
        /// </summary>
        public void NextBranchButtonCommand(PropertyChangedEventArgs args)
        {
            AddCommand(
            () =>
            {
                // 対局中は使用不可
                if (GameMode.IsConsideration())
                {
                    kifuManager.Tree.NextBranch();
                    UpdateKifuSelectedIndex();
                }
            });
        }

        /// <summary>
        /// 棋譜の分岐削除ボタン
        /// </summary>
        public void EraseBranchButtonCommand(PropertyChangedEventArgs args)
        {
            AddCommand(
            () =>
            {
                // 対局中は使用不可
                if (GameMode.IsConsideration())
                {
                    kifuManager.Tree.EraseBranch();
                    UpdateKifuSelectedIndex();
                }
            });
        }

        /// <summary>
        /// 棋譜の1手削除ボタン
        /// </summary>
        public void RemoveLastMoveButtonCommand(PropertyChangedEventArgs args)
        {
            AddCommand(
            () =>
            {
                // 対局中は使用不可
                if (GameMode.IsConsideration())
                {
                    // 末尾に移動して…
                    kifuManager.GotoLastMove();

                    // 1手戻る
                    kifuManager.EnableKifuList = true;
                    kifuManager.UndoMoveAndRemoveKifu();
                    kifuManager.EnableKifuList = false;

                    // これにより検討中の局面が変更になる可能性がある。
                    // KifuControlのほうで選択行の更新イベントが発生して、巡り巡って
                    // KifuListSelectedIndexChangedCommand()が呼び出される。
                    // KifuListSelectedIndexChangedCommand()のなかで、残り時間の巻き戻しなどを
                    // しているので、この処理で問題ない。
                }
            });
        }

        /// <summary>
        /// 編集した盤面を代入する
        /// 盤面編集用。
        ///
        /// DoMoveCommand()と同様の理由によりOnCompletedを取れる。
        ///
        /// 注意)
        /// GameScreenControlを用いる場合は、このメソッドを使わずに
        /// GameScreenControlの同名のメソッドを用いること。
        /// </summary>
        public void SetSfenCommand(string sfen , Action OnCompleted = null)
        {
            AddCommand(
            ()=>
            {
                try
                {
                    // 盤面編集中以外使用不可
                    if (InTheBoardEdit)
                    {

                        var error = kifuManager.FromString($"sfen {sfen}");
                        // sfenのparser経由で代入するのが楽ちん。
                        if (error != null)
                            TheApp.app.MessageShow(error, MessageShowType.Error);
                    }
                } finally
                {
                    OnCompleted?.Invoke();
                }

            }
            );
        }

        /// <summary>
        /// ゲームモードを変更する。
        /// nextMode : 次のモード。盤面編集モード、検討モードなど。対局中には遷移できない。
        /// </summary>
        public void ChangeGameModeCommand(GameModeEnum nextMode)
        {
            AddCommand(
            () =>
            {
                // いずれにせよ、対局中は受理しない。
                if (InTheGame)
                    return;

                // InTheGameの値を変更するのは、このworker threadのみなので、
                // これにより、「!InTheGameならInTheBoardEditをtrueにする」という操作のatomic性が保証される。

                // また、検討中であれば、エンジンを停止させる必要があるが、それはGameModeのsetterで行う。

                GameMode = nextMode;
            }
            );
        }

        /// <summary>
        /// 開始局面をsfenで与えて、そのあとの指し手をmovesとして渡すとそれを棋譜として読み込む。
        /// 継ぎ盤用。ply手進めた局面にする。
        /// </summary>
        public void SetBoardDataCommand(MiniShogiBoardData data , int ply)
        {
            AddCommand(
            () =>
            {
                Debug.Assert(data != null);

                // 対局中ではないので、EnableKifuList == falseになっているが、
                // 一時的にこれをtrueにしないと、読み込んだ棋譜に対して、Tree.KifuListが同期しない。
                // ゆえに、読み込みの瞬間だけtrueにして、そのあとfalseに戻す。
                kifuManager.EnableKifuList = true;
                var error = kifuManager.FromString(Core.Util.RootSfenAndMovesToUsiString(data.rootSfen , data.moves));
                kifuManager.EnableKifuList = false;

                if (error != null)
                    TheApp.app.MessageShow(error, MessageShowType.Error);
                else
                {
                    // rootの局面からply手進める
                    UpdateKifuSelectedIndex(ply);
                }

            }
            );
        }

        /// <summary>
        /// 検討中に検討ウィンドウの「候補手」のところが変更になった時に呼び出される。
        /// </summary>
        /// <param name="multiPv"></param>
        public void ChangeMultiPvCommand(/*int instance_number , */ int multiPv)
        {
            AddCommand(
            () =>
            {
                // 保存しておく。(次回検討時用に)
                var config = TheApp.app.Config;
                config.ConsiderationMultiPV = multiPv;

                // エンジンによる検討モードでないなら受理しない。
                if (GameMode != GameModeEnum.ConsiderationWithEngine)
                    return;

                // これで検討ウィンドウがクリアされて再度思考を開始するはず…。
                NotifyTurnChanged();
            });
        }

#region 形勢グラフ用。あとで見直す。

        /// <summary>
        /// 評価値の更新
        /// </summary>
        public void ThinkReportChangedCommand(Usi.UsiThinkReportMessage message)
        {
            if (message.type != Usi.UsiEngineReportMessageType.UsiThinkReport) return;

            var thinkReport = message.data as Usi.UsiThinkReport;
            if (thinkReport.Moves == null && thinkReport.InfoString == null) return;
            if (thinkReport.MultiPvString != null && thinkReport.MultiPvString != "1") return;
            if (thinkReport.Eval == null || thinkReport.Eval.Eval == EvalValue.NoValue) return;

            var currentNode = thinkReport.KifuNode;
            if (currentNode == null)
                return; // わからん。詰将棋とかで思考情報がないのでは..ToDo:あとでなおす[2020/05/21]

            if (currentNode.thinkmsgs == null)
                currentNode.thinkmsgs = new System.Collections.Generic.List<Usi.UsiThinkReportMessage>();
            currentNode.thinkmsgs.Add(message);


            // ToDo: 場合によっては GameMode が InTheGame から外れてから対局中の思考を受信する場合があるので、numberの振り分け方を再考する
            var number = message.number + (GameMode != GameModeEnum.InTheGame ? 2 : 0);

            while (currentNode.evalList.Count <= number) currentNode.evalList.Add(new EvalValueEx(EvalValue.NoValue, ScoreBound.Exact));
            switch (number)
            {
                case 0:
                    currentNode.evalList[number] = thinkReport.Eval;
                    break;
                case 1:
                    currentNode.evalList[number] = thinkReport.Eval.negate();
                    break;
                default:
                    currentNode.evalList[number] = kifuManager.Position.sideToMove == Color.BLACK ? thinkReport.Eval : thinkReport.Eval.negate();
                    break;
            }
        }

        /// <summary>
        /// 評価値グラフ用データ生成
        /// </summary>
        public EvaluationGraphData GetEvaluationGraphDataCommand(EvaluationGraphType evalType = EvaluationGraphType.TrigonometricSigmoid, bool reverse = false)
        {
            var dataList = new System.Collections.Generic.List<GameEvaluationData>
            {
                new GameEvaluationData{ values = new System.Collections.Generic.List<EvalValue>() },
                new GameEvaluationData{ values = new System.Collections.Generic.List<EvalValue>() },
            };
            var ply = kifuManager.Tree.gamePly - 1;
            var n = kifuManager.Tree.currentNode;
            var endIndex = -1;
            while (ply >= 0)
            {
                if (n.evalList != null)
                {
                    while (n.evalList.Count > dataList.Count) dataList.Add(new GameEvaluationData{ values = new System.Collections.Generic.List<EvalValue>() });
                    for (var p = 0; p < n.evalList.Count; ++p)
                    {
                        while (ply >= dataList[p].values.Count) dataList[p].values.Add(EvalValue.NoValue);
                        dataList[p].values[ply] = n.evalList[p].Eval;
                        endIndex = System.Math.Max(endIndex, ply);
                    }
                }
                if (n.prevNode == null) break;
                n = n.prevNode;
                --ply;
            }
            for (n = kifuManager.Tree.currentNode, ply = kifuManager.Tree.gamePly; n.moves.Count > 0; ++ply)
            {
                n = n.moves[0].nextNode;
                if (n == null) break;
                if (n.evalList == null) continue;
                while (n.evalList.Count > dataList.Count) dataList.Add(new GameEvaluationData{ values = new System.Collections.Generic.List<EvalValue>() });
                for (var p = 0; p < n.evalList.Count; ++p)
                {
                    while (ply >= dataList[p].values.Count) dataList[p].values.Add(EvalValue.NoValue);
                    dataList[p].values[ply] = n.evalList[p].Eval;
                    endIndex = System.Math.Max(endIndex, ply);
                }
            }
            foreach (var data in dataList)
            {
                while (data.values.Count <= endIndex) data.values.Add(EvalValue.NoValue);
            }
            return new EvaluationGraphData
            {
                data_array = dataList.ToArray(),
                maxIndex = endIndex + 1,
                selectedIndex = kifuManager.Tree.gamePly - 1,
                type = evalType,
                reverse = reverse,
            };
        }
#endregion

        /// <summary>
        /// UI側から、worker threadで実行して欲しいコマンドを渡す。
        /// View-ViewModelアーキテクチャにおいてViewからViewModelにcommandを渡す感じ。
        /// ここで渡されたコマンドは、CheckUICommand()で吸い出されて実行される。
        /// </summary>
        /// <param name="command"></param>
        private void AddCommand(UICommand command)
        {
            // workerを作っていないなら、自分のスレッドで実行すれば良い。
            if (NoThread)
            {
                command();
                return;
            }

            lock (UICommandLock)
            {
                UICommands.Add(command);
            }
        }

#endregion
    }
}
