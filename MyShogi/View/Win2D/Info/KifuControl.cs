﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using MyShogi.App;
using MyShogi.Model.Common.ObjectModel;
using MyShogi.Model.Common.Tool;
using MyShogi.Model.Shogi.Core;
using MyShogi.Model.Shogi.Kifu;

namespace MyShogi.View.Win2D
{
    public partial class KifuControl : UserControl
    {
        /// <summary>
        /// 棋譜表示用のコントロール
        ///
        /// InitViewModel(Form)を初期化のために必ず呼び出すこと。
        /// </summary>
        public KifuControl()
        {
            InitializeComponent();

            if (TheApp.app.DesignMode)
                return;

            InitListView();

            var fm = new FontSetter(this, "KifuWindow");
            Disposed += (sender,args) => { fm.Dispose(); };
        }

        // -- properties

        public class KifuControlViewModel : NotifyObject
        {
            /// <summary>
            /// 棋譜リスト上の現在選択されている行
            /// 
            /// 双方向databindによって、LocalGameServerの同名のプロパティと紐付けられている。
            /// </summary>
            public int KifuListSelectedIndex
            {
                get { return GetValue<int>("KifuListSelectedIndex"); }
                set { SetValue<int>("KifuListSelectedIndex",value); }
            }

            /// <summary>
            /// KifuListSelectedIndexの値を変更して、イベントを発生させない。
            /// </summary>
            /// <param name="i"></param>
            public void SetKifuListSelectedIndex(int i)
            {
                SetValueAndNotRaise("KifuListSelectedIndex", i);
            }

            /// <summary>
            /// 棋譜リストの項目の数。KifuListSelectedIndexをこれより進めるべきではない。
            /// </summary>
            public int KifuListCount;

            /// <summary>
            /// KifuListを表現する仮想プロパティ
            /// LocalGameServerの同名のプロパティとDataBindによって接続されていて、
            /// あちらで更新があると、これらのプロパティの更新通知が来るのでそれをハンドルする。
            /// </summary>
            public List<KifuListRow> KifuList = new List<KifuListRow>();
            public string KifuListAdded;
            public object KifuListRemoved;

            /// <summary>
            /// 本譜ボタンがクリックされた。
            /// </summary>
            public object MainBranchButtonClicked;

            /// <summary>
            /// 次分岐ボタンがクリックされた。
            /// </summary>
            public object NextBranchButtonClicked;

            /// <summary>
            /// 分岐削除ボタンがクリックされた。
            /// </summary>
            public object EraseBranchButtonClicked;

            /// <summary>
            /// 最後の指し手を削除する。
            /// </summary>
            public object RemoveLastMoveClicked;

            /// <summary>
            /// フローティングモードなのかなどを表す。
            /// </summary>
            public DockState DockState
            {
                get { return GetValue<DockState>("DockState"); }
                set { SetValue<DockState>("DockState", value); }
            }

            /// <summary>
            /// LocalGameServer.InTheGameModeが変更されたときに呼び出されるハンドラ
            /// 「本譜」ボタン、「次分岐」ボタンなどを非表示/表示を切り替える。
            /// </summary>
            public bool InTheGame
            {
                get { return GetValue<bool>("InTheGame"); }
                set { SetValue<bool>("InTheGame", value); }
            }
        }

        public KifuControlViewModel ViewModel = new KifuControlViewModel();

        /// <summary>
        /// 外部から初期化して欲しい。
        /// </summary>
        /// <param name="parent"></param>
        public void InitViewModel(Form parent)
        {
            ViewModel.AddPropertyChangedHandler("KifuListSelectedIndex", setKifuListIndex, parent);
            ViewModel.AddPropertyChangedHandler("KifuList", KifuListChanged, parent);
            ViewModel.AddPropertyChangedHandler("KifuListAdded", KifuListAdded, parent);
            ViewModel.AddPropertyChangedHandler("KifuListRemoved", KifuListRemoved, parent);
            ViewModel.AddPropertyChangedHandler("InTheGame", InTheGameChanged , parent);
        }

        ///// <summary>
        ///// [UI Thread] : 表示している棋譜の行数
        ///// </summary>
        //public int KifuListCount
        //{
        //    get { return listBox1.Items.Count; }
        //}

        // -- 以下、棋譜ウインドウに対するオペレーション

        /// <summary>
        /// [UI Thread] : 棋譜ウィンドウ上、一手戻るボタン
        /// 局面が一手戻るとは限らない。
        /// </summary>
        public void RewindKifuListIndex()
        {
            ViewModel.KifuListSelectedIndex = Math.Max( 0 , ViewModel.KifuListSelectedIndex - 1);
        }

        /// <summary>
        /// [UI Thread] : 棋譜ウィンドウ上、一手進むボタン
        /// 局面が一手進むとは限らない。
        /// </summary>
        public void ForwardKifuListIndex()
        {
            ViewModel.KifuListSelectedIndex = Math.Min(ViewModel.KifuListCount - 1 , ViewModel.KifuListSelectedIndex + 1);
        }

        /// <summary>
        /// ListViewの4列目(総消費時間)を残りいっぱいサイズにする。
        /// 
        /// ※ 4列目を表示していない時は3列目が残り幅いっぱいになるようにする。
        /// </summary>
        private void UpdateListViewColumnWidth()
        {
            var col = listView1.Columns;

            // Column 生成前
            if (col.Count == 0)
                return;

            using (var sbc = new SuspendLayoutBlock(this))
            {
                var last = enable_total_time ? 3 : 2;
                var sum = 0; //  listView1.Margin.Left + listView1.Margin.Right;
                foreach (var i in All.Int(last))
                    sum += col[i].Width;

                // これ、ちゃんと設定してやらないと水平スクロールバーが出てきてしまう。
                // ClientSizeはスクロールバーを除外したサイズ。
                // →　スクロールバーが出ていない状態で計算して、
                // そのあとスクロールバーが出ると困るのだが…。押し戻す処理をするか。
                // スクロールバーを常に表示するモードがないので、スクロールバーが出ているならその分を控えて計算するか。

                // スクロールバーが10pxより小さいことはありえないので、それを超えているならスクロールバーが出ている。
                /*
                var scrollbar = listView1.Width - listView1.ClientSize.Width > 10;
                var width = scrollbar ?
                    listView1.ClientSize.Width :
                    listView1.ClientSize.Width - 22; // 実測でこれくらい high dpiだと変わるかも..
                */
                // →　これやめる。スクロールバーが出るとClientSizeが変化するのだから、そのときリサイズイベントが起きるのでは。

                var width = listView1.ClientSize.Width;
                var newWidth = Math.Max(width - sum, 0);

                // Widthにはマイナスの値を設定しても0に補整される。この結果、上のMax()がないと、newWidthがマイナスだと
                // このifは成立してしまい、代入によってイベントが生起されるので無限再帰となる。
                if (col[last].Width != newWidth)
                    col[last].Width = newWidth; //残りいっぱい分にする
            }
        }

        /// <summary>
        /// [UI thread] : 内部状態が変わったのでボタンの有効、無効を更新するためのハンドラ。
        /// 
        /// ViewModel.InTheGameが変更になった時に呼び出される。
        /// </summary>
        /// <param name="inTheGame"></param>
        private void UpdateButtonLocation()
        {
            // 最小化したのかな？
            if (Width == 0 || Height == 0 || listView1.ClientSize.Width == 0)
                return;

            using (var slb = new SuspendLayoutBlock(this))
            {

                UpdateListViewColumnWidth();

                var inTheGame = ViewModel.InTheGame;

                // 非表示だったものを表示したのであれば、これによって棋譜が隠れてしまう可能性があるので注意。
                //var needScroll = !button1.Visible && !inTheGame;

                // ボタンの表示は対局外のときのみ
                button1.Visible = !inTheGame;
                button2.Visible = !inTheGame;
                button3.Visible = !inTheGame;
                button4.Visible = !inTheGame;

                // フォントサイズ変更ボタンが有効か
                // 「+」「-」ボタンは、メインウインドウに埋め込んでいないときのみ
                // → やめよう　メインウインドウ埋め込み時も有効にしよう。
                var font_button_enable = !inTheGame; // && ViewModel.DockState != DockState.InTheMainWindow;

                button5.Visible = font_button_enable;
                button6.Visible = font_button_enable;

                // -- ボタンなどのリサイズ

                // ボタン高さ

                // 対局中は非表示。
                int bh = button1.Height;
                int x = font_button_enable ? Width / 5 : Width / 4;
                int y = button1.Visible ? Height - bh : Height;

                listView1.Location = new Point(0, 0);
                listView1.Size = new Size(Width, y);

                if (!inTheGame)
                {
                    button1.Location = new Point(x * 0, y);
                    button1.Size = new Size(x, bh);
                    button2.Location = new Point(x * 1, y);
                    button2.Size = new Size(x, bh);
                    button3.Location = new Point(x * 2, y);
                    button3.Size = new Size(x, bh);
                    button4.Location = new Point(x * 3, y);
                    button4.Size = new Size(x, bh);

                    button5.Location = new Point(x * 4, y);
                    button5.Size = new Size(x / 2, bh);
                    button6.Location = new Point((int)(x * 4.5), y);
                    button6.Size = new Size(x / 2, bh);
                }

                //if (needScroll)
                {
                    // 選択行が隠れていないことを確認しなければ..。
                    // SelectedIndexを変更すると、SelectedIndexChangedイベントが発生してしまうので良くない。
                    // 現在は、対局が終了した瞬間であるから、棋譜の末尾に移動して良い。
                    //SetListViewSelectedIndex(listView1.Items.Count - 1);
                    // →　こことは限らない。DockWindow化しただけかも知れない。

                    var index = ViewModel.KifuListSelectedIndex;
                    EnsureVisible(index);
                }
            }
        }

        /// <summary>
        /// ListViewのindexの行が画面に表示されるようにする。
        /// </summary>
        /// <param name="index"></param>
#if MONO
        private async void EnsureVisible(int index)
#else
        private void EnsureVisible(int index)
#endif
        {
            // Mono(Mac/Linux)は、EnsureVisibleで落ちるらしい。絶対Mono側の原因
            // →　Visible == falseのときにスクローバーの高さの計算を間違えるようだ。
            // このとき、EnsureVisibleを呼び出さずに帰れば問題ない。
            if (!Visible)
                return;

#if MONO
            // Linux環境だと、これ入れないとハングする。
            // cf. https://twitter.com/hnakada123/status/1062564183512776704
            //
            // たぶんX11のメッセージングが、Monoが想定しているのと異なるためだと思うけども…。
            // 棋譜ウインドウをControls.Add()したあと、DoEvents()的な何かをしないといけないということだと思う。

            await System.Threading.Tasks.Task.Delay(0);
#endif

            // 範囲チェックを行う。
            if (0 <= index && index < listView1.Items.Count)
                listView1.EnsureVisible(index);
        }

        /// <summary>
        /// ViewModel.InTheGameの値が変更になったときに呼び出されるハンドラ
        /// </summary>
        /// <param name="args"></param>
        private void InTheGameChanged(PropertyChangedEventArgs args)
        {
            using (var s = new SuspendLayoutBlock(this))
            {
                UpdateButtonLocation();
                UpdateButtonState();
            }
        }

        // -- initialize design

        /// <summary>
        /// LisTviewのheader、列の幅などの初期化。
        /// </summary>
        private void InitListView()
        {
            listView1.FullRowSelect = true;
            //listView1.GridLines = true;
            listView1.Sorting = SortOrder.None;
            listView1.View = System.Windows.Forms.View.Details;

            var ply_string = new ColumnHeader();
            ply_string.Text = "手数";
            ply_string.Width = 50;
            ply_string.TextAlign = HorizontalAlignment.Center;

            var move_string = new ColumnHeader();
            move_string.Text = "指し手";
            move_string.Width = 100;
            move_string.TextAlign = HorizontalAlignment.Left;

            var time_string = new ColumnHeader();
            time_string.Text = "時間";
            time_string.Width = 60;
            time_string.TextAlign = HorizontalAlignment.Right;

            var sum_time_string = new ColumnHeader();
            sum_time_string.Text = " 総時間";
            sum_time_string.Width = 0; // これはのちにresizeされる
            sum_time_string.TextAlign = HorizontalAlignment.Left;

            // 総消費時間を表示するのか(これは再起動後に有効)
            enable_total_time = TheApp.app.Config.KifuWindowDisplayTotalTime != 0;

            var header = enable_total_time ?
                new[] { ply_string, move_string, time_string, sum_time_string } :
                new[] { ply_string, move_string, time_string };
            
            listView1.Columns.AddRange(header);

            //listView1.AutoResizeColumns( ColumnHeaderAutoResizeStyle.ColumnContent);

            foreach (var index in All.Int(3))
            {
                int w1 = listView1.Columns[index].Width;
                int w2 = TheApp.app.Config.KifuColumnWidth[index];
                listView1.Columns[index].Width = w2 == 0 ? w1 : w2; // w2が初期化直後の値なら、採用しない。
                // これだと幅を0にすると保存されなくなってしまうのか…。そうか…。保存するときに1にしておくべきなのか…。
            }

        }

        /// <summary>
        /// 総消費時間を表示するのか(これは再起動後に有効なので起動時の値を保持しておく)
        /// </summary>
        private bool enable_total_time;

        // -- handlers

        /// <summary>
        /// [UI thread] : リストが1行追加されたときに呼び出されるハンドラ
        /// </summary>
        private void KifuListAdded(PropertyChangedEventArgs args)
        {
            using (var slb = new SuspendLayoutBlock(this))
            {
                // 増えた1行がargs.valueに入っているはず。
                var row = args.value as KifuListRow;

                listView1.ItemSelectionChanged -= listView1_ItemSelectionChanged;

                listView1.Items.Add(KifuListRowToListItem(row));

                ViewModel.KifuListCount = listView1.Items.Count;
                ViewModel.KifuList.Add(row); // ここも同期させておく。

                var lastIndex = listView1.Items.Count - 1;
                ViewModel.SetKifuListSelectedIndex(lastIndex);

                // 末尾の項目を選択しておく。
                SetListViewSelectedIndex(lastIndex);

                listView1.ItemSelectionChanged += listView1_ItemSelectionChanged;

                // item数が変化したはずなので、「消一手」ボタンなどを更新する。
                UpdateButtonState();
            }
        }

        /// <summary>
        /// KifuListRowを、棋譜ウインドウのListViewで表示するListViewItemの形式に変換する。
        /// </summary>
        /// <param name="row"></param>
        private ListViewItem KifuListRowToListItem(KifuListRow row)
        {
            var list = enable_total_time ?
                new[] { row.PlyString, row.MoveString, $"{row.ConsumptionTime} " , $" {row.TotalConsumptionTime}" }:
                new[] { row.PlyString, row.MoveString, $"{row.ConsumptionTime} "};
            return new ListViewItem( list );
        }

        /// <summary>
        /// [UI thread] : リストが1行削除されたときに呼び出されるハンドラ
        /// </summary>
        private void KifuListRemoved(PropertyChangedEventArgs args)
        {
            if (listView1.Items.Count == 0)
                return; // なんで？

            listView1.ItemSelectionChanged -= listView1_ItemSelectionChanged;

            listView1.Items.RemoveAt(listView1.Items.Count - 1);

            EnsureVisible(listView1.Items.Count - 1);
//            ViewModel.SetKifuListSelectedIndex(listBox1.Items.Count - 1);

            // → 「待った」によるUndoと「消一手」による末尾の指し手の削除でしかこのメソッドが
            // 呼び出されないので、ここで選択行が変更になったイベントを生起させるべき。
            // 対局中の「待った」に対しては、そのハンドラでは、対局中は何も処理をしないし、
            // 検討中ならば、残り時間の巻き戻しとNotifyTurnChanged()を行うのでこの書き方で問題ない。
            ViewModel.KifuListSelectedIndex = listView1.Items.Count - 1;

            ViewModel.KifuListCount = listView1.Items.Count;
            ViewModel.KifuList.RemoveAt(ViewModel.KifuList.Count - 1); // ここも同期させておく。

            listView1.ItemSelectionChanged += listView1_ItemSelectionChanged;

            // 1手戻った結果、「次分岐」があるかも知れないのでそのボタン状態を更新する。
            UpdateButtonState();
        }

        /// <summary>
        /// [UI thread] : リストが(丸ごと)変更されたときに呼び出されるハンドラ
        /// 　　UI上で駒を動かした時は、分岐が発生する場合があるので、毎回丸ごと渡ってくる。(ちょっと無駄な気も..)
        /// </summary>
        private void KifuListChanged(PropertyChangedEventArgs args)
        {
            // ここでListBoxをいじって、listBox1_SelectedIndexChanged()が呼び出されるのは嫌だから抑制する。

            listView1.ItemSelectionChanged -= listView1_ItemSelectionChanged;

            // 現在の選択行を復元する。
            //var selected = GetListViewSelectedIndex();

            var list = args.value as List<KifuListRow>;
            listView1.BeginUpdate();

            // 差分更新だけして、List.Items.Add()とRemove()ではDCが解放されず、リソースリークになるっぽい。
            // これはWindowsのListBoxの出来が悪いからだと思うが…。
            // これだと連続対局においてDCが枯渇してしまう。

            // 何も考えずに丸ごと書き換えるコード
            listView1.Items.Clear();
            // AddRange()で書けない。ごめん。
            foreach(var e in list)
                listView1.Items.Add(KifuListRowToListItem(e));

            ViewModel.KifuListCount = listView1.Items.Count;
            ViewModel.KifuList = list;

            listView1.EndUpdate();
            listView1.ItemSelectionChanged += listView1_ItemSelectionChanged;

            // 現在の選択行を復元する。
            //if (0 <= selected && selected < listView1.Items.Count)
            //    SetListViewSelectedIndex(selected);

            // →　勝手に選択行を作るのはよろしくない。
            // 項目数が1個(開始局面)のみのときだけ、そこを選択するようにする。
            if (listView1.Items.Count == 1)
                SetListViewSelectedIndex(0);

            // 再度、Selectedのイベントが来るはずなのでその時にボタン状態を更新する。
            //UpdateButtonState();
        }

        /// <summary>
        /// [UI thread]
        /// 棋譜の途中の指し手において、選択行が表示範囲の3行ほど手前になるように調整する。
        /// (横スクロール型のアクションゲームとかでよくあるやつ。)
        /// </summary>
        private void AdjustScrollTop()
        {
            var selected = ViewModel.KifuListSelectedIndex;

            var top_item = listView1.TopItem;
            // この変数、nullがありうるようだ。質が悪い…。
            if (top_item == null)
                return;

            var top = top_item.Index;
            var itemHeight = (listView1.ItemHeight == 0) ? 16 : listView1.ItemHeight;
            var visibleCount = listView1.ClientSize.Height / itemHeight;
            var bottom = top + visibleCount; // これListBoxのpropertyにないのだ…。何故なのだ…。

            // スクロール時にこの行数分だけ常に余裕があるように見せる。
            // 縦幅を狭めているときは、marginを縮める必要がある。
            var margin = Math.Min(2 /* デフォルトで2行 */ , (visibleCount - 1) / 2);

#if false
            if (top + margin > selected)
                top = selected - margin;
            else if (selected + margin + 1 >= bottom)
                top = selected - (visibleCount - margin - 1);
#endif
            // いま欲しいのはtopではない。

            if (top + margin > selected)
                EnsureVisible(Math.Max(selected - margin, 0));
            else if (selected + margin + 1 >= bottom)
                EnsureVisible(Math.Min(selected + margin , listView1.Items.Count - 1));
        }


        /// <summary>
        /// [UI thread] : 棋譜の読み込み時など、LocalServer側の要請により、棋譜ウィンドウを指定行に
        /// フォーカスを当てるためのハンドラ
        /// </summary>
        private void setKifuListIndex(PropertyChangedEventArgs args)
        {
            // 選べる要素が存在しない。
            if (listView1.Items.Count == 0)
                return;

            var selectedIndex = (int)args.value;

            // 範囲外なら押し戻す。
            if (selectedIndex < 0)
                selectedIndex = 0;
            else if (listView1.Items.Count <= selectedIndex)
                selectedIndex = listView1.Items.Count - 1;

            // 押し戻された可能性があるので、"ViewModel.KifuListSelectedIndex"に書き戻しておく。値が同じであれば変更イベントは発生しない。
            ViewModel.KifuListSelectedIndex = selectedIndex;
            SetListViewSelectedIndex(selectedIndex);

            AdjustScrollTop();
        }

        /// <summary>
        /// ListBoxのSelectedIndexのgetter相当のメソッド
        /// </summary>
        /// <returns></returns>
        private int GetListViewSelectedIndex()
        {
            var items = listView1.SelectedItems;
            if (items.Count == 0)
                return -1;

            var index = items[0].Index;
            return index;
        }

        /// <summary>
        /// ListBoxのSelectedIndexのsetter相当のメソッド
        /// </summary>
        /// <param name="index"></param>
        private void SetListViewSelectedIndex(int index)
        {
            listView1.ItemSelectionChanged -= listView1_ItemSelectionChanged;
            if (0 <= index && index < listView1.Items.Count)
            {
                EnsureVisible(index);
                
                // ListBox相当のSelectedIndexをemulationする。

                listView1.Items[index].Selected = true;
            }

            UpdateButtonState();

            listView1.ItemSelectionChanged += listView1_ItemSelectionChanged;
        }

        private void listView1_CheckUnselect(object sender,EventArgs args)
        {
            if (listView1.SelectedItems.Count == 0 && lastSelectedIndex < listView1.Items.Count)
                listView1.Items[lastSelectedIndex].Selected = true;
            // いま解除されたものを再度選択してやる。

            // 呼び出されたということは、このhandlerが設定されているわけで、それを解除する
            Application.Idle -= listView1_CheckUnselect;
        }
        private int lastSelectedIndex;

        /// <summary>
        /// 選択行が変更されたので、ViewModelにコマンドを送信してみる。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView1_ItemSelectionChanged(object sender , ListViewItemSelectionChangedEventArgs e)
        {
            if (!e.IsSelected)
            {
                // 選択が解除になる。これは無視して良い。
                // 解除になったあと再度選択されているかが問題なのでIdleイベントに突っ込む。
                // このアイデアは以下の掲示板の書き込みによるもの。
                // cf. ListViewの選択関連のイベント : https://dobon.net/vb/bbs/log3-55/32345.html
                lastSelectedIndex = e.ItemIndex;
                Application.Idle += listView1_CheckUnselect;

                return;
            }

#if false
            var items = listView1.SelectedItems;
            Console.WriteLine(items.Count);
            foreach (ListViewItem c in items)
                Console.WriteLine(">" + c.Index);
#endif
            // UIにより選択が移り変わるときに一度非選択の状態になり、このときindex == -1なので 0番目のselectをしてしまう。
            // index == -1ならこれを無視してイベントを生起しない。
            var index = GetListViewSelectedIndex();
            if (index == -1)
                return;

            ViewModel.SetValueAndRaisePropertyChanged("KifuListSelectedIndex", index);

            UpdateButtonState();
        }

        /// <summary>
        /// 分岐棋譜の時だけ、「消分岐」「次分岐」ボタンを有効にする。
        /// </summary>
        private void UpdateButtonState()
        {
            using (var slb = new SuspendLayoutBlock(this))
            {
                var index = GetListViewSelectedIndex();
                var s = index < 0 ? null : listView1.Items[index];
                if (s != null)
                {
                    var item = (s as ListViewItem).SubItems[0].Text;

                    // 本譜ボタン
                    var e = item.StartsWith(">");
                    button1.Enabled = e;

                    if (e)
                        item = item.Substring(1, 1); // 1文字目をskipして2文字目を取得 

                    var e2 = item.StartsWith("+") || item.StartsWith("*");
                    button2.Enabled = e2;
                    button3.Enabled = e2;
                }
                // Items[0] == "開始局面"なので、そこ以降に指し手があればundo出来るのではないかと。(special moveであろうと)
                button4.Enabled = listView1.Items.Count > 1;
            }
        }

        /// <summary>
        /// 本譜ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            ViewModel.RaisePropertyChanged("MainBranchButtonClicked");
        }

        /// <summary>
        /// 次分岐ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            ViewModel.RaisePropertyChanged("NextBranchButtonClicked");
        }

        /// <summary>
        /// 分岐消去ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            ViewModel.RaisePropertyChanged("EraseBranchButtonClicked");
        }

        /// <summary>
        /// 消す1手ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            ViewModel.RaisePropertyChanged("RemoveLastMoveClicked");
        }

        /// <summary>
        /// リサイズ。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KifuControl_SizeChanged(object sender, EventArgs e)
        {
            if (TheApp.app.DesignMode)
                return;

            // メインウインドウに埋め込み時もフォント固定する。
            //if (ViewModel.DockState != DockState.InTheMainWindow)

            using (var slb = new SuspendLayoutBlock(this))
            {
                UpdateButtonLocation();
                UpdateListViewColumnWidth();
            }
        }

        /// <summary>
        /// 「+」「-」ボタンのEnableを更新する。
        /// </summary>
        private void UpdateButtonEnable()
        {
            var fontsize = TheApp.app.Config.FontManager.KifuWindow.FontSize;
            button5.Enabled = fontsize < FontManager.MAX_FONT_SIZE;
            button6.Enabled = fontsize > FontManager.MIN_FONT_SIZE;
        }

        /// <summary>
        /// 文字を大きくする「+」ボタン
        ///
        /// ウインドウ時のみ有効。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            var fm = TheApp.app.Config.FontManager;
            if (fm.KifuWindow.FontSize < FontManager.MAX_FONT_SIZE)
            {
                fm.KifuWindow.FontSize++;
                fm.RaisePropertyChanged("FontChanged", "KifuWindow");
            }
        }

        /// <summary>
        /// 文字を小さくする「-」ボタン
        /// 
        /// ウインドウ時のみ有効。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            var fm = TheApp.app.Config.FontManager;
            if (fm.KifuWindow.FontSize > FontManager.MIN_FONT_SIZE)
            {
                fm.KifuWindow.FontSize--;
                fm.RaisePropertyChanged("FontChanged", "KifuWindow");
            }
        }

        /// <summary>
        /// 列の幅を変更したときのハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView1_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            // 最後の列の幅は記録しない
            // (総消費時間を表示していない状態で3列目を記録してしまい、そのあと総消費時間(4列目)を表示すると
            // 3列目が残り幅いっぱいになっていて、4列目が表示されていないように見えるため)
            var last = enable_total_time ? 3 : 2;

            var index = e.ColumnIndex;
            if (!(0 <= index && index < last))
                return; // 範囲外？

            // この設定、Globalに紐づけておく。
            // 変更された値が0なら1として保存する。(0 は、初期化されてないときの値を意味するため)
            var w = listView1.Columns[index].Width;
            TheApp.app.Config.KifuColumnWidth[index] = w == 0 ? 1 : w;

            // 4列目(総消費時間)を残り幅いっぱいにする。
            UpdateListViewColumnWidth();
        }

        /// <summary>
        /// スクロールバーが非表示から表示になったときにもこのイベントが発生するはず。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView1_ClientSizeChanged(object sender, EventArgs e)
        {
            // 4列目(総消費時間)を残り幅いっぱいにする。
            UpdateListViewColumnWidth();
        }

    }
}
