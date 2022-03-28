using System.Diagnostics;
using System.Windows;
using Yukarinette;

namespace YukarinetteToTwitchChat
{
    /// <summary>
    /// SettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : Window
    {
        public SettingWindow()
        {
            InitializeComponent();

            var config = ConfigData.Instance;
            if (TwitchClient.Validate(config.Token))
            {
                AccountStatus.Text = ConfigData.Instance.LoginUser;
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var w = new WaitForBrowserWindow(LogWrite);
            var url = string.Format(
                "https://id.twitch.tv/oauth2/authorize?client_id=x2jplirqcks7jzuql5b4djhdt4o640&redirect_uri={0}&response_type=token&scope=chat:read%20chat:edit&force_verify=true",
                "http://localhost:62643/");
            Process.Start(url);
            var result = w.ShowDialog();

            if (result ?? false)
            {
                TwitchClient.Validate(w.Result);
            }

            AccountStatus.Text = ConfigData.Instance.LoginUser;
        }

        public void LogWrite(string str)
        {
            YukarinetteConsoleMessage.Instance.WriteMessage($"[YukarinetteToTwitchChatPlugin] {str}");
        }
    }
}
