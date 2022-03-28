using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using Yukarinette;

namespace YukarinetteToTwitchChat
{
    public sealed class ConfigData
    {
        private static readonly object _lock = new object();
        private static ConfigData instance;

        private string settingPath;

        public static ConfigData Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (_lock)
                    {
                        if (instance == null)
                        {
                            instance = new ConfigData();
                        }
                    }
                }
                return instance;
            }
        }

        private ConfigData()
        {
            string fileName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            settingPath = Path.Combine(Path.Combine(YukarinetteCommon.AppSettingFolder, "plugins"), fileName + ".config");
        }

        public string Token { get; set; } = "";

        public string LoginUser { get; set; } = "";

        public void Load()
        {
            try
            {
                using (FileStream fileStream = new FileStream(settingPath, FileMode.Open))
                {
                    using (StreamReader streamReader = new StreamReader((Stream)fileStream, Encoding.UTF8))
                    {
                        var c = (ConfigData)new XmlSerializer(typeof(ConfigData)).Deserialize((TextReader)streamReader);
                        Token = c.Token;
                        LoginUser = c.LoginUser;
                    }
                }
                YukarinetteLogger.Instance.Info((object)("setting load ok. SettingPath=" + settingPath));
            }
            catch (Exception ex)
            {
                YukarinetteLogger.Instance.Error((object)ex);
                YukarinetteConsoleMessage.Instance.WriteMessage($"{YukarinetteToTwitchChatPlugin.PluginName} の設定ファイルが読み取れませんでした。初期値で動作します。");
            }
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(this.settingPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(this.settingPath));
                using (FileStream fileStream = new FileStream(this.settingPath, FileMode.Create))
                {
                    using (StreamWriter streamWriter = new StreamWriter((Stream)fileStream, Encoding.UTF8))
                        new XmlSerializer(typeof(ConfigData)).Serialize((TextWriter)streamWriter, this);
                }
                YukarinetteLogger.Instance.Info((object)("setting save ok. SettingPath=" + this.settingPath));
            }
            catch (Exception ex)
            {
                YukarinetteLogger.Instance.Error((object)ex);
                YukarinetteConsoleMessage.Instance.WriteMessage("SofTalk の設定ファイルの保存に失敗しました。");
            }
            YukarinetteLogger.Instance.Debug((object)"end.");
        }
    }
}
