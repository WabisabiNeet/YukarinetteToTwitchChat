using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Yukarinette;

namespace YukarinetteToTwitchChat
{
    public class YukarinetteToTwitchChatPlugin : IYukarinetteInterface
    {
        public static string PluginName = "ゆかりねっとTwitchチャットプラグイン";

        public override string Name => PluginName;

        TwitchClient client = new TwitchClient();

        public YukarinetteToTwitchChatPlugin() : base()
        {
            YukarinetteConsoleMessage.Instance.WriteMessage($"{YukarinetteCommon.AppSettingFolder}");
        }

        public override void Loaded()
        {
            base.Loaded();

            var config = ConfigData.Instance;
            config.Load();
        }

        public override void Setting()
        {
            var w = new SettingWindow();
            w.ShowDialog();
        }

        public override void SpeechRecognitionStart()
        {
            YukarinetteConsoleMessage.Instance.WriteMessage($"SpeechRecognitionStart.");

            var config = ConfigData.Instance;
            if (string.IsNullOrEmpty(config.Token) || string.IsNullOrEmpty(config.LoginUser))
            {
                var message = $"{PluginName}がTwitchと紐づけられていません。プラグインの設定からTwitchと連携してください。";
                YukarinetteConsoleMessage.Instance.WriteMessage($"{message}");
                throw new YukarinetteException(message);
            }

            if (!TwitchClient.Validate(config.Token))
            {
                var message = $"{PluginName}とTwitchの紐づけが何かおかしいです。プラグインの設定からTwitchと再度連携してください。";
                YukarinetteConsoleMessage.Instance.WriteMessage($"{message}");
                throw new YukarinetteException(message);
            }

            client.Connect($"{ConfigData.Instance.LoginUser}", $"oauth:{ConfigData.Instance.Token}");
        }

        public override void SpeechRecognitionStop()
        {
            YukarinetteConsoleMessage.Instance.WriteMessage($"SpeechRecognitionStop.");
            client.Disconnect();
        }

        public override void Speech(string text)
        {
            client.SendChat(text);
        }
    }
}
