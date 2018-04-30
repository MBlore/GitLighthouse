using LibGit2Sharp;
using Lighthouse.Business;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lighthouse
{
    /// <summary>
    /// Helper class for managing branches.
    /// </summary>
    public class BranchManager
    {
        private Repository repo;
        private Business.Config.Repository repoConfig;

        public BranchManager(Repository repo, Business.Config.Repository repoConfig)
        {
            this.repo = repo;
            this.repoConfig = repoConfig;
        }

        /// <summary>
        /// Returns a list of branches to consider merging.
        /// </summary>
        public List<Branch> GetBranchesToMerge()
        {
            var branches = new List<Branch>();

            var cacheConfig = Config.CacheConfigManager.Load();
            var branchCaches = cacheConfig.branches.ToList();

            foreach (var b in repo.Branches)
            {
                if (b.IsCurrentRepositoryHead)
                    continue;

                var names = b.FriendlyName.Split('/');
                var branchName = names[names.Length - 1];

                if (repoConfig.excludeBranches.Contains(branchName))
                    continue;

                if (IsBranchExpired(b, branchName))
                    continue;

                var hasChanged = UpdateBranchCache(branchCaches, b, branchName);
                if (!hasChanged)
                    continue;

                branches.Add(b);
            }

            cacheConfig.branches = branchCaches.ToArray();
            Config.CacheConfigManager.Save(cacheConfig);

            return branches;
        }

        /// <summary>
        /// Updates a branch in the cache, and returns true if the branch has changed.
        /// </summary>
        private bool UpdateBranchCache(List<Config.Branch> branchCaches, Branch b, string branchName)
        {
            var branchCache = branchCaches.FirstOrDefault(x => x.name == branchName);

            if (branchCache != null)
            {
                if (branchCache.lastKnownSha == b.Tip.Sha)
                {
                    // Branch hasn't changed.
                    Logger.Log("Skipping branch " + branchName + " - no changes.");
                    return false;
                }
                else
                {
                    branchCache.lastKnownSha = b.Tip.Sha;
                }
            }
            else
            {
                branchCaches.Add(new Config.Branch()
                {
                    name = branchName,
                    lastKnownSha = b.Tip.Sha
                });
            }

            return true;
        }

        /// <summary>
        /// Returns true if the specified branch has expired.
        /// </summary>
        private bool IsBranchExpired(Branch b, string branchName)
        {
            if (repoConfig.ignoreBranchesOlderThanDays > 0)
            {
                var ts = DateTime.Now - b.Commits.First().Committer.When;

                if (ts.TotalDays > repoConfig.ignoreBranchesOlderThanDays)
                {
                    Logger.Log("Ignoring branch " + branchName + " - out of date.");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Finds conflicts in the specified list of branches and saves a conflict report.
        /// </summary>
        public void BuildBranchReport(List<Branch> branchesToMerge)
        {
            var reportData = Config.ReportManager.Load();
            var mergedPairs = new List<string>();

            var repoReportData = InitializeReportData(reportData);

            foreach (var sourceBranch in branchesToMerge)
            {
                Logger.Log("Looking for conflicts from branch: " + sourceBranch.FriendlyName);

                ClearReportItem(repoReportData, sourceBranch);

                foreach (var targetBranch in branchesToMerge)
                {
                    if (targetBranch == sourceBranch)
                        continue;

                    Logger.Log("Testing branch " + targetBranch.FriendlyName);

                    if (BranchesAlreadyMerged(mergedPairs, sourceBranch, targetBranch))
                        continue;

                    var result = CheckoutAndMerge(sourceBranch, targetBranch);

                    if (result.Status == MergeStatus.Conflicts)
                    {
                        RecordConflicts(repoReportData, sourceBranch, targetBranch, targetBranch.Commits.First());
                    }

                    mergedPairs.Add(sourceBranch.CanonicalName + " <- " + targetBranch.CanonicalName);

                    repo.Reset(ResetMode.Hard);
                }
            }

            File.WriteAllText("report.json", JsonConvert.SerializeObject(reportData, Formatting.Indented));
        }

        private void RecordConflicts(Config.RepoReport repoReportData, Branch sourceBranch, Branch targetBranch, Commit lastCommit)
        {
            foreach (var conflict in repo.Index.Conflicts)
            {
                Logger.Log("Conflicted on branch " + sourceBranch.FriendlyName + " with branch " + targetBranch.FriendlyName + ": " + conflict.Ancestor.Path);

                RecordConflict(
                    repoReportData,
                    conflict.Ours.Path,
                    sourceBranch.FriendlyName.Split('/').Last(),
                    targetBranch.FriendlyName.Split('/').Last(),
                    lastCommit.Author.Name,
                    lastCommit.Author.When);
            }
        }

        private MergeResult CheckoutAndMerge(Branch sourceBranch, Branch targetBranch)
        {
            Commands.Checkout(repo, sourceBranch);

            var mo = new MergeOptions()
            {
                CommitOnSuccess = false,
                FastForwardStrategy = FastForwardStrategy.NoFastForward
            };

            var result = repo.Merge(targetBranch, new Signature("test", "test@test.com", DateTimeOffset.Now), mo);

            return result;
        }

        private static bool BranchesAlreadyMerged(List<string> mergedPairs, Branch sourceBranch, Branch targetBranch)
        {
            if (mergedPairs.Contains(targetBranch.CanonicalName + " <- " + sourceBranch.CanonicalName) ||
                mergedPairs.Contains(sourceBranch.CanonicalName + " <- " + targetBranch.CanonicalName))
            {
                Logger.Log("Branches have already been compared - skipping...");
                return true;
            }

            return false;
        }

        private void ClearReportItem(Config.RepoReport repoReportData, Branch sourceBranch)
        {
            var reportItem = repoReportData.branches.FirstOrDefault(x => x.name == sourceBranch.FriendlyName.Split('/').Last());

            if (reportItem != null)
            {
                repoReportData.branches.Remove(reportItem);
            }
        }

        private Config.RepoReport InitializeReportData(Config.Report reportData)
        {
            var repoReportData = reportData.repos.FirstOrDefault(x => x.name == repoConfig.name);

            if (repoReportData == null)
            {
                repoReportData = new Config.RepoReport()
                {
                    name = repoConfig.name,
                    branches = new List<Config.BranchReport>()
                };

                reportData.repos.Add(repoReportData);
            }

            return repoReportData;
        }

        private void RecordConflict(
            Config.RepoReport reportData,
            string path,
            string sourceBranch,
            string mergeBranch,
            string mergeAuthor,
            DateTimeOffset mergeDate)
        {
            var reportItem = reportData.branches.FirstOrDefault(x => x.name == sourceBranch);

            if (reportItem == null)
            {
                reportItem = new Config.BranchReport()
                {
                    name = sourceBranch,
                    conflictingBranches = new List<Config.ConflictingBranchReport>()
                };

                reportData.branches.Add(reportItem);
            }

            var conflictBranch = reportItem.conflictingBranches.FirstOrDefault(x => x.name == mergeBranch);

            if (conflictBranch == null)
            {
                conflictBranch = new Config.ConflictingBranchReport()
                {
                    name = mergeBranch,
                    conflictingPaths = new List<string>()
                };

                reportItem.conflictingBranches.Add(conflictBranch);
            }

            conflictBranch.lastCommitAuthor = mergeAuthor;
            conflictBranch.lastCommitDate = mergeDate.ToString();
            conflictBranch.conflictingPaths.Add(path);
        }
    }
}
