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
        private readonly IHostRepository _hostRepo;

        public AboutController(IHostRepository hostRepo)
        {
            _hostRepo = hostRepo;
        }

        public async Task<IActionResult> Index()
        {
            var hostEntities = await _hostRepo.GetAllHostsAsync();
            var hosts = hostEntities?.Select(h => h.ToViewModel()).ToList() ?? new List<HostViewModel>();
            if (!hosts.Any())
            {
                hosts = SampleData.GetHosts();
            }

            ViewBag.Hosts = hosts;
            return View();
        }
    }
}
