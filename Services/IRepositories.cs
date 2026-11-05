#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using TimeToTalk;

namespace Podcast.Web.Services
{
    public interface IEpisodeRepository
    {
        Task<IEnumerable<PodcastEpisode>> GetAllEpisodesAsync();
        Task<IEnumerable<PodcastEpisode>> GetLatestEpisodesAsync(int count = 6);
        Task<IEnumerable<PodcastEpisode>> GetTrendingEpisodesAsync(int count = 3);
        Task<IEnumerable<PodcastEpisode>> GetEpisodesByTopicAsync(string topicSlug);
        Task<IEnumerable<PodcastEpisode>> SearchEpisodesAsync(string query);
        Task<PodcastEpisode?> GetEpisodeBySlugAsync(string slug);
        Task<IEnumerable<PodcastEpisode>> GetRelatedEpisodesAsync(string slug, int count = 3);
    }

    public interface IHostRepository
    {
        Task<IEnumerable<PodcastHost>> GetAllHostsAsync();
        Task<PodcastHost?> GetHostBySlugAsync(string slug);
    }

    public interface ITopicRepository
    {
        Task<IEnumerable<PodcastTopic>> GetAllTopicsAsync();
        Task<PodcastTopic?> GetTopicBySlugAsync(string slug);
    }
}
