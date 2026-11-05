using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using LegacyWeb.Models;

namespace LegacyWeb.Controllers
{
    public class ContactController : Controller
    {
        public ActionResult Index()
        {
            return View(new ContactForm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(ContactForm model, HttpPostedFileBase file)
        {
            ViewBag.RawForm = Request.Form;
            ViewBag.BoundModel = model;

            if (file != null && file.ContentLength > 0)
            {
                ViewBag.FileName = file.FileName;
                ViewBag.FileContentType = file.ContentType;
                ViewBag.FileLength = file.ContentLength;

                var dir = Server.MapPath("~/App_Data/Contacts");
                Directory.CreateDirectory(dir);
                var safeName = Guid.NewGuid().ToString("N").Substring(0, 8) + "_" + Path.GetFileName(file.FileName);
                file.SaveAs(Path.Combine(dir, safeName));
                ViewBag.SavedAs = safeName;
            }

            return View("Result");
        }
    }
}
