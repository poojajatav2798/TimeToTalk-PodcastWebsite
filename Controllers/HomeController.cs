#nullable enable
using Kentico.Content.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Podcast.Web.Models;
using Podcast.Web.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeToTalk;

namespace Podcast.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IEpisodeRepository _episodeRepo;
        private readonly IHostRepository _hostRepo;
        private readonly ITopicRepository _topicRepo;
        private readonly IWebPageDataContextRetriever _dataContextRetriever;

        public HomeController(
            IEpisodeRepository episodeRepo, 
            IHostRepository hostRepo, 
            ITopicRepository topicRepo,
            IWebPageDataContextRetriever dataContextRetriever)
        {
            _episodeRepo = episodeRepo;
            _hostRepo = hostRepo;
            _topicRepo = topicRepo;
            _dataContextRetriever = dataContextRetriever;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Fetch content from Kentico Content Hub via Repositories
            var episodeEntities = await _episodeRepo.GetAllEpisodesAsync();
            var hostEntities = await _hostRepo.GetAllHostsAsync();
            var topicEntities = await _topicRepo.GetAllTopicsAsync();

            // 2. Map strongly-typed ReusableContentTypes to ViewModels
            var episodes = episodeEntities?.Select(e => e.ToViewModel()).ToList() ?? new List<EpisodeViewModel>();
            var hosts = hostEntities?.Select(h => h.ToViewModel()).ToList() ?? new List<HostViewModel>();
            var topics = topicEntities?.Select(t => t.ToViewModel()).ToList() ?? new List<TopicViewModel>();

            // 3. Fallback to SampleData if Content Hub is not yet populated by editors
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

            // 4. Pass typed ViewModels to ViewBag for Razor rendering
            ViewBag.Hosts = hosts;
            ViewBag.LatestEpisodes = episodes.Take(4).ToList();
            ViewBag.TrendingEpisodes = episodes.Where(e => e.IsTrending).Take(3).ToList();
            ViewBag.Topics = topics;

            // 5. Return View() which contains the Page Builder editable area Html.Kentico().EditableAreaAsync("home_main_area")
            // This prevents the "has no template assigned" exception while enabling full Page Builder widgets!
            return View();
        }
    }
}
