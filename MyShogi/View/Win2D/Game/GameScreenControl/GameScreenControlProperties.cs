﻿using MyShogi.Model.Common.Math;
using MyShogi.Model.Common.ObjectModel;
using MyShogi.Model.Shogi.LocalServer;
using System.Drawing;

namespace MyShogi.View.Win2D
{
    /// <summary>
    /// 対局画面を表現するクラス
    /// 
    /// 描画が完全に抽象化されているので、
    /// 一つのMainDialogが複数のGameScreenを持つことが出来る。
    /// </summary>
    public partial class GameScreenControl
    {
        /// <summary>
        /// 描画設定など一式
        /// 親クラスから初期化の時に設定される。
        /// </summary>
        public GameScreenControlSetting Setting { get; set; }

        /// <summary>
        /// 駒台のバージョン
        /// 
        /// このGameScreenのアスペクト比により、(横幅を狭めると)自動的に2が選ばれる。
        /// 
        /// 0 : 通常の駒台
        /// 1 : 細長い駒台
        /// 
        /// </summary>
        public int PieceTableVersion = 0;

        /// <summary>
        /// 元画像から画面に描画するときに横・縦方向の縮小率とオフセット値(affine変換の係数)
        /// Draw()で描画するときに用いる。
        /// </summary>
        public AffineMatrix AffineMatrix;

        /// <summary>
        /// ユーザー操作に対して、このViewがどういう状態にあるかを表現する変数
        /// 駒を持ち上げている状態であるだとか、王手を回避していない警告ダイアログを出すだとか
        /// </summary>
        public GameScreenControlViewState viewState { get; private set; } = new GameScreenControlViewState();

        /// <summary>
        /// 画面が汚れているかどうかのフラグ。
        /// これを定期的に監視して、trueになっていれば、親からOnDraw()を呼び出してもらうものとする。
        /// </summary>
        public bool Dirty
        {
            get { return (dirty || animatorManager.Dirty /* animatorが生きてる */) && !surpressDraw; }
            private set
            {
                // Thread生成なしにLocalGameServerを動作させているなら、即座に画面描画すべき。(これ用にタイマーも回ってないので)
                // dirtyがTrue
                if (gameServer != null && gameServer.NoThread)
                {
                    if (value)
                        Invalidate();
                    dirty = false;
                }
                else
                    dirty = value;
            }
        }
        private bool dirty;

        /// <summary>
        /// 描画の抑制フラグ。これが立っているときは描画を抑制する。
        /// </summary>
        private bool surpressDraw;

        /// <summary>
        /// 残り持ち時間だけが更新されたので部分的に描画して欲しいフラグ
        /// (未実装)　あとで考える。
        /// </summary>
        //public bool dirtyRestTime { get; set; }

        /// <summary>
        /// 関連付けられているLocalGameServerのインスタンスを返す。
        /// これは外部からSettingにセットされている。
        /// </summary>
        public LocalGameServer gameServer { get {
                if (Setting == null)
                    return null;// まだセットされていない。
                return Setting.gameServer; }
        }

        /// <summary>
        /// 関連付けられているKifuControlのインスタンスを返す。
        /// </summary>
        public KifuControl kifuControl {  get { return kifuControl1; } }

        /// <summary>
        /// LocalGameServerのEngineInfoが変更になった時に呼び出されるdelegate。
        /// 思考エンジンの読み筋などを外部に出力したい時は、これを設定すること。
        ///
        /// 注意) これを呼び出すスレッドはUI Threadではない。
        /// これは、queuingしてUIに反映させないと、連続対局のときに更新が間に合わないからである。
        /// </summary>
        public PropertyChangedEventHandler ThinkReportChanged;

        /// <summary>
        /// マウスカーソルのある位置(DrawSpriteなどで指定する座標系において)
        /// 駒を持ち上げた時用
        /// </summary>
        private Point MouseClientLocation
        {
            get
            {
                // MouseClientLocationをcaptureしたときから、盤面反転がなされたなら
                // 画面の真ん中を中心として180度回転させた座標を返す。
                return MouseClientLocationReverse == gameServer.BoardReverse ?
                    mouseClientLocation :
                    new Point(board_img_size.Width - mouseClientLocation.X , board_img_size.Height - mouseClientLocation.Y);
            }
            set { mouseClientLocation = value; }
        }
        private Point mouseClientLocation;

        /// <summary>
        /// MouseClientLocationに座標を保存したときのgameServer.BoardReverseの値
        /// (その後、BoardReverseが変化したら、そこに移動させなければならないので…)
        /// </summary>
        private bool MouseClientLocationReverse;
    }
}
