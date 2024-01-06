// Program made by Elias Ailenei!

using HtmlAgilityPack;
using System.ComponentModel.Design;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Net.Http;
using Microsoft.SqlServer.Server;
using System.IO;

class NiniteForCMD
{
    static string[] selected;
    static string[] value;
    static bool isArgs;
    static string[] selectedValue;
    static string downLocation, userInput, userLower, download, noFilter;
    static int pointss = 1;
    static string [] args = Environment.GetCommandLineArgs();
    static async Task Main()
    {
       
        DataTable dataTable = new DataTable();
        userInput = "SHOW TITLE";
        dataTable.Columns.Add("VALUE", typeof(string));
        dataTable.Columns.Add("SRC", typeof(string));
        dataTable.Columns.Add("TITLE", typeof(string));
        bool exit = false;
        Console.WriteLine("Scraping Ninite... Please wait...");
        await GetData(dataTable);
        Console.Clear();
        Show(dataTable, userInput);
        Console.WriteLine("");
        Console.WriteLine("llWelcome to NiniteForCMD! Type HELP to get started.");
        Console.WriteLine("");
        value = new string[128];
        selectedValue = new string[128];
        value[0] = "placehold";
       
        foreach (DataRow row in dataTable.Rows)
        {
            value[pointss] = row["VALUE"].ToString();

            pointss++;

        }
        while (!exit)
        {
            if (args.Length > 1)
            {
                argsMode(dataTable, exit);
                exit = true;
            } else
            {
                userLower = GetUserInput();
                userInput = userLower.ToUpper();
                Selecter(dataTable, userInput, exit, userLower);
            }
            
        }
    }
    static void argsMode(DataTable dataTable, bool exit)
    {
        Console.WriteLine("Using args!");
        isArgs = true;
        string input = string.Join(" ", args);
        string[] arr = input.Split(',');
        if (arr.Length > 0)
        {
            arr[0] = string.Join(" ", arr[0].Split(' ').Skip(1));
        }
        for (int i = 0; i < arr.Length; i++)
        {
            userInput = arr[i];
            Console.WriteLine($"arr[{i + 1}] = {arr[i].Trim()}");
            Selecter(dataTable, userInput.ToUpper(), exit, userLower);
        }
    }
    static void Selecter(DataTable dataTable, string userInput, bool exit, string userlower)
    {

        switch (userInput)
        {
            case "HELP":
                Help();
                break;
            default:
                if (userInput.StartsWith("SHOW"))
                {
                    Show(dataTable, userInput);
                }
                else if (userInput.StartsWith("EXPORT") | (userInput.StartsWith(" EXPORT")))
                {
                    Export(dataTable, userInput);
                }
                else if (userInput.StartsWith("SELECT"))
                {
                    Select(dataTable, userInput, exit);
                }
                else if (userInput.StartsWith("LOCATION"))
                {
                    noFilter = " ";
                    DownloadLoc();
                }
                else
                {

                    Console.WriteLine("ERROR: " + userInput + " isnt part of this program. Please type in HELP to get started.");
                }
                break;
            case "EXIT":
                exit = true;
                Environment.Exit(0);
                break;
            case "CLEAR":
                Console.Clear();
                break;
            case "URL":
                Console.WriteLine(URLCreate());
                break;
            case "DOWNLOAD":
                bool Install = false;
                Download(Install);
                break;
            case "INSTALL":
                bool Instalsl = true;
                Download(Instalsl);
                break;
            case "DEBUG":
                Console.ReadLine();
                break;




        }
    }
    static async Task GetData(DataTable dataTable)
    {
        HashSet<string> uniqueTITLEs = new HashSet<string>();
        using (HttpClient client = new HttpClient())
        {

            try
            {
                string url = "https://ninite.com/";
                string htmlContent = await client.GetStringAsync(url);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(htmlContent);
                HtmlNodeCollection liNodes = doc.DocumentNode.SelectNodes("//li");
                if (liNodes != null)
                {
                    foreach (HtmlNode liNode in liNodes)
                    {
                        HtmlNode labelNode = liNode.SelectSingleNode(".//label");
                        if (labelNode != null)
                        {
                            string VALUE = liNode.SelectSingleNode(".//input")?.GetAttributeValue("VALUE", "");
                            string SRC = labelNode.SelectSingleNode(".//img")?.GetAttributeValue("SRC", "");
                            string TITLE = labelNode.GetAttributeValue("TITLE", "");
                            string labelText = labelNode.InnerText.Trim();


                            if (!uniqueTITLEs.Contains(TITLE))
                            {
                                dataTable.Rows.Add(VALUE, SRC, labelText);
                                uniqueTITLEs.Add(TITLE);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No li nodes found in the HTML.");
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Error fetching the webpage: {e.Message}");
            }


        }
    }
   static void DownloadLoc()
    {
        try {
            int findLoc = 0;
            if (isArgs)
            {
                foreach (string com in args)
                {
                    if (!com.Contains("LOCATION"))
                    {
                        findLoc++;
                    } else
                    {
                        break;
                    }
                    
                }
                string location = args[findLoc + 1];
                downLocation = location;
                Console.WriteLine($"Download location: {location}");
            }
            string[] split = userLower.Split(' ');
            if (split.Length >= 2)
            {
                string location = split[1];
                downLocation = location;
                Console.WriteLine($"Download location: {location}");
            }
            else
            {
                Console.WriteLine("Invalid input for LOCATION. Please provide a valid location.");
            }
        }catch (Exception e)
        {
            //Console.WriteLine (e.ToString());
            //if (userLower == null)
            //{
            //    Console.WriteLine("NULL!");
            //}
        }
        
    }

    static private void Show(DataTable dataTable, string userin)
    {
        string[] split = userin.Split(' ');
        string toPass = " ";
        if (split.Length > 2)
        {
            Console.WriteLine("ERROR: Invalid SHOW input, type HELP for more");
        }
        else
        {
            foreach (string word in split)
            {
                toPass = word;
            }
        }
        if (toPass == "SELECTED")
        {
            Console.WriteLine("The following is selected:");
            foreach (string items in selected)
            {
                Console.WriteLine(items + " ");
            }
        }
        else
        {
            int itemsPerRow = 4;
            int itemsPerColumn = 1000;
            try
            {
                int itemCount = dataTable.Rows.Count;
                int maxItems = Math.Min(itemsPerRow * itemsPerColumn, itemCount);
                int maxTITLELength = dataTable.AsEnumerable().Select(row => row.Field<string>(toPass)).Max(TITLE => TITLE.Length);

                for (int i = 0; i < maxItems; i++)
                {
                    DataRow dataRow = dataTable.Rows[i];
                    string TITLE = dataRow[toPass].ToString();
                    Console.Write(TITLE.PadRight(maxTITLELength + 2));

                    if ((i + 1) % itemsPerRow == 0)
                    {
                        Console.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("INVALD INPUT! Type HELP for more!");
            }

        }




    }

    static string GetUserInput()
    {
        Console.Write("INPUT>> ");
        string userIn = Console.ReadLine();
        return userIn;
    }

    static void Help()
    {
        string helpText = @"
CMD Help:

For command-line arguments, use the "","" to declare a space so for example:
Normally, you would do:

SELECT 'Chrome' + 'Java (AdoptOpenJDK) x64 11' + '.NET Desktop Runtime 6'
INSTALL 

But, for command-line arguments, you would do:
NiniteForCMD.exe SELECT 'Chrome' + 'Java (AdoptOpenJDK) x64 11' + '.NET Desktop Runtime 6' ,INSTALL 
                                                                                           ^
                                                                                           |
                                                                                           |
                                  Please don't leave a gap as only "" "" is going to be recognised!

Argument Help:

EXPORT - Prints out a TXT file from the database e.g., EXPORT ALL . Accepted args--> ALL, VALUE, SCR, TITLE, SELECTED

SELECT - Select one or many program e.g., SELECT 'Java (AdoptOpenJDK) x64 11' . YOU MUST USE ''! Accepted args --> ALL, (TITLE/VALUE)

INSTALL - Install the selected programs e.g., INSTALL. YOU MUST USE ''! Note: you must select what your want to download!

DOWNLOAD - Download the selected programs e.g., DOWNLOAD. YOU MUST USE ''! Accepted args --> Note: you must select what your want to download!

LOCATION - Specify where you want to save setup.exe e.g., LOCATION C:\Users\Mike\Desktop\ . Accepted args --> Any real path, you may use lower cases.

SHOW - Show the database e.g., SHOW VALUE . Accepted args --> VALUE, SCR, TITLE, SELECTED

CLEAR - Clears the console.

HELP - Shows this dialogue.

EXIT - Ends session.
";


        Console.WriteLine(helpText);

    }

    static void Export(DataTable dataTable, string userin)
    {
        StringBuilder exportData = new StringBuilder();
        string[] split = userin.Split(' ');
        string toPass = " ";
        if (split.Length > 2)
        {
            Console.WriteLine("ERROR: Invalid EXPORT input, type HELP for more");
        }
        else
        {
            foreach (string word in split)
            {
                toPass = word;
            }
        }
        try
        {
            if (toPass == "ALL")
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    exportData.Append(row["VALUE"].ToString());
                    exportData.Append(" | ");
                    exportData.Append(row["SRC"].ToString());
                    exportData.Append(" | ");
                    exportData.AppendLine(row["TITLE"].ToString());

                }
                string toExport = downLocation + "EXPORT.TXT";
                Console.WriteLine(toExport);
                File.WriteAllText(toExport, exportData.ToString());
            }
            else if (toPass == "SELECTED")
            {
                foreach (string words in selected)
                {
                    exportData.AppendLine(words.ToString());
                }
            }
            else
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    exportData.AppendLine(row[toPass].ToString());
                }
            }
            File.WriteAllText("EXPORT.TXT", exportData.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine("INVALD INPUT! Type HELP for more!");
        }
    }

    static void Select(DataTable dataTable, string userin, bool exit)
    {
        int point = 0;
        bool found = false;
        string pattern = @"'([^']+)'";
        MatchCollection matches = Regex.Matches(userin, pattern);

        if (userin == "SELECT ALL")
        {
            selected = new string[dataTable.Rows.Count];
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                selectedValue[i] = dataTable.Rows[i]["VALUE"].ToString();
            }
        }
        else
        {
            if (matches.Count == 0)
            {
                Console.WriteLine("INVALID INPUT! Program will now exit. Re-open and type in HELP.");
                Console.ReadLine();
                Environment.Exit(0);
            }
            else
            {
                selected = new string[matches.Count];
                for (int i = 0; i < matches.Count; i++)
                {
                    selected[i] = matches[i].Groups[1].Value.ToUpper();
                }
            }




            foreach (string substring in selected)
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    DataRow row = dataTable.Rows[i];
                    string title = row["TITLE"].ToString().ToUpper();
                    if (title.Contains(substring))
                    {
                        selectedValue[point] = value[i + 1];
                        point++;
                        found = true;
                    }
                }

                if (!found)
                {
                    Console.WriteLine($"No matching item found for '{substring}'");
                }
            }
        }
    }

    static async Task DownloadFile(string fileUrl, string savePath)
    {
        await Task.Run(() =>
        {
            Console.WriteLine("Download started");
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(fileUrl, savePath);
            }
        });
        
    }


    static String URLCreate()
    {
        string regex = @"-+$";
        StringBuilder uri = new StringBuilder();
        try
        {

            foreach (string word in selectedValue)
            {
                if (word != null)
                {
                    uri.Append(word);
                    uri.Append("-");
                }
                
            }

            string newOut = System.Text.RegularExpressions.Regex.Replace(uri.ToString(), regex, "");
            string acc = "https://ninite.com/" + newOut + "/";
            return acc;

        }
        catch (Exception ex)
        {
            return "INVALID INPUT! Type HELP to get started!";
        }
    }
    static async void Download(bool Install)
    {
        download = URLCreate() + "ninite.exe";
        await DownloadFile(download,downLocation +  "setup.exe");
        if (Install)
        {
            Process.Start(downLocation + "setup.exe");
        }
    }


}





