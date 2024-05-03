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
    static DataTable dataTable; // the local database that saves the data
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
        string language = null; // here are just the placeholders for the command line arguments
        string esdmode = null;
        string torrentdownload = null;
        string help = "placehold";
       // input might be --ESDMode=True --WinVer=Windows_10 --Release=22H2 --Language=German

        for (int i = 0; i < args.Length; i++) // this is to populate the variables from the command line arguments
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

            esd = true; // sets the ESDMode function as true
            
        }
        if (string.Equals(torrentdownload, "true", StringComparison.OrdinalIgnoreCase))
        {

            torrent = true; // this allows for torrents to be downloaded

        }
        string[] validVersions = { "Windows 7", "Windows 8", "Windows 10", "Windows 11" }; // the versions that the API supports
        dataTable = CreateTable(esd); // this creates the local database, check the comments at CreateTable for more

        if (winVer == null)
        {
            OutHelp(); // if any input is invalid, then the program will show the help dialog
        }
        else if (!Array.Exists(validVersions, v => v == winVer)) // here is an example of an error when the user inputs a wrong windows version like Windows 8.1
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
                MakeTXT(winVer); // this outputs all the releases avaliable for the selected windows version
            }
            if (release != null & winVer != null)
            {
                filer1(release);
            }
            if (release != null && winVer != null)
            {
                string processedRelease = argsFormat(release); // removes the _
                DataRow matchingRow = FindMatchingRow("Version Type", processedRelease); // checks if the release is a real thing

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
                await MakeLangTXT(); // this will create a text file with all the languages avaliable
            }
            if (language != null)
            {
                string langURL = await GetLangURL(language); // this generates a URL based on the language given
                if (langURL != null)
                {
                    ISOFilter(langURL); // this will format the url and then download it
                }

            }

        }
      
    }

    static DataRow FindMatchingRow(string columnName, string searchValue) // simple function which will will search for the input inside a row
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
    static void OutHelp() // This will display the help page when the user needs it
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
        Console.ReadLine();
    }

    static DataTable CreateTable(bool esd) // this function is first called to scrape the site and get the correct data
    {
        
        string searchfor;
        if (esd == true)
        {
            
            searchfor = "Operating Systems - (ESD)"; // the website has different links for ESD
        }
        else
        {
          
            searchfor = "Operating Systems"; // these are ISO or torrents
        }
        DataTable dataTable = new DataTable(); // new datatable created
        var newload = new HtmlWeb();
        var url = newload.Load("https://files.rg-adguard.net/category");
        var link = url.DocumentNode
            .Descendants("a")
            .FirstOrDefault(a => a.InnerText == searchfor); // searches for all a nodes 
        dataTable = new DataTable();
        if (link != null)
        {
            var load = new HtmlWeb();
            document = load.Load(link.GetAttributeValue("href", "")); // gets all the hrefs into the table

            dataTable.Columns.Add("Name");
            dataTable.Columns.Add("Version Type");
            dataTable.Columns.Add("URL");
            DisplayAnchorTags(document.DocumentNode, dataTable); // passes data on to be filtered
        }
        return dataTable;
    }

    static void MakeTXT(string winVer)
    {
        string selectedVersion = winVer;

        DataRow[] filteredRows = dataTable.Select($"Name LIKE '%{selectedVersion}%'"); // Normally, we will get all the windows versions for example windows 10 and windows 11. We only filter what we need
      

        string outputFilePath = "output.txt"; // new text file is made //writing and reading a file
        using (StreamWriter writer = new StreamWriter(outputFilePath))
        {
            foreach (DataRow row in filteredRows)
            {
                string versionType = row["Version Type"].ToString();
                writer.WriteLine(versionType); // writes all the releases for the given windows version
            }
        }

        Console.WriteLine("Warning! New files have been made! Check the local path for output!");
    }

    static async Task<string> GetLangURL(string selectedLanguage)
    {
        string url = getiso; // this is composed by the main website + the release
        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(url);
        var htmlContent = await response.Content.ReadAsStringAsync(); // gets the data from the site

        var document = new HtmlAgilityPack.HtmlDocument();
        document.LoadHtml(htmlContent);

        var anchorTags = document.DocumentNode.SelectNodes("//a"); // chooses all the a nodes, the website has made it in a way that all directories are just a hrefs

        if (anchorTags != null)
        {
            foreach (var anchorTag in anchorTags)
            {
                entryUrl = anchorTag.GetAttributeValue("href", "");
                language = anchorTag.InnerHtml;

                language = Regex.Replace(language, "<.*?>", string.Empty); // cleans any impurities in the link

                if (string.IsNullOrWhiteSpace(entryUrl) || string.IsNullOrWhiteSpace(language)) // from here
                    continue;

                if (!entryUrl.StartsWith("https://"))
                    continue;

                if (Regex.IsMatch(language, "[^a-zA-Z]"))
                    continue; // to here, the website has some garbage such as his twitter account or the websites title. Ive filtered the links more

                if (selectedLanguage.Equals(language, StringComparison.OrdinalIgnoreCase))
                {
                    return entryUrl; // give the URL with the correct language so main website + the release + language
                }
            }
        }

        return null;
    }

    static async Task MakeLangTXT() // this is the same as GetLangURL execpt that insted of only returing the language we want, we show all languages avaliable into a text file
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


    static void filer1(string release) // a filter to only give out the release we want
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
        // Retrieve all anchor tags within the provided HTML node
        var anchorTags = node.Descendants("a");

        foreach (var anchorTag in anchorTags)
        {
            string name = anchorTag.InnerText;
            string url = anchorTag.GetAttributeValue("href", "");

            // Check if the extracted name is not null or empty
            if (!string.IsNullOrWhiteSpace(name))
            {
                // Check if the name contains a valid Windows version
                if (IsWindowsVersionInRange(name))
                {
                    // If the name contains a valid version, extract the version type, remove it from the name, and add the details to the DataTable
                    string versionType = GetVersionType(name); 
                    name = RemoveVersionType(name); 

                    // Add the name, version type, and URL to the DataTable
                    dataTable.Rows.Add(name, versionType, url);
                }

                DisplayAnchorTags(anchorTag, dataTable);
            }
        }
    }

    
    static bool IsWindowsVersionInRange(string name)
    {
        // These are the accepted Windows versions avaliable
        string[] validVersions = { "Windows 7", "Windows 8", "Windows 8.1", "Windows 10", "Windows 11" };

        foreach (string version in validVersions)
        {
            // Check if the name contains the current version
            if (name.Contains(version))
            {
                // If the version is found in the name, return true
                return true;
            }
        }

        return false;
    }


    static string RemoveVersionType(string name) // this removes any unsupported windows versions like Windows 10 Mobile Enterprise
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

    static string GetVersionType(string name) // gets the version from the input given
    {
        int index = name.IndexOf("Windows", StringComparison.OrdinalIgnoreCase);
        if (index != -1)
        {
            string versionType = name.Substring(index + 7).Trim();
            return versionType;
        }
        return string.Empty;
    }
    static List<string[]> GetUrlsAndHypertexts(string url) // an a href extractor
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
    static void ISOFilter(string langURL) // this method gets the url ready to be downloaded
    {

        if (esd)
        {
            ext = ".esd"; // if ESDMode is on then we expect a ESD file
        }
        else
        {
            ext = ".iso";
        }
        bool found = false;
        string foundUrl = "";
        string targetUrl = langURL;
        List<string[]> urlsAndHypertexts = GetUrlsAndHypertexts(targetUrl); // gets all the links from the main site + release + language

        if (urlsAndHypertexts.Count > 0)
        {
            string pattern = @"^\d.*?x64.*?\"+ ext +"$"; // we ony want x64 bit versions of the release

            List<string[]> filteredArray = new List<string[]>();

            foreach (string[] row in urlsAndHypertexts)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(row[1], pattern))
                {
                    filteredArray.Add(row); // if found then add them
                }
            }

            if (filteredArray.Count == 0) // some links have en-us at the end which makes the regex not work, we use another regex in this case
            {
                string langPattern = @"(en-us|en-gb|es-es|es|fr-fr|fr|de-de|de|zh-cn|zh|ja-jp|ja|ko-kr|ko|it-it|it|pt-br|pt|pt-pt|ru-ru|ru|ar-sa|ar|tr-tr|tr|nl-nl|nl|pl-pl|pl|sv-se|sv|nb-no|nb|da-dk|da|fi-fi|fi|el-gr|el|he-il|he|hi-in|hi|th-th|th|vi-vn|vi|uk-ua|uk|cs-cz|cs|hu-hu|hu|ro-ro|ro|bg-bg|bg|ms-my|ms|id-id|id|fil-ph|fil|bn-in|bn|ur-pk|ur|pa-in|pa|ta-in|ta).*64.*[^\w]x64[^\w].*\" + ext + "$";

                foreach (string[] row in urlsAndHypertexts)
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(row[1], langPattern, RegexOptions.IgnoreCase))
                    {
                        filteredArray.Add(row); // add the urls that have the language we need
                        break;
                    }


                    if (filteredArray.Count > 0)
                    {
                        break; // we stop the for each loop just in case
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
                        swFilteredArray.Add(row); // we don't want arm64 releases as PortableISO is x64 based only
                    }
                }

                if (swFilteredArray.Count > 0)
                {
                    filteredArray = swFilteredArray;
                }
            }

            if (filteredArray.Count > 0)
            {
                foundUrl = filteredArray[0][0]; // now we give the correct url and to get ready for the install
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
            POST(foundUrl); // only start install once we have found the correct version
        }
    }




    public static void POST(string foundUrl)
    {
        Task task = MakePostRequestWithRetry(foundUrl); // this will now make a POST request to get the microsoft link
        task.Wait();
        Console.WriteLine("Press Enter to exit.");

    }
    public static async Task MakePostRequestWithRetry(string foundUrl)
    {
        string url = foundUrl; // this is our good url
        string progressFileName = "progress.txt"; // this is where we give progress so that other apps can use it

        using (HttpClient httpClient = new HttpClient())
        {
            var payloadOfficial = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("dl_official", "Test") // this is for if the file is an .iso or an .esd
            });

            var payloadBt = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("dl_bt", "Test") // this is for .torrent
            });

            HttpResponseMessage response;

            response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = payloadOfficial
            }, HttpCompletionOption.ResponseHeadersRead); // we now get the response from the POST server, this will give us a url we can use

            if (response.IsSuccessStatusCode) // status code must be 200 for this 
            {
                try
                {
                    await ESDDownload(response, progressFileName); //downloads file async
                }
                catch (Exception ex)
                {
                    if (ex is System.UriFormatException uriException && uriException.Message.Contains("An invalid request URI was provided")) // this will show if the version is no longer hosted
                    {
                        File.WriteAllText("NoAva.txt", "True");
                        Console.WriteLine("MS error, file no longer hosted. Try a newer version...");
                    }
                    else
                    {
                      continueDlnd = true; // sometimes microsoft likes to take breaks when downloading e.g its not just one file stream as they have many CDN's
                       await ESDDownload(response, progressFileName);
                    }
                }
            }

            else
            {
                usingTorrent = true; // we assume that we are working with a torrent
                response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = payloadBt
                }, HttpCompletionOption.ResponseHeadersRead);

                if (response.IsSuccessStatusCode)
                {
                   await TorrentDownload(response, progressFileName); //download the torrent file

                    Console.WriteLine("File download completed!");
                    using (StreamWriter writer = new StreamWriter("usingtor.txt")) // torrents are only avaliable for MSWISO and not for PortableISO
                    {
                        writer.Write("True");
                    }
                    Console.WriteLine(torrent);
                    if (torrent)
                    {
                        string torrentUrl = "test.torrent";

                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = "aria2c", // I use aria2c as its FOSS
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
                                AddProgress(WhtOut); // adds the progress from aria2c to the text file if progress is needed
                                

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
        if (response.IsSuccessStatusCode) // only works when code is 200
        {
            long? totalSize = response.Content.Headers.ContentLength;
            string continMode = continueDlnd ? "Append" : "Create"; // if not continueDlnd then its create otherwise append onto the file
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

                    totalBytesRead += bytesRead; // this read the amount of bytes transfered

                    if (totalSize.HasValue)
                    {
                        int progressPercentage = (int)(((double)totalBytesRead / totalSize.Value) * 100); // we do some calculations to get a good percentage

                        if (progressPercentage != prevPercent)
                        {
                            Console.WriteLine($"{progressPercentage}%"); // display progress
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
        using (FileStream fs = new FileStream("test" + ext, FileMode.Create)) // we are only downloading a small file so no need for appending
        using (StreamWriter progressWriter = new StreamWriter(progressFileName))
        {
            byte[] buffer = new byte[8192];
            int bytesRead;
            long totalBytesRead = 0;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fs.WriteAsync(buffer, 0, bytesRead);

                totalBytesRead += bytesRead; // this read the amount of bytes transfered

                if (totalSize.HasValue)
                {
                    double progressPercentage = (double)totalBytesRead / totalSize.Value;// we do some calculations to get a good percentage
                    progressWriter.WriteLine($"{progressPercentage:P2}");
                    Console.WriteLine($"{progressPercentage:P2}"); // display progress
                }
            }
        }
    }
    
    static  void AddProgress(string common) // adds progress to the text file
    {
        string pattern = @"\((\d+)%\)";
        MatchCollection matches = Regex.Matches(common, pattern);
        string newInput = "";
        foreach (Match newMatch in matches)
        {
            newInput += newMatch.Groups[1].Value + "%"; // makes sure that we only put the percentage and not other things like the amount of peers or download speed ect.
        }

        using (StreamWriter writer = new StreamWriter("progress.txt", true))
        {
            writer.WriteLine(newInput); // write to file
            Console.WriteLine(newInput);
        }
    }




}
