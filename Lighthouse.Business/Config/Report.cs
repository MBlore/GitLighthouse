using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lighthouse.Config
{
    public class Report
    {
        public List<RepoReport> repos { get; set; }
    }

    public class RepoReport
    {
        public string name { get; set; }
        public List<BranchReport> branches { get; set; }
    }

    public class BranchReport
    {
        public string name { get; set; }
        public List<ConflictingBranchReport> conflictingBranches { get; set; }
    }

    public class ConflictingBranchReport
    {
        public string name { get; set; }
        public string lastCommitAuthor { get; set; }
        public string lastCommitDate { get; set; }
        public List<string> conflictingPaths { get; set; }
    }

    public class ReportManager
    {
        public static Report Load(string filename = "report.json")
        {
            if (File.Exists(filename))
            {
                var config = JsonConvert.DeserializeObject<Report>(File.ReadAllText(filename));
                return config;
            }

            return new Report() { repos = new List<RepoReport>() };
        }

        public static void Save(Report config)
        {
            File.WriteAllText("report.json", JsonConvert.SerializeObject(config, Formatting.Indented));
        }
    }
}
