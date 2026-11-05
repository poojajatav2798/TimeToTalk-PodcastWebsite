#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using TimeToTalk;

namespace Podcast.Web.Models
{
    public static class ModelMappingExtensions
    {
        public static HostViewModel ToViewModel(this PodcastHost host)
        {
            if (host == null) return new HostViewModel();

            var badges = new List<string>();
            if (!string.IsNullOrWhiteSpace(host.HostSpecialties))
            {
                badges.AddRange(host.HostSpecialties.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
            }
            else if (!string.IsNullOrWhiteSpace(host.HostRole))
            {
                badges.AddRange(host.HostRole.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
            }

            string slug = !string.IsNullOrWhiteSpace(host.HostSlug)
                ? host.HostSlug
                : (host.HostName?.ToLowerInvariant().Trim().Replace(" ", "-") ?? "host");

            return new HostViewModel
            {
                Name = host.HostName ?? "Host",
                Category = host.HostRole ?? "Creator",
                Bio = host.HostBiography ?? host.HostSpecialties ?? "",
                Specialties = host.HostSpecialties ?? "",
                Badges = badges,
                Slug = slug,
                ImageUrl = !string.IsNullOrEmpty(host.HostPhoto?.Url) 
                    ? host.HostPhoto.Url 
                    : "/images/profile/cute-smiling-woman-outdoor-portrait.jpg",
                IsVerified = host.HostIsVerified,
                TwitterUrl = !string.IsNullOrEmpty(host.HostTwitterURL) ? host.HostTwitterURL : "#",
                FacebookUrl = !string.IsNullOrEmpty(host.HostFacebookURL) ? host.HostFacebookURL : "#",
                InstagramUrl = !string.IsNullOrEmpty(host.HostInstagramURL) ? host.HostInstagramURL : "#",
                LinkedInUrl = !string.IsNullOrEmpty(host.HostLinkedInURL) ? host.HostLinkedInURL : "#",
                PinterestUrl = !string.IsNullOrEmpty(host.HostPinterestUrl) ? host.HostPinterestUrl : "#",
                WhatsAppUrl = !string.IsNullOrEmpty(host.HostWhatsAppUrl) ? host.HostWhatsAppUrl : "#",
                YouTubeUrl = !string.IsNullOrEmpty(host.HostYouTubeUrl) ? host.HostYouTubeUrl : "#",
                PrimaryButtonText = host.HostPrimaryButtonText ?? "",
                SecondaryButtonText = host.HostSecondaryButtonText ?? ""
            };
        }

        public static EpisodeViewModel ToViewModel(this PodcastEpisode episode)
        {
            if (episode == null) return new EpisodeViewModel();

            var host = episode.EpisodeHost?.FirstOrDefault();
            string hostName = host?.HostName ?? "Pod Talk Host";
            string hostRole = host?.HostRole ?? "Host";
            string hostImage = host?.HostPhoto?.Url ?? "/images/profile/cute-smiling-woman-outdoor-portrait.jpg";
            bool hostVerified = host?.HostIsVerified ?? false;

            string thumbnailUrl = !string.IsNullOrEmpty(episode.EpisodeThumbnail?.Url)
                ? episode.EpisodeThumbnail.Url
                : "/images/podcast/27376480_7326766.jpg";

            string audioUrl = !string.IsNullOrEmpty(episode.AudioUrl?.Url)
                ? episode.AudioUrl.Url
                : "#";

            string topic = !string.IsNullOrWhiteSpace(episode.EpisodeTopicName) 
                ? episode.EpisodeTopicName 
                : (!string.IsNullOrWhiteSpace(episode.EpisodeTopics) ? episode.EpisodeTopics : "General");

            string slug = !string.IsNullOrWhiteSpace(episode.EpisodeSlug)
                ? episode.EpisodeSlug
                : (!string.IsNullOrWhiteSpace(episode.EpisodeTopicSlug)
                    ? episode.EpisodeTopicSlug
                    : (episode.SystemFields != null && !string.IsNullOrWhiteSpace(episode.SystemFields.ContentItemName)
                        ? episode.SystemFields.ContentItemName
                        : $"episode-{episode.EpisodeNumber}"));

            return new EpisodeViewModel
            {
                Id = episode.SystemFields?.ContentItemID ?? episode.EpisodeNumber,
                EpisodeNumber = episode.EpisodeNumber,
                Title = episode.EpisodeTitle ?? "Untitled Episode",
                Slug = slug,
                ShortDescription = episode.EpisodeShortDescription ?? "",
                Description = episode.EpisodeDescription ?? episode.EpisodeShortDescription ?? "",
                ThumbnailUrl = thumbnailUrl,
                AudioUrl = audioUrl,
                DurationMinutes = episode.EpisodeDurationMinutes > 0 ? episode.EpisodeDurationMinutes : 30,
                PublishDate = episode.PublishDate != default ? episode.PublishDate : DateTime.Now,
                HostName = hostName,
                HostTitle = hostRole,
                HostImageUrl = hostImage,
                HostIsVerified = hostVerified,
                Topic = topic,
                IsTrending = episode.EpisodeIsTrending,
                ListensCount = !string.IsNullOrWhiteSpace(episode.EpisodeListensCount) ? episode.EpisodeListensCount : "120k",
                LikesCount = !string.IsNullOrWhiteSpace(episode.EpisodeLikesCount) ? episode.EpisodeLikesCount : "4.5k",
                CommentsCount = !string.IsNullOrWhiteSpace(episode.EpisodeCommentsCount) ? episode.EpisodeCommentsCount : "148",
                DownloadsCount = !string.IsNullOrWhiteSpace(episode.EpisodeDownloadsCount) ? episode.EpisodeDownloadsCount : "35k"
            };
        }

        public static TopicViewModel ToViewModel(this PodcastTopic topic)
        {
            if (topic == null) return new TopicViewModel();

            int episodeCount = topic.TopicEpisodeCount > 0 ? topic.TopicEpisodeCount : 10;
            if (topic.TopicEpisodeCount <= 0 && !string.IsNullOrWhiteSpace(topic.EpisodeCountLabel))
            {
                var digits = System.Text.RegularExpressions.Regex.Match(topic.EpisodeCountLabel, @"\d+");
                if (digits.Success && int.TryParse(digits.Value, out var parsed))
                {
                    episodeCount = parsed;
                }
            }

            string slug = !string.IsNullOrWhiteSpace(topic.TopicSlug)
                ? topic.TopicSlug
                : (topic.TopicTitle?.ToLowerInvariant().Trim().Replace(" ", "-") ?? "topic");

            return new TopicViewModel
            {
                Name = topic.TopicTitle ?? "Topic",
                Title = topic.TopicTitle ?? "Topic",
                Description = topic.TopicDescription ?? "",
                Slug = slug,
                ImageUrl = !string.IsNullOrEmpty(topic.TopicImageUrl?.Url) 
                    ? topic.TopicImageUrl.Url 
                    : "/images/topics/woman-practicing-yoga-mat-home.jpg",
                EpisodeCount = episodeCount
            };
        }
    }
}
