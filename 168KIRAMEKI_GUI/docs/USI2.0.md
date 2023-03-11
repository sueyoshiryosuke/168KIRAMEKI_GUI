﻿
# USI 2.0


USIプロトコルは、将棋エンジンで幅広く使われているが、現代の将棋エンジンの仕様として問題点が色々あるので、独自に拡張したUSI 2.0というプロトコルを定義する。
	- 当面の議論には以下のスレッドを用いる。
	- [USI2.0の要望/提案](https://github.com/yaneurao/MyShogi/issues/24)
  - GitHubのissueのところで議論するの、あまり筋が良くないのでいったんcloseしました。また別のところを用意したいと思います。(ブログなど)


# プロトコルの拡張の方針


- なるべく後方互換性を保つ。
	- 例えば、エンジン設定ファイルに「ponderhit時に残り時間を送るように」と書いてあれば、
	サーバー側(GUI)から、ponderhit時に残り時間が付与されてやってくる。
	思考エンジン側はそれを解釈するが、思考エンジン側は、それが付与されていなくともきちんと(従来通りの)動作をすることを保証する。
	- こうなっていれば、思考エンジンは新プロトコルに対応し、新プロトコルの恩恵にあずかりつつも、
	従来のUSIプロトコル用の思考エンジンとして振る舞うことも可能である。

- 思考エンジンの実装者に負担がかかるような拡張はなるべくしない。
	- エンジン設定ファイルで、思考エンジン側が受け入れるプロトコル拡張について表明できるので、思考エンジン側として、受け入れたい拡張だけを選択することが出来る。

- USIプロトコルの闇の部分を定義する
	- USIプロトコルで、明記されておらず、実装依存になっていた部分についてきちんと定義しなおしてやる。MyShogi側では、それらの挙動を保証していく。


## USIプロトコルで規定されていない部分を新たに規定する


- エンジン側との入出力、EncodeをUTF8とする。
  - SJISだと仮定して info stringなどで日本語メッセージ("↑"など)を送っていたエンジンがあったが、
    いまや評価値の"↑"などはlowerboundがサポートされたため用いなくなった。
    そのため、エンジン側が日本語を送る機会は多くないはずだし、このタイミングでUTF8に変更しても困るエンジンはないはず。

- "setoption"のvalueでInt64型(64-bit 符号付き整数)まで許容することをプロトコル上、保証する。
	- いまどきのPCでnode数制限をして探索させようとした時、Int32だと桁が足りないことがあるため。

- "isready"のあとのtime out時間は、30秒程度とする。これを超えて、評価関数の初期化、hashテーブルの確保をしたい場合、
  思考エンジン側から定期的に何らかのメッセージ(改行可)を送るべきである。(keep alive的な処理)
  - ShogiGUIではすでにそうなっているので、MyShogiもそれに追随する。
  - また、やねうら王のエンジン側は、"isready"を受け取ったあと、"readyok"を返すまで5秒ごとに改行を送るように修正する。

- "usinewgame"のあとの(探索中ではないときの)"setoption"コマンドを受付るものとする。
	- 検討モードにおいてMultiPVの値を途中で変更したいため。
	- やねうら王、Aperyなど多くの思考エンジンですでにそうなっている。
	- hashは"isready"のタイミングでoptionの値を見て確保するので、setoptionで値を変更しても即座に反映されるわけではないにせよ、setoption自体は有効であるべき。

- "info string"のencoding問題
	- info stringとして漢字を出力したい場合、そのencodingを決めておかないとGUI側できちんと表示できない。
	- GUI側としてはutf8にしたいがそうなっていない思考エンジンがあるので、GUI側の設定で変えられるようにするか、
	エンジン設定ファイル("engine_define.xml")でencodingを記述するようにすべきか。(検討中)

- "isready"の前に"setoption"を送らないといけない問題
  - 思考エンジンは、"isready"に対して時間のかかる処理(評価関数ファイルの読み込み、置換表の確保・ゼロクリアなど)を行う必要があるが、
  評価関数ファイルの存在するフォルダを"setoption"コマンドで受け取りたい場合、isreadyの前に"setoption"が完了している必要がある。
  - GUI側はこのことを保証しなければならないし、保証するべきである。

- 定跡にhitしたときにbestmoveしか返さない場合、検討モードで使うときにbestmoveの情報しか表示できない。定跡にhitしたときもinfo pv..などで定跡を読み筋として返すことを推奨する。

- "info pv.."のUSIで規定されていない文字列の問題。
  - pv以下に含まれる文字として以下の文字列を指し手文字列として新たに加える。(やねうら王には以前からサポートしている)
    - "win" : 入玉宣言勝ち
    - "rep_draw" : 千日手引き分け
    - "rep_win"  : 千日手勝ち(連続王手の千日手)
    - "rep_lose" : 千日手負け
    - "rep_sup"  : 優等局面
    - "rep_inf"  : 劣等局面
    - "resign"   : 投了

- "info pv.."なら"pv.."は最後に書け問題。
  - "info pv"でUSIで規定されていない文字列を出力するときGUIはそれをそのまま読み筋のところに表示なければならない。
    ところが、途中から"score cp xx"とか"nps xx"とかが出現する可能性があると、GUI側はそれら("score"とか"nps"とか)が
    出現しないかを注意深く調べながら、解釈する必要性が出てくるし、pvとして指し手のあとに "score = 50"のような出力も出来なくなる。
    (定跡の指し手などについてこういうことがしたいことがなくはない)
    ゆえに、"info pv.."を送るとき、エンジン側は"info score cp xx pv .."のように "pv"は一番最後に書いて、pv以下は、読み筋文字列のみにして、
    GUI側が解釈できなくなったところ以降はそのまま残りを読み筋ウィンドウに表示されることを想定しておくべきである。
  - やねうら王、いまこうなっていない。近いうちに、こう修正して、GUI側もこれを前提にしたコードに修正する予定。

- "info time XXX"が意味がない問題
  - info timeで思考エンジンは時間を返すことができるが、GUI側としては、GUI側での計測で何秒かかったかを表示してあるほうが便利であるため、
    この値を用いないし、思考エンジンが出力する必要性もないと思うので、GUI側としては出力を必要としない。(出力しても良いが、GUI側では無視する)

- "gameover [ win | lose | draw]"だと、対局中断時などに送信すべき文字列が規定されていない。
  - 対局中断のときは、"gameover unknown"を送るように拡張する。
  - やねうら王などでは"gameover"文字列は無視しているので影響ないはず。

- 詰将棋エンジンに対して、ノード制限をしたいため、go mate nodes [ノード数]という"nodes"を指定できるようにする。
  - 詰将棋エンジンに対してgo mate [数字]とした場合、この数字の部分として、探索時間[ms]を指定できる。ここまではUSIプロトコルで規定されている。
  - 詰将棋エンジンに対してgo mate depth [深さ]でdepthも指定したいが、現在主流のdf-pnは深さ制限、あまり相性が良くない。考え中。
  - この nodes , depth の拡張に対応しているかは、engine_define.xmlのExtendedProtocolで指定できる。
  - もともとUCIのほうではgo nodes Xとかgo depth Xとかできるので、Stockfishはこれに対応していて、Stockfishを参考に書かれている将棋ソフトも
  自動的にこれに対応している。やねうら王の場合、将棋所が対応していないので、NodesLimitとかDepthLimitとかのオプションを勝手に導入していたのだが、
  GUI側が対応してくれるなら、変なオプションを導入せずに、UCIにあるnodes、depthを使ったほうがいいような気はする。
    - やねうら王の場合は、NodesLimit、DepthLimitsの値は、go nodes X とか go depth Xとかの値で上書きするような処理になっている。


## 必須オプション項目の追加


- "MultiPV"というオプション項目を必須とする。
	- これがないと検討モードなどで困るため。
	- やねうら王、Aperyなど多くの思考エンジンにすでに実装されている。

- "Threads"というオプション項目を必須とする。
	- いまどきマルチスレッドに対応していない思考エンジンはないため。

- "USI_Hash" , "USI_Ponder"はUSIプロトコルの規定では"usi"に対してoptionのリストとして送らなくても良いことになっているが、わざわざオプションを隠し持っているようなエンジン実装は考えられないため、送ることを強制するように変更する。
	- "USI_Hash"ではなく、"Hash"を採用しているエンジンがあり(やねうら王、Gpsfishなど)、それに対応するためにもそれらを送ることを前提としないと困る。
	- "USI_Hash"ではなく、"Hash"を採用しているなら、"Hash"のほうを送信すれば良いものとする。


# 拡張コマンド(決定済みのもの)


- 備考 : 思考エンジン設定で拡張を表明したものだけ、サーバー(GUI側)からその拡張に基づいてコマンドが送られてくる。
	- 表明しなければ、従来通り(従来のUSIプロトコル)で送られてくる。
	- ここにある拡張は、思考エンジン側に実装を一切強要しない。思考エンジン側は自由に取捨選択して、自分の望む拡張だけを行うことが出来る。

- 拡張名 : "GoCommandTimeExtention"
	- goコマンドでbyoyomi(秒読み)が先後同じ値になってしまう。相手と自分とは異なる秒読み設定が出来るべきである。
	- 例) go ... bbyoyomi 1000 wbyoyomi 3000
	- 思考エンジン側が相手の残り時間を見てタイムマネージメントをしたいとき(現状、そういう処理までしている思考エンジンはあまりないが…)、
		相手の秒読み設定がわからないと困るため。
	- "go ponder" , "ponderhit"時に残り時間が送られてこない(前者は相手の残り時間が来ない)件、修正すべき。

- 拡張名 : "UseHashCommandExtension"
	- USIプロトコルでは置換表サイズの設定に"USI_Hash"が使われているが、
		将棋所は、個別に置換表サイズを設定できないため、思考エンジン側が独自のオプションで
		置換表サイズの設定が出来るようにしてあるのが実状である。(やねうら王は、これに該当する。)
	- この拡張を有効にすると、GUI側は、思考エンジンに対して"USI_Hash"の代わりに"Hash"を送信する。
	- この拡張を用意することにより、GUI側からやねうら王のようなエンジンに対してHashの設定が透過的に出来るようになる。


# 拡張コマンド(議論中)


- 手数が伸びてきたときに、初期局面から毎回エンジンに送るのは無駄であって、前回局面の続き(最大2手)を送れば十分である。"posdiff move1 move2"
	- 前回の局面は削除していないはずであるから、実装は難しくないはず。
	- あまり仕様として美しくないか…。なくすか考え中。


# 拡張コマンド(採用しないもの)


- 思考エンジンは"usi"→"usiok"に引き続き、"usiversion"に対して"2.0"のようにUSIプロトコルのサポートしているバージョンを返す。
	- これなくしたほうが良いのでは。エンジン設定ファイル("engine_define.xml")のほうで可能な限り記述するようにして、
		思考エンジンに無駄な実装負担をかけないのが今風なのではないかと…。
	- →　なくす。


# multi ponderについて


- multi ponderについてはGUI側で対応する予定。(思考エンジン側、プロトコル側の修正はなくて済む予定)
	- 例) GUI側の設定で"multi ponder 5"になっていれば5つのインスタンスを生成して、指定秒(通例0.1秒とか)だけ
	1つ目のインスタンスでMultiPV 5で探索して、5つの候補手を得て、それらに対して、5つのインスタンスで"go ponder"する。
	そのあとponderhitしたインスタンスに対してだけ"ponderhit"を送信して、残りのインスタンスにはstopを送る。


# エンジン設定のファイル仕様


- エンジンが評価関数のために消費するメモリ量、エンジンの棋力、バナー、CPU別の実行ファイル名などを記述する必要がある。
	- ファイル名 : "engine_define.xml" エンジンの実行ファイルと同じフォルダに入れる
	- エンジン自体は、GUI配下のengine/フォルダにエンジン別にフォルダを作成して入れるものとする。

- "engine_define.xml"の仕様
	- MyShogi.Model.Shogi.EngineDefine/EngineDefine.csにあるclass EngineDefineをXmlSerializerでシリアライズしたもの。
	- シリアライズ/デシリアライズの手順は同ファイル内にある。

- "engine_define.xml"の編集ツール
	- engine_define.xmlを手で書くのは少し面倒なので(xml形式なのでテキストエディタでも出来なくはないが…)、MyShogiのほうに編集用のダイアログを用意しようと思っている。
	- エンジン追加は、従来ソフトのようにメニュー上からエンジンを追加するのではなく、このengine_define.xmlを思考エンジンのフォルダに生成して配置してやることで
	エンジン追加扱いとされるようにしようと思っている。


# かきかけ

- かきかけ
