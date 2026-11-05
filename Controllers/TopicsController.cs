#nullable enable
using Microsoft.AspNetCore.Mvc;
using Podcast.Web.Models;
using Podcast.Web.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Podcast.Web.Controllers
{
    public class TopicsController : Controller
    {
        private readonly ITopicRepository _topicRepo;

        public TopicsController(ITopicRepository topicRepo)
        {
            _topicRepo = topicRepo;
        }

        // GET: /topics
        public async Task<IActionResult> Index()
        {
            var topicEntities = await _topicRepo.GetAllTopicsAsync();
            var topics = topicEntities?.Select(t => t.ToViewModel()).ToList() ?? new List<TopicViewModel>();

            if (!topics.Any())
            {
                topics = SampleData.GetTopics();
            }

            return View(topics);
        }
    }
}
