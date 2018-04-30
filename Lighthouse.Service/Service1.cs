using Lighthouse.Business.Config;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lighthouse.Service
{
    public partial class Service1 : ServiceBase
    {
        private Timer pollTimer;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);

            pollTimer = new Timer(new TimerCallback(TimerCallback), null, 0, Properties.Settings.Default.PollIntervalSeconds*1000);
        }

        private void PauseTimer()
        {
            pollTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void ResumeTimer()
        {
            var interval = Properties.Settings.Default.PollIntervalSeconds * 1000;
            pollTimer.Change(interval, interval);
        }

        private void TimerCallback(object state)
        {
            try
            {
                PauseTimer();

                var config = ConfigManager.Load();

                foreach(var repo in config.repositories)
                {
                    ProcessRepository(repo);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
            finally
            {
                ResumeTimer();
            }
        }

        private static void ProcessRepository(Repository repoConfig)
        {
            Logger.Log("Processing repo " + repoConfig.name);

            try
            {
                using (var repoInstance = RepoManager.InitRepo(repoConfig))
                {
                    var bm = new BranchManager(repoInstance, repoConfig);

                    var branchesToMerge = bm.GetBranchesToMerge();

                    bm.BuildBranchReport(branchesToMerge);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Failed during processing of repo.");
                Logger.Log(ex.ToString());
            }
        }

        protected override void OnStop()
        {
        }
    }
}
