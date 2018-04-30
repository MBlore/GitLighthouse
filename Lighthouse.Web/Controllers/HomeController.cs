using Lighthouse.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Lighthouse.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "What is Git Lighthouse?";

            return View();
        }

        public JsonResult GetRepos()
        {
            var config = ReportManager.Load(Properties.Settings.Default.ReportLocation);

            return new JsonResult()
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = config.repos.Select(x => x.name)
            };
        }

        public JsonResult GetBranches(string repoName)
        {
            var config = ReportManager.Load(Properties.Settings.Default.ReportLocation);

            return new JsonResult()
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = config.repos.First(x => x.name == repoName).branches.Select(x => x.name)
            };
        }

        public JsonResult GetConflicts(string repoName, string branchName)
        {
            var config = ReportManager.Load(Properties.Settings.Default.ReportLocation);

            return new JsonResult()
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = config.repos.First(x => x.name == repoName)
                    .branches.First(x => x.name == branchName)
                    .conflictingBranches
            };
        }
    }
}