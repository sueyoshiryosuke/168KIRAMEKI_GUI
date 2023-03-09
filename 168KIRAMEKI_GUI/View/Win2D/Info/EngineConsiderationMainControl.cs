﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using MyShogi.App;
using MyShogi.Model.Common.Collections;
using MyShogi.Model.Common.ObjectModel;
using MyShogi.Model.Shogi.Core;
using MyShogi.Model.Shogi.Data;
using MyShogi.Model.Shogi.LocalServer;
using MyShogi.Model.Shogi.Usi;

namespace MyShogi.View.Win2D
{
    /// <summary>
    /// 思考エンジンの出力(最大 2CPU分) + ミニ盤面
    /// </summary>
    public partial class EngineConsiderationMainControl : UserControl
    {
        public EngineConsiderationMainControl()
        {
            InitializeComponent();

            InitSpliter();
            InitEngineConsiderationControl();
            InitFont();

            // タイマー開始
            //lastDispatchTime.Start();
        }

        #region ViewModel

        /// <summary>
        /// このクラスで用いるViewModel
        /// </summary>
        public class EngineConsiderationDialogViewModel : NotifyObject
        {
            /// <summary>
            /// 閉じるボタンが押された時に発生するイベント
            /// </summary>
            public object CloseButtonClicked
            {
                get { return GetValue<object>("CloseButtonClicked"); }
                set { SetValue<object>("CloseButtonClicked", value); }
            }

            /// <summary>
            /// 右クリックメニュー「メイン棋譜にこの読み筋を分岐棋譜として送る(&S)」
            /// がクリックされた時に発生するイベント。
            /// </summary>
            public MiniShogiBoardData SendToMainKifu
            {
                get { return GetValue<MiniShogiBoardData>("SendToMainKifu"); }
                set { SetValue<MiniShogiBoardData>("SendToMainKifu", value); }
            }

            /// <summary>
            /// 右クリックメニュー「メイン棋譜をこの読み筋で置き換える(&R)」
            /// がクリックされた時に発生するイベント。
            /// </summary>
            public MiniShogiBoardData RepalceMainKifu
            {
                get { return GetValue<MiniShogiBoardData>("RepalceMainKifu"); }
                set { SetValue<MiniShogiBoardData>("RepalceMainKifu", value); }
            }

        }

        public EngineConsiderationDialogViewModel ViewModel = new EngineConsiderationDialogViewModel();

        #endregion

        #region Perform // キーボードショートカット

        /// <summary>
        /// 検討時に選択行を1行下に移動する。
        /// キーハンドラから呼び出される。
        /// </summary>
        public void PerformUp()
        {
            // 検討時用なら先手用に送っておけば十分。
            ConsiderationInstance(0).PerformUp();
        }

        /// <summary>
        /// 検討時に選択行を1行上に移動する。
        /// キーハンドラから呼び出される。
        /// </summary>
        public void PerformDown()
        {
            // 検討時用なら先手用に送っておけば十分。
            ConsiderationInstance(0).PerformDown();
        }

        /// <summary>
        /// 検討時に選択行を先頭に移動する。
        /// キーハンドラから呼び出される。
        /// </summary>
        public void PerformHead()
        {
            // 検討時用なら先手用に送っておけば十分。
            ConsiderationInstance(0).PerformHead();
        }

        /// <summary>
        /// 検討時に選択行を末尾に移動する。
        /// キーハンドラから呼び出される。
        /// </summary>
        public void PerformTail()
        {
            // 検討時用なら先手用に送っておけば十分。
            ConsiderationInstance(0).PerformTail();
        }

        #endregion

        #region UsiThinkReportMessage

        /// <summary>
        /// ミニ盤面の初期化
        /// 必ず呼び出すべし。
        /// </summary>
        public void Init(bool boardReverse)
        {
            miniShogiBoard1.Init(boardReverse);
        }

        /// <summary>
        /// 読み筋などのメッセージを受け取り、ququeに積む。
        ///
        /// OnIdle()が呼び出されたときにdispatchする。
        /// </summary>
        public void EnqueueThinkReportMessage(UsiThinkReportMessage message)
        {
            thinkQuque.Add(message);
        }

        /// <summary>
        /// 最後のDispatchした時刻。
        /// </summary>
        //private Stopwatch lastDispatchTime = new Stopwatch();

        /// <summary>
        /// [UI thread] : 一定時間ごとに(MainDialogなどのidle状態のときに)このメソッドを呼び出す。
        ///
        /// queueに積まれているUsiThinkReportMessageを処理する。
        /// </summary>
        public void OnIdle()
        {
#if false
            // 前回dispatch(≒表示)してから200[ms]以上経過するまでメッセージをbufferingしておく。
            // (このメソッドが呼び出されるごとに1つずつメッセージが増えているような状況は好ましくないため)
            if (lastDispatchTime.ElapsedMilliseconds < 200)
                return;
#endif

            var queue = thinkQuque.GetList();
            if (queue.Count > 0)
            {
                using (var slb = new SuspendLayoutBlock(this))
                {
                    // 子コントロールも明示的にSuspendLayout()～ResumeLayout()しないといけないらしい。
                    // Diagnosing Performance Problems with Layout : https://blogs.msdn.microsoft.com/jfoscoding/2005/03/04/suggestions-for-making-your-managed-dialogs-snappier/
                    foreach (var c in All.Int(2))
                        ConsiderationInstance(c).SuspendLayout();

                    // →　これでも処理、間に合わないぎみ
                    // 以下、メッセージのシュリンクを行う。

                    // 要らない部分をskipDisplay == trueにしてしまう。
                    // j : 前回のNumberOfInstanceの位置(ここ以降しか見ない)

                    for (int i = 0, j = 0; i < queue.Count; ++i)
                    {
                        var e = queue[i];
                        switch (e.type)
                        {
                            case UsiEngineReportMessageType.NumberOfInstance:
                                // ここ以前のやつ要らん。
                                for (var k = j; k < i; ++k)
                                    queue[k].skipDisplay = true;
                                j = i;
                                break;

                            case UsiEngineReportMessageType.SetRootSfen:
                                // rootが変わるので、以前のrootに対する思考内容は不要になる。
                                for (var k = j; k < i; ++k)
                                {
                                    var q = queue[k];
                                    var t = q.type;
                                    if ((t == UsiEngineReportMessageType.SetRootSfen || t == UsiEngineReportMessageType.UsiThinkReport)
                                        && q.number == e.number
                                        )
                                        q.skipDisplay = true;
                                }
                                j = i;
                                break;
                        }
                    }

                    foreach (var e in queue)
                        DispatchThinkReportMessage(e);

                    foreach (var c in All.Int(2))
                        ConsiderationInstance(c).ResumeLayout();
                }
            }

#if false
            // 次回に呼び出された時のためにDispatchした時刻からの時間を計測する。
            lastDispatchTime.Reset();
            lastDispatchTime.Start();
#endif
        }

        /// <summary>
        /// [UI thread] : 読み筋などのメッセージを受け取り、ququeに積む。
        /// UsiEngineReportMessageTypeの内容に応じた処理に振り分ける。
        /// </summary>
        /// <param name="message"></param>
        private void DispatchThinkReportMessage(UsiThinkReportMessage message)
        {
            switch (message.type)
            {
                case UsiEngineReportMessageType.NumberOfInstance:
                    if (!message.skipDisplay)
                    {
                        // 非表示なので検討ウィンドウが表示されているなら消しておく。
                        Visible = message.number != 0;
                        SetEngineInstanceNumber(message.number);

#if false
                    // このメッセージに対して、継ぎ盤の局面を初期化したほうが良いのでは…。
                    miniShogiBoard1.BoardData = new MiniShogiBoardData()
                    {
                        rootSfen = BoardType.NoHandicap.ToSfen()
                    };
                    // →　このメッセージの直後にrootSfenが来るはずだから、それに応じて、その局面で初期化するようにする
#endif
                        must_init_miniboard = true;
                    }
                    break;

                case UsiEngineReportMessageType.SetGameMode:
                    if (!message.skipDisplay)
                    {
                        var gameMode = (GameModeEnum)message.data;
                        var b = (gameMode == GameModeEnum.ConsiderationWithEngine);

                        // MultiPV用の表示に
                        ConsiderationInstance(0).ViewModel.EnableMultiPVComboBox = b;
                        ConsiderationInstance(0).SortRanking = b;
                    }
                    break;

                case UsiEngineReportMessageType.SetEngineName:
                    if (!message.skipDisplay)
                    {
                        var engine_name = message.data as string;
                        ConsiderationInstance(message.number).EngineName = engine_name;
                    }
                    break;

                case UsiEngineReportMessageType.SetRootSfen:
                    var sfen = message.data as string;
                    if (!message.skipDisplay)
                        ConsiderationInstance(message.number).RootSfen = sfen;

                    // まだミニ盤面の初期化がなされていないならば。
                    if (must_init_miniboard)
                    {
                        miniShogiBoard1.BoardData = new MiniShogiBoardData()
                        {
                            rootSfen = sfen
                        };
                        must_init_miniboard = false;
                    }
                    break;

                case UsiEngineReportMessageType.UsiThinkEnd:
                    if (!message.skipDisplay)
                        ConsiderationInstance(message.number).DisplayThinkEnd();
                    break;
                    
                case UsiEngineReportMessageType.UsiThinkReport:

                    if (!message.skipDisplay)
                    {
                        var thinkReport = message.data as UsiThinkReport;
                        ConsiderationInstance(message.number).AddThinkReport(thinkReport);
                    }
                    // TODO : ここで評価値グラフのためのデータ構造体の更新を行うべき。

                    break;
            }
        }

        /// <summary>
        /// 読み筋を積んでおくqueue。
        /// 
        /// EnqueueThinkReportMessage()とOnIdle()で用いる。
        /// </summary>
        private SynchronizedList<UsiThinkReportMessage> thinkQuque = new SynchronizedList<UsiThinkReportMessage>();

#endregion

        // -- properties

        /// <summary>
        /// MiniShogiBoardの表示、非表示を切り替えます。
        /// (MiniShogiBoardの乗っかっているSplitConainerの幅をゼロにする。MiniShogiBoard自体は手を加えない。)
        /// </summary>
        public bool MiniShogiBoardTabVisible
        {
            set {
                splitContainer2.Panel2.Visible = value;
                splitContainer2.Panel2Collapsed = !value;
                splitContainer2.IsSplitterFixed = !value;

                // MiniBoard、スレッドが回っているわけでもないし、
                // 画面が消えていれば更新通知等、来ないのでは？
            }
        }

        /// <summary>
        /// ミニ盤面のinstanceを返す。
        /// RemoveMiniShogiBoard()しているときも有効。(nullにはならない)
        /// </summary>
        public MiniShogiBoard MiniShogiBoard { get { return miniShogiBoard1; } }

        /// <summary>
        /// 形勢グラフのinstanceを返す。
        /// RemoveEvalGraph()しているときも有効。(nullにはならない)
        /// </summary>
        public EvalGraphControl EvalGraphControl { get { return evalGraphControl1;  } }

        /// <summary>
        /// ミニ盤面をこのControlから除外する。
        /// </summary>
        public void RemoveMiniShogiBoard()
        {
            if (!tabControl1.TabPages.Contains(tabPage1))
                return; // 追加されてませんけど？
            tabControl1.TabPages.Remove(tabPage1);

            tabPage1.Controls.Remove(miniShogiBoard1);
            if(tabControl1.TabCount == 0) MiniShogiBoardTabVisible = false;
        }

        /// <summary>
        /// ミニ盤面をこのControlに戻す。
        /// RemoveMiniShogiBoard()で除外していたものを元に戻す。
        /// </summary>
        public void AddMiniShogiBoard()
        {
            MiniShogiBoardTabVisible = true;
            if (tabControl1.TabPages.Contains(tabPage1))
                return; // 追加されてますけど？
            tabPage1.Controls.Add(miniShogiBoard1);
            tabControl1.TabPages.Insert(0, tabPage1);
        }

        /// <summary>
        /// 形勢グラフをこのControlから除外する。
        /// </summary>
        public void RemoveEvalGraph()
        {
            if (!tabControl1.TabPages.Contains(tabPage2))
                return; // 追加されてませんけど？
            tabControl1.TabPages.Remove(tabPage2);

            tabPage2.Controls.Remove(evalGraphControl1);
            if(tabControl1.TabCount == 0) MiniShogiBoardTabVisible = false;
        }

        /// <summary>
        /// 形勢グラフをこのControlに戻す。
        /// RemoveEvalGraph()で除外していたものを元に戻す。
        /// </summary>
        public void AddEvalGraph()
        {
            MiniShogiBoardTabVisible = true;
            if (tabControl1.TabPages.Contains(tabPage2))
                return; // 追加されてますけど？
            tabPage2.Controls.Add(evalGraphControl1);
            tabControl1.TabPages.Add(tabPage2);
        }

        /// <summary>
        /// 読み筋を表示するコントロールのinstanceを返す。
        /// </summary>
        /// <param name="n">
        /// 
        /// n = 0 : 先手用
        /// n = 1 : 後手用
        /// 
        /// ただし、SetEngineInstanceNumber(1)と設定されていれば、
        /// 表示されているのは1つのみであり、先手用のほうしかない。
        /// 
        /// </param>
        /// <returns></returns>
        public EngineConsiderationControl ConsiderationInstance(int n)
        {
            switch (n)
            {
                case 0: return engineConsiderationControl1;
                case 1: return engineConsiderationControl2;
            }
            return null;
        }

        /// <summary>
        /// エンジンのインスタンス数を設定する。
        /// この数だけエンジンの読み筋を表示する。
        /// </summary>
        /// <param name="n"></param>
        public void SetEngineInstanceNumber(int n)
        {
            if (n == 1)
            {
                splitContainer1.Panel2.Visible = false;
                splitContainer1.Panel2Collapsed = true;
                splitContainer1.IsSplitterFixed = true;
            }
            else if (n == 2)
            {
                splitContainer1.Panel2.Visible = true;
                splitContainer1.Panel2Collapsed = false;
                splitContainer1.IsSplitterFixed = false;
            }
        }

        // -- handlers

        private void splitContainer2_Resize(object sender, EventArgs e)
        {
            UpdateBoardHeight(true);
        }

        private void splitContainer2_SplitterMoved(object sender, SplitterEventArgs e)
        {
            // splitterの位置調整を行ってしまうと無限再帰になってしまうので、
            // この時はsplitterの位置調整は行わない。
            UpdateBoardHeight(false);
        }

        private void EngineConsiderationDialog_Resize(object sender, EventArgs e)
        {
            InitSpliter2Position();
        }

        // -- design adjustment

        /// <summary>
        /// spliterに関して、基本的なレイアウト設定にする。
        /// </summary>
        private void InitSpliter()
        {
            var h = splitContainer1.Height;
            var sh = splitContainer1.SplitterWidth;
            splitContainer1.SplitterDistance = (h - sh) / 2; // ちょうど真ん中に

            InitSpliter2Position();
        }

        /// <summary>
        /// エンジンの思考表示controlの初期化
        /// </summary>
        private void InitEngineConsiderationControl()
        {
            SetEngineInstanceNumber(1);

            foreach (var i in All.Int(2))
            {
                var instance = ConsiderationInstance(i);

                instance.ViewModel.AddPropertyChangedHandler("PvClicked", (h) =>
                  {
                      var data = h.value as MiniShogiBoardData;
                      SendPvToMiniboard(data);
                  });

                // 検討Controlから来た右クリックメニューのメッセージをそのままこのクラスのViewModelに通知。
                instance.ViewModel.Bind("SendToMainKifu", ViewModel, DataBindWay.OneWay);
                instance.ViewModel.Bind("RepalceMainKifu", ViewModel, DataBindWay.OneWay);
            }
        }

        /// <summary>
        /// 現在の選択行のPVをMiniShogiBoardに送る
        /// </summary>
        public void SendCurrentPvToMiniBoard()
        {
            ConsiderationInstance(0).SendCurrentPvToMiniBoard();
        }

        /// <summary>
        /// PV(最善応手列)をお抱えのMiniShogiBoardに反映させる。
        /// </summary>
        /// <param name="data"></param>
        private void SendPvToMiniboard(MiniShogiBoardData data)
        {
            // 自分がminiShogiBoardを抱えているなら、強制表示。
            if (tabControl1.TabPages.Contains(tabPage1))
                MiniShogiBoardTabVisible = true;

            miniShogiBoard1.BoardData = data;
        }

        /// <summary>
        /// ミニ盤面の縦横比
        /// </summary>
        float aspect_ratio = 1.05f;
        //float aspect_ratio = 1.5f; // debug用に幅広くしておくと、棋譜ウィンドウが表示されるのでデバッグが捗る。

        /// <summary>
        /// splitContainer2のsplitterの位置を調整する。
        /// </summary>
        private void InitSpliter2Position()
        {
            // 親Form側が最小化されたりすると、Resizeイベントが発生して、そのときClientSize == (0,0)だったりする。
            if (ClientSize.IsEmpty)
                return;

            var board_height = Math.Max(tabPage1.Height, 1);
            // →　toolStrip1をMiniShogiBoardに移動させた。

            // 継ぎ盤があるなら、その領域は最大でも横幅の1/4まで。
            var board_width = Math.Max((int)(board_height * aspect_ratio), 1);
            var max_board_width = Math.Max(ClientSize.Width * 1 / 4, 1);

            if (board_width > max_board_width)
            {
                board_width = max_board_width;
                // 制限した結果、画面に収まらなくなっている可能性がある。
                board_height = Math.Max((int)(board_width / aspect_ratio), 1);
            }

            int dist = ClientSize.Width - splitContainer2.SplitterWidth - board_width;
            splitContainer2.SplitterDistance = Math.Max(dist, 1);

            DockMiniBoardTab(board_width, board_height);
        }

        /// <summary>
        /// ユーザーのSplitterの操作に対して、MiniBoardの高さを調整する。
        /// splitterAdjest : splitterの位置の調整も行うのか？
        /// </summary>
        private void UpdateBoardHeight(bool splitterAdjest)
        {
            var board_width = Math.Max(ClientSize.Width - splitContainer2.SplitterWidth - splitContainer2.SplitterDistance, 1);
            var max_board_height = Math.Max(tabPage1.Height, 1);
            var board_height = Math.Max((int)(board_width / aspect_ratio), 1);

            if (board_height > max_board_height)
            {
                board_height = max_board_height;
                board_width = Math.Max((int)(board_height * aspect_ratio), 1);

                if (splitterAdjest)
                {
                    // 横幅減ったはず。spliterの右側、無駄であるから、詰める。
                    int dist = ClientSize.Width - splitContainer2.SplitterWidth - board_width;
                    splitContainer2.SplitterDistance = Math.Max(dist, 1);
                }
            }

            DockMiniBoardTab(board_width, board_height);
        }

        /// <summary>
        /// miniShogiBoardをToolStripのすぐ上に配置する。
        /// </summary>
        private void DockMiniBoardTab(int board_width, int board_height)
        {
            tabControl1.Size = new Size(splitContainer2.Panel2.Width, splitContainer2.Panel2.Height);

            // miniShogiBoardをToolStripのすぐ上に配置する。
            int y = tabPage1.Height - board_height /* - toolStrip1.Height*/;

            miniShogiBoard1.Size = new Size(board_width, board_height);
            miniShogiBoard1.Location = new Point(0, y);
            evalGraphControl1.Size = new Size(tabPage2.Width, tabPage2.Height);
            evalGraphControl1.Location = new Point(0, 0);
        }

        /// <summary>
        /// 次回のrootSfenが送られてきたときにその局面でミニ盤面を初期化しないといけないフラグ
        /// </summary>
        private bool must_init_miniboard = false;

        /// <summary>
        /// フォントの初期化
        /// </summary>
        private void InitFont()
        {
            // --- フォントの変更。即時反映

            // 検討ウインドウの文字フォント
            var fontSetter1 = new FontSetter(this, "ConsiderationWindow");
            //var fontSetter2 = new FontSetter(this.toolStrip1, "SubToolStrip"); // → MiniShogiBoardに移動させた。

            // ミニ盤面の上のタブの文字フォント
            var fontSetter2 = new FontSetter(this.tabControl1, "MiniBoardTab");

            Disposed += (sender, args) => {
                fontSetter1.Dispose();
                fontSetter2.Dispose();
            };
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            DockMiniBoardTab(miniShogiBoard1.Width, miniShogiBoard1.Height);
        }

        // -- test code


#if false
        /// <summary>
        /// AddInfoTest()で使う、カウンター
        /// </summary>
        private int add_info_test_number = 0;

        /// <summary>
        /// 適当に読み筋をAddInfoしてやるテスト。
        /// </summary>
        private void AddInfoTest()
        {
            if (add_info_test_number == 0)
                engineConsiderationControl1.RootSfen = BoardType.NoHandicap.ToSfen();

            List<Move> moves;
            switch (add_info_test_number++ % 6)
            {
                case 0: moves = new List<Move>() { Util.MakeMove(Square.SQ_77, Square.SQ_76), Util.MakeMove(Square.SQ_33, Square.SQ_34) }; break;
                case 1: moves = new List<Move>() { Util.MakeMove(Square.SQ_27, Square.SQ_26), Util.MakeMove(Square.SQ_33, Square.SQ_34), Util.MakeMove(Square.SQ_55, Square.SQ_34) }; break;
                case 2: moves = new List<Move>() { Util.MakeMove(Square.SQ_57, Square.SQ_56), Util.MakeMove(Square.SQ_83, Square.SQ_84) }; break;
                case 3: moves = new List<Move>() { Util.MakeMove(Square.SQ_37, Square.SQ_36), Util.MakeMove(Square.SQ_51, Square.SQ_52) ,
                    Util.MakeMove(Square.SQ_57, Square.SQ_56), Util.MakeMove(Square.SQ_83, Square.SQ_84)}; break;
                case 4:
                    moves = new List<Move>() { Util.MakeMove(Square.SQ_37, Square.SQ_36), Util.MakeMove(Square.SQ_51, Square.SQ_52) ,
                    Util.MakeMove(Square.SQ_57, Square.SQ_56), Util.MakeMove(Square.SQ_83, Square.SQ_84)}; break;
                case 5:
                    moves = new List<Move>() { Util.MakeMove(Square.SQ_47, Square.SQ_46), Util.MakeMove(Square.SQ_51, Square.SQ_52) ,
                    Util.MakeMove(Square.SQ_57, Square.SQ_56), Util.MakeMove(Square.SQ_83, Square.SQ_84)}; break;
                default: moves = null; break;
            }

            engineConsiderationControl1.AddThinkReport(new UsiThinkReport()
            {
                ThinkingTime = new TimeSpan(0, 0, 3),
                Depth = 15,
                SelDepth = 20,
                Nodes = 123456789,
                Eval = EvalValue.Mate - 1 /*(EvalValue)1234*/,
                Moves = moves
            });
        }

        /// <summary>
        /// ミニ盤面に試しに局面をセットしてみるテスト用のコード
        /// Init()のなかではまだminiShogiBoard1のhandleが初期化されてないのでInit()のなかから呼び出すのは不可。
        /// </summary>
        public void BoardSetTest()
        {
            miniShogiBoard1.BoardData = new MiniShogiBoardData()
            {
                rootSfen = BoardType.NoHandicap.ToSfen(),
                moves = new List<Move>() { Util.MakeMove(Square.SQ_77, Square.SQ_76), Util.MakeMove(Square.SQ_33, Square.SQ_34) }
            };
        }
#endif

    }
}
