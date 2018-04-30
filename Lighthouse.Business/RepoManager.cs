using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lighthouse.Business.Config;

namespace Lighthouse
{
    /// <summary>
    /// Manages setting up and connecting to a repository.
    /// </summary>
    public class RepoManager
    {
        private static string RepoFolder = "repos";

        /// <summary>
        /// Connects to the configured repository, cloning it if it has not yet been downloaded.
        /// </summary>
        public static LibGit2Sharp.Repository InitRepo(Business.Config.Repository repoConfig)
        {
            // Verify repo exists, else clone it.
            var repoPath = RepoFolder + "\\" + repoConfig.name;

            if (!Directory.Exists(repoPath + "\\.git"))
            {
                Logger.Log("Repo doesn't exist, cloning...");
                CloneRepo(repoConfig);
            }

            // Reset the repo back to a clean state.
            Logger.Log("Resetting repo...");
            var repo = new LibGit2Sharp.Repository(repoPath);
            repo.Reset(ResetMode.Hard);

            // Fetch latest refs.
            Logger.Log("Fetching...");
            var fetchOptions = new FetchOptions()
            {
                CredentialsProvider = (url, user, cred) => GetCredentials(repoConfig)
            };

            foreach(var remote in repo.Network.Remotes)
            {
                var logMessage = "";
                var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);

                Commands.Fetch(repo, remote.Name, refSpecs, fetchOptions, logMessage);
            }

            Logger.Log("Repo initialized.");
            return repo;
        }

        /// <summary>
        /// Clones a git repository from the specified repository config.
        /// </summary>
        private static void CloneRepo(Business.Config.Repository repoConfig)
        {
            var co = new CloneOptions();

            co.CredentialsProvider = (url, user, cred) => GetCredentials(repoConfig);

            var repoPath = RepoFolder + "\\" + repoConfig.name;

            if (!Directory.Exists(repoPath))
                Directory.CreateDirectory(repoPath);

            LibGit2Sharp.Repository.Clone("https://MartinBlore@bitbucket.org/MartinBlore/testrepo.git", repoPath, co);
        }

        /// <summary>
        /// Initialize a credentials object from config for a repository.
        /// </summary>
        private static Credentials GetCredentials(Business.Config.Repository repoConfig)
        {
            Credentials creds = null;

            if (repoConfig.useDefaultCredentials)
            {
                creds = new DefaultCredentials();
            }
            else
            {
                creds = new UsernamePasswordCredentials
                {
                    Username = repoConfig.username,
                    Password = repoConfig.password
                };
            }

            return creds;
        }
    }
}
