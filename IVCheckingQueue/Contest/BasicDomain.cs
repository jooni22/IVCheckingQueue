using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVCheckingQueue.Contest
{
    public class BasicDomain
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public string WinningAuthor { get; set; }

        public BasicDomain() { }

        public static async Task<IEnumerable<BasicDomain>> GetDomainsAsync()
        {
            var doc = await new HtmlWeb().LoadFromWebAsync("https://instantview.telegram.org/contest");
            var domainsNodes = doc.DocumentNode.SelectNodes("//*[@data-domain]");
            var domains = new List<BasicDomain>();
            foreach (var node in domainsNodes)
            {
                var name = node.GetAttributeValue("data-domain", string.Empty);
                var bd = new BasicDomain()
                {
                    Name = name,
                    Status = node.SelectSingleNode(".//*[contains(@class,\"iv-deadline\") or @class=\"status-winner\"]")?.InnerText,
                    WinningAuthor = node.SelectSingleNode(".//*[contains(@class,\"contest-item-candidate\")]/a")?.InnerText
                };
                domains.Add(bd);
            }
            return domains;

        }

        public static IEnumerable<BasicDomain> GetDomains()
        {
            var domains = GetDomainsAsync().GetAwaiter().GetResult();
            return domains;
        }

        public Domain Advance()
        {
            var domain = AdvanceAsync().GetAwaiter().GetResult();
            return domain;
        }

        public async Task<Domain> AdvanceAsync()
        {
            var domain = await Domain.CreateAsync(Name, Status);
            return domain;
        }


    }
}
