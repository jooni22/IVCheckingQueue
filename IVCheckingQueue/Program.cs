using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace IVCheckingQueue
{
    class Program
    {
        public static int length = 0;
        static string domainConfigFile = @"domainConfig.json";
        static DomainConfig domainConfig = new DomainConfig();
        static string sourcePath = @"source.txt";
        public static void FileExists(string file)
        {
            if (!File.Exists(file))
            {
                Console.WriteLine("File " + file + " not exists");
                Console.ReadLine();
                Environment.Exit(0);
            }
        }
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            FileExists(domainConfigFile);
            domainConfig = DomainConfig.ReadFromJson(domainConfigFile);
            if (!File.Exists(sourcePath))
            {
                GetDomains();
                Console.WriteLine("Domains loaded");
                Console.WriteLine("Press any key to continue...");
            }
            bool repeat = true;
            while (repeat)
            {
                string[] variants = { "Get queue", "Get new domains on checking" };
                switch (Menu(variants))
                {
                    case 0:
                        Dictionary<string, string> domainDic = new Dictionary<string, string>();
                        string[] source = File.ReadAllLines(sourcePath);
                        domainDic = source.Select(item => item.Split(' ')).ToDictionary(s => s[0], s => s[1]);
                        foreach (var line in domainDic)
                        {
                            if (line.Key.Length > length) length = line.Key.Length;
                        }
                        while (true)
                        {
                            GetInfo(domainDic);
                            Thread.Sleep(domainConfig.UpdateTime * 1000);
                            Console.Clear();
                        }
                    case 1:
                        GetDomains();
                        Console.WriteLine("Domains loaded");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadLine();
                        Console.Clear();
                        break;
                    default:
                        break;
                }
            }
        }
        public static void AddSpaces(string line)
        {
            for (int i = 0; i < length - line.Length; i++)
            {
                Console.Write(" ");
            }
        }
        public static void GetDomains()
        {
            List<string> domainList = new List<string>();
            string ivDomain = "https://instantview.telegram.org/contest/";
            HtmlWeb webDoc = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument driver = webDoc.Load(ivDomain);
            var domains = driver.DocumentNode.SelectNodes(".//*[@class=\"list-group-contest-item\"][.//*[@class=\"iv-deadline\"][contains(text(),\"checking\")]]");
            foreach (var element in domains)
            {
                var dom = element.SelectSingleNode(".//*[@class=\"contest-item-domain\"]/a").InnerText;
                domainList.Add(dom);
            }
            Dictionary<string, double> domainDic = new Dictionary<string, double>();
            int i = 1;
            Parallel.ForEach(domainList, new ParallelOptions() { MaxDegreeOfParallelism = 20 }, element =>
            {
                HtmlWeb webDoc1 = new HtmlWeb();
                HtmlAgilityPack.HtmlDocument driver1 = webDoc.Load(ivDomain + element);
                var date = driver1.DocumentNode.SelectSingleNode("//*[@class=\"contest-section\"][1]//*[@class=\"list-group-contest-item\"][1]//*[@class=\"contest-item-date\"]").InnerText;
                var parsedDate = DateTime.ParseExact(date, "MMM d 'at' h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).AddDays(3).Subtract(DateTime.Now).TotalHours;
                domainDic.Add(element, Math.Round(parsedDate, 2));
                Console.WriteLine(i + "." + element + " " + parsedDate.ToString("0.00"));
                i++;
            });
            domainDic = domainDic.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            using (StreamWriter file = new StreamWriter(sourcePath))
                foreach (var element in domainDic)
                {
                    file.WriteLine($"{element.Key} {element.Value}h"); 
                }
        }
        public static void GetInfo(Dictionary<string, string> dic)
        {
            string ivDomain = "https://instantview.telegram.org/contest/";
            HtmlWeb webDoc = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument driver = webDoc.Load(ivDomain);
            List<string> myDomainsList = domainConfig.Domains;
            var page = driver.DocumentNode.SelectSingleNode(".//*[@class=\"list-group-contest-rows\"]");
            int i = 0;
            foreach (var element in dic)
            {
                i++;
                var elementNode = page.SelectSingleNode($".//*[@data-domain=\"{element.Key}\"]");
                if (myDomainsList.Contains(element.Key))
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                }
                Console.Write($"{i.ToString("000")}." + element.Key);
                Console.ResetColor();
                AddSpaces(element.Key);
                string status = "";
                if (elementNode == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Removed");
                    Console.ResetColor();
                    continue;
                }
                if (elementNode.SelectSingleNode($".//*[contains(@class,\"iv-deadline\")]") != null)
                {
                    status = elementNode.SelectSingleNode($".//*[contains(@class,\"iv-deadline\")]").InnerText;
                }
                else if (elementNode.SelectSingleNode($".//*[@class=\"status-winner\"]") != null)
                {
                    status = elementNode.SelectSingleNode($".//*[@class=\"status-winner\"]").InnerText;
                }
                else
                {
                    status = "Empty";
                }
                if (status == "checking")
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                if (status == "Winner '19")
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                }
                if (status == "Empty")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                Console.Write(status.PadLeft(15).PadRight(25));
                Console.ResetColor();
                if (status != "Empty")
                {
                    string user = elementNode.SelectSingleNode($".//*[contains(@class,\"contest-item-candidate\")]/a").InnerText;
                    if (user == domainConfig.userName || user == domainConfig.userNameSecond) Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(user.PadRight(30));
                    Console.ResetColor();
                }
                Console.Write(element.Value);
                Console.WriteLine();
                if (i == domainConfig.FocusSize) break;
            }
        }
        public static int Menu(string[] variants)
        {
            bool repeatMenu = true;
            int highlight = 0;
            int key = 0;
            Console.CursorVisible = false;
            int numberOfItems = variants.Length - 1;
            while (repeatMenu)
            {
                for (int i = 0; i <= numberOfItems; i++)
                {
                    if (i == highlight)
                    {
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    Console.WriteLine(variants[i]);
                    Console.ResetColor();
                }
                var readKey = Console.ReadKey();

                switch (readKey.Key)
                {
                    case ConsoleKey.UpArrow:
                        if (highlight > 0)
                        {
                            highlight--;
                        }
                        else if (highlight == 0)
                        {
                            highlight = numberOfItems;
                            break;
                        }
                        break;
                    case ConsoleKey.DownArrow:
                        if (highlight < numberOfItems)
                        {
                            highlight++;

                        }
                        else if (highlight == numberOfItems)
                        {
                            highlight = 0;
                            break;
                        }
                        break;
                    case ConsoleKey.Enter:
                        repeatMenu = false;
                        key = highlight;
                        Console.Clear();
                        Console.CursorVisible = true;
                        break;
                    default:
                        break;
                }
                Console.SetCursorPosition(0, 0);
            }
            return key;
        }
    }
}
