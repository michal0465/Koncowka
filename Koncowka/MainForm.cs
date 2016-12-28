using Newtonsoft.Json.Linq;
using Quobject.SocketIoClientDotNet.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Koncowka
{
    public partial class MainForm : Form
    {
        private string clientName;
        private List<Bitmap> images = new List<Bitmap>();
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

            ImageList(clientName);

            if (images.Count > 0)
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
                ImageList(clientName);

                if (images.Count > 0)
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

        private void ImageList(string dirName)
        {
            var fileEntries = Directory.EnumerateFiles(dirName, "*.*")
            .Where(s => s.EndsWith(".png") || s.EndsWith(".jpg") || s.EndsWith(".jpeg") || s.EndsWith(".bmp"));

            pictureBox.Image = null;
            images.Clear();

            foreach (var file in fileEntries)
            {
                try
                {
                    using (var fs = new System.IO.FileStream(file, System.IO.FileMode.Open))
                    {
                        var bmp = new Bitmap(fs);
                        images.Add(ReSize((Bitmap)bmp.Clone()));
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
            pictureBox.Image = images[0];
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
            if (images.Count > 0)
            {
                if (counter >= images.Count)
                    counter = 0;
                pictureBox.Image = images[counter];
                counter++;
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
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

                ImageList(clientName);
                if (images.Count > 0)
                {
                    try
                    {
                        pictureBox.Image = images[0];
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

        public event EventHandler NewContent;
    }
}

