#nullable enable
using Microsoft.AspNetCore.Mvc;
using Podcast.Web.Services;
using System.Threading.Tasks;
using TimeToTalk;

namespace Podcast.Web.Controllers
{
    public class ContactController : Controller
    {
        private readonly ITiyaContentRepository _contentRepo;

        public ContactController(ITiyaContentRepository contentRepo)
        {
            _contentRepo = contentRepo;
        }

        public async Task<IActionResult> Index()
        {
            var contactPage = await _contentRepo.GetContactPageContentAsync();
            var settings = await _contentRepo.GetWebsiteSettingsAsync();

            ViewBag.ContactPage = contactPage;
            ViewBag.Settings = settings;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Submit(string name, string email, string company, string message)
        {
            TempData["SuccessMessage"] = $"Thank you, {name}! Your message has been sent successfully. We will get back to you shortly.";
            return RedirectToAction(nameof(Index));
        }
    }
}
