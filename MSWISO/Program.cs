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

    static async Task Main(string[] args)
    {
        string winVer = null;
        string release = null;
        string language = null;
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
                }
            }
        }

        string[] validVersions = { "Windows 7", "Windows 8", "Windows 10", "Windows 11" };
        dataTable = CreateTable();

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
                    Console.WriteLine("Language URL found:");
                    Console.WriteLine(langURL);
                    ISOFilter(langURL);
                }

            }

        }

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
        Console.WriteLine();
        Console.WriteLine("Help = Launches this message, if you also didn't use any options, help will also show...");

    }

    static DataTable CreateTable()
    {
        DataTable dataTable = new DataTable();
        var newload = new HtmlWeb();
        var url = newload.Load("https://files.rg-adguard.net/category");
        var link = url.DocumentNode
            .Descendants("a")
            .FirstOrDefault(a => a.InnerText == "Operating Systems");
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
        bool found = false;
        string foundUrl = "";
        string targetUrl = langURL;
        List<string[]> urlsAndHypertexts = GetUrlsAndHypertexts(targetUrl);

        if (urlsAndHypertexts.Count > 0)
        {
            string pattern = @"^\d.*\.iso$";
            List<string[]> filteredArray = new List<string[]>();

            foreach (string[] row in urlsAndHypertexts)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(row[1], pattern) && row[1].Contains("x64"))
                {
                    filteredArray.Add(row);
                }
            }

            if (filteredArray.Count == 0)
            {
                string langPattern = @"(en-us|en-gb|es-es|es|fr-fr|fr|de-de|de|zh-cn|zh|ja-jp|ja|ko-kr|ko|it-it|it|pt-br|pt|pt-pt|ru-ru|ru|ar-sa|ar|tr-tr|tr|nl-nl|nl|pl-pl|pl|sv-se|sv|nb-no|nb|da-dk|da|fi-fi|fi|el-gr|el|he-il|he|hi-in|hi|th-th|th|vi-vn|vi|uk-ua|uk|cs-cz|cs|hu-hu|hu|ro-ro|ro|bg-bg|bg|ms-my|ms|id-id|id|fil-ph|fil|bn-in|bn|ur-pk|ur|pa-in|pa|ta-in|ta).*64.*\.iso$";

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
        Task task = MakePostRequest(foundUrl);
        task.Wait();
        Console.WriteLine("Press Enter to exit.");

    }
    public static async Task MakePostRequest(string foundUrl)
    {
        string url = foundUrl;
        string progressFileName = "progress.txt";
        bool usingtorrent = false;

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
                long? totalSize = response.Content.Headers.ContentLength;

                using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                using (FileStream fs = new FileStream("test.iso", FileMode.Create))
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

                Console.WriteLine("File download completed!");
            }
            else
            {
                usingtorrent = true;
                response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = payloadBt
                }, HttpCompletionOption.ResponseHeadersRead);

                if (response.IsSuccessStatusCode)
                {
                    long? totalSize = response.Content.Headers.ContentLength;

                    using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                    using (FileStream fs = new FileStream("test.torrent", FileMode.Create))
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

                    Console.WriteLine("File download completed!");
                }
                else
                {

                }
            }
        }
        if (usingtorrent)
        {
            string torrentUrl = "test.torrent";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "aria2c",
                Arguments = $"--show-console-readout=true \"{torrentUrl}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };



            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();

                using (StreamWriter progressWriter = new StreamWriter(progressFileName))
                {
                    while (!process.StandardOutput.EndOfStream)
                    {
                        string line = process.StandardOutput.ReadLine();
                        Console.WriteLine(line);
                        progressWriter.WriteLine(line);
                    }
                }

                process.WaitForExit();
            }
        }


    }



}
