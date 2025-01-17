﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MyShogi.App;
using MyShogi.Model.Common.Collections;
using MyShogi.Model.Common.ObjectModel;
using MyShogi.Model.Common.Process;
using MyShogi.Model.Common.String;
using MyShogi.Model.Common.Tool;
using MyShogi.Model.Common.Utility;
using MyShogi.Model.Shogi.Core;

namespace MyShogi.Model.Shogi.Usi
{
    /// <summary>
    /// USI engineとのやりとりを抽象化するクラス
    /// 
    /// 読み筋をNotifyObjectを使って通知した時、UIスレッドに渡されて描画される。
	/// この時、次の局面に行っていないことを保証される。
    /// なぜなら、BeginInvoke()で呼び出すとき、UIスレッドのメソッドが呼び出される順が入れ替わることはなく、
    /// 最初に局面の初期化コマンド(EngineInfoのSetRootSfen)から、PV送信(EngineInfoのEngineConsiderationPvData)の
    /// 送信順は保証されるため、異なる局面のPVを検討ウィンドウが表示してしまうということはない。

    /// </summary>
    public class UsiEngine : NotifyObject
    {
        public UsiEngine()
        {
            State = UsiEngineState.Init;

            ThinkingBridge = new UsiEngineThinkingBridge()
            {
                SendCommand = SendCommand
            };
        }

        /// <summary>
        /// 思考エンジンに接続する。
        /// </summary>
        /// <param name="data"></param>
        public void Connect(ProcessNegotiatorData data)
        {
            Disconnect(); // 前の接続があるなら切断する。

            Log.Write(LogInfoType.UsiServer, $"思考エンジンに接続 , ExeFilePath = {data.ExeFilePath}");

            try
            {
                negotiator = new ProcessNegotiator();
                negotiator.Connect(data);
                negotiator.CommandReceived += UsiCommandHandler;
                // ProcessNegotiator.Read()を呼び出すまではハンドラの処理が行われないので、
                // この形で書いても、最初のメッセージを取りこぼすことはない。

                ChangeState(UsiEngineState.Connected);
            }
            catch (Exception ex)
            {
                // 思考エンジンに接続できないとき、Win32Exceptionが飛んでくる
                ChangeState(UsiEngineState.ConnectionFailed);
                
                Exception = new Exception("思考エンジンへの接続に失敗しました。\r\nファイル名 : " + data.ExeFilePath +
                    "\r\n" + ex.Pretty());
            }
        }

        /// <summary>
        /// エンジンに対して応答をしたい時に定期的に呼び出す。
        /// これを呼び出したスレッドで処理される。
        /// </summary>
        public void OnIdle()
        {
            if (negotiator == null)
                return;

            try
            {
                negotiator.Read();

                // "usi"から15秒、"readyok"から30秒。ただし延長あり。
                if (State == UsiEngineState.WaitUsiOk && DateTime.Now >= timeoutTime)
                    ChangeState(UsiEngineState.ConnectionTimeout);
                else if (State == UsiEngineState.WaitReadyOk && DateTime.Now >= timeoutTime)
                    ChangeState(UsiEngineState.IsReadyTimeout);

            }
            catch (Exception ex)
            {
                //Exception = new Exception("思考エンジンとの通信で例外が発生しました。\r\n" + ex.Pretty());
                // →　例外のなかに例外があって読みづらいメッセージ。

                Exception = ex;
            }

            if (negotiator.ProcessTerminated && Exception == null /* 例外が飛んできて終了したわけではなさそう */)
                Exception = new Exception("思考エンジンが予期せず終了しました。");
        }

        /// <summary>
        /// エンジンに思考させる。
        /// Thinkingの時に呼び出された場合、現在のThinkに対してstopを呼び出して、
        /// bestmoveが返ってきてから次のthinkを行う。
        /// </summary>
        /// <param name="usiPositionString"></param>
        public void Think(Kifu.KifuNode node, string usiPositionString , UsiThinkLimit limit , Color sideToMove)
        {
            if (State != UsiEngineState.InTheGame)
            {
                // ゲーム中以外でThinkを呼び出している。
                // 駒を持ち上げて、その状態で検討ボタンを押すとNotifyTurnChanged()が呼び出されて、
                // エンジン側が未初期化なのにこのメソッドが呼び出されるなど…。
                // このあと、エンジンの初期化が終わったタイミングで再度、NotifyTurnChanged()から
                // このメソッドが呼び出されるはずなので、今回は単に無視しておくだけで良い。

                Log.Write(LogInfoType.UsiServer , "InTheGameではないのにThink()を呼び出している。");
                return;
            }

            if (IsMateSearch)
                ThinkingBridge.Think(node, $"position {usiPositionString}" , $"go {limit.ToUsiMateString(sideToMove)}");
            else
                ThinkingBridge.Think(node, $"position {usiPositionString}" , $"go {limit.ToUsiString(sideToMove)}");
        }

        /// <summary>
        /// いますぐに指させる。
        /// go ponderに対して bestmoveが欲しいときにもこれを用いる。
        /// </summary>
        public void MoveNow()
        {
            // 思考中であれば、stopコマンドを送信することで思考を中断できる(はず)
            ThinkingBridge.Stop();
        }

        /// <summary>
        /// エンジンに対してコマンドを送信する。(デバッグ用)
        /// 普段は、このクラスが自動的にやりとりをするので外部からこのメソッドを呼び出すことはない。(はず)
        /// 
        /// 基本的にノンブロッキングだと考えられる。
        /// </summary>
        /// <param name="command"></param>
        public void SendCommand(string command)
        {
            if (negotiator == null)
                return;

            try
            {
                negotiator.Write(command);
            }
            catch (Exception ex)
            {
                Exception = new Exception("思考エンジンとの通信が切断されました。\r\n" + ex.Pretty());
            }

        }

        public void Disconnect()
        {
            if (State != UsiEngineState.Init)
                SendCommand("quit");

            if (negotiator != null)
            {
                negotiator.Dispose();
                negotiator = null;
            }
        }

        public void Dispose()
        {
            ThinkReport = UsiEngineReportMessageType.UsiThinkEnd;
            Disconnect();
        }

        // -- public members

        /// <summary>
        /// エンジンから受け取ったoptionの一覧
        /// 
        /// "State" propertyをハンドルして、State == UsiOkに変化した時にこのValueをセットしなおしたり、
        /// 変更したりすると良い。(その値が"setoption"でエンジンに渡される。)
        /// </summary>
        public List<UsiOption> OptionList { get; } = new List<UsiOption>();

        /// <summary>
        /// エンジンの設定ダイアログ用であるか？
        /// Connect()の前に設定すべし。
        /// 
        /// これがtrueだと"usi"コマンドは送信するが、"isready"コマンドの送信はしない。
        /// これがfalseだと"usi"送信→"usiok"受信→"isready"送信→"readyok"受信→"usinewgame"送信 まで待つ。
        /// </summary>
        public bool EngineSetting { get; set; }

        // -- 以下、engine側から渡された情報など

        /// <summary>
        /// エンジンのオリジナル名を取得または設定します。
        /// "id name ..."と渡されたもの。
        /// </summary>
        public string OriginalName { get; set; }

        /// <summary>
        /// エンジンの開発者名を取得または設定します。
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// USIプロトコルによってengine側から送られてきた"bestmove .."を解釈した指し手
        /// </summary>
        public Move BestMove { get { return ThinkingBridge.BestMove; } }

        /// <summary>
        /// USIプロトコルによってengine側から送られてきた"bestmove .. ponder .."のponderで指定された指し手を解釈した指し手
        /// </summary>
        public Move PonderMove { get { return ThinkingBridge.PonderMove; } }

        public int MultiPV { set { ThinkingBridge.MultiPV = value; } }

        /// <summary>
        /// 通常探索なのか、詰将棋探索なのか。
        /// IsMateSearch == trueなら詰将棋探索
        /// </summary>
        public bool IsMateSearch { get; set; }

        /// <summary>
        /// エンジンの状態。
        /// </summary>
        public UsiEngineState State
        {
            get { return GetValue<UsiEngineState>("State"); }
            private set { SetValue<UsiEngineState>("State", value); }
        }

        /// <summary>
        /// 読み筋。
        /// USIプロトコルの"info ..."をparseした内容が入る。
        /// 親では、このイベントを捕捉すれば良い。
        /// </summary>
        public /*UsiThinkReport or UsiEngineReportMessageType*/object ThinkReport
        {
            get { return GetValue<object>("ThinkReport");}
            set { SetValue<object>("ThinkReport",value); }
        }

        /// <summary>
        /// 例外が発生したときにここに代入される。
        /// エンジンの接続エラーなど。
        /// </summary>
        public Exception Exception
        {
            get;set;
        }

        // -- private members

        private ProcessNegotiator negotiator;

        /// <summary>
        /// 現在思考中であるかどうかの状態管理フラグ
        /// </summary>
        private UsiEngineThinkingBridge ThinkingBridge { get; set; }

        /// <summary>
        /// "usi"コマンド,"isready"を思考ンジンに送信した時刻。思考エンジンは起動時にすぐに応答するように作るべき。
        /// 一応、タイムアウトを監視する。ただし思考エンジンから改行など何らかのkeep alive的なメッセージが送られてきた場合、
        /// これを延長する。
        /// </summary>
        private DateTime timeoutTime;

        /// <summary>
        /// timeoutTimeを初期化する。
        /// </summary>
        private void ResetTimeOutTime()
        {
            var config = TheApp.app.Config;
            switch (State)
            {
                // "usi"→"usiok"まで15秒。ただし延長あり。
                case UsiEngineState.WaitUsiOk:
                    // 0が設定されていれば無制限にしておく。
                    // →　一般ユーザー向けのソフトとしてわかりにくい。
                    // var t1 = config.UsiOkTimeOut == 0 ? int.MaxValue : config.UsiOkTimeOut;
                    // timeoutTime = DateTime.Now + new TimeSpan(0,0,t1);

                    timeoutTime = DateTime.MaxValue; // time-out無効化。

                    break;

                // "isready"→"readyok"まで30秒。ただし延長あり。
                case UsiEngineState.WaitReadyOk:
                    //var t2 = config.ReadyOkTimeOut == 0 ? int.MaxValue : config.ReadyOkTimeOut;
                    //timeoutTime = DateTime.Now + new TimeSpan(0,0,t2);
                    // 評価関数ファイルの読み込みでDMA転送とかで、単coreのCPUだとCPU時間自体がもらえない可能性も…。
                    // →　よくないアイデアであった。[2020/01/27]

                    timeoutTime = DateTime.MaxValue; // time-out無効化。

                    break;

                default:
                    Debug.Assert(false);
                    break;
            }
        }

        // -- private methods

        private void ChangeState(UsiEngineState state)
        {
            var oldState = State;
            State = state; // この瞬間にイベントが発生するので、これを先にやっておかないとSendSetOptionList()などで困る。

            Log.Write(LogInfoType.UsiServer, $"ChangeState()で{oldState.ToString()}から{state.ToString()}に状態が変化した。");

            switch (state)
            {
                case UsiEngineState.Connected:
                    // 接続されたので"usi"と送信
                    SendCommand("usi");
                    State = UsiEngineState.WaitUsiOk;

                    // これ、応答タイムアウトがある。15秒。
                    // これを超えるなら定期的に改行などkeep alive的な何かをエンジン側から定期的に送信すべきである。
                    ResetTimeOutTime();
                    break;

                case UsiEngineState.UsiOk:
                    // このタイミングで状態が変更になったことを通知すべき
                    // エンジン設定時には、この状態で待たせておけば良い。

                    // 対局時には、このあと "isready"を送信して readyokを待つ。
                    if (!EngineSetting)
                    {
                        // このタイミングでoptionを先に送らないとEvalDirの変更などが間に合わない。
                        SendSetOptionList();

                        SendCommand("isready");
                        State = UsiEngineState.WaitReadyOk;

                        // これもタイムアウトがある。30秒。
                        // これを超えるなら定期的に改行などkeep alive的な何かをエンジン側から定期的に送信すべきである。
                        ResetTimeOutTime();
                    }
                    break;

                case UsiEngineState.InTheGame:
                    // "readyok"が来たのでusinewgameを送信して、対局の局面を送信できるようにしておく。
                    SendCommand("usinewgame");
                    break;

                // -- time out

                case UsiEngineState.ConnectionTimeout:
                    throw new Exception("エンジンからの応答がtimeoutになりました。エンジンのusiコマンドに対する応答が遅すぎます。" +
                        "毎回タイムアウトになるなら、設定→エンジン補助設定でusiコマンドに対するタイムアウトまでの時間を増やしてください。");

                case UsiEngineState.IsReadyTimeout:
                    throw new Exception("エンジンからの応答がtimeoutになりました。エンジンのisreadyコマンドに対する応答が遅すぎます。" +
                        "毎回タイムアウトになるなら、設定→エンジン補助設定でisreadyコマンドに対するタイムアウトまでの時間を増やしてください。");

                // これ以外の変化に対する応答は必要ない。
            }
        }


        /// <summary>
        /// 思考エンジンの標準出力から送られてきたコマンドの解釈用
        /// </summary>
        /// <param name="command"></param>
        private void UsiCommandHandler(string command)
        {
            try
            {
                // keep alive的な何かかも知れないので、思考エンジン側からメッセージが送られてきた以上、
                // タイムアウト時間を延長してやる。
                if (State == UsiEngineState.WaitUsiOk || State == UsiEngineState.WaitReadyOk)
                    ResetTimeOutTime();

                // 空行なら解釈するまでもない。
                if (string.IsNullOrWhiteSpace(command))
                    return;

                // 前後の空白は削除しておきます。
                var trimmedCommand = command.Trim();
                //Log.Info("{0}> {1}", LogName, trimmedCommand);

                var scanner = new Scanner(trimmedCommand);

                switch (scanner.ParseText())
                {
                    case "usiok":
                        HandleUsiOk();
                        break;

                    case "readyok":
                        HandleReadyOk();
                        break;

                    case "id":
                        HandleId(scanner);
                        break;

                    case "option":
                        HandleOption(scanner);
                        break;

                    case "bestmove":
                        HandleBestMove(scanner);
                        break;

                    case "info":
                        HandleInfo(scanner);
                        break;

                    // u2bやBonadapterのためのスペシャルコマンド
                    case "B<":
                        break;

                    case "checkmate":
                        HandleCheckmate(scanner);
                        break;

                    default:
                        //Log.Error("unknown usi command: {0}", trimmedCommand);
                        break;
                }

            } catch (Exception ex)
            {
                // 例外をログに出力しておく。
                Log.Write(LogInfoType.UsiParseError, $"例外が発生しました。: {ex.Message}");
            }
        }

        /// <summary>
        /// usiok コマンドを処理します。
        /// </summary>
        private void HandleUsiOk()
        {
            if (State != UsiEngineState.WaitUsiOk)
            {
                throw new UsiException(
                    "usiokコマンドを不正なタイミングで受信しました。");
            }

            // "usiok"に対してエンジン設定などを渡してやる。

            //ComplementOptions();

            // この変更メッセージをハンドルしてDefaultOptionをセットしてくれていることを期待する。
            ChangeState(UsiEngineState.UsiOk);
        }

#if false
        // "USI_Ponder"と"USI_Hash"をわざわざ隠し持っているようなエンジン実装は考えられない。
        // このoptionを送らなくて良いというUSIプロトコルの規定は廃止べきである。

        /// <summary>
        /// 必要ならUSI_PonderやUSI_Hashなど必要なオプションを追加します。
        /// </summary>
        private void ComplementOptions()
        {
            // 判定はオプション名のみで行います。
            if (!OptionList.Any(_ => _.Name == UsiOption.USI_Hash.Name))
            {
                OptionList.Insert(0, UsiOption.USI_Hash.Clone());
            }

            if (!OptionList.Any(_ => _.Name == UsiOption.USI_Ponder.Name))
            {
                OptionList.Insert(0, UsiOption.USI_Ponder.Clone());
            }
        }
#endif

        /// <summary>
        /// setoptionコマンドをまとめて送信します。
        /// </summary>
        public void SendSetOptionList()
        {
            var list = OptionList
                .Where(_ => _.OptionType != UsiOptionType.Button) // Button型以外はそのまま垂れ流してOk.
                .Select(_ => _.CreateSetOptionCommandString())
                .Where(_ => !_.Empty())
                //.Select(_ => _ + '\n')
                .ToArray();

            // 応答を待つ必要はない。どんどん流し込む。
            foreach (var command in list)
                SendCommand(command);
        }

        /// <summary>
        /// 終局のメッセージをエンジン側に送信する
        /// </summary>
        /// <param name="move"></param>
        public void SendGameOver(MoveGameResult result)
        {
            SendCommand("gameover " + result.ToUsi());
            ChangeState(UsiEngineState.GameOver);
        }

        /// <summary>
        /// SendGameOver()のあと、再度ゲームをスタートする。
        /// </summary>
        public void SendIsReady()
        {
            // "isready"は再度送信してやらないと駄目。"UsiOk"に状態を戻せば、再送されるはず。
            ChangeState(UsiEngineState.UsiOk);
        }

        /// <summary>
        /// readyok コマンドを処理します。
        /// </summary>
        private void HandleReadyOk()
        {
            if (State != UsiEngineState.WaitReadyOk)
            {
                throw new UsiException(
                    "readyokコマンドが不正なタイミングで送られました。");
            }

            // 読み込みが終わったタイミングでエンジンの優先度を下げます。
            negotiator.UpdateProcessPriority();

            ChangeState(UsiEngineState.InTheGame);
        }

        /// <summary>
        /// id name などのコマンドを処理します。
        /// </summary>
        private void HandleId(Scanner scanner)
        {
            switch (scanner.ParseWord())
            {
                case "name":
                    OriginalName = scanner.LastText;
                    break;
                case "author":
                case "auther": // typoも受け入れる
                    Author = scanner.LastText;
                    break;
                default:
                    throw new UsiException(
                        "invalid command: " + scanner.Text);
            }
        }

        /// <summary>
        /// option コマンドを処理します。
        /// </summary>
        private void HandleOption(Scanner scanner)
        {
            var option = UsiOption.Parse(scanner.Text);

            // "usi"は一度しか送っていないので同じ名前のoptionが二度送られてくることは想定しなくて良いはずなのだが、
            // 一応きちんと処理しておく。
            AddOption(option);
        }

        /// <summary>
        /// OptionListにoptionを追加する。
        /// nameが重複していれば追加せずに置き換える。
        /// </summary>
        /// <param name="option"></param>
        private void AddOption(UsiOption option)
        {
            for (int i = 0; i < OptionList.Count; ++i)
                if (OptionList[i].Name == option.Name)
                {
                    OptionList[i] = option;
                    return;
                }

            OptionList.Add(option);
        }

        /// <summary>
        /// bestmove コマンドを処理します。
        /// </summary>
        private void HandleBestMove(Scanner scanner)
        {
            try
            {
                Move move = Move.NONE , ponder = Move.NONE;
                var moveSfen = scanner.ParseText();

                // まず、特殊な指し手を調べます。
                switch (moveSfen)
                {
                    case "resign":
                        move = Move.RESIGN;
                        break;
                    case "win":
                        move = Move.WIN;
                        break;
                }

                // 上記に該当しなかった。
                if (move == Move.NONE)
                {
                    move = Core.Util.FromUsiMove(moveSfen);
                    if (move == Move.NONE)
                    {
                        // 解釈できない文字列
                        throw new UsiException(
                            moveSfen + ": SFEN形式の指し手が正しくありません。");
                    }
                }

                // 後続があって、"ponder"と書いてある。
                if (!scanner.IsEof)
                {
                    if (scanner.ParseText() != "ponder")
                    {
                        // "ponder"以外はこれないはずなのに…。
                        throw new UsiException(
                            "invalid command: " + scanner.Text);
                    }

                    // ponderの指し手は'(null)'などが指定されることもあるので、
                    // 指せなくてもエラーにはしません。
                    var ponderSfen = scanner.ParseText();
                    ponder = Core.Util.FromUsiMove(ponderSfen);
                }

                // 確定したので格納しておく。
                // 消費時間、サーバー計測で格納しておいて、それを用いて時間の計算をすべきような気もする。
                // (いまは、このbestmoveを取り出した時までの時間になっている)
                if (ThinkingBridge.BestMoveReceived(move,ponder))
                    ThinkReport = UsiEngineReportMessageType.UsiThinkEnd;
            }
            catch (UsiException ex)
            {
                // 例外を出力しておく。
                Log.Write(LogInfoType.UsiParseError, $"例外が発生しました。: {ex.Message}");
            }
        }

        /// <summary>
        /// infoコマンドを処理します。
        /// </summary>
        private void HandleInfo(Scanner scanner)
        {
            try
            {
                var info = new UsiThinkReport();
                var parseEnd = false;
                while (!scanner.IsEof && !parseEnd)
                {
                    switch (scanner.ParseText())
                    {
                        // hash使用率 1000分率返ってくるので10で割って100分率に変換して代入する。
                        case "hashfull":
                            info.HashPercentage = (float)scanner.ParseInt() / 10.0f;
                            break;

                        // nps
                        case "nps":
                            info.Nps = scanner.ParseInt();
                            break;

                        // 現在の探索手
                        case "currmove":
                            info.CurrentMove = scanner.ParseText();
                            break;

                        // 探索ノード数
                        case "nodes":
                            info.Nodes = scanner.ParseInt();
                            break;

                        // 探索深さ,選択探索深さ

                        // ここに文字が入っている可能性があるので(upperbound/lowerboundを表現するための"↑"など)文字列として扱う。
                        case "depth":
                            info.Depth = scanner.ParseText();
                            break;

                        case "seldepth":
                            info.SelDepth = scanner.ParseText();
                            break;

                        case "score":
                            info.Eval = HandleInfoScore(scanner);
                            break;

                        case "pv":
                            info.Moves = HandlePVSeq(scanner);
                            //parseEnd = true; // "pv"はそのあと末尾まで。

                            // ここから、解釈できない文字列はinfo.MovesSuffixに追加。
                            info.MovesSuffix = HandlePVSuffix(scanner);

                            break;

                        // リポート情報のみ更新
                        // このtimeは無視して、server timeが入るべき。
                        case "time":
                            //info.ElapsedTime = TimeSpan.FromMilliseconds(scanner.ParseInt());

                            scanner.ParseText();
                            break;

                        case "multipv":
                            info.MultiPV = (int)scanner.ParseInt();
                            break;

                        case "string":
                            info.InfoString = scanner.LastText; // 残り全部
                            parseEnd = true;
                            break;
#if false
                        // なんかよくわからん。あとで考える。

                        case "count":
                            GodwhaleCount = scanner.ParseInt();
                            break;
                        case "ranking":
                            GodwhaleRank = scanner.ParseInt();
                            break;

                        // 無視
                        case "currmovenumber":
                        case "cpuload":
                        case "refutation":
                        case "currline":
                        case "id": // クジラちゃん用
                            scanner.ParseText();
                            break;
#endif

                        // エラー
                        default:
                            throw new Exception();
                    }
                }

                // 次のThink()が呼び出されているなら、この読み筋は、無効化されなくてはならない。
                if (!ThinkingBridge.IsStopping)
                {
                    // 思考時間はgoコマンドからの時間を用いる。
                    // 思考エンジンから送られてきた値は採用しない。
                    info.ElapsedTime = ThinkingBridge.ElapsedTime;

                    // 受信したinfoに対応する元のKifuNodeを取得する。
                    info.KifuNode = ThinkingBridge.CurrentNode;

                    ThinkReport = info;
                }
            } catch
            {
                throw new UsiException("info 文字列の解析に失敗 : " + scanner.Text);
            }
        }

        /// <summary>
        /// USIのPVの文字列を構築する。
        /// </summary>
        /// <param name="scanner"></param>
        /// <returns></returns>
        private List<Move> HandlePVSeq(Scanner scanner)
        {
            var list = new List<Move>();

            while (!scanner.IsEof)
            {
                var token = scanner.PeekText();
                Move move = Move.NONE;
                switch(token)
                {
                    // USIの規定にはないが、やねうら王で読み筋に使っている特殊な指し手
                    // "win"以外は、"bestmove"に対して受理はしないが、pvとしては受理するのでここに書いてやる。

                    case "win": move = Move.WIN; break;
                    case "rep_win":  move = Move.REPETITION_WIN; break;
                    case "rep_lose": move = Move.REPETITION_LOSE; break;
                    case "rep_draw": move = Move.REPETITION_DRAW; break;
                    case "rep_sup":  move = Move.REPETITION_SUP; break;
                    case "rep_inf":  move = Move.REPETITION_INF; break;
                    case "resign":   move = Move.RESIGN; break;

                    default: move = Core.Util.FromUsiMove(token); break;
                }

                if (move == Move.NONE)
                    break;
                scanner.ParseText();
                list.Add(move);
            }
            return list;
        }

        /// <summary>
        /// "info"に出てきうるtoken
        /// </summary>
        private string[] InfoTokens = new[]{ "hashfull" , "nps" , "currmove" , "nodes" , "depth" , "seldepth" ,
            "score", "pv" , "time" , "multipv" , "string"};

        /// <summary>
        /// 読み筋のうち解釈できない文字列をまとめてつなげて返す。
        ///
        /// InfoTokensのtokenが出現したところで終了。
        /// </summary>
        /// <param name="scanner"></param>
        /// <returns></returns>
        private string HandlePVSuffix(Scanner scanner)
        {
            var sb = new StringBuilder();

            while (!scanner.IsEof)
            {
                var token = scanner.PeekText();
                if (InfoTokens.Contains(token))
                    break;

                scanner.ParseText();
                sb.Append(' ');
                sb.Append(token);
            }

            return sb.ToString();
        }

        /// <summary>
        /// "info .. score xxx"の"score"の直後の文字列をparseする
        /// </summary>
        /// <param name="scanner"></param>
        /// <returns></returns>
        private EvalValueEx HandleInfoScore(Scanner scanner)
        {
            EvalValue eval;
            switch (scanner.ParseText())
            {
                case "cp":
                    var valueText = scanner.ParseText();
                    eval = (EvalValue)int.Parse(valueText);
                    break;

                case "mate":
                    eval = ParseMate(scanner.ParseText());
                    break;

                default:
                    return null;
            }

            ScoreBound bound;

            // lowerbound/upperboundを取得
            // (この指定はmateに対しても起こりうる)

            var peek = scanner.PeekText(); // peekします
            if (peek == "upperbound")
            {
                bound = ScoreBound.Upper;
                scanner.ParseText();
            }
            else if (peek == "lowerbound")
            {
                bound = ScoreBound.Lower;
                scanner.ParseText();
            } else
                bound = ScoreBound.Exact;

            // ここでエンジンによっては、"120↑"のような表現がありうる。
            // "upperbound","lowerbound"がサポートされていなかったころの名残。
            // ここではそれを許容しない。

            return new EvalValueEx(eval,bound);
        }

        /// <summary>
        /// "go mate"に対しては "bestmove"ではなく、"checkmate.."という文字列が返ってくる。
        /// これをparseする。
        /// </summary>
        /// <param name="scanner"></param>
        public void HandleCheckmate(Scanner scanner)
        {
            EvalValueEx eval = null;

            var moves = new List<Move>();
            if (scanner.PeekText("nomate"))
            {
                // 不詰を表現している(ことにする)
                moves.Add(Move.MATE_ENGINE_NO_MATE);

            } else if (scanner.PeekText("notimplemented")){

                // 手番側が王手をされているとき、詰将棋エンジンが実装されていない。
                moves.Add(Move.MATE_ENGINE_NOT_IMPLEMENTED);

            } else if (scanner.PeekText("timeout"))
            {
                // 時間切れ
                moves.Add(Move.MATE_TIMEOUT);
            }
            else
            {
                // 詰みを発見した。

                while (!scanner.IsEof)
                {
                    var token = scanner.ParseText();
                    var move = Core.Util.FromUsiMove(token);
                    if (move == Move.NONE)
                        break;
                    moves.Add(move);
                }

                // {moves.Count}手で詰み…とは限らないのでエンジンによってはこれあまり良くなかったり？
                eval = new EvalValueEx(EvalValue.Mate - moves.Count, ScoreBound.Exact);

                // 手数不明の詰み
                //eval = new EvalValueEx(EvalValue.MatePlus , ScoreBound.Exact);
            }

            // 次のThink()が呼び出されているなら、この読み筋は、無効化されなくてはならない。
            if (!ThinkingBridge.IsStopping)
            {
                ThinkReport = new UsiThinkReport()
                {
                    Moves = moves,
                    Eval = eval,
                    ElapsedTime = ThinkingBridge.ElapsedTime, // 消費時間はサーバー計測
                };
            }

            // 確定したので格納しておく。
            if (ThinkingBridge.BestMoveReceived(moves[0] , Move.NONE))
                ThinkReport = UsiEngineReportMessageType.UsiThinkEnd;
        }

        /// <summary>
        /// 詰みになったときの手数をパースします。
        /// </summary>
        /// <example>
        /// +
        /// -10
        /// +5↑
        /// </example>
        public static EvalValue ParseMate(string text)
        {
            if (text.Empty())
            {
                throw new ArgumentNullException("text");
            }

            var trimmedText = text.Trim();

            // "+"とかあるのでこれがparseできないといけない。
            var value = StringToInt(trimmedText);

            if (value == 0)
            {
                if (trimmedText[0] == '+')
                {
                    return EvalValue.MatePlus;
                }
                else if (trimmedText[0] == '-')
                {
                    return EvalValue.MatedMinus;
                }
                else
                {
                    //throw new ShogiException(
                    //    trimmedText + ": メイト手数が正しくありません。");

                    // 本来は先頭に+/-が必要ですが、そうなっていないソフトも多いので
                    // ここでは現状に合わせてエラーにはしないことにします。
                    return EvalValue.Mate;
                }
            }
            else if (value > 0)
                return EvalValue.Mate - value;
            else
                return EvalValue.Mated - value;
        }


        /// <summary>
        /// 数値に変換可能な部分のみを数値に直します。
        /// "+"とか"-"とかもparseできないといけない。
        /// </summary>
        private static int StringToInt(string text)
        {
            var isNegative = false;
            var startIndex = 0;
            var result = 0L;

            if (text.Empty())
            {
                throw new ArgumentNullException("text");
            }

            if (text[0] == '-')
            {
                isNegative = true;
                startIndex = 1;
            }
            else if (text[0] == '+')
            {
                startIndex = 1;
            }

            for (var i = startIndex; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    continue;
                }

                if ('0' <= text[i] && text[i] <= '9')
                {
                    var n = text[i] - '0';

                    result = result * 10 + n;
                    if (result > int.MaxValue || result < int.MinValue)
                    {
                        throw new OverflowException(
                            text + ": 評価値がオーバーフローしました。");
                    }
                }
                else
                {
                    if (i == startIndex)
                    {
                        throw new ArgumentException(
                            text + ": 評価値が正しくありません。");
                    }

                    break;
                }
            }

            return (int)(result * (isNegative ? -1 : +1));
        }

    }
}
