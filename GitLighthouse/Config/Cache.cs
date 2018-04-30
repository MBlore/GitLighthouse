using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lighthouse.Config
{
    public class CacheConfig
    {
        public Branch[] branches { get; set; }
    }

    public class Branch
    {
        public string name { get; set; }
        public string lastKnownSha { get; set; }
    }

    public class CacheConfigManager
    {
        public static CacheConfig Load()
        {
            if (File.Exists("cache.json"))
            {
                var config = JsonConvert.DeserializeObject<CacheConfig>(File.ReadAllText("cache.json"));
                return config;
            }

            return new CacheConfig() { branches = new Branch[0] };
        }

        public static void Save(CacheConfig config)
        {
            File.WriteAllText("cache.json", JsonConvert.SerializeObject(config, Formatting.Indented));
        }
    }
}


