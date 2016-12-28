using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Koncowka
{
    public static class Sync
    {
        static public async Task<int> SynchronizeFiles(string url, string clientName)
        {
            try
            {
                Content picturesOnServer = new Content(GetResponse(url, clientName));

                DirectoryInfo di = new DirectoryInfo(clientName);
                if (!di.Exists)
                    Directory.CreateDirectory(clientName);

                string[] fileEntries = Directory.GetFiles(clientName);
                List<string> filesToDownload = new List<string>();
                //pliki do pobrania
                if (fileEntries.Length > 0 && picturesOnServer.ArrayOfSrcs.Length > 0)
                {
                    foreach (string fileOnServer in picturesOnServer.ArrayOfSrcs)
                    {
                        foreach (string fileOnDisk in fileEntries)
                        {
                            if (fileOnServer.Contains(Path.GetFileName(fileOnDisk)))
                                break;
                            if (fileOnDisk == fileEntries.Last())
                                filesToDownload.Add(fileOnServer);
                        }
                    }
                    //usuwanie zbednych plikow
                    foreach (string fileOnDisk in fileEntries)
                    {
                        foreach (string fileOnServer in picturesOnServer.ArrayOfSrcs)
                        {
                            if (fileOnDisk.Contains(Path.GetFileName(fileOnServer)))
                                break;
                            if (fileOnServer == picturesOnServer.ArrayOfSrcs.Last())
                                File.Delete(fileOnDisk);
                        }
                    }
                }
                else
                {
                    filesToDownload = picturesOnServer.ArrayOfSrcs.ToList<string>();
                }

                if (filesToDownload.Count > 0)
                {
                    await Downloader(filesToDownload, clientName);
                }
                return picturesOnServer.ArrayOfSrcs.Length;
            }
            catch (JsonReaderException)
            {
                MessageBox.Show("Wystąpił bład podczas pobierania danych.");
                return 0;
            }
            catch (IOException)
            {
                MessageBox.Show("Wystąpił problem podczas synchoronizacji danych.");
                return 0;
            }
        }

        private static string GetResponse(string url, string userName)
        {
            try
            {
                WebRequest request = WebRequest.Create(url + userName);
                request.Method = "POST";
                WebResponse response = request.GetResponse();
                Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();
                response.Close();
                responseFromServer = responseFromServer.Replace('\"', '\'');

                return responseFromServer;
            }
            catch (WebException)
            {
                MessageBox.Show("Błąd połączenia z serwerem");
                return "";
            }
            catch (UriFormatException)
            {
                MessageBox.Show("Zła nazwa klienta");
                return "";
            }
        }

        private static async Task Downloader(List<string> filesToDownload, string userName)
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    foreach (var file in filesToDownload)
                    {
                        await wc.DownloadFileTaskAsync(new Uri(file.ToString()),
                                       string.Format("{0}\\{1}", userName, Path.GetFileName(file.ToString())));
                    }
                }
            }
            catch (WebException)
            {
                MessageBox.Show("Błąd podczas pobierania danych");
            }
        }
    }
}
