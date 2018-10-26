﻿using System.Windows.Forms;
using MyShogi.App;
using MyShogi.Model.Common.ObjectModel;

namespace MyShogi.View.Win2D
{
    /// <summary>
    /// 対局盤面などがあるメインウインドウ
    ///
    /// メインメニューにぶら下がっているToolStripのボタンのショートカットハンドラの初期化
    /// </summary>
    public partial class MainDialog : Form
    {
        /// <summary>
        /// メインウインドウにぶら下がっているToolTipのShortcutKeyを設定する。
        /// </summary>
        public void UpdateToolStripShortcut(PropertyChangedEventArgs args=null)
        {
            var config = TheApp.app.Config;
            var shortcut = TheApp.app.KeyShortcut;
            shortcut.InitEvent2();; // このdelegateにShortcutキーのハンドラを登録していく。

            // 棋譜の1手戻る/進むキー
            switch (config.KifuWindowPrevNextKey)
            {
                case 0: // なし
                    break;
                case 1: // ←と→
                    shortcut.AddEvent2((sender, e) => { if (e.KeyCode == Keys.Left ) { toolStripButton9.PerformClick(); e.Handled = true; } });
                    shortcut.AddEvent2((sender, e) => { if (e.KeyCode == Keys.Right ) { toolStripButton10.PerformClick(); e.Handled = true; }});
                    break;
                case 2: // ↑と↓
                    shortcut.AddEvent2((sender, e) => { if (e.KeyCode == Keys.Up) { toolStripButton9.PerformClick(); e.Handled = true; } });
                    shortcut.AddEvent2((sender, e) => { if (e.KeyCode == Keys.Down) { toolStripButton10.PerformClick(); e.Handled = true; } });
                    break;

            }

            // 次の1手に移動する特殊キー
            // 使わないキー、殺しておかないとListViewが反応しかねない。
            switch (config.KifuWindowNextSpecialKey)
            {
                case 0: // なし
                    shortcut.AddEvent2((sender, e) => { if (e.KeyCode == Keys.Space) { e.Handled = true; } });
                    shortcut.AddEvent2((sender, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; } });
                    break;

                case 1: // スペースキー
                    shortcut.AddEvent2((sender, e) => { if (e.KeyCode == Keys.Space) { toolStripButton10.PerformClick(); e.Handled = true; } });
                    shortcut.AddEvent2((sender, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; } });
                    break;

                case 2: // Enterキー
                    shortcut.AddEvent2((sender, e) => { if (e.KeyCode == Keys.Space) { e.Handled = true; } });
                    shortcut.AddEvent2((sender, e) => { if (e.KeyCode == Keys.Enter) { toolStripButton10.PerformClick(); e.Handled = true; } });
                    break;
            }

            // 最初に戻る/最後に進むキー
            switch (config.KifuWindowFirstLastKey)
            {
                case 0: // なし
                    break;

                case 1: // ↑と↓
                    shortcut.AddEvent2((sender, e) => { if (e.KeyCode == Keys.Up) { toolStripButton12.PerformClick(); e.Handled = true; } });
                    shortcut.AddEvent2((sender, e) => { if (e.KeyCode == Keys.Down) { toolStripButton13.PerformClick(); e.Handled = true; } });
                    break;

                case 2: // ←と→
                    shortcut.AddEvent2((sender, e) => { if (e.KeyCode == Keys.Left) { toolStripButton12.PerformClick(); e.Handled = true; } });
                    shortcut.AddEvent2((sender, e) => { if (e.KeyCode == Keys.Right) { toolStripButton13.PerformClick(); e.Handled = true; } });
                    break;

                case 3: // PageUpとPageDown
                    shortcut.AddEvent2((sender, e) => { if (e.KeyCode == Keys.PageUp) { toolStripButton12.PerformClick(); e.Handled = true; } });
                    shortcut.AddEvent2((sender, e) => { if (e.KeyCode == Keys.PageDown) { toolStripButton13.PerformClick(); e.Handled = true; } });
                    break;
            }

        }

    }
}