using HtmlAgilityPack;
using System;
using System.Collections.Generic;
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
            FileExists(domainConfigFile);
            domainConfig = DomainConfig.ReadFromJson(domainConfigFile);
            string sourcePath = @"source.txt";
            FileExists(sourcePath);
            string[] source = File.ReadAllLines(sourcePath);
            List<string> domainList = new List<string>();
            foreach (var line in source)
            {
                string a = line.Split(' ').First();
                if (a.Length > length) length = a.Length;
                domainList.Add(a);
            }
            while (true)
            {
                GetInfo(domainList);
                Thread.Sleep(domainConfig.UpdateTime * 1000);
                Console.Clear();
            }
        }
        public static void AddSpaces(string line)
        {
            for (int i = 0; i < length - line.Length; i++)
            {
                Console.Write(" ");
            }
        }
        public static void GetInfo(List<string> list)
        {
            string ivDomain = "https://instantview.telegram.org/contest/";
            HtmlWeb webDoc = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument driver = webDoc.Load(ivDomain);
            List<string> myDomainsList = domainConfig.Domains;
            var page = driver.DocumentNode.SelectSingleNode(".//*[@class=\"list-group-contest-rows\"]");
            foreach (var element in list)
            {
                var elementNode = page.SelectSingleNode($".//*[@data-domain=\"{element}\"]");
                if (myDomainsList.Contains(element))
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                }
                Console.Write(element);
                Console.ResetColor();
                AddSpaces(element);
                Console.Write(" - ");
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
                Console.Write(status);
                Console.ResetColor();
                Console.Write(" - ");
                if (status != "Empty")
                {
                    string user = elementNode.SelectSingleNode($".//*[contains(@class,\"contest-item-candidate\")]/a").InnerText;
                    if (user == domainConfig.userName || user == domainConfig.userNameSecond) Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(user);
                    Console.ResetColor();
                }
                Console.WriteLine();
            }
        }
    }
}
