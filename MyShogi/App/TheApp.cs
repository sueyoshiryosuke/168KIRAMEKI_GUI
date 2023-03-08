﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using MyShogi.Model.Common.ObjectModel;
using MyShogi.Model.Common.Tool;
using MyShogi.Model.Resource.Images;
using MyShogi.Model.Resource.Sounds;
using MyShogi.Model.Shogi.EngineDefine;
using MyShogi.Model.Shogi.LocalServer;

// とりま、Windows用
// あとで他環境用を用意する(かも)
using MyShogi.View.Win2D;

namespace MyShogi.App
{
    /// <summary>
    /// このアプリケーション
    /// singletonで生成
    /// </summary>
    public partial class TheApp
    {
        #region main
        /// <summary>
        /// ここが本アプリのエントリーポイント
        /// </summary>
        public void Run()
        {
#if true

            // -- リリース時
            try
            {
                DevTest();
                Main();
            } catch (Exception ex)
            {
                // これを表示するようにしておくと、開発環境以外で実行した時のデバッグが楽ちん。
                MessageShow(ex);
            }
#else
            // -- 開発(デバッグ)時

            // 開発時に例外がここでcatchされてしまうとデバッグがしにくいので
            // 開発時にはこちらを使う。(といいかも)
            DevTest();
            Main(args);
#endif
        }

        /// <summary>
        /// 開発時のテストコード
        /// </summary>
        private void DevTest()
        {
            // -- 駒素材画像の変換

            //ImageConverter.ConvertPieceImage();
            //ImageConverter.ConvertBoardNumberImage();

            // -- 各エンジン用の設定ファィルを書き出す。

            //EngineDefineSample.WriteEngineDefineFiles2018();
        }

        /// <summary>
        /// メインの処理。
        /// 
        /// 各インスタンスを生成して、イベントのbindを行い、メインダイアログの実行を開始する。
        /// </summary>
        private void Main()
        {
            // -- working directoryの変更

            // 拡張子関連付けやショートカットなどで他のフォルダがworking directoryに設定されていることがある。
            {
                try
                {
                    var dirpath = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Environment.GetCommandLineArgs()[0]));
                    Directory.SetCurrentDirectory(dirpath);
                } catch { }
            }


            // -- global configの読み込み

            Config = GlobalConfig.CreateInstance();

            // -- 各インスタンスの生成と、それぞれのbind作業

            // -- 画像の読み込み

            {
                ImageManager = new ImageManager();
                ImageManager.Update(); // ここでconfigに従い、画像が読み込まれる。

                // GlobalConfigのプロパティ変更に対して、このimageManagerが呼び出されるようにbindしておく。

                Config.AddPropertyChangedHandler("BoardImageVersion", ImageManager.UpdateBoardImage);
                Config.AddPropertyChangedHandler("BoardImageColorVersion", ImageManager.UpdateBoardImage);
                Config.AddPropertyChangedHandler("TatamiImageVersion", ImageManager.UpdateBoardImage);
                Config.AddPropertyChangedHandler("TatamiImageColorVersion", ImageManager.UpdateBoardImage);
                Config.AddPropertyChangedHandler("PieceTableImageVersion", ImageManager.UpdateBoardImage);

                Config.AddPropertyChangedHandler("PieceImageVersion", ImageManager.UpdatePieceImage);
                Config.AddPropertyChangedHandler("PieceImageColorVersion", ImageManager.UpdatePieceImage);

                Config.AddPropertyChangedHandler("PieceAttackImageVersion", ImageManager.UpdatePieceAttackImage);

                Config.AddPropertyChangedHandler("LastMoveFromColorType", ImageManager.UpdatePieceMoveImage);
                Config.AddPropertyChangedHandler("LastMoveToColorType", ImageManager.UpdatePieceMoveImage);
                Config.AddPropertyChangedHandler("PickedMoveFromColorType", ImageManager.UpdatePieceMoveImage);
                Config.AddPropertyChangedHandler("PickedMoveToColorType", ImageManager.UpdatePieceMoveImage);

                Config.AddPropertyChangedHandler("BoardNumberImageVersion", ImageManager.UpdateBoardNumberImage);
            }

            // -- メインの対局ウィンドゥ

            var mainDialog = new MainDialog();
            mainForm = mainDialog;

            // -- 対局controllerを1つ生成して、メインの対局ウィンドゥのViewModelに加える

            var gameServer = new LocalGameServer();
            mainDialog.Init(gameServer);

            // -- 盤・駒が変更されたときにMainDialogのメニューの内容を修正しないといけないので更新がかかるようにしておく。

            // 表示設定

            Config.AddPropertyChangedHandler("BoardImageVersion", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("BoardImageColorVersion", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("TatamiImageVersion", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("TatamiImageColorVersion", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("PieceImageVersion", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("PieceImageColorVersion", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("PromotePieceColorType", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("FlipWhitePromoteDialog", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("PieceAttackImageVersion", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("BoardNumberImageVersion", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("LastMoveFromColorType", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("LastMoveToColorType", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("PickedMoveFromColorType", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("PickedMoveToColorType", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("TurnDisplay", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("MemoryLoggingEnable", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("FileLoggingEnable", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("NegateEvalWhenWhite", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("KifuWindowKifuVersion", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("ConsiderationWindowKifuVersion", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("DisplayNameTurnVersion", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("EnableGameEffect", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("PickedMoveDisplayStyle", mainDialog.ForceRedraw);

            // 棋譜・検討ウインドウの高さ・幅

            Config.AddPropertyChangedHandler("KifuWindowWidthType", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("KifuWindowWidthType", mainDialog.ResizeKifuControl, mainDialog);
            Config.AddPropertyChangedHandler("KifuWindowWidthType", mainDialog.UpdateMenuItems, mainDialog);
            Config.AddPropertyChangedHandler("ConsiderationWindowHeightType", mainDialog.ForceRedraw);
            Config.AddPropertyChangedHandler("ConsiderationWindowHeightType", mainDialog.ResizeConsiderationControl, mainDialog);
            Config.AddPropertyChangedHandler("ConsiderationWindowHeightType", mainDialog.UpdateMenuItems , mainDialog);

            // 操作設定

            Config.AddPropertyChangedHandler("KifuWindowPrevNextKey", mainDialog.UpdateToolStripShortcut);
            Config.AddPropertyChangedHandler("KifuWindowNextSpecialKey", mainDialog.UpdateToolStripShortcut);
            Config.AddPropertyChangedHandler("KifuWindowFirstLastKey", mainDialog.UpdateToolStripShortcut);
            Config.AddPropertyChangedHandler("ConsiderationWindowPrevNextKey", mainDialog.UpdateToolStripShortcut);
            
            // DockWindow

            Config.KifuWindowDockManager.AddPropertyChangedHandler("DockState", mainDialog.UpdateMenuItems, mainDialog);
            Config.EngineConsiderationWindowDockManager.AddPropertyChangedHandler("DockState", mainDialog.UpdateMenuItems, mainDialog);
            Config.MiniShogiBoardDockManager.AddPropertyChangedHandler("DockState", mainDialog.UpdateMenuItems, mainDialog);
            Config.EvalGraphDockManager.AddPropertyChangedHandler("DockState", mainDialog.UpdateMenuItems, mainDialog);

            // -- ロギング用のハンドラをセット

            // メモリ上でのロギング
            Log.log1 = new MemoryLog();

            var FileLoggingEnable = new PropertyChangedEventHandler((args) =>
            {
                if (Config.FileLoggingEnable)
                {
                    var now = DateTime.Now;
                    Log.log2 = new FileLog($"log{now.ToString("yyyyMMddHHmm")}.txt");
                }
                else
                {
                    if (Log.log2 != null)
                        Log.log2.Dispose();
                    Log.log2 = null;
                }
            });

            Config.AddPropertyChangedHandler("FileLoggingEnable", FileLoggingEnable);

            // 上のハンドラを呼び出して、必要ならばロギングを開始しておいてやる。
            FileLoggingEnable(null);


            // 初期化が終わったのでgameServerの起動を行う。
            gameServer.Start();

            // サウンド
            SoundManager = new SoundManager();
            SoundManager.Start();

            // 終了するときに設定ファイルに書き出すコード
            Application.ApplicationExit += new EventHandler((sender, e) =>
            {
                // メインウィンドウのサイズを保存
                SaveMainDialogSize();

                // 設定ファイルの保存
                SaveConfig();

                // サウンドマネージャーの停止
                SoundManager.Dispose();

                // 起動しているGameServerすべてを明示的に終了させる必要がある。(そこにぶら下がっているエンジンを停止させるため)
                if (gameServer != null)
                    gameServer.Dispose();
            });
            
            // -- メインダイアログを生成して、アプリの開始

            Application.Run(mainDialog);
        }
        #endregion

        #region properties

        // -- それぞれのViewModel
        // 他のViewModelにアクションが必要な場合は、これを経由して通知などを行えば良い。
        // 他のViewに直接アクションを起こすことは出来ない。必ずViewModelに通知などを行い、
        // そのViewModelのpropertyをsubscribeしているViewに間接的に通知が行くという仕組みを取る。

        /// <summary>
        /// 画像の読み込み用。本GUIで用いる画像はすべてここから取得する。
        /// </summary>
        public ImageManager ImageManager { get; private set; }

        /// <summary>
        /// GUIの全体設定
        /// </summary>
        public GlobalConfig Config { get; private set; }

        /// <summary>
        /// エンジン設定(最初のアクセスの時に読み込む。遅延読み込み。)
        /// </summary>
        public List<EngineDefineEx> EngineDefines
        {
            get {
                lock (this)
                {
                    if (engine_defines == null)
                        engine_defines = EngineDefineUtility.GetEngineDefines();
                    return engine_defines;
                }
            }
        }
        private List<EngineDefineEx> engine_defines;

        /// <summary>
        /// [UI Thread] : EngineConfigを返す。
        /// (エンジンのオプションの共通設定、個別設定が格納されている。)
        /// </summary>
        public EngineConfigs EngineConfigs
        {
            get
            {
                lock (this)
                {
                    /// 遅延読み込み。
                    if (engine_configs == null)
                        engine_configs = EngineConfigUtility.GetEngineConfig();
                    return engine_configs;
                }
            }
        }
        private EngineConfigs engine_configs;

        /// <summary>
        /// サウンドマネージャー
        /// </summary>
        public SoundManager SoundManager { get; private set; }

        /// <summary>
        /// Visual Studioのデザインモードであるかの判定。
        /// デザインモードだとconfigが未代入なのでnullのはずであるから…。
        ///
        /// Form.DesignModeは、Formのコンストラクタでは未代入であるので使えない。
        /// こういう方法に頼らざるを得ない。Formクラスの設計ミスであるように思う。
        /// </summary>
        public bool DesignMode { get { return Config == null; } }

        /// <summary>
        /// 終了時にエンジンオプションの設定ファイルを消すフラグ
        /// </summary>
        public bool DeleteEngineOption { get; set; }

        /// <summary>
        /// 終了時にGlobalOptionのファイルを消すフラグ
        /// </summary>
        public bool DeleteGlobalOption { get; set; }

        /// <summary>
        /// サブウインドウでキーボードショートカットを自前でハンドルするためのもの。
        /// </summary>
        public KeyboardShortcutHelper KeyShortcut { get; } = new KeyboardShortcutHelper();

        /// <summary>
        /// singletonなinstance。それぞれのViewModelなどにアクセスしたければ、これ経由でアクセスする。
        /// </summary>
        public static TheApp app = new TheApp();

        #endregion

        #region public members

        /// <summary>
        /// デバッグ用に、デバッグウインドウにメッセージを出力する。
        ///
        /// MainDialogの初期化が終わってからでないと呼び出してはならない。
        /// </summary>
        /// <param name="message"></param>
        public static void WriteLog(string message)
        {
            // ここに引っかかったとしたら、Log.log1の初期化前だから。
            Debug.Assert(Log.log1 != null);

            Log.log1.Write(LogInfoType.DebugMessage, message);
        }

        #endregion

        #region privates

        /// <summary>
        /// MainDialogのウィンドウサイズをGlobalConfigに代入する。(次回起動時に復元するため)
        /// </summary>
        private void SaveMainDialogSize()
        {
        }

        /// <summary>
        /// 終了時に削除フラグが立っていなければ、このまま設定(GlobalConfigと各エンジンのオプション)を保存する。
        /// 削除フラグが立っていれば設定ファイルを削除する。
        /// </summary>
        private void SaveConfig()
        {
            if (DeleteGlobalOption)
                Config.Delete();
            else
                Config.Save();

            if (DeleteEngineOption)
                EngineConfigUtility.DeleteEngineConfig();
            else
                if (engine_configs != null)
                engine_configs.Save();
        }
        #endregion
    }
}
