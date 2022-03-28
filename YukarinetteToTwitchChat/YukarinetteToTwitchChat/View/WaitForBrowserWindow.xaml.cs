using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace YukarinetteToTwitchChat
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class WaitForBrowserWindow : Window
    {
        public const string RedirectUri = "http://localhost:62643/";
        private HttpListener _listener;

        private Action<string> logWrite;

        public string Result { get; private set; }

        public string RefreshToken { get; private set; }

        public WaitForBrowserWindow(Action<string> logger)
        {
            InitializeComponent();
            logWrite = logger;

            Task.Run(waitResponse);
        }

        private async Task waitResponse()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(RedirectUri);
            _listener.Start();
            logWrite($"{RedirectUri} listen start...");
            try
            {
                while (true)
                {
                    try
                    {
                        bool closeWindow = false;
                        HttpListenerContext contextAsync = await _listener.GetContextAsync();
                        HttpListenerResponse response = contextAsync.Response;
                        response.ContentType = "text/html";
                        string str1 = contextAsync.Request.QueryString["access_token"];
                        if (!string.IsNullOrEmpty(str1))
                        {
                            logWrite($"access_token[{str1}]");
                            Result = str1;
                            closeWindow = true;
                        }
                        string str2 = contextAsync.Request.QueryString["code"];
                        if (!string.IsNullOrEmpty(str2))
                        {
                            logWrite($"access_token[{str2}]");
                            Result = str2;
                            closeWindow = true;
                        }
                        if (!string.IsNullOrEmpty(contextAsync.Request.QueryString["error"])) closeWindow = true;

                        byte[] buffer = Encoding.UTF8.GetBytes(oauthResponseHtml);
                        Stream output = response.OutputStream;
                        output.Write(buffer, 0, buffer.Length);
                        response.Close();

                        if (closeWindow)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                this.DialogResult = true;
                            });
                            break;
                        }
                        response = null;
                    }
                    catch (HttpListenerException ex)
                    {
                        if (!_listener.IsListening)
                            break;
                    }
                    catch (Exception ex)
                    {
                        logWrite(ex.ToString());
                        break;
                    }
                    finally
                    {
                        logWrite($"while end.");
                    }
                }
            }
            finally
            {

            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this._listener?.Close();
        }

        #region
        private string oauthResponseHtml = @"<!DOCTYPE html>
<html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <meta charset = ""utf-8"" />

    <script type=""text/javascript"">
		window.onload = function()
        {
            if (window.location.hash)
            {
                window.location.href = ""?"" + window.location.hash.substring(1);
            }
            else
            {
                document.body.innerText = 'Authorization ' + (window.location.search.startsWith('?error') ? 'Canceled' : 'Succeeded');
                if (window.navigator.userAgent.toLowerCase().indexOf('firefox') == -1)
                {
                    window.open('about:blank', '_self').close();
                }
            }
        }
	</script>
</head>
<body></body>
</html>";
        #endregion
    }
}
