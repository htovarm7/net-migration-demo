using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using LegacyWeb.Models;

namespace LegacyWeb.Controllers
{
    // Not a migrated page: lives entirely in legacy, same as Uploads. The form posts
    // to this same controller/host, so there is no cross-app redirect involved - a GET
    // here is always answered by this app, never by the modern one.
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
            // 1) The raw dictionary exactly as it arrived on the wire.
            ViewBag.RawForm = Request.Form;

            // 2) The same data, already bound into a typed object by MVC's model binder.
            ViewBag.BoundModel = model;

            // 3) The multipart file part, inspected directly in the controller - not just
            // saved blind. HttpPostedFileBase exposes name/type/size without reading bytes.
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
