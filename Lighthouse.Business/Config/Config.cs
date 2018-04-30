using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lighthouse.Business.Config
{
    public class Config
    {
        public Repository[] repositories { get; set; }
    }

    public class Repository
    {
        public string name { get; set; }
        public string url { get; set; }
        public bool useDefaultCredentials { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string[] excludeBranches { get; set; }
        public int ignoreBranchesOlderThanDays { get; set; }
    }

    public class ConfigManager
    {
        public static Config Load()
        {
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            return config;
        }

        public static void Save(Config config)
        {
            File.WriteAllText("config.json", JsonConvert.SerializeObject(config, Formatting.Indented));
        }
    }
}



