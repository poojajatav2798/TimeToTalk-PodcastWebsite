#nullable enable
using Microsoft.AspNetCore.Mvc;
using Podcast.Web.Models;
using Podcast.Web.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeToTalk;

namespace Podcast.Web.Controllers
{
    public class AboutController : Controller
    {
        private readonly ITiyaContentRepository _contentRepo;

        public AboutController(ITiyaContentRepository contentRepo)
        {
            _contentRepo = contentRepo;
        }

        public async Task<IActionResult> Index()
        {
            var aboutPage = await _contentRepo.GetAboutPageContentAsync();
            var teamMembers = await _contentRepo.GetTeamMembersAsync();

            ViewBag.AboutPage = aboutPage;
            ViewBag.TeamMembers = teamMembers;

            return View();
        }
    }
}
