using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Koncowka
{
    public static class Sync
    {
        static public async Task<string[]> SynchronizeFiles(string url, string clientName)
        {
            try
            {
                string response = GetResponse(url, clientName);
                DataCampaign item = JsonConvert.DeserializeObject<DataCampaign>(response);

                string[] fileEntries = Directory.GetFiles(clientName);
                List<string> filesToDownload = new List<string>();
                List<string> filesOnServer = new List<string>();

                DirectoryInfo di = new DirectoryInfo(clientName);
                if (!di.Exists)
                    Directory.CreateDirectory(clientName);

                if (item.data.found == true)
                {
                    foreach (Items it in item.data.playlist.items)
                    {
                        filesOnServer.Add(it.url);
                    }
                }
                else
                {
                    foreach (string fileOnDisk in fileEntries)
                    {
                        File.Delete(fileOnDisk);
                    }
                }


                //pliki do pobrania
                if (fileEntries.Length > 0 && filesOnServer.Count > 0)
                {
                    foreach (string fileOnServer in filesOnServer)
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
                        foreach (string fileOnServer in filesOnServer)
                        {
                            if (fileOnDisk.Contains(Path.GetFileName(fileOnServer)))
                                break;
                            if (fileOnServer == filesOnServer.Last())
                                File.Delete(fileOnDisk);
                        }
                    }
                }
                else
                {
                    if (filesOnServer != null)
                        filesToDownload = filesOnServer.ToList<string>();
                }

                if (filesToDownload.Count > 0)
                {
                    await Downloader(filesToDownload, clientName);
                }
                if (item.data.found == true)
                {
                    Serialize(item, clientName);

                    return new string[] { item.data.campaign.start, item.data.campaign.end };
                }

                return new string[] { "01-01-2017", "01-01-2018" };
            }
            catch (JsonReaderException)
            {
                MessageBox.Show("Wystąpił bład podczas pobierania danych.");
                return new string[] { "01-01-2017", "01-01-2018" };
            }
            catch (IOException)
            {
                MessageBox.Show("Wystąpił problem podczas synchoronizacji danych.");
                return new string[] { "01-01-2017", "01-01-2018" };
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
                //MessageBox.Show("Błąd połączenia z serwerem");
                return "";
            }
            catch (UriFormatException)
            {
                //MessageBox.Show("Zła nazwa klienta");
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
                //MessageBox.Show("Błąd podczas pobierania danych");
            }
        }

        private static void Serialize(Object obj, string clientName)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(string.Format("{0}\\Campaign.bin", clientName),
                                     FileMode.Create,
                                     FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, obj);
            stream.Close();
        }
    }
}
