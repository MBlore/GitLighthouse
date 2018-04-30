using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lighthouse.Business
{
    class ConflictingBranch
    {
        public ConflictingBranch()
        {
            ConflictPaths = new List<string>();
        }

        public string Name { get; set; }
        public string LastCommitAuthor { get; set; }
        public DateTimeOffset LastCommitDate { get; set; }
        public List<string> ConflictPaths { get; set; }
    }
}