using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVCheckingQueue
{
    public class DomainConfig
    {
        [JsonProperty("userName")]
        public string userName { get; set; }

        [JsonProperty("userNameSecond")]
        public string userNameSecond { get; set; }

        [JsonProperty("updateTime")]
        public int UpdateTime { get; set; }

        [JsonProperty("domains")]
        public List<string> Domains { get; set; }

        public static DomainConfig FromJson(string json) => JsonConvert.DeserializeObject<DomainConfig>(json);

        public string ToJson() => JsonConvert.SerializeObject(this, Formatting.Indented);

        public static DomainConfig ReadFromJson(string pathFile)
        {
            string jsonString = System.IO.File.ReadAllText(pathFile);
            var config = FromJson(jsonString);
            return config;
        }

        public static void WriteToJson(DomainConfig data, string pathFile)
        {
            var jsonString = data.ToJson();
            System.IO.File.WriteAllText(pathFile, jsonString);
        }
    }
}
