using Newtonsoft.Json.Linq;
using Quobject.SocketIoClientDotNet.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AxWMPLib;

namespace Koncowka
{
    public partial class MainForm : Form
    {
        private string clientName;
        private List<Object> files = new List<Object>();
        private Timer timer;
        private int counter;
        private const string url = "http://ti.tambou.pl:3000/user-images/";
        private const string wsUrl = "ws://ti.tambou.pl:3000/";
        private dynamic socket;
       
        public MainForm()
        {
            InitializeComponent();

            userComboBox.DisplayMember = "Text";
            userComboBox.ValueMember = "Value";
            var items = new[]
            {
                new { Text = "test-client-0", Value = "test-client-0" },
                new { Text = "test-client-1", Value = "test-client-1" },
                new { Text = "test-client-2", Value = "test-client-2" }
            };

            userComboBox.DataSource = items;
            userComboBox.SelectedIndex = 0;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            clientName = (string)userComboBox.SelectedValue;
            TimerReset();
            files.Clear();
            counter = 0;

            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                Online();
            }
            else
            {
                Offline();
            }
        }

        private async void Online()
        {
            btnConnect.Enabled = false;
            btnDisconnect.Enabled = true;
            socket = Connect(wsUrl, clientName);

            this.Text = "Synchronizacja plików...";
            await Sync.SynchronizeFiles(url, clientName);
            this.Text = this.Name;

            FileList(clientName);

            if (files.Count > 0)
            {
                TimerInit();
            }
            else
                this.Text = "Brak plików do wyświetlenia.";
        }

        private void Offline()
        {
            this.Text = "Wyświetlanie plików w trybie offline.";
            DirectoryInfo di = new DirectoryInfo(clientName);
            if (di.Exists)
            {
                FileList(clientName);

                if (files.Count > 0)
                {
                    TimerInit();
                }
                else
                    this.Text = "Brak plików do wyświetlenia.";
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                Disconnect(socket);
                btnConnect.Enabled = true;
                btnDisconnect.Enabled = false;
            }
        }

        private void FileList(string dirName)
        {
            List<string> videos = new List<string>();

            var imageEntries = Directory.EnumerateFiles(dirName, "*.*")
                .Where(s => s.EndsWith(".png") || s.EndsWith(".jpg") || s.EndsWith(".jpeg") || s.EndsWith(".bmp"));

            var videoEntries = Directory.EnumerateFiles(dirName, "*.*")
            .Where(s => s.EndsWith(".mp4") || s.EndsWith(".avi"));

            pictureBox.Image = null;
            files.Clear();
            videos.Clear();

            foreach (var s in videoEntries)
            {
                files.Add(s);
            }

            foreach (var file in imageEntries)
            {
                try
                {
                    using (var fs = new System.IO.FileStream(file, System.IO.FileMode.Open))
                    {
                        var bmp = new Bitmap(fs);
                        files.Add(ReSize((Bitmap)bmp.Clone()));
                    }
                }
                catch (ArgumentException)
                {
                    this.Text = string.Format("Plik: '{0}' jest uszkodzony", file);
                    File.Delete(file);
                }
                catch (IOException)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        this.Text = "Wystąpił problem podczas synchoronizacji danych.";
                    });
                }
            }
        }

        private Bitmap ReSize(Bitmap image)
        {
            if (image.Height > Screen.PrimaryScreen.Bounds.Height)
            {
                float scaleHeight = image.Height / (float)Screen.PrimaryScreen.Bounds.Height;
                image = new Bitmap(image, (int)(image.Width / scaleHeight), Screen.PrimaryScreen.Bounds.Height);
            }
            if (image.Width > Screen.PrimaryScreen.Bounds.Width)
            {
                float scaleWidth = image.Width / (float)Screen.PrimaryScreen.Bounds.Width;
                image = new Bitmap(image, Screen.PrimaryScreen.Bounds.Width, (int)(image.Height / scaleWidth));
            }

            return image;
        }

        private void TimerInit()
        {
            if (files[counter] is Bitmap)
                pictureBox.Image = (Bitmap)files[0];
            else
            {
                axWindowsMediaPlayer1.Visible = true;
                //axWindowsMediaPlayer1.uiMode = "none";
                PlayFile((string)files[counter]);
                timer.Stop();
                counter++;
                if (files[counter] is Bitmap)
                {
                    pictureBox.Image = (Bitmap)files[counter];
                }
            }

            counter = 1;
            timer.Interval = 5000;
            timer.Tick += new EventHandler(TimerElapsed);
            timer.Start();
        }

        private void TimerReset()
        {
            if (timer != null)
                timer.Dispose();
            timer = new Timer();
        }

        private void TimerElapsed(object sender, EventArgs e)
        {
            if (files.Count > 0)
            {
                if (counter >= files.Count)
                    counter = 0;

                if (files[counter] is Bitmap)
                {
                    pictureBox.Image = (Bitmap)files[counter];
                    counter++;
                }
                else if (files[counter] is string)
                {
                    axWindowsMediaPlayer1.Visible = true;
                    //axWindowsMediaPlayer1.uiMode = "none";
                    PlayFile((string)files[counter]);
                    timer.Stop();
                    counter++;
                    if (files[counter] is Bitmap)
                    {
                        pictureBox.Image = (Bitmap)files[counter];
                    }
                }
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (axWindowsMediaPlayer1.fullScreen)
                    axWindowsMediaPlayer1.fullScreen = false;

                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                pictureBox.Dock = DockStyle.Bottom;
                btnDisconnect.Visible = true;
                btnConnect.Visible = true;
            }
            if (e.KeyCode == Keys.F1)
            {
                this.WindowState = FormWindowState.Maximized;
                this.FormBorderStyle = FormBorderStyle.None;
                pictureBox.Dock = DockStyle.Fill;
                pictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
                btnDisconnect.Visible = false;
                btnConnect.Visible = false;
            }
        }
        private void Disconnect(dynamic socket)
        {
            socket.Close();

        }
        private dynamic Connect(string hostUrl, string userName)
        {
            NewContent += Synchronize;
            var options = new IO.Options() { IgnoreServerCertificateValidation = true, AutoConnect = true, ForceNew = true };
            var socket = IO.Socket(hostUrl, options);
            socket.Connect();
            var jsonObject = new JObject();
            jsonObject.Add("name", userName);
            socket.Emit("client-connect", jsonObject);

            socket.On("sync", () =>
            {
                OnNewContent(EventArgs.Empty);
            });

            return socket;
        }

        private void OnNewContent(EventArgs e)
        {
            NewContent?.Invoke(NewContent, e);
        }

        private async void Synchronize(object sender, EventArgs e)
        {
            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                this.Invoke((MethodInvoker)delegate
                {
                    this.Text = "Synchronizacja plików...";
                });

                await Sync.SynchronizeFiles(url, clientName);
                this.Invoke((MethodInvoker)delegate
                {
                    this.Text = this.Name;
                });

                FileList(clientName);
                if (files.Count > 0)
                {
                    try
                    {
                        if (files[counter] is Bitmap)
                            pictureBox.Image = (Bitmap)files[0];
                        else
                        {
                            axWindowsMediaPlayer1.Visible = true;
                            //axWindowsMediaPlayer1.uiMode = "none";
                            PlayFile((string)files[counter]);
                            timer.Stop();
                            counter++;
                            if (files[counter] is Bitmap)
                            {
                                pictureBox.Image = (Bitmap)files[counter];
                            }
                        }
                    }
                    catch
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            this.Text = "Wystąpi błąd podczas synchronizacji plików";
                        });
                    }
                    counter = 1;
                }
            }
            else
                this.Text = "Brak połączenia z serwerem.";
        }

        private void PlayFile(String url)
        {
            axWindowsMediaPlayer1.URL = url;
            axWindowsMediaPlayer1.settings.autoStart = true;

            axWindowsMediaPlayer1.PlayStateChange += new AxWMPLib._WMPOCXEvents_PlayStateChangeEventHandler(Player_PlayStateChange);
        }

        private void Player_PlayStateChange(object sender, _WMPOCXEvents_PlayStateChangeEvent e)
        {
            switch (e.newState)
            {
                case 1:    // Stopped
                    timer.Start();
                    axWindowsMediaPlayer1.Visible = false;
                    break;

                case 2:    // Paused
                    axWindowsMediaPlayer1.fullScreen = false;
                    break;

                case 3:    // Playing
                    //axWindowsMediaPlayer1.fullScreen = true;
                    break;
                default:
                    break;
            }
        }

        public event EventHandler NewContent;
    }
}

