using IrcDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Yukarinette;

namespace YukarinetteToTwitchChat
{
    public class TwitchClient
    {
        public TwitchIrcClient client;

        public TwitchClient()
        {
        }

        public static bool Validate(string token)
        {
            var req = WebRequest.CreateHttp("https://id.twitch.tv/oauth2/validate");
            req.Headers.Add("Authorization", $"Bearer {token}");
            HttpWebResponse res = null;
            try
            {
                YukarinetteConsoleMessage.Instance.WriteMessage($"[YukarinetteToTwitchChatPlugin] Validate request...");
                res = req.GetResponse() as HttpWebResponse;
                if (res?.StatusCode != HttpStatusCode.OK)
                {
                    YukarinetteConsoleMessage.Instance.WriteMessage($"[YukarinetteToTwitchChatPlugin] Validate failed. StatusCode:[{res.StatusCode} {res.StatusDescription}]");
                    return false;
                }

                StreamReader sr = new StreamReader(res.GetResponseStream(), Encoding.UTF8);
                var jsonStr = sr.ReadToEnd();

                var validateRes = JsonSerializer.Deserialize<ValidateResponse>(jsonStr);
                YukarinetteConsoleMessage.Instance.WriteMessage($"[YukarinetteToTwitchChatPlugin] login:{validateRes.Login}");

                var config = ConfigData.Instance;
                config.Token = token;
                config.LoginUser = validateRes.Login;
                config.Save();

                return true;
            }
            catch (Exception ex)
            {
                YukarinetteConsoleMessage.Instance.WriteMessage($"[YukarinetteToTwitchChatPlugin] Validate error.");
                YukarinetteConsoleMessage.Instance.WriteMessage($"{ex}");
                return false;
            }
            finally
            {
                res?.Close();
            }
        }

        public bool Connect(string user, string pass)
        {
            YukarinetteConsoleMessage.Instance.WriteMessage($"Connect.");

            if (client != null)
            {
                client.Dispose();
                client = null;
            }

            var server = "irc.chat.twitch.tv";
            var username = user;
            var password = pass;

            client = new TwitchIrcClient();
            client.FloodPreventer = new IrcStandardFloodPreventer(4, 2000);
            client.Disconnected += IrcClient_Disconnected;
            client.Registered += IrcClient_Registered;

            // Wait until connection has succeeded or timed out.

            try
            {
                using (var registeredEvent = new ManualResetEventSlim(false))
                {
                    using (var connectedEvent = new ManualResetEventSlim(false))
                    {
                        client.Connected += (sender2, e2) => connectedEvent.Set();
                        client.Registered += (sender2, e2) => registeredEvent.Set();
                        client.Connect(server, false,
                            new IrcUserRegistrationInfo()
                            {
                                NickName = username,
                                Password = password,
                                UserName = username
                            });
                        if (!connectedEvent.Wait(10000))
                        {
                            YukarinetteConsoleMessage.Instance.WriteMessage($"Connection to '{server}' timed out.");
                            return false;
                        }
                    }
                    YukarinetteConsoleMessage.Instance.WriteMessage($"Now connected to '{server}'.");
                    if (!registeredEvent.Wait(10000))
                    {
                        YukarinetteConsoleMessage.Instance.WriteMessage($"Could not register to '{server}'.");
                        return false;
                    }
                }

                YukarinetteConsoleMessage.Instance.WriteMessage($"Now registered to '{server}' as '{username}'.");
            }
            catch (Exception ex)
            {
                YukarinetteConsoleMessage.Instance.WriteMessage($"Connect error: {ex}");
            }

            return true;
        }

        public void Disconnect()
        {
            client.Disconnect();
            client.Dispose();
            client = null;
        }

        public void SendChat(string message)
        {
            var config = ConfigData.Instance;
            client.SendRawMessage($"PRIVMSG #{config.LoginUser} :{message}");
        }

        private static void IrcClient_Registered(object sender, EventArgs e)
        {
            YukarinetteConsoleMessage.Instance.WriteMessage($"IrcClient_Registered.");

            var client = (IrcClient)sender;

            client.LocalUser.NoticeReceived += IrcClient_LocalUser_NoticeReceived;
            client.LocalUser.MessageReceived += IrcClient_LocalUser_MessageReceived;
            client.LocalUser.JoinedChannel += IrcClient_LocalUser_JoinedChannel;
            client.LocalUser.LeftChannel += IrcClient_LocalUser_LeftChannel;
        }

        private static void IrcClient_LocalUser_NoticeReceived(object sender, IrcMessageEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;
            YukarinetteConsoleMessage.Instance.WriteMessage($"Notice: {e.Text}.");
        }

        private static void IrcClient_LocalUser_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;

            if (e.Source is IrcUser)
            {
                // Read message.
                YukarinetteConsoleMessage.Instance.WriteMessage($"({e.Source.Name}): {e.Text}.");
            }
            else
            {
                YukarinetteConsoleMessage.Instance.WriteMessage($"({e.Source.Name}) Message: {e.Text}.");
            }
        }

        private static void IrcClient_LocalUser_JoinedChannel(object sender, IrcChannelEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;

            e.Channel.MessageReceived += IrcClient_Channel_MessageReceived;
            e.Channel.NoticeReceived += IrcClient_Channel_NoticeReceived;

            YukarinetteConsoleMessage.Instance.WriteMessage($"You joined the channel {e.Channel.Name}.");
        }

        private static void IrcClient_LocalUser_LeftChannel(object sender, IrcChannelEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;

            e.Channel.MessageReceived -= IrcClient_Channel_MessageReceived;
            e.Channel.NoticeReceived -= IrcClient_Channel_NoticeReceived;

            YukarinetteConsoleMessage.Instance.WriteMessage($"You left the channel {e.Channel.Name}.");
        }

        private static void IrcClient_Channel_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            var channel = (IrcChannel)sender;
            if (e.Source is IrcUser)
            {
                // Read message.
                YukarinetteConsoleMessage.Instance.WriteMessage($"[{channel.Name}]({e.Source.Name}): {e.Text}.");
            }
            else
            {
                YukarinetteConsoleMessage.Instance.WriteMessage($"[{channel.Name}]({e.Source.Name}) Message: {e.Text}.");
            }
        }

        private static void IrcClient_Channel_NoticeReceived(object sender, IrcMessageEventArgs e)
        {
            var channel = (IrcChannel)sender;

            YukarinetteConsoleMessage.Instance.WriteMessage($"[{channel.Name}] Notice: {e.Text}.");
        }

        private static void IrcClient_Disconnected(object sender, EventArgs e)
        {
            YukarinetteConsoleMessage.Instance.WriteMessage($"IrcClient_Disconnected.");
        }
    }
}
