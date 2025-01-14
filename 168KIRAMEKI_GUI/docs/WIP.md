﻿
# 『将棋神やねうら王』(2018年8月31日発売) アップデートなどの作業進捗 (Work In Progress)


- このファイルは、既知の問題や作業中の内容を明示することにより、それらに対するバグ報告を未然に防ぐためのものです。

- 全般的な操作説明については[オンラインマニュアル](online_manual.md)をご覧ください。
- アップデート関連の情報、サウンドが再生されない、CPU負荷が高すぎる/低すぎるなどについては、[よくある質問](faq.md)をご覧ください。
- デバッグ機能の使い方などは、[開発者向け機能](dev_manual.md)をご覧ください。
- バグ、要望等は、こちらの記事にどうぞ。→ [『将棋神やねうら王』Update3までの遊戯施設 / やねうら王ブログ](http://yaneuraou.yaneu.com/2018/10/06/%E3%80%8E%E5%B0%86%E6%A3%8B%E7%A5%9E%E3%82%84%E3%81%AD%E3%81%86%E3%82%89%E7%8E%8B%E3%80%8Fupdate3%E3%81%BE%E3%81%A7%E3%81%AE%E9%81%8A%E6%88%AF%E6%96%BD%E8%A8%AD/)


# Update3(2020年1月下旬ごろリリース予定)に向けて作業中です(ここに書かれているすべての機能追加をお約束するものではありません)


// 次にやる

- 将棋DBの駒落ち　の手合割のあとのスペースが入っているKIFファイルが読み込めない。(遠藤さん)

- エンジン増やした時に3ページ目が表示されない。(odagakiさん)

- 棋譜を開いたときに最初の局面からにするかどうか。(coji cojiさん)

- 音声の再生、完全に同時にするのではなく、
	棋譜読み上げ　＞　秒読み　＞　駒音
　を優先するようなオプションを追加する。(fxst24さん)

- ファイルヒストリー(MRUF)
  - ファイルヒストリー、閉じる前に開いていた局面を開いて欲しい。(まふさん)

- AWS側のエンジンを利用できるようにする。

- 棋譜ウインドウ
  - 棋譜分岐、分岐の指し手、一覧が表示されて欲しい気が。

- USIプロトコル対応エンジンを任意に追加する機能
  - もうちょっと他の仕様が固まってからでないとまずそう。
- エンジンの設定について
  - エンジンの個別設定をコピーする機能(同エンジン対局がしやすくなる) (odagakiさん)

// そのあとやる予定

- 棋譜ウインドウの総消費時間、SpecialMove(終局のときの特殊な指し手)が00:00:00になるの、どうなのかよく考える。

- 再開ボタン
  - 中断局の再開が出来る。
  - 持ち時間設定、残り時間はそのまま。
  - この時は、振り駒処理をスキップする。
  - 棋譜の途中から対局をするとき、そこより手数を戻そうとすると持ち時間がゼロになるので時間切れ負けになる件
  - 対局終了時に、消していた開始局面以降の分岐を復元する。激指はそうなっている。(T.Kさん)

- 棋譜ウインドウ
  - 棋譜ウインドウのメッセージング処理変更。高速化。
  - 棋譜コメントの表示、編集等
  - 思考エンジンの読み筋を棋譜コメントに記録する


# Update4以降で対応を検討中


- コンピュータ同士の対局終了時に「先手勝ち」のような対局エフェクトの表示。(N.Fさん)


- 対局ごとにエンジン初期化の時間がかかるの何とかならないか。(tatuさん)
  - エンジンのinstance管理をやってからにしよう。

- ウインドウのレイアウトマネージャー。

- 対局設定
  - SkillLevel + 秒固定の棋力プリセットがあってもいいのでは。(odagakiさん)

- 対局補助設定
  - 時間計測「秒未満切り捨て、最低1秒」「秒未満切り捨て、最低0秒」「秒未満も記録」
    - ストップウォッチ方式の計測も出来るように。(これはUpdate4以降かも)

- 駒得のみの評価関数「駒得大好きくん 2018」の思考エンジン追加。

- 形勢グラフ(作業中)
	- このウィンドウ邪魔なので、フロート機能・検討ウインドウに埋める機能などを持たせる予定。
  - ToolStripの◀▶で局面を移動させた時にその局面での形勢表示にならない。
    - 検討ウィンドウのほうと合わせ、通知のハンドラちゃんと書く。
  - 着手や終局の直後に入れ違いで前の局面の思考が送られてきた時に、次の局面の評価値として記録してしまう。
    - あとで修正する。
  - このソフトで保存した棋譜を再度読み込んだ時にその評価値が形勢グラフに反映されない。
  - 駒落ちの時、詰まされると落ちる。(tkponさん)
    - 形勢グラフの範囲外にプロットしている模様。

- 棋譜解析
  - 期待勝率に基づく悪手判定
  - MultiPV / 詰め探索などを併用した悪手度の判定
  - 棋力を設定して、その棋力にそぐわない指し手の検出機能
  - 分岐をすべて解析対象とするオプション

- ponder(「相手の手番で先読み」)
  - この実装、わりとややこしくて、激指ユーザーとかがターゲットなので必須機能とも言えないので後回し。

- 棋譜ウインドウ
  - 棋譜の自動再生機能

- オンラインマニュアル
  - 1画面の大きな画像でまず全体的な使い方を説明したほうが良いのでは…。
  - チュートリアル画像、動画などあったほうが良いのでは。

- 戦型選択(居飛車・振飛車より細かい粒度で)
  - かねてよりやりたいと考えているが、かなり大掛かりな作業なので次回作での対応になるかも。

- 検討モードで2つの指し手(居飛車と振り飛車の指し手など)を比較したい。(うめもどきさん)

- 棋譜出力
  - 開始日時の出力(ohataroさん)
  - 詰んだ次の手番は「投了」で統一すべき。(ohtaroさん)


# その他、対応検討中の要望


- 棋譜読み上げの音声素材の入っているフォルダを選択可能に。(ohtaroさん)
  - それなら画面素材も同様に変更したい気も…。

- 棋譜を保存して読み込むと、駒落ち設定・エンジンのプリセットの情報が表示されない。(宏さん)
  - KIF形式で保存するところがないので仕方がない意味も。KIF形式のコメントとして保存するか？


# 改修履歴


- "1.3.9"→"1.4.0" [2020/01/28]-[2020/01/XX]


- "1.3.8"→"1.3.9" [2020/01/27]-[2020/01/28]
  - 表示設定ダイアログ、最大化できるの防いでなかったのを修正。
  - 表示設定でミニ盤面のタブのところの文字フォントも変更できるように。
  - 文字のデフォルトフォントサイズ調整
    - 棋譜ウインドウの文字フォント 11pt -> 7pt
    - 検討ウインドウの文字フォント 11pt -> 9pt
    - ミニ盤面のボタンの文字フォント 13pt -> 9pt
    - 設定ダイアログ関連 11pt -> 9pt
  - 設定ダイアログ
    - フォントサイズが大きいと文字の下部が断ち切れるのを修正。
    - 貼り付けてある画像の下部の境界線が見えてなかったのを修正。
    - テキストと画像が左端ぴったりになっていたの修正。
  - 検討ウインドウのレイアウトまわり、細かいところを調整
    - 検討ウインドウで「着順」ボタンのサイズがおかしかったの修正。
    - 検討ウインドウのNPSなどが表示されるテキストボックス、上下に対してセンタリングする。
    - 検討ウインドウの読み筋が表示されるリストビュー、BottomにDockさせていたのNoneに変更。(くっついてるのが気持ち悪かったので)
    - 検討ウインドウの読み筋が表示されるリストビュー、上と左のマージンを考慮した位置に表示されるように変更。
  - マルチディスプレイ対応の強化
    - メッセージダイアログをメインウィンドウが存在するほうのスクリーン(ディスプレイ)に表示させるようにした。
  - エンジン補助設定の"isready"に対する"readyok"応答時間の設定を削除
  - 形勢グラフ追加(jnoryさんのプルリクによるもの) , デバッグ中


- "1.3.7"→"1.3.8" [2018/11/18]-[2018/11/20]
  - メニューに「棋譜編集」追加。
    - 「棋譜編集」に棋譜のマージ追加。 // 作業中
  - 検討ウインドウの手順を分岐棋譜として送るときは棋譜を破壊していないという扱いにする。(警告が出るのがうざいので)
  - 検討ウインドウの手順でメイン棋譜を置き換えるとき、メイン棋譜に開始局面しか存在しないのであれば、警告を出さないようにする。
    (開始局面に手順が加わっただけなので破壊していることにはならないという考え)
  - 同じCPU同士の対局でそれぞれのengineに割当てられるメモリが非対称になっている。AutoHashの計算修正する。
  - エンジン側、置換表のゼロクリアを並列化する。
    - 空き物理メモリがたくさんある環境でエンジン初期化時間が短縮した。


- "1.3.6"→"1.3.7" [2018/11/12]-[2018/11/17]
  - 検討ウインドウの右クリックメニュー追加。
    - メイン棋譜にこの読み筋を分岐棋譜として送る(&S)
    - メイン棋譜をこの読み筋で置き換える(&R)
    - 読み筋を表示のままの文字列でクリップボードに貼り付ける(&P)
    - 読み筋をKIF形式でクリップボードに貼り付ける(&K)
  - V1.3.6でLinuxでうまく動いてなかった件を修正。
  - 将棋神のプリセットに、SkillLevel = 20の指定を追加する。
    (将棋神も、棋力に関して一定の状態を担保するべきという考えから。)


- "1.3.5"→"1.3.6" [2018/11/10]-[2018/11/12]
  - 詰検討ダイアログで探索ノード数を制限できるように。
    - tanuki-詰将棋エンジンをノード制限に対応
    - USI2.0プロトコルとしてnodesを有効に。
  - 棋譜ウインドウ、検討ウインドウのメインウインドウへの埋込み時の幅、高さを変更したときに
    メニュー項目が更新されていなかったの修正。
  - 対局結果一覧ウインドウがメインウインドウに対してセンタリングされていかなったの修正。
  - メニューの「対局結果一覧」をウインドウのところから「対局」の配下に移動。
  - メインウインドウに埋め込み時に畳の左端に表示するモードを追加(kumaさん)
    - メインウインドウの左右が余っているときに右寄せとかできればいいのか…。
    - メインウインドウも何とかできたほうが良さそうだけど、これ大変っぽいな。今回はここまでにしておこう。
  - ミニ盤面のフロート機能追加。
    - メニュー　→　ウインドウ　→　ミニ盤面
  - 設定→「エンジン補助設定」追加。
  - isreadyのあとのKeep Aliveの処理、うまくいっていない？(AO_o10yanさん)
    - 処理はちゃんと書けているようだが、評価関数ファイルの読み込みでDMA転送とかで、単coreのCPUだとCPU時間自体がもらえない可能性も…。
  - "usi"に対するtime outと、"isready"に対するtime outとを分ける。


- "1.3.4"→"1.3.5" [2018/11/08]-[2018/11/09]
  - 検討設定にnode数、深さの指定追加。
    - 詰検討設定のほうは、詰将棋エンジン側が対応していないので、その2つの設定は無効化しておく。
      - tanuki-詰将棋エンジン側、nodes指定ぐらいは対応できないものだろうか…。
        - USIプロトコル的に非対応なのか。そうか…。なら仕方がないな。
    - 棋力プリセットが選べても良さそうなものだが、それはわりと難しい…。
  - 連続対局で対局開始時に不正な局面になる。(masaさん)
    - 連続対局2局目以降、エンジンの初期化を待つコードを潰していました。修正しました。
  - エンジンのタイムアウト処理、きちんと判定できていなかったの修正。
  - エンジン側、Hashメモリのゼロクリア、2GBごとに進捗を読み筋のところに出力するように変更。
    - エンジンまた全部差し替え(´ω｀)


- "1.3.3"→"1.3.4" [2018/11/07]-[2018/11/08]
  - 検討・詰検討が終わったタイミングで何らかの通知が欲しい。(5ch 将棋スレ144 >> 300)
    - 思考中は、検討ウインドウのエンジン名のところを濃いオレンジにする。
    - 時間無制限の検討だと定跡hitしてもbestmove返さないのでオレンジのままだが…。仕方ないな。
  - tanuki-詰将棋エンジン、Hash確保を"isready"に対して行うように変更。
    - これでエンジン初期化中のダイアログが表示されるようになった。
    - でもゼロクリアしてないので、初期化に要する時間はわずか…。
  - 検討ウインドウの評価値、後手のときも先手から見た評価値をデフォルトとするように変更する。
    - 検討で使用すると1手ごとに符号反転してわかりにくかった。考えが足りてなかった。ごめん。
  - Hashの値が超巨大だとtime outになるのを修正。(5ch 将棋スレ >> 158)
    - 30秒をtime out時間として、それを超えるならkeep aliveのためにエンジン側から何らかのメッセージ(改行可)を送らないといけないことにする。
    - エンジン側、一式差し替え。
  - 駒をクリックして持ち上げている状態で、検討ボタンを押して、エンジン初期化中に駒を移動完了させるとエンジンが例外で停止していたの修正。
  - 思考エンジンが敵陣の歩の不成などを読むかどうかのエンジンオプションが欲しい。(ｋ＆ｙさん)
    - GenerateAllLegalMoves オプション追加。
    - これに伴い、engine_define.xml更新。
  - バージョン情報ウインドウに最小化、最大化、要らない。(5ch 将棋スレ144 >> 300)
  - 対局設定、Shogi960が初期設定になっていたの修正。(ohtaroさん)


- "1.3.2"→"1.3.3" [2018/10/27]-[2018/11/06]
  - Shogi960実装。対局設定ダイアログで開始局面としてShogi960を選択できる。
    - cf. Shogi960について考えてみた : http://yaneuraou.yaneu.com/2018/11/01/shogi960%E3%81%AB%E3%81%A4%E3%81%84%E3%81%A6%E8%80%83%E3%81%88%E3%81%A6%E3%81%BF%E3%81%9F/
  - ミニ盤面のショートカットキーの追加 デフォルト、Ctrl+上下左右。
    - 操作設定に「ミニ盤面」のタブ追加。
  - 検討ウインドウの現在選択行の上下のデフォルトをShift←→に変更。
  - 検討ウインドウの現在選択行の先頭/末尾をShift↑↓に割当て。
  - 検討ウインドウの現在選択されている読み筋に対して「再クリック」もしくは「スペースキー」で再度ミニ盤面にその読み筋が反映されて欲しい。
    - デフォルトではEnterキーに設定。
    - 操作設定のところに追加。
  - 検討ウインドウの「+-」の列幅記憶したものが次回反映していなかったの修正。(キメラさん)
  - 棋譜ウインドウをフロートさせているときにショートカットキーがときどき利かなくなる問題を修正。
  - 検討ウインドウのreadonlyのテキストボックス、tab stopをオフにする。
  - メニューの「ウインドウ」の「棋譜ウインドウ」と「検討ウインドウ」の順番入れ替える。
    - (棋譜が先にあって欲しい気がした)
  - ショートカットキー、Shift+Ctrl+RなどでもCtrl+Rとみなされていたの修正。


- "1.3.1b"→"1.3.2" [2018/10/20]-[2018/10/27]
  - 検討ウインドウで上下に相当するショートカットキーの割当て追加。
    - 操作設定の検討のタブ
    - デフォルトをShift+↑↓に変更。
  - Ubuntu18.04でサウンドが正常に再生されるようになった。
    - cf. https://github.com/jnory/MyShogiSoundPlayer/releases/tag/v0.1.2
  - 操作設定の「棋譜」のところに
    最初に戻る/最後に進むキー
    1手進む/戻るキー
    などの設定追加。
    - スペースキーを押したときに棋譜ウインドウの前回マウスカーソルで選択した行に移動していたの修正。
  - メニューの「設定」→「操作設定」追加
  - 表示設定の「操作」タブは「ダイアログ」とrename。
  - 棋譜の記法で駒打ちのときには必ず「打」を記すオプション追加。表示設定の棋譜のところ。「拡張」
    - 棋譜表記は、以下のサイトを参考に忠実に守っているのだが、「打」の文字が無いのはわかりにくいと不評。
    - 日本将棋連盟 「棋譜の表記方法」: https://www.shogi.or.jp/faq/kihuhyouki.html
  - Ubuntu18.04で動作するように修正。
    - システム情報を開くと落ちてたの修正。
    - エンジンバナーが表示されてなかったの修正。
    - 起動時に検討ウインドウがメインウインドウに埋め込まれた時に、盤面のリペントがされないことがあったの修正。
    - バージョン表示ダイアログがWebBrowserを使った実装になっていたので、使わないように一から書き直す。
    - ListViewのOwenerDrawで、無限ループになる。→　MonoのListViewのバグ。オーナードローをやめる。
    - メニューの「設定」のプルダウンメニューにフォント設定の内容が反映していなかったのを修正。(Monoのバグ)
  - 段位プリセットにレーティング併記するようにした。例:「(R400)」
  - やねうら王2018のEngineDefineに書かれている実行ファイル名の先頭が小文字になっていたのでMac/Linux環境で実行に失敗していた問題を修正。
    (EngineDefine作り直した)
  - Mono(Mac)でKifuControl.csの以下のEnsureVisibleで例外が出る件、呼び出しているところ、Mac用に #if #endifで囲む。
    - Linuxも同様の問題があるらしい。現バージョンのMono自体のbugっぽい。
  - 起動時に棋譜ウインドウが一瞬画面左上あたりに表示されるの修正。
  - MonoAPI.csのGetFreePhysicalMemory、MacとLinuxの両方の単位間違えてたので修正。
  - Mac : 最新のサウンドプレイヤーで動くように修正。 #62(jnoryさんのプルリク)


- "1.3.1"→"1.3.1b" [2018/10/19]-[2018/10/19]
  - 棋譜ウインドウ、ListViewのheader生成前に列幅調整しようとして(特定環境で)落ちるの修正。(tanishiさん)
  - 棋譜ウインドウ、スクロールバーが出てくると時間の列が隠れるの修正。
    - スクロールバーが出るときにClientSizeが変わるのでハンドルするようにした。


- "1.3.0"→"1.3.1" [2018/10/15]-[2018/10/19]
  - 総消費時間を表示していないときは、「時間」の列が残り幅いっぱいになるようにする。
  - 棋譜の表示形式を変更すると表示設定ダイアログを開くときに、「この変更が反映するのは次回起動時です」というダイアログが出ていたの修正。
  - 棋譜に総消費時間を表示するかを表示設定で選べるようにした。(デフォルト、オフ)
  - 検討ウインドウで水平スクロールバーが出ていたの修正。
  - 検討ウインドウの読み筋の幅を計算するコード、サイズが小さいと無限再帰になりかねないの修正。
  - 検討ボタンを押した時に検討ウインドウが非表示の場合、自動で表示するようにした。
    - これは、さすがに表示してくれないと不便だと思う。
  - 棋譜ウインドウ、メインウインドウに埋め込み時もフォントサイズを固定するように変更。
    - 棋譜ウインドウ、ListViewを使って書き直す。
    - こうしておかないと等幅フォント以外で消費時間のところがずれる。
    - 手数、指し手、(消費)時間、総(消費)時間
    - 棋譜ウインドウ、メインウインドウに埋め込み時も + - でフォントサイズ変更を可能に
    - 棋譜ウインドウ、メインウインドウに埋め込み時のフォントサイズを自動調整するのやめて、表示設定に従うように変更する。
  - 検討ウインドウ、選択行、focusがないときでも見えるように。
    - ListViewをOwnerDrawに変更した。
  - 表示設定のフォント変更、即時反映にする。
    - フォント設定のところに「+」と「-」を追加する。このボタンで1pt up/down
    - 棋譜ウインドウは対応まだ。
  - 形勢表記、駒落ちのときは上手・下手にする。(tanadai56さん)
  - DockWindow(フロートさせているWindow)、タイトルバーのダブルクリックで最大化されるの抑制。
  - 棋譜ウインドウ、ResizeのときなどにちらつくのでSuspendLayout～ResumeLayout追加。
  - MonoAPI.csのGetFreePhysicalMemory()すでに1024で割ってたの修正。(fxst24さん)
  - LinuxのGetCurrentCpu()などを ao-o10yanさんのプルリクを参考に修正。


- "1.2.9"→"1.3.0" [2018/10/14]-[2018/10/15]
  - Font、重複して解放してて落ちることがあったのを修正。
  - マウス追随モードで成り・不成のダイアログが出ているときに「転」ボタンを押すと180度回転させた升に移動させるように見える。
    - マウス座標がそこから動かないと仮定した処理になっているため。修正。
  - 成り/不成の選択、後手側だと180度回転させてあって欲しい。(ぐららるさん)
    - 表示設定の操作のところに追加。相手番ではflip(180度回転)をデフォルトとする。
    - 成り・不成のダイアログが出ているときに「転」ボタンを押してもうまく動くように調整。
  - ◀ ▶ がMSゴシックだと小さい問題、どうするべきか…。
    - ここのボタンのフォント、Yu Gothic UI  12ptをデフォルトに変更。
  - ToolStripExで、ボタンがないところをクリックされてもFocusが移るように修正。(T.Kさん)
    - MenuStripExも同様。
  - ToolStripの深い部分のFontオブジェクトが不正な値で落ちるようになっていたので修正。 #59(jnoryさんのプルリク)
    - この修正で、Macでもメニューの項目も無事表示されるようになった。(はず)
  - Mac/Linuxで簡単にビルド出来るように。
    - ソースコードの修正なしにビルドできるようになった。
    - サウンドについては対応を検討中。


- "1.2.8"→"1.2.9" [2018/10/14]-[2018/10/14]
  - 表示設定の盤のところから盤の色味を変更できるようにした。
  - 表示設定の駒のところから駒の色味を変更できるようにした。
  - 表示設定の駒のところから畳の色味を変更できるようにした。
  - 表示設定に畳のタブ追加。
  - 表示設定ダイアログのレイアウト調整


- "1.2.7"→"1.2.8" [2018/10/12]-[2018/10/13]
  - メインメニューのボタン、ミニ盤面のボタンのToolStripの文字フォントも表示設定のフォントで設定したものが反映するようになった。
  - Mac(Mono環境)でMessageBox.Show()が使えない件
    - 自前で描画することに。
  - 表示設定のフォントのところ、ラベルの位置がずれているの修正。
  - ToolTip、文字フォントによっては文字がはみ出ていたの修正。
  - 評価値に対して、先手有利などの説明文を表示するオプション追加。これデフォルトでONにする。
    激指に合わせる。
      ３００以上　有利
      ８００以上　優勢
      ２０００以上　勝勢
  - 表示設定のところに「形勢を評価値のところに表示する(次の局面以降有効)」を追加。
  - 棋譜ウインドウ、フロート→非表示→細長い駒台→メインウインドウに埋め込み→普通の駒台にすると落ちることがあるのを修正。
  - 棋譜ウインドウ、フロートから戻したときにDockFillのまま、メインウインドウに埋め込まれることがあるのを修正。
  - エンジン検討しながら消一手で消す前の局面を思考したままになる。(T.Kさん)
    - うまく書けた気がする。エンバグしてませんように…。


- "1.2.6"→"1.2.7" [2018/10/09]-[2018/10/12]
  - エンジン設定の共通設定のタブ、説明文がヘッダーとしてあったほうが良いのでは。(フルートさん)
  - 連続対局の時のファイル名の勝敗の出力が先後入れ替えを考慮したものになっていたのを修正。
  - 対局設定ダイアログでエンジンバナーのToolTipとしてエンジンの説明文を表示するように。
  - エンジン選択ダイアログでバナー画像をクリックしても選択できるように。(odagakiさん)
  - 対局設定でコンピューターを選んでエンジンを選ばずに閉じて再起動したときに対局設定ダイアログを選ぶとエンジン選択画面が出てくるのを修正。
  - 表示設定ダイアログにフォントのタブ追加。検討ウインドウ、棋譜ウインドウなどの文字フォント、ToolTip、MenuStripなどフォントサイズが変更できるようになった。
  - 対局設定ダイアログの後手の時間無制限が、「後手の時間設定を個別にする」がオフでもEnableになっている。(フルートさん)
    - 修正しました。
  - 対局画面の一番上のタイトルバーに「棋譜ファイル名」を表示する。(宏さん)
  - 手駒を掴んだときにマウスカーソルに追随していなかったの修正。
  - 盤面編集で移動できない升への駒のドラッグが利かなくなっていたのを修正。
  - 駒をドラッグで移動させたとき、一瞬、移動元の升に描画されるのを修正。(島田さん)
    - 成り不成りの選択の時も同様。
    - 盤面編集時も同様。


- "1.2.5"→"1.2.6" [2018/10/08]-[2018/10/09]
  - 駒の移動にマウスドラッグを許容する/しないの設定追加。
  - 駒をドラッグで移動できない升に移動させたときは、ユーザーの誤った操作であり、
    そのときにそこにある駒を掴む挙動になっているの修正する。
  - 駒をクリックしたときにマウスカーソルに駒が追随する設定の追加。(宏さん)
    - いい感じだったので、こっちをデフォルトとする。
    - 表示設定→駒のところから変更可能
  - 対局設定で連続対局を行うのチェックを外した時に回数の入力欄をdisableにする。(masaさん)
  - 表示設定ダイアログのすべての項目にTooltip追加
  - 表示設定ダイアログ、文字が収まりきらないのでもう少し間隔開ける。(masaさん)
  - Macで空きメモリ容量を計算するコードを追加。 #58 (jnoryさんのプルリク)


- "1.2.4"→"1.2.5" [2018/10/06]-[2018/10/08]
  - 対局開始時のエフェクトと振り駒のエフェクトの設定を分離する
  - メニューの「設定」に「表示設定」追加。ここで画像つきでわかりやすく選択できるようにした。
    - メニューの「表示」削除。
  - FontUtilityにParentのnullチェック追加。(海苔さんからの指摘)
  - メニューの「ファイル」→「設定の初期化」を「設定」のところに移動。
  - メニューの「設定」→「音声設定」追加。メニューから「音声」削除。
    - 「音声設定」にサウンド・音声全体のオン/オフ追加。
    - 「音声設定」に対局開始/終了時の挨拶の読み上げのオン/オフ追加。


## 過去の改修履歴

- ここ以前の改修履歴については、[過去の改修履歴](過去の改修履歴.md)のほうに移動させました。
