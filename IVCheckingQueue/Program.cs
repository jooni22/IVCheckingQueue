using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IVCheckingQueue.Contest;

namespace IVCheckingQueue
{
    class Program
    {
        const int FocusSize = 25;

        static List<Domain> Domains { get; set; } = new List<Domain>();
        static List<Domain> FocusedDomains { get; set; } = new List<Domain>();
        static List<Domain> UpdatedDomains { get; set; } = new List<Domain>();

        static string DomainConfigFile = @"domainConfig.json";
        static DomainConfig DomainConfig = new DomainConfig();
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
            Console.OutputEncoding = System.Text.Encoding.GetEncoding(866);
            FileExists(DomainConfigFile);
            DomainConfig = DomainConfig.ReadFromJson(DomainConfigFile);
            var thr = new System.Threading.Thread(BackgroudProcessing)
            {
                IsBackground = true
            };
            thr.Start();
            while (Domains.Count == 0)
                Thread.Sleep(25);
            Console.Clear();
            while (true)
            {
                int i = 0;
                foreach (var d in FocusedDomains)
                {
                    ++i;
                    Console.Write($"{i}.".PadRight(4));
                    PrintDomain(d);
                }
                Console.WriteLine();
                Console.WriteLine($"{DomainConfig.userName} is #{Domains.FindIndex(_x => _x.Authors.Contains(DomainConfig.userName))}");
                Console.WriteLine($"{DomainConfig.userNameSecond} is #{Domains.FindIndex(_x => _x.Authors.Contains(DomainConfig.userNameSecond))}");
                Thread.Sleep(DomainConfig.UpdateTime * 1000);
                Console.Clear();
            }
        }

        static async void BackgroudProcessing()
        {
            InitializeDomains();
            Domains = Domains.OrderBy(x => x.WinningTemplatePublished).ToList();
            FocusedDomains = Domains.Where(x => x.Status == "checking").Take(focusSize).ToList();
            var last = FocusedDomains.Last();
            while (true)
            {
                if (FocusedDomains.Count < focusSize)
                {
                    Domains = Domains.OrderBy(x => x.WinningTemplatePublished).ToList();
                    var addition = Domains.Where(x=>!FocusedDomains.Contains(x)).Take(focusSize - FocusedDomains.Count);
                    FocusedDomains.AddRange(addition);
                }
                var bd = await BasicDomain.GetDomainsAsync();
                foreach (var d in FocusedDomains)
                {
                    await d.UpdateAsync(true);
                    d.Status = bd.FirstOrDefault(x => x.Name == d.Name).Status;
                    if (string.IsNullOrEmpty(d.Status))
                        d.Status = "Empty";
                }
                last = FocusedDomains.Last();
                var old = FocusedDomains.Where(x => x.Changed && DateTime.Now.Subtract(x.LastChange).TotalMinutes > 10).ToList();
                foreach (var d in old)
                {
                    FocusedDomains.Remove(d);
                }
                UpdatedDomains = FocusedDomains.Where(x => x.Changed).ToList();
                await Task.Delay(DomainConfig.UpdateTime * 1000);
            }
        }

        static void InitializeDomains()
        {
            Console.WriteLine("Retrieving domains list");
            var basicDomains = Contest.BasicDomain.GetDomains().Where(x => x.Status == "checking").ToList();
            Console.WriteLine("Initial data collection");
            var ts = new List<Task<Domain>>();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Parallel.ForEach(basicDomains, x => ts.Add(x.AdvanceAsync()));
            ts.RemoveAll(x => x == null);
            Task.WhenAll(ts);
            Domains = ts.Select(x => x.Result).ToList();
            Console.Title = $"{Domains.Count} domains parsed in {sw.Elapsed.TotalSeconds.ToString("0.00")} s";
            Console.WriteLine("Done");
        }

        static void PrintDomain(Contest.Domain domain)
        {
            var myDomainsList = DomainConfig.Domains;
            if (myDomainsList.Contains(domain.Name))
            {
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Black;
            }
            Console.Write(domain.Name.PadRight(35));
            Console.ResetColor();
            if (domain.Changed)
                Console.BackgroundColor = ConsoleColor.DarkBlue;
            if (domain.Status == "checking")
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            if (domain.Status == "Winner '19")
            {
                Console.ForegroundColor = ConsoleColor.Blue;
            }
            if (domain.Status == "Empty")
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            Console.Write(domain.Status.PadRight(14));
            Console.ResetColor();
            if (domain.Status != "Empty")
            {

                if (domain.Authors.Any(x => x == DomainConfig.userName || x == DomainConfig.userNameSecond)) Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(domain.Authors.First().PadRight(35));
                Console.ResetColor();
                if (domain.Status == "checking")
                    Console.Write($"-{DateTime.Now.Subtract(domain.WinningTemplatePublished).Subtract(TimeSpan.FromDays(3)).TotalDays.ToString("0.0")} d");
                else
                    Console.Write($"{TimeSpan.FromDays(3).Subtract(DateTime.Now.Subtract(domain.WinningTemplatePublished)).TotalHours.ToString("0.0")} h".PadLeft(1));
            }
            Console.WriteLine();

        }
    }
}
