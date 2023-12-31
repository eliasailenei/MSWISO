// Thanks for using my program! Please consider to support me by giving me tips! -Elias Ailenei github.com/eliasailenei
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net;
using System.Diagnostics;

class Program
{
    static DataTable dataTable;
    static HtmlAgilityPack.HtmlDocument document;
    static string getiso;
    static string entryUrl;
    static string language;
    static string passthrough;
    static string foundUrl;
    static bool continueDlnd;
    static bool esd = false;
   static bool torrent = false;
    static string ext;
    static bool usingTorrent = false;
    static string downloc = null;
    static async Task Main(string[] args)
    {
        string winVer = null;
        string release = null;
        string language = null;
        string esdmode = null;
        string torrentdownload = null;
        string help = "placehold";
       

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg.StartsWith("--"))
            {
                string[] parts = arg.Substring(2).Split('=');
                if (parts.Length == 2)
                {
                    string variableName = parts[0];
                    string variableValue = parts[1];
                    variableValue = argsFormat(variableValue);
                    if (variableName == "WinVer") winVer = variableValue;
                    else if (variableName == "Release") release = variableValue;
                    else if (variableName == "Language") language = variableValue;
                    else if (variableName == "Help") help = variableValue;
                    else if (variableName == "ESDMode") esdmode = variableValue;
                    else if (variableName == "TorrentDownload") torrentdownload = variableValue;
                    else if (variableName == "Location") downloc = variableValue;
                }
            }
        }
        if (string.Equals(esdmode, "true", StringComparison.OrdinalIgnoreCase))
        {

            esd = true;
            
        }
        if (string.Equals(torrentdownload, "true", StringComparison.OrdinalIgnoreCase))
        {

            torrent = true;

        }
        string[] validVersions = { "Windows 7", "Windows 8", "Windows 10", "Windows 11" };
        dataTable = CreateTable(esd);

        if (winVer == null)
        {
            OutHelp();
        }
        else if (!Array.Exists(validVersions, v => v == winVer))
        {
            Console.WriteLine($"Invalid Windows version: {winVer}");
            Console.WriteLine("Please choose from the following:");
            foreach (string version in validVersions)
            {
                Console.WriteLine(version);
            }
            Console.WriteLine("Include the full Windows version e.g. --WinVer=Windows_11");
        }
        else
        {
            if (winVer != null)
            {
                MakeTXT(winVer);
            }
            if (release != null & winVer != null)
            {
                filer1(release);
            }
            if (release != null && winVer != null)
            {
                string processedRelease = argsFormat(release);
                DataRow matchingRow = FindMatchingRow("Version Type", processedRelease);

                if (matchingRow != null)
                {
                    getiso = matchingRow["URL"].ToString();
                }
                else
                {
                    Console.WriteLine($"No matching row found for Release: {processedRelease}");
                    Console.WriteLine("Please check your input and try again.");
                    return;
                }
            }
            if (language == null)
            {
                await MakeLangTXT();
            }
            if (language != null)
            {
                string langURL = await GetLangURL(language);
                if (langURL != null)
                {
                    ISOFilter(langURL);
                }

            }

        }
        Console.ReadLine();
    }

    static DataRow FindMatchingRow(string columnName, string searchValue)
    {
        foreach (DataRow row in dataTable.Rows)
        {
            string value = row[columnName].ToString();
            if (value.Equals(searchValue, StringComparison.OrdinalIgnoreCase))
            {
                return row;
            }
        }
        return null;
    }

    static string argsFormat(string input)
    {
        return input.Replace('_', ' ');
    }
    static void OutHelp()
    {
        Console.WriteLine("Welcome to MicroSoft Windows ISO generator!");
        Console.WriteLine();
        Console.WriteLine("This program uses databases from files.rg-adguard.net to provide you with genuine Windows download links. You might get a direct link from Microsoft itself or an archived version, which came from Microsoft when your file was available at a previous time.");
        Console.WriteLine();
        Console.WriteLine("This program is part of PortableISO, but can be used separately!");
        Console.WriteLine();
        Console.WriteLine("Many thanks to the creators of aria2c.exe, any issues please say on my GitHub.");
        Console.WriteLine();
        Console.WriteLine("How to use:");
        Console.WriteLine("-----------");
        Console.WriteLine("Please note that you cannot use spaces in the options; instead, use underscores (_) for spaces. Here are the available options:");
        Console.WriteLine();
        Console.WriteLine("WinVer = Your chosen windows version, use case MSWISO.exe --WinVer=Windows_7");
        Console.WriteLine(" |");
        Console.WriteLine(" |--> The following is available = Windows 7, 8 (includes 8.1), 10, 11");
        Console.WriteLine("Note: For Windows 8.1, use Windows_8. Windows 7, 8, and 10 are not recommended for regular use!");
        Console.WriteLine();
        Console.WriteLine("Release = Your chosen release from WinVer, use case MSWISO.exe --WinVer=Windows_7 --Release=7,_with_Service_Pack_1");
        Console.WriteLine(" |");
        Console.WriteLine(" |--> You can see different Windows version releases from a text file provided.");
        Console.WriteLine("Note: To generate the text file, leave Release blank e.g MSWISO.exe --WinVer=Windows_7 --Release=");
        Console.WriteLine();
        Console.WriteLine("Language = Your chosen language from WinVer, use case MSWISO.exe --WinVer=Windows_7 --Release=7,_with_Service_Pack_1 --Language=German");
        Console.WriteLine(" |");
        Console.WriteLine(" |--> You can see different Windows version languages from a text file provided.");
        Console.WriteLine("Note: To generate the text file, leave Languages blank but not Releases MSWISO.exe --WinVer=Windows_7 --Release=7,_with_Service_Pack_1 --Language=");
        Console.WriteLine();
        Console.WriteLine("ESDMode = If you only want the ESD, use case MSWISO.exe --ESDMode=True --WinVer=Windows_7 --Release=7,_with_Service_Pack_1 --Language=German");
        Console.WriteLine("Note: Anything else than a 'True' is seen as False.");
        Console.WriteLine();
        Console.WriteLine("TorrentDownload = If you are happy to use archived versions of image, use case MSWISO.exe --TorrentDownload=True --WinVer=Windows_7 --Release=7,_with_Service_Pack_1 --Language=German");
        Console.WriteLine("Note: Anything else than a 'True' is seen as False.");
        Console.WriteLine();
        Console.WriteLine("Help = Launches this message, if you also didn't use any options, help will also show...");

    }

    static DataTable CreateTable(bool esd)
    {
        
        string searchfor;
        if (esd == true)
        {
            
            searchfor = "Operating Systems - (ESD)";
        }
        else
        {
          
            searchfor = "Operating Systems";
        }
        DataTable dataTable = new DataTable();
        var newload = new HtmlWeb();
        var url = newload.Load("https://files.rg-adguard.net/category");
        var link = url.DocumentNode
            .Descendants("a")
            .FirstOrDefault(a => a.InnerText == searchfor);
        dataTable = new DataTable();
        if (link != null)
        {
            var load = new HtmlWeb();
            document = load.Load(link.GetAttributeValue("href", ""));

            dataTable.Columns.Add("Name");
            dataTable.Columns.Add("Version Type");
            dataTable.Columns.Add("URL");
            DisplayAnchorTags(document.DocumentNode, dataTable);
        }
        return dataTable;
    }

    static void MakeTXT(string winVer)
    {
        string selectedVersion = winVer;

        DataRow[] filteredRows = dataTable.Select($"Name LIKE '%{selectedVersion}%'");
      

        string outputFilePath = "output.txt";
        using (StreamWriter writer = new StreamWriter(outputFilePath))
        {
            foreach (DataRow row in filteredRows)
            {
                string versionType = row["Version Type"].ToString();
                writer.WriteLine(versionType);
            }
        }

        Console.WriteLine("Warning! New files have been made! Check the local path for output!");
    }

    static async Task<string> GetLangURL(string selectedLanguage)
    {
        string url = getiso;
        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(url);
        var htmlContent = await response.Content.ReadAsStringAsync();

        var document = new HtmlAgilityPack.HtmlDocument();
        document.LoadHtml(htmlContent);

        var anchorTags = document.DocumentNode.SelectNodes("//a");

        if (anchorTags != null)
        {
            foreach (var anchorTag in anchorTags)
            {
                entryUrl = anchorTag.GetAttributeValue("href", "");
                language = anchorTag.InnerHtml;

                language = Regex.Replace(language, "<.*?>", string.Empty);

                if (string.IsNullOrWhiteSpace(entryUrl) || string.IsNullOrWhiteSpace(language))
                    continue;

                if (!entryUrl.StartsWith("https://"))
                    continue;

                if (Regex.IsMatch(language, "[^a-zA-Z]"))
                    continue;

                if (selectedLanguage.Equals(language, StringComparison.OrdinalIgnoreCase))
                {
                    return entryUrl;
                }
            }
        }

        return null;
    }

    static async Task MakeLangTXT()
    {
        string url = getiso;
        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(url);
        var htmlContent = await response.Content.ReadAsStringAsync();

        var document = new HtmlAgilityPack.HtmlDocument();
        document.LoadHtml(htmlContent);

        var anchorTags = document.DocumentNode.SelectNodes("//a");

        if (anchorTags != null)
        {
            string outputFilePath = "languages.txt";
            using (StreamWriter writer = new StreamWriter(outputFilePath))
            {
                foreach (var anchorTag in anchorTags)
                {
                    string entryUrl = anchorTag.GetAttributeValue("href", "");
                    string language = anchorTag.InnerHtml;

                    language = Regex.Replace(language, "<.*?>", string.Empty);

                    if (string.IsNullOrWhiteSpace(entryUrl) || string.IsNullOrWhiteSpace(language))
                        continue;

                    if (!entryUrl.StartsWith("https://"))
                        continue;

                    if (Regex.IsMatch(language, "[^a-zA-Z]"))
                        continue;

                    writer.WriteLine(language);
                }
            }
        }
    }


    static void filer1(string release)
    {
        string selectedVersionType = release;

        DataRow[] filteredRows = dataTable.Select($"[Version Type] = '{selectedVersionType}'");

        if (filteredRows.Length > 0)
        {
            getiso = filteredRows[0]["URL"].ToString();
        }
        else
        {
            getiso = string.Empty;
        }
    }

    static void DisplayAnchorTags(HtmlNode node, DataTable dataTable)
    {
        var anchorTags = node.Descendants("a");

        foreach (var anchorTag in anchorTags)
        {
            string name = anchorTag.InnerText;
            string url = anchorTag.GetAttributeValue("href", "");

            if (!string.IsNullOrWhiteSpace(name))
            {
                if (IsWindowsVersionInRange(name))
                {
                    string versionType = GetVersionType(name);
                    name = RemoveVersionType(name);

                    dataTable.Rows.Add(name, versionType, url);
                }

                DisplayAnchorTags(anchorTag, dataTable);
            }
        }
    }

    static bool IsWindowsVersionInRange(string name)
    {
        string[] validVersions = { "Windows 7", "Windows 8", "Windows 8.1", "Windows 10", "Windows 11" };

        foreach (string version in validVersions)
        {
            if (name.Contains(version))
            {
                return true;
            }
        }

        return false;
    }

    static string RemoveVersionType(string name)
    {
        string updatedName = name;

        int index = name.IndexOf("Windows", StringComparison.OrdinalIgnoreCase);
        if (index != -1)
        {
            int endIndex = name.IndexOf(",", index, StringComparison.OrdinalIgnoreCase);
            if (endIndex != -1)
            {
                updatedName = name.Substring(0, endIndex).Trim();
                updatedName = Regex.Replace(updatedName, @"\([^()]*\)|\b\w+\(.*?\)\s*", "").Trim();
                updatedName = updatedName.Replace("Mobile Enterprise", "").Trim();
                return updatedName;
            }
        }
        int indexs = name.IndexOf("with", StringComparison.OrdinalIgnoreCase);
        if (indexs != -1)
        {
            updatedName = name.Substring(0, indexs).Trim();
            updatedName = updatedName.Replace("Mobile Enterprise", "").Trim();
            return updatedName;
        }

        updatedName = Regex.Replace(updatedName, @"\([^()]*\)", "").Trim();
        updatedName = updatedName.Replace("Mobile Enterprise", "").Trim();
        return updatedName;
    }

    static string GetVersionType(string name)
    {
        int index = name.IndexOf("Windows", StringComparison.OrdinalIgnoreCase);
        if (index != -1)
        {
            string versionType = name.Substring(index + 7).Trim();
            return versionType;
        }
        return string.Empty;
    }
    static List<string[]> GetUrlsAndHypertexts(string url)
    {
        List<string[]> urlsAndHypertexts = new List<string[]>();
        try
        {
            using (WebClient client = new WebClient())
            {
                string html = client.DownloadString(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                var links = doc.DocumentNode.SelectNodes("//a");

                if (links != null)
                {
                    foreach (var link in links)
                    {
                        string href = link.GetAttributeValue("href", "");
                        string text = link.InnerText.Trim();
                        if (!string.IsNullOrEmpty(href) && !string.IsNullOrEmpty(text))
                        {
                            urlsAndHypertexts.Add(new string[] { href, text });
                        }
                    }
                }
            }
        }
        catch (WebException e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }

        return urlsAndHypertexts;
    }
    static void ISOFilter(string langURL)
    {

        if (esd)
        {
            ext = ".esd";
        }
        else
        {
            ext = ".iso";
        }
        bool found = false;
        string foundUrl = "";
        string targetUrl = langURL;
        List<string[]> urlsAndHypertexts = GetUrlsAndHypertexts(targetUrl);

        if (urlsAndHypertexts.Count > 0)
        {
            string pattern = @"^\d.*?x64.*?\"+ ext +"$"; 

            List<string[]> filteredArray = new List<string[]>();

            foreach (string[] row in urlsAndHypertexts)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(row[1], pattern))
                {
                    filteredArray.Add(row);
                }
            }

            if (filteredArray.Count == 0)
            {
                string langPattern = @"(en-us|en-gb|es-es|es|fr-fr|fr|de-de|de|zh-cn|zh|ja-jp|ja|ko-kr|ko|it-it|it|pt-br|pt|pt-pt|ru-ru|ru|ar-sa|ar|tr-tr|tr|nl-nl|nl|pl-pl|pl|sv-se|sv|nb-no|nb|da-dk|da|fi-fi|fi|el-gr|el|he-il|he|hi-in|hi|th-th|th|vi-vn|vi|uk-ua|uk|cs-cz|cs|hu-hu|hu|ro-ro|ro|bg-bg|bg|ms-my|ms|id-id|id|fil-ph|fil|bn-in|bn|ur-pk|ur|pa-in|pa|ta-in|ta).*64.*[^\w]x64[^\w].*\" + ext + "$";

                foreach (string[] row in urlsAndHypertexts)
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(row[1], langPattern, RegexOptions.IgnoreCase))
                    {
                        filteredArray.Add(row);
                        break;
                    }


                    if (filteredArray.Count > 0)
                    {
                        break;
                    }
                }
            }

            if (filteredArray.Count == 0)
            {
                List<string[]> swFilteredArray = new List<string[]>();

                foreach (string[] row in urlsAndHypertexts)
                {
                    if (row[1].IndexOf("SW", StringComparison.OrdinalIgnoreCase) >= 0 && !(row[1].IndexOf("arm64", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        swFilteredArray.Add(row);
                    }
                }

                if (swFilteredArray.Count > 0)
                {
                    filteredArray = swFilteredArray;
                }
            }

            if (filteredArray.Count > 0)
            {
                foundUrl = filteredArray[0][0];
                Console.WriteLine(foundUrl);
                found = true;
            }
            else
            {
                Console.WriteLine("No URLs and Hypertexts matching the filter found.");
            }
        }
        else
        {
            Console.WriteLine("No URLs and Hypertexts found.");
        }

        if (found)
        {
            POST(foundUrl);
        }
    }




    public static void POST(string foundUrl)
    {
        Task task = MakePostRequestWithRetry(foundUrl);
        task.Wait();
        Console.WriteLine("Press Enter to exit.");

    }
    public static async Task MakePostRequestWithRetry(string foundUrl)
    {
        string url = foundUrl;
        string progressFileName = "progress.txt";

        using (HttpClient httpClient = new HttpClient())
        {
            var payloadOfficial = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("dl_official", "Test")
            });

            var payloadBt = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("dl_bt", "Test")
            });

            HttpResponseMessage response;

            response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = payloadOfficial
            }, HttpCompletionOption.ResponseHeadersRead);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    await ESDDownload(response, progressFileName);
                }
                catch (Exception ex)
                {
                    if (ex is System.UriFormatException uriException && uriException.Message.Contains("An invalid request URI was provided"))
                    {
                        File.WriteAllText("NoAva.txt", "True");
                        Console.WriteLine("MS error, file no longer hosted. Try a newer version...");
                    }
                    else
                    {
                      continueDlnd = true;
                       await ESDDownload(response, progressFileName);
                    }
                }
            }

            else
            {
                usingTorrent = true;
                response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = payloadBt
                }, HttpCompletionOption.ResponseHeadersRead);

                if (response.IsSuccessStatusCode)
                {
                   await TorrentDownload(response, progressFileName);

                    Console.WriteLine("File download completed!");
                    using (StreamWriter writer = new StreamWriter("usingtor.txt"))
                    {
                        writer.Write("True");
                    }
                    Console.WriteLine(torrent);
                    if (torrent)
                    {
                        string torrentUrl = "test.torrent";

                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = "aria2c",
                            Arguments = $"--show-console-readout=true \"{torrentUrl}\" --dir=\"{downloc}\"\\ ",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        using (Process process = new Process { StartInfo = startInfo })
                        {
                            process.Start();
                            while (!process.StandardOutput.EndOfStream)
                            {
                                string WhtOut = process.StandardOutput.ReadLine();
                                AddProgress(WhtOut);
                                

                            }
                            process.WaitForExit();
                        }

                    }
                }
            }


        }
    }
    static async Task ESDDownload(HttpResponseMessage response, string progressFileName)
    {
        if (response.IsSuccessStatusCode)
        {
            long? totalSize = response.Content.Headers.ContentLength;
            string continMode = continueDlnd ? "Append" : "Create";
            FileMode fileMode = continMode == "Append" ? FileMode.Append : FileMode.Create;

            using (Stream contentStream = await response.Content.ReadAsStreamAsync())
            using (FileStream fs = new FileStream(downloc + "test" + ext, fileMode))
            {
                byte[] buffer = new byte[8192];
                int bytesRead;
                long totalBytesRead = 0;
                int prevPercent = -1;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fs.WriteAsync(buffer, 0, bytesRead);

                    totalBytesRead += bytesRead;

                    if (totalSize.HasValue)
                    {
                        int progressPercentage = (int)(((double)totalBytesRead / totalSize.Value) * 100);

                        if (progressPercentage != prevPercent)
                        {
                            Console.WriteLine($"{progressPercentage}%");
                            prevPercent = progressPercentage;
                        }
                    }
                }
            }
        }
    }


    static async Task TorrentDownload (HttpResponseMessage response, string progressFileName)
    {
        long? totalSize = response.Content.Headers.ContentLength;
        using (Stream contentStream = await response.Content.ReadAsStreamAsync())
        using (FileStream fs = new FileStream("test" + ext, FileMode.Create))
        using (StreamWriter progressWriter = new StreamWriter(progressFileName))
        {
            byte[] buffer = new byte[8192];
            int bytesRead;
            long totalBytesRead = 0;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fs.WriteAsync(buffer, 0, bytesRead);

                totalBytesRead += bytesRead;

                if (totalSize.HasValue)
                {
                    double progressPercentage = (double)totalBytesRead / totalSize.Value;
                    progressWriter.WriteLine($"{progressPercentage:P2}");
                    Console.WriteLine($"{progressPercentage:P2}");
                }
            }
        }
    }
    
    static  void AddProgress(string common)
    {
        string pattern = @"\((\d+)%\)";
        MatchCollection matches = Regex.Matches(common, pattern);
        string newInput = "";
        foreach (Match newMatch in matches)
        {
            newInput += newMatch.Groups[1].Value + "%";
        }

        using (StreamWriter writer = new StreamWriter("progress.txt", true))
        {
            writer.WriteLine(newInput);
            Console.WriteLine(newInput);
        }
    }




}
