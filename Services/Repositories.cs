#nullable enable
using CMS.ContentEngine;
using CMS.DataEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeToTalk;

namespace Podcast.Web.Services
{
    public class Repositories : IEpisodeRepository, IHostRepository, ITopicRepository
    {
        private readonly IContentQueryExecutor _queryExecutor;

        public Repositories(IContentQueryExecutor queryExecutor)
        {
            _queryExecutor = queryExecutor;
        }

        // EPISODE REPOSITORY 
        public async Task<IEnumerable<PodcastEpisode>> GetAllEpisodesAsync()
        {
            var query = new ContentItemQueryBuilder()
                .ForContentType(PodcastEpisode.CONTENT_TYPE_NAME);
            return await _queryExecutor.GetMappedResult<PodcastEpisode>(query);
        }

        public async Task<IEnumerable<PodcastEpisode>> GetLatestEpisodesAsync(int count = 6)
        {
            var query = new ContentItemQueryBuilder()
                .ForContentType(
                    PodcastEpisode.CONTENT_TYPE_NAME,
                    q => q.OrderBy(
                            nameof(PodcastEpisode.PublishDate),
                            "DESC")
                          .TopN(count));

            return await _queryExecutor.GetMappedResult<PodcastEpisode>(query);
        }

        public async Task<IEnumerable<PodcastEpisode>> GetTrendingEpisodesAsync(int count = 3)
        {
            var query = new ContentItemQueryBuilder()
                .ForContentType(PodcastEpisode.CONTENT_TYPE_NAME,
                    q => q.Where(where => where.WhereTrue(nameof(PodcastEpisode.EpisodeIsTrending)))
                          .OrderBy(nameof(PodcastEpisode.PublishDate), "DESC")
                          .TopN(count));
            return await _queryExecutor.GetMappedResult<PodcastEpisode>(query);
        }

        public async Task<IEnumerable<PodcastEpisode>> GetEpisodesByTopicAsync(string topicSlug)
        {
            var query = new ContentItemQueryBuilder()
                .ForContentType(PodcastEpisode.CONTENT_TYPE_NAME,
                    q => q.Where(where =>
                        where.WhereEquals(nameof(PodcastEpisode.EpisodeTopics), topicSlug)
                          .Or()
                          .WhereEquals(nameof(PodcastEpisode.EpisodeTopicSlug), topicSlug)));
            return await _queryExecutor.GetMappedResult<PodcastEpisode>(query);
        }

        public async Task<IEnumerable<PodcastEpisode>> SearchEpisodesAsync(string queryString)
        {
            if (string.IsNullOrWhiteSpace(queryString))
            {
                return await GetAllEpisodesAsync();
            }

            var query = new ContentItemQueryBuilder()
                .ForContentType(PodcastEpisode.CONTENT_TYPE_NAME,
                    q => q.Where(where =>
                        where.WhereContains(nameof(PodcastEpisode.EpisodeTitle), queryString)
                          .Or()
                          .WhereContains(nameof(PodcastEpisode.EpisodeShortDescription), queryString)));
            return await _queryExecutor.GetMappedResult<PodcastEpisode>(query);
        }

        public async Task<PodcastEpisode?> GetEpisodeBySlugAsync(string slug)
        {
            var query = new ContentItemQueryBuilder()
                .ForContentType(PodcastEpisode.CONTENT_TYPE_NAME,
                    q => q.Where(where =>
                        where.WhereEquals(nameof(PodcastEpisode.EpisodeSlug), slug)
                          .Or()
                          .WhereEquals(nameof(PodcastEpisode.EpisodeTopicSlug), slug)).TopN(1));
            var result = await _queryExecutor.GetMappedResult<PodcastEpisode>(query);
            return result.FirstOrDefault();
        }

        public async Task<IEnumerable<PodcastEpisode>> GetRelatedEpisodesAsync(string slug, int count = 3)
        {
            var target = await GetEpisodeBySlugAsync(slug);
            if (target == null) return Enumerable.Empty<PodcastEpisode>();

            var query = new ContentItemQueryBuilder()
                .ForContentType(PodcastEpisode.CONTENT_TYPE_NAME,
                    q => q.Where(where =>
                        where.WhereEquals(nameof(PodcastEpisode.EpisodeTopics), target.EpisodeTopics)
                          .WhereNotEquals(nameof(PodcastEpisode.EpisodeSlug), slug))
                          .TopN(count));
            return await _queryExecutor.GetMappedResult<PodcastEpisode>(query);
        }

        // HOST REPOSITORY 
        public async Task<IEnumerable<PodcastHost>> GetAllHostsAsync()
        {
            var query = new ContentItemQueryBuilder()
                .ForContentType(PodcastHost.CONTENT_TYPE_NAME);
            return await _queryExecutor.GetMappedResult<PodcastHost>(query);
        }

        public async Task<PodcastHost?> GetHostBySlugAsync(string slug)
        {
            var query = new ContentItemQueryBuilder()
                .ForContentType(PodcastHost.CONTENT_TYPE_NAME,
                    q => q.Where(where =>
                        where.WhereEquals(nameof(PodcastHost.HostSlug), slug)
                          .Or()
                          .WhereEquals(nameof(PodcastHost.HostName), slug)).TopN(1));
            var result = await _queryExecutor.GetMappedResult<PodcastHost>(query);
            return result.FirstOrDefault();
        }

        // TOPIC REPOSITORY 
        public async Task<IEnumerable<PodcastTopic>> GetAllTopicsAsync()
        {
            var query = new ContentItemQueryBuilder()
                .ForContentType(PodcastTopic.CONTENT_TYPE_NAME);
            return await _queryExecutor.GetMappedResult<PodcastTopic>(query);
        }

        public async Task<PodcastTopic?> GetTopicBySlugAsync(string slug)
        {
            var query = new ContentItemQueryBuilder()
                .ForContentType(PodcastTopic.CONTENT_TYPE_NAME,
                    q => q.Where(where =>
                        where.WhereEquals(nameof(PodcastTopic.TopicSlug), slug)
                          .Or()
                          .WhereEquals(nameof(PodcastTopic.TopicTitle), slug)).TopN(1));
            var result = await _queryExecutor.GetMappedResult<PodcastTopic>(query);
            return result.FirstOrDefault();
        }
    }
}
