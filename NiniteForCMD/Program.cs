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

class NiniteForCMDs
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
       
        DataTable dataTable = new DataTable(); // new table created
        userInput = "SHOW TITLE"; // This is put here so that the user sees the programs avaliable first time
        dataTable.Columns.Add("VALUE", typeof(string));
        dataTable.Columns.Add("SRC", typeof(string)); 
        dataTable.Columns.Add("TITLE", typeof(string));
        bool exit = false;
        Console.WriteLine("Scraping Ninite... Please wait...");
        await GetData(dataTable); // scrapes Ninite for the latest data
        Console.Clear();
        Show(dataTable, userInput); // shows the programs avaliable
        Console.WriteLine("");
        Console.WriteLine("Welcome to NiniteForCMD! Type HELP to get started.");
        Console.WriteLine("");
        value = new string[128];
        selectedValue = new string[128];
        value[0] = "placehold";
       
        foreach (DataRow row in dataTable.Rows)
        {
            value[pointss] = row["VALUE"].ToString(); // populate the array with the values of the programs e.g Google Chrome is chrome

            pointss++;

        }
        while (!exit)
        {
            if (args.Length > 1)
            {
                argsMode(dataTable, exit);// this program can use CLI arugments and treats every input as one line entered
                exit = true; // because we are using CLI exit immediately
            } else
            {
                userLower = GetUserInput(); // get user input
                userInput = userLower.ToUpper(); // makes it all caps to make it easier to search
                Selecter(dataTable, userInput, exit, userLower); // this is where each command is sent to its corresponding method
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
            arr[0] = string.Join(" ", arr[0].Split(' ').Skip(1)); // removes the whitespace
        }
        for (int i = 0; i < arr.Length; i++)
        {
            userInput = arr[i];
            Console.WriteLine($"arr[{i + 1}] = {arr[i].Trim()}");
            Selecter(dataTable, userInput.ToUpper(), exit, userLower); // this wil now execute the CLI arugments one by one
        }
    }
    static void Selecter(DataTable dataTable, string userInput, bool exit, string userlower)
    {

        switch (userInput)
        {
            case "HELP":
                Help(); //shows help
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
        HashSet<string> uniqueTITLEs = new HashSet<string>(); // this is so we don't have duplicates
        using (HttpClient client = new HttpClient())
        {

            try // we use try as sometimes network is down
            {
                string url = "https://ninite.com/"; // calling api
                string htmlContent = await client.GetStringAsync(url);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(htmlContent); // gets the data from the site
                HtmlNodeCollection liNodes = doc.DocumentNode.SelectNodes("//li"); // we only care for li nodes
                if (liNodes != null)
                {
                    foreach (HtmlNode liNode in liNodes)
                    {
                        HtmlNode labelNode = liNode.SelectSingleNode(".//label"); // select one of the programs
                        if (labelNode != null)
                        {
                            string VALUE = liNode.SelectSingleNode(".//input")?.GetAttributeValue("VALUE", ""); // we extract the value
                            string SRC = labelNode.SelectSingleNode(".//img")?.GetAttributeValue("SRC", ""); // we extract the image locations
                            string TITLE = labelNode.GetAttributeValue("TITLE", ""); // we extract the actual name
                            string labelText = labelNode.InnerText.Trim();


                            if (!uniqueTITLEs.Contains(TITLE)) // we make sure that data only occurs once
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
                foreach (string com in args) // this finds the location where the location string is located at as text is normally all caps 
                {
                    if (!com.Contains("LOCATION"))
                    {
                        findLoc++;
                    } else
                    {
                        break;
                    }
                    
                }
                string location = args[findLoc + 1]; // find the true location
                int problem = location.IndexOf(',');
                if (problem != -1)
                {
                    downLocation = location.Substring(0, problem); 
                } else
                {
                    downLocation = location; // we assume that this is the actual location
                }
                
                Console.WriteLine($"Download location: {downLocation}");
            } else
            {
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
            }
        }catch (Exception e)
        {
            //Console.WriteLine (e.ToString()); // debug purposes only
            //if (userLower == null)
            //{
            //    Console.WriteLine("NULL!");
            //}
        }
        
    }

    static private void Show(DataTable dataTable, string userin)
    { // we might have text like SHOW VALUE
        string[] split = userin.Split(' ');
        string toPass = " "; 
        if (split.Length > 2) // makes sure that its valid input
        {
            Console.WriteLine("ERROR: Invalid SHOW input, type HELP for more");
        }
        else
        {
            foreach (string word in split)
            {
                toPass = word; // gets the last word after SHOW in this case VALUE
            }
        }
        if (toPass == "SELECTED")
        {
            Console.WriteLine("The following is selected:");
            foreach (string items in selected) // selected only needs to show what the user has selected
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
                int maxItems = Math.Min(itemsPerRow * itemsPerColumn, itemCount); //simple math
                int maxTITLELength = dataTable.AsEnumerable().Select(row => row.Field<string>(toPass)).Max(TITLE => TITLE.Length); 

                for (int i = 0; i < maxItems; i++)
                {
                    DataRow dataRow = dataTable.Rows[i];
                    string TITLE = dataRow[toPass].ToString(); // gets the data from selected row
                    Console.Write(TITLE.PadRight(maxTITLELength + 2));

                    if ((i + 1) % itemsPerRow == 0) // this spaces out the text
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
        Console.Clear();
        return userIn; // just returns the user input
    }

    static void Help()
    { // when user is stuck
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
                toPass = word; // gets the command word E.g ALL or selected
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
                    exportData.AppendLine(row[toPass].ToString()); // the user can say EXPORT TITLE, in this case we only need data from title row
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
        string pattern = @"'([^']+)'"; // use input is like 'Chrome' + 'VLC' so we remove the plus and ' //regex
        MatchCollection matches = Regex.Matches(userin, pattern);

        if (userin == "SELECT ALL") 
        {
            selected = new string[dataTable.Rows.Count];
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                selectedValue[i] = dataTable.Rows[i]["VALUE"].ToString(); // put all the values in the array so its ready to be downloaded
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
                    selected[i] = matches[i].Groups[1].Value.ToUpper(); // put all the data that is found so in this case Chrome and VLC
                }
            }




            foreach (string substring in selected) // this part of the code checks if the user input is actually part of the data set it checks if userinput matches to what there is in Title, if there is a match then get the location from title and get the result from value
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
    static String URLCreate()
    {
        string regex = @"-+$";
        StringBuilder uri = new StringBuilder();
        try
        {

            foreach (string word in selectedValue) // adds a dash at every value in the array so chrome-vlc-
            {
                if (word != null)
                {
                    uri.Append(word);
                    uri.Append("-");
                } else
                {
                    break;
                }
                
            }

            string newOut = System.Text.RegularExpressions.Regex.Replace(uri.ToString(), regex, ""); // removes any - at the end if there is any
            string acc = "https://ninite.com/" + newOut + "/"; //creates a new valid url
            return acc;

        }
        catch (Exception ex)
        {
            return "INVALID INPUT! Type HELP to get started!";
        }
    }
    static async void Download(bool Install)
    {
        download = URLCreate() + "ninite.exe"; // creates the ur; //writing and reading a file
        using (WebClient client = new WebClient())
        {
            client.DownloadFile(download, downLocation + "setup.exe");// downloads the file
        }
        if (Install)
        {
            Process.Start(downLocation + "setup.exe"); // open the file after it has been downloaded
        }
    }


}





