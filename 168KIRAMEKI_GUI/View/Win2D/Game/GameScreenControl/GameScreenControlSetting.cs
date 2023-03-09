﻿using MyShogi.Model.Common.ObjectModel;
using MyShogi.Model.Shogi.LocalServer;

namespace MyShogi.View.Win2D
{
    /// <summary>
    /// GameScreenControlの描画設定一式。
    /// 
    /// これを初期化時に渡して、この設定に従って描画される。
    /// </summary>
    public class GameScreenControlSetting
    {
        public GameScreenControlSetting()
        {
            // -- デフォルト設定

            NamePlateVisible = true;
        }

        /// <summary>
        /// メインウィンドウに付随している上部のToolStripの
        /// ボタンのEnable/Disableを切り替えたい時のcallback用のデリゲート
        /// 
        /// nullのままにしておくと呼び出されない。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="enable"></param>
        public delegate void SetButtonHandler(MainDialogToolStripButtonEnum name, bool enable);

        /// <summary>
        /// ToolStripのボタンの変更delegate
        /// </summary>
        public SetButtonHandler SetButton { get; set; }

        /// <summary>
        /// メインウィンドウ側のMenuを更新するためのハンドラ
        /// </summary>
        public PropertyChangedEventHandler UpdateMenuItems { get; set; }

        /// <summary>
        /// ゲームサーバー本体。外部で生成して渡す。
        /// </summary>
        public LocalGameServer gameServer { get; set; }

        /// <summary>
        /// ネームプレートに対局者名の描画をします。(デフォルト true)
        ///
        /// ※　普通の駒台のときのネームプレート自体描画したくないのだが、Bitmap合成後のため、それはできないのであった…。
        /// ※　細長い駒台のときはネームプレートの描画自体を抑制している。
        /// </summary>
        public bool NamePlateVisible { get; set; }

        /// <summary>
        /// 棋譜ウインドウのDockStateを無視します。(デフォルト false)
        /// これをtrueにすると、常に棋譜ウインドウがこのControlに埋まっている形になります。
        /// </summary>
        public bool IgnoreKifuDockState { get; set; }
    }
}
