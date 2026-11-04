using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LegacyWeb.Services;

namespace LegacyWeb.Controllers
{
    // Not a migrated page: this feature lives entirely in the legacy app, so it is never
    // routed through AuthRoutingFilter's Legacy/Modern table. Files saved here only ever
    // touch this app's own folder - the modern app can't see or write into it.
    public class UploadsController : Controller
    {
        private string UploadsPath => Server.MapPath("~/App_Data/Uploads");

        public ActionResult Index()
        {
            Directory.CreateDirectory(UploadsPath);
            ViewBag.Session = HttpContext.Items["Session"] as SessionResult;
            ViewBag.Files = new DirectoryInfo(UploadsPath).GetFiles().OrderByDescending(f => f.CreationTimeUtc)
                .Select(f => f.Name).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                Directory.CreateDirectory(UploadsPath);
                var safeName = Guid.NewGuid().ToString("N").Substring(0, 8) + "_" + Path.GetFileName(file.FileName);
                file.SaveAs(Path.Combine(UploadsPath, safeName));
            }

            return RedirectToAction("Index");
        }
    }
}
