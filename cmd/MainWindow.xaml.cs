using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace cmd
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        int sessionID, userID;

        public MainWindow()
        {
            ShowInTaskbar = false;
            Opacity = 1;
            InitializeComponent();

            userID = getUserID();

            sessionID = createSession();


            if (sessionID == -1)
            {
                MessageBox.Show("CREATE SESSION ERROR");
                Environment.Exit(0);
            }

            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(timer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();

        }

        private int getUserID()
        {
            int userID = 0;

            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\control");

            if (key == null)
            {
                userID = registerUserID();
                RegistryKey control = Registry.CurrentUser.CreateSubKey("Software\\control");
                control.SetValue("id", userID);
            }
            else
            {
                userID = (int)key.GetValue("id");
            }

            return userID;
        }

        private int registerUserID()
        {
            int id = 0;

            while (!int.TryParse(request("http://akita123.atwebpages.com/main.php?type=5"), out id) || id <= 0)
            {
                Thread.Sleep(1000);
            }

            return id;
        }

        private int createSession()
        {
            int id;

            while (!int.TryParse(request("http://akita123.atwebpages.com/main.php?type=1&userid=" + userID), out id))
            {
                Thread.Sleep(1000);
            }

            return id;
        }

        private string[] getCommand()
        {
            string responseFromServer = request("http://akita123.atwebpages.com/main.php?type=3&ssid=" + sessionID + "&touserid=" + userID);

            string[] sep = { "</br>" };

            return responseFromServer.Split(sep, StringSplitOptions.None);
        }

        private void sendMess(string mess)
        {
            string url = "http://akita123.atwebpages.com/main.php?type=2&cmd=mess " + mess + "&fromuserid=" + userID + "&touserid=1&ssid=" + sessionID;
            request(url);
        }

        private string request(string url)
        {
            WebRequest request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);

            string responseFromServer = reader.ReadToEnd();

            reader.Close();
            response.Close();

            return responseFromServer;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            string[] cmds = getCommand();

            foreach (string cmd in cmds)
            {
                if (cmd != null && cmd != "")
                {
                    cmdProc(cmd);
                }
            }
        }

        private void cmdProc(string cmd)
        {
            if (cmd.ToUpper().StartsWith("MESS"))
            {
                Opacity = 1;

                txtChatBox.AppendText("SERVER: " + cmd.Substring(5) + Environment.NewLine);
                txtChatBox.ScrollToEnd();
            }
        }

        private void send()
        {
            if (txtMessage.Text != "")
            {
                sendMess(txtMessage.Text);
                txtChatBox.AppendText("CLIENT: " + txtMessage.Text + Environment.NewLine);
                txtChatBox.ScrollToEnd();
                txtMessage.Text = "";
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            send();
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                send();
            }
        }
    }
}
