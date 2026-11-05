#nullable enable
using Microsoft.AspNetCore.Mvc;

namespace Podcast.Web.Controllers
{
    public class ContactController : Controller
    {
        // GET: /contact
        public IActionResult Index()
        {
            return View();
        }

        // POST: /contact
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Submit(string name, string email, string company, string message)
        {
            TempData["SuccessMessage"] = $"Thank you, {name}! Your message has been sent successfully. We will get back to you shortly.";
            return RedirectToAction(nameof(Index));
        }
    }
}
