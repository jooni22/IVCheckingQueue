using HtmlAgilityPack;
using Newtonsoft.Json;
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
        static string sourcePath = @"source.json";
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
            Console.Title = "IVCheckingQueue";
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            FileExists(domainConfigFile);
            domainConfig = DomainConfig.ReadFromJson(domainConfigFile);
            if (!File.Exists(sourcePath))
            {
                GetDomains();
            }
            bool repeat = true;
            while (repeat)
            {
                string[] variants = { "Get queue", "Get new domains on checking" };
                switch (Menu(variants))
                {
                    case 0:
                        List<Domain> domain = new List<Domain>();
                        string jsonString = System.IO.File.ReadAllText(sourcePath);
                        domain = JsonConvert.DeserializeObject<List<Domain>>(jsonString);
                        foreach (var line in domain)
                        {
                            if (line.Name.Length > length) length = line.Name.Length;
                        }
                        while (true)
                        {
                            GetInfo(domain);
                            Console.SetWindowPosition(0, 0);
                            Thread.Sleep(domainConfig.UpdateTime * 1000);
                            Console.Clear();
                        }
                    case 1:
                        GetDomains();
                        break;
                    default:
                        break;
                }
            }
        }
        public static void AddSpaces(string line)
        {
            for (int i = 0; i < length + 5 - line.Length; i++)
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
            var domainNodes = driver.DocumentNode.SelectNodes(".//*[@class=\"list-group-contest-item\"][.//*[@class=\"iv-deadline\"][contains(text(),\"checking\")]]");
            if (domainNodes == null)
            {
                Console.WriteLine("0 domains in checking state");
                Console.ReadLine();
                return;
            }
            foreach (var element in domainNodes)
            {
                var dom = element.SelectSingleNode(".//*[@class=\"contest-item-domain\"]/a").InnerText;
                domainList.Add(dom);
            }
            List<Domain> domains = new List<Domain>();
            int i = 1;
            object sync = new object();
            Parallel.ForEach(domainList, new ParallelOptions() { MaxDegreeOfParallelism = 20 }, element =>
            {
                HtmlWeb webDoc1 = new HtmlWeb();
                HtmlAgilityPack.HtmlDocument driver1 = webDoc.Load(ivDomain + element);
                var date = driver1.DocumentNode.SelectSingleNode("//*[@class=\"contest-section\"][1]//*[contains(@class,\"list-group-contest-item\")][1]//*[@class=\"contest-item-date\"]").InnerText;
                var parsedDate = DateTime.ParseExact(date, "MMM d 'at' h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).AddDays(3).Subtract(DateTime.Now).TotalHours;
                var issues = driver1.DocumentNode.SelectSingleNode("//*[@class=\"contest-section\"][1]//*[contains(@class,\"list-group-contest-item\")][1]//*[@class=\"contest-item-status-disputed\"]");
                Domain domain = new Domain
                {
                    Name = element,
                    Date = Math.Round(parsedDate, 2),
                };
                Console.Write(i + "." + element + " " + parsedDate.ToString("0.00"));
                if (issues != null)
                {
                    domain.Issues = issues.InnerText;
                    Console.Write(" " + issues.InnerText);
                }
                Console.WriteLine();
                lock (sync)
                {
                    domains.Add(domain);
                }
                i++;
            });
            domains = domains.OrderBy(x => x.Date).ToList();
            string json = JsonConvert.SerializeObject(domains, Formatting.Indented);
            File.WriteAllText(sourcePath, json);
            Console.WriteLine("Domains loaded");
            Console.WriteLine("Press any key to continue...");
            Console.ReadLine();
            Console.Clear();
        }
        public static void GetInfo(List<Domain> list)
        {
            list = UpdateIssues(list);
            string json = JsonConvert.SerializeObject(list, Formatting.Indented);
            File.WriteAllText(sourcePath, json);
            string ivDomain = "https://instantview.telegram.org/contest/";
            HtmlWeb webDoc = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument driver = webDoc.Load(ivDomain);
            List<string> myDomainsList = domainConfig.Domains;
            var page = driver.DocumentNode.SelectSingleNode(".//*[@class=\"list-group-contest-rows\"]");
            var total = page.SelectNodes(".//*[@class=\"list-group-contest-item\"]").Count();
            var winners = page.SelectNodes(".//*[@class=\"status-winner\"]").Count();
            int checking = 0;
            if (page.SelectNodes(".//*[@class=\"iv-deadline\"][contains(text(),\"checking\")]") != null)
            {
                checking = page.SelectNodes(".//*[@class=\"iv-deadline\"][contains(text(),\"checking\")]").Count();
            }
            var soon = page.SelectNodes(".//*[contains(@class,\"iv-deadline\")][contains(@class,\"soon\")]").Count();
            var checkWait = total - (checking + winners);
            Console.WriteLine($"Domains: {total}; Winners: {winners} ( {(winners * 100.0 / total).ToString("00.00")}% ); Checking: {checking} ( {(checking * 100.0 / total).ToString("00.00")}% ); Waiting: {checkWait} ( {(checkWait * 100.0 / total).ToString("00.00")}% ); Soon: {soon}");
            int i = 0;
            foreach (var element in list)
            {
                i++;
                var elementNode = page.SelectSingleNode($".//*[@data-domain=\"{element.Name}\"]");
                if (myDomainsList.Contains(element.Name))
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                }
                Console.Write($"{i.ToString("0").PadLeft(3)}." + element.Name);
                Console.ResetColor();
                AddSpaces(element.Name);
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
                Console.Write(status.PadRight(15));
                Console.ResetColor();
                if (status != "Empty")
                {
                    string user = elementNode.SelectSingleNode($".//*[contains(@class,\"contest-item-candidate\")]/a").InnerText;
                    if (user == domainConfig.userName || user == domainConfig.userNameSecond) Console.ForegroundColor = ConsoleColor.Green;
                    if (user.Length > 22) user = Truncate(user, 22) + "...";
                    Console.Write(user.PadRight(25));
                    Console.ResetColor();
                    Console.Write(element.Date.ToString("0.00") + "h".PadRight(8));
                }
                if (!string.IsNullOrEmpty(element.Issues)) Console.Write(element.Issues);
                Console.WriteLine();
                if (i == domainConfig.FocusSize) break;
            }
        }
        public static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
        public static List<Domain> UpdateIssues(List<Domain> list)
        {
            string ivDomain = "https://instantview.telegram.org/contest/";
            Parallel.ForEach(list, new ParallelOptions() { MaxDegreeOfParallelism = 20 }, element =>
            {
                HtmlWeb webDoc1 = new HtmlWeb();
                HtmlAgilityPack.HtmlDocument driver1 = webDoc1.Load(ivDomain + element);
                var issues = driver1.DocumentNode.SelectSingleNode("//*[@class=\"contest-section\"][1]//*[contains(@class,\"list-group-contest-item\")][1]//*[@class=\"contest-item-status-disputed\"]");
                if (issues != null) element.Issues = issues.InnerText;
            });
            return list;
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
