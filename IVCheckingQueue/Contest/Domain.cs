using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace IVCheckingQueue.Contest
{
    public class Domain
    {
        public List<string> Authors
        {
            get => _authors;
            set
            {
                CheckForChange(_authors, value);
                _authors = value;
            }
        }
        private List<string> _authors;

        public DateTime WinningTemplatePublished
        {
            get => _winningTemplatePublished;
            set
            {
                CheckForChange(_winningTemplatePublished, value);
                _winningTemplatePublished = value;
            }
        }
        private DateTime _winningTemplatePublished;

        public string Name { get; set; }

        public string Status
        {
            get => _status;
            set
            {
                CheckForChange(_status, value);
                _status = value;
            }
        }
        private string _status;

        public DateTime LastUpdate { get; set; }
        public DateTime LastChange { get; set; }
        public bool Changed { get; set; }

        public Domain(string name)
        {
            Name = name;
        }

        public Domain(string name, string status) : this(name)
        {
            Status = status;
        }

        public static async Task<Domain> CreateAsync(string name, string status, bool ignoreStatus = true)
        {
            var domain = new Domain(name, status);
            await domain.UpdateAsync(ignoreStatus);
            return domain;
        }

        public async Task UpdateAsync(bool ignoreStatus = false)
        {
            var url = "https://instantview.telegram.org/contest/" + $"{Name}/";
            var doc = await new HtmlWeb().LoadFromWebAsync(url);
            var section = doc.DocumentNode.Descendants().Where(_x => _x.HasClass("contest-section")).FirstOrDefault();
            var activeTemplates = section.Descendants().Where(_x => _x.HasClass("list-group-contest-item")).ToList();
            if (activeTemplates.Count > 0)
            {
                Authors = activeTemplates.Select(x => x.Descendants().Where(_x => _x.HasClass("contest-item-author")).SingleOrDefault()).Where(x => x != null).Select(x => x.InnerText).ToList();
                //  Mar 24 at 6:27 PM
                var date = activeTemplates.First().Descendants().Where(_x => _x.HasClass("contest-item-date")).SingleOrDefault().InnerText;
                WinningTemplatePublished = DateTime.ParseExact(date, "MMM d 'at' h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                if (!ignoreStatus)
                {
                    var bd = await BasicDomain.GetDomainsAsync();
                    Status = bd.Where(x => x.Name == Name).Single().Status;
                }
            }
            else
            {
                Status = "Empty";
                Authors.Clear();
                WinningTemplatePublished = default(DateTime);

            }
        }

        public void Update(bool ignoreStatus = false)
        {
            UpdateAsync(ignoreStatus).GetAwaiter().GetResult();
        }

        public static async Task<IEnumerable<Domain>> GetAllDomainsAsync()
        {
            var basicDomains = (await BasicDomain.GetDomainsAsync());
            var domains = new List<Domain>();
            await Task.Run(() => Parallel.ForEach(basicDomains, x => domains.Add(x.Advance())));
            return domains;
        }

        private void CheckForChange(string a, string b)
        {
            if (a != null && b != null)
                if (a != b)
                    UpdateChangeStatus();
        }
        private void CheckForChange(List<string> a, List<string> b)
        {
            if (a != null && b != null)
                if (!a.SequenceEqual(b))
                    UpdateChangeStatus();
        }
        private void CheckForChange(DateTime a, DateTime b)
        {
            if (a != default(DateTime) && b != default(DateTime))
                if (a != b)
                    UpdateChangeStatus();
        }


        private void UpdateChangeStatus()
        {
            LastChange = DateTime.Now;
            Changed = true;
        }
    }
}
