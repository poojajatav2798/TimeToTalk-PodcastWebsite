#nullable enable
using Microsoft.AspNetCore.Mvc;
using Podcast.Web.Models;
using Podcast.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Podcast.Web.Controllers
{
    public class EpisodesController : Controller
    {
        private readonly IEpisodeRepository _episodeRepo;
        private readonly ITopicRepository _topicRepo;

        public EpisodesController(IEpisodeRepository episodeRepo, ITopicRepository topicRepo)
        {
            _episodeRepo = episodeRepo;
            _topicRepo = topicRepo;
        }

        // GET: /episodes
        public async Task<IActionResult> Index(string? topic, string? filter, int page = 1)
        {
            const int pageSize = 6;
            if (page < 1) page = 1;

            // 1. Fetch episodes and topics from Content Hub
            var episodeEntities = await _episodeRepo.GetAllEpisodesAsync();
            var topicEntities = await _topicRepo.GetAllTopicsAsync();

            var episodes = episodeEntities?.Select(e => e.ToViewModel()).ToList() ?? new List<EpisodeViewModel>();
            var topics = topicEntities?.Select(t => t.ToViewModel()).ToList() ?? new List<TopicViewModel>();

            // Fallback to sample data if Content Hub is empty
            if (!episodes.Any()) episodes = SampleData.GetEpisodes();
            if (!topics.Any()) topics = SampleData.GetTopics();

            // 2. Filter by topic or trending
            if (!string.IsNullOrWhiteSpace(topic))
            {
                episodes = episodes.Where(e =>
                    e.Topic.Equals(topic, StringComparison.OrdinalIgnoreCase) ||
                    e.Slug.Contains(topic, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else if (filter?.Equals("trending", StringComparison.OrdinalIgnoreCase) == true)
            {
                episodes = episodes.Where(e => e.IsTrending).ToList();
            }

            // 3. Pagination
            int totalEpisodes = episodes.Count;
            int totalPages = (int)Math.Ceiling(totalEpisodes / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var pagedEpisodes = episodes
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Topic = topic;
            ViewBag.Filter = filter;
            ViewBag.AllTopics = topics.Select(t => t.Title).Distinct().ToList();

            return View(pagedEpisodes);
        }

        // GET: /episodes/{id}
        [Route("episodes/{id}")]
        public async Task<IActionResult> Detail(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction(nameof(Index));
            }

            EpisodeViewModel? episode = null;

            // Try fetch from Kentico Content Hub by slug
            var episodeEntity = await _episodeRepo.GetEpisodeBySlugAsync(id);
            if (episodeEntity != null)
            {
                episode = episodeEntity.ToViewModel();
            }
            else if (int.TryParse(id, out int episodeId))
            {
                var allEntities = await _episodeRepo.GetAllEpisodesAsync();
                var match = allEntities?.FirstOrDefault(e => e.SystemFields?.ContentItemID == episodeId || e.EpisodeNumber == episodeId);
                if (match != null)
                {
                    episode = match.ToViewModel();
                }
            }

            // Fallback to SampleData
            if (episode == null)
            {
                episode = SampleData.GetEpisodes().FirstOrDefault(e => 
                    e.Id.ToString() == id || 
                    e.Slug.Equals(id, StringComparison.OrdinalIgnoreCase));
            }

            if (episode == null)
            {
                return NotFound();
            }

            // Fetch related episodes
            var relatedEntities = await _episodeRepo.GetRelatedEpisodesAsync(id, count: 3);
            var related = relatedEntities?.Select(e => e.ToViewModel()).ToList() ?? new List<EpisodeViewModel>();
            if (!related.Any())
            {
                related = SampleData.GetEpisodes().Where(e => e.Id != episode.Id).Take(3).ToList();
            }

            ViewBag.RelatedEpisodes = related;
            return View(episode);
        }
    }
}
