using System.Windows.Forms;
using MyShogi.App;
using MyShogi.Model.Resource.Images;

namespace MyShogi.View.Win2D
{
    public partial class AboutYaneuraOu : Form
    {
        public AboutYaneuraOu()
        {
            InitializeComponent();

            InitView();

            FontUtility.ReplaceFont(this, TheApp.app.Config.FontManager.SettingDialog);
        }

        private void InitView()
        {
#if false
            var file_name =
                TheApp.app.Config.CommercialVersion == 0 ?
                "html/about_dialog_opensource_version.html" :
                "html/about_dialog_commercial_version.html";

            webBrowser1.Navigate(Path.Combine(Application.StartupPath,file_name).ToString());

            // -- バージョン文字列をJavaScript経由でインジェクションする。

            // 1回メッセージループを処理しないとNavigate()が終わらない。
            Application.DoEvents();

            var version_string = "MyShogi Version " + GlobalConfig.MYSHOGI_VERSION_STRING;
            webBrowser1.Document.InvokeScript("add_version_string", new string[] { version_string });
#endif

            // →　Mac/Linuxで動作しないのでWebBrowserを使わない実装に変更。

            using (var slb = new SuspendLayoutBlock(this))
            {
                // 「いろは煌盤」用カスタム
                var commercial = TheApp.app.Config.CommercialVersion;
                var message1 =
                    "いろは煌盤（きらめきばん） Version " + GlobalConfig.KIRAMEKI_MYSHOGI_VERSION_STRING + "\r\n" +
                    "Base: MyShogi Version " + GlobalConfig.MYSHOGI_VERSION_STRING + "\r\n";
                var message2 =
                     commercial == 0 ?
                    "(C)2022-2023 いろは煌盤（きらめきばん）"
                    : "(C)2018 Mynavi Publishing Corporation / MyShogi Project\r\nMyShogi Project : https://github.com/yaneurao/MyShogi";

                label1.Text = message1;
                textBox1.Text = message2;

                //pictureBox1.Image = TheApp.app.ImageManager.GameLogo.image;
            }
        }
    }
}
