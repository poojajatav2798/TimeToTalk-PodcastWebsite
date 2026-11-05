using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;
using Podcast.Web.Models;
using Podcast.Web.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeToTalk;

[assembly: RegisterWebPageRoute(
    contentTypeName: HomePage.CONTENT_TYPE_NAME,
    controllerType: typeof(Podcast.Web.Controllers.HomeController)
)]

namespace Podcast.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IEpisodeRepository _episodeRepo;
        private readonly IHostRepository _hostRepo;
        private readonly ITopicRepository _topicRepo;

        public HomeController(IEpisodeRepository episodeRepo, IHostRepository hostRepo, ITopicRepository topicRepo)
        {
            _episodeRepo = episodeRepo;
            _hostRepo = hostRepo;
            _topicRepo = topicRepo;
        }

        public async Task<IActionResult> Index()
        {
            var episodeEntities = await _episodeRepo.GetAllEpisodesAsync();
            var hostEntities = await _hostRepo.GetAllHostsAsync();
            var topicEntities = await _topicRepo.GetAllTopicsAsync();

            var episodes = episodeEntities?.Select(e => e.ToViewModel()).ToList() ?? new List<EpisodeViewModel>();
            var hosts = hostEntities?.Select(h => h.ToViewModel()).ToList() ?? new List<HostViewModel>();
            var topics = topicEntities?.Select(t => t.ToViewModel()).ToList() ?? new List<TopicViewModel>();

            if (!hosts.Any())
            {
                hosts = SampleData.GetHosts();
            }
            if (!episodes.Any())
            {
                episodes = SampleData.GetEpisodes();
            }
            if (!topics.Any())
            {
                topics = SampleData.GetTopics();
            }

            ViewBag.Hosts = hosts;
            ViewBag.LatestEpisodes = episodes.Take(4).ToList();
            ViewBag.TrendingEpisodes = episodes.Where(e => e.IsTrending).Take(3).ToList();
            ViewBag.Topics = topics;

            return View();
        }
    }
}
