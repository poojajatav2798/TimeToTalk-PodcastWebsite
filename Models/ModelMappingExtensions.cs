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
                badges.AddRange(host.HostSpecialties.Split(new[] { ',', ';', '|', '&', '/' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
            }
            else if (!string.IsNullOrWhiteSpace(host.HostRole))
            {
                badges.AddRange(host.HostRole.Split(new[] { ',', ';', '|', '&', '/' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
            }

            string slug = !string.IsNullOrWhiteSpace(host.HostSlug)
                ? host.HostSlug
                : (host.HostName?.ToLowerInvariant().Trim().Replace(" ", "-") ?? "host");

            // Ensure rich skills per host as in the real podcast website
            if (!badges.Any())
            {
                var n = (host.HostName ?? "").ToLowerInvariant();
                if (n.Contains("candice"))
                {
                    badges.Add("Storytelling");
                    badges.Add("Business");
                }
                else if (n.Contains("william"))
                {
                    badges.Add("Creative");
                    badges.Add("Design");
                }
                else if (n.Contains("chan"))
                {
                    badges.Add("Education");
                    badges.Add("Technology");
                }
                else if (n.Contains("elsa"))
                {
                    badges.Add("Influencer");
                    badges.Add("Design");
                }
                else if (n.Contains("taylor") || n.Contains("anna"))
                {
                    badges.Add("Modeling");
                    badges.Add("Fashion");
                }
                else if (n.Contains("nick") || n.Contains("david"))
                {
                    badges.Add("Acting");
                }
                else
                {
                    badges.Add("Storytelling");
                }
            }

            string defaultHostImage = slug switch
            {
                "candice-wu" => "/images/profile/cute-smiling-woman-outdoor-portrait.jpg",
                "chan" => "/images/profile/handsome-asian-man-listening-music-through-headphones.jpg",
                "william" => "/images/profile/smart-attractive-asian-glasses-male-standing-smile-with-freshness-joyful-casual-blue-shirt-portrait-white-background.jpg",
                "elsa" => "/images/profile/smiling-business-woman-with-folded-hands-against-white-wall-toothy-smile-crossed-arms.jpg",
                _ => (host.HostName?.ToLowerInvariant() switch
                {
                    var n when n != null && n.Contains("candice") => "/images/profile/cute-smiling-woman-outdoor-portrait.jpg",
                    var n when n != null && n.Contains("chan") => "/images/profile/handsome-asian-man-listening-music-through-headphones.jpg",
                    var n when n != null && n.Contains("william") => "/images/profile/smart-attractive-asian-glasses-male-standing-smile-with-freshness-joyful-casual-blue-shirt-portrait-white-background.jpg",
                    var n when n != null && n.Contains("elsa") => "/images/profile/smiling-business-woman-with-folded-hands-against-white-wall-toothy-smile-crossed-arms.jpg",
                    var n when n != null && n.Contains("anna") => "/images/profile/cute-smiling-woman-outdoor-portrait.jpg",
                    var n when n != null && n.Contains("james") => "/images/profile/handsome-asian-man-listening-music-through-headphones.jpg",
                    var n when n != null && n.Contains("maria") => "/images/profile/woman-posing-black-dress-medium-shot.jpg",
                    var n when n != null && n.Contains("david") => "/images/profile/man-portrait.jpg",
                    _ => "/images/profile/cute-smiling-woman-outdoor-portrait.jpg"
                })
            };

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
                    : defaultHostImage,
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
            string hostName = host?.HostName ?? "";
            string hostRole = host?.HostRole ?? "";
            string defaultHostThumb;
            bool hostVerified = host?.HostIsVerified ?? false;

            if (host != null)
            {
                defaultHostThumb = hostName.ToLowerInvariant() switch
                {
                    var n when n.Contains("candice") => "/images/profile/cute-smiling-woman-outdoor-portrait.jpg",
                    var n when n.Contains("chan") => "/images/profile/handsome-asian-man-listening-music-through-headphones.jpg",
                    var n when n.Contains("william") => "/images/profile/smart-attractive-asian-glasses-male-standing-smile-with-freshness-joyful-casual-blue-shirt-portrait-white-background.jpg",
                    var n when n.Contains("elsa") => "/images/profile/smiling-business-woman-with-folded-hands-against-white-wall-toothy-smile-crossed-arms.jpg",
                    var n when n.Contains("anna") => "/images/profile/cute-smiling-woman-outdoor-portrait.jpg",
                    var n when n.Contains("james") => "/images/profile/handsome-asian-man-listening-music-through-headphones.jpg",
                    var n when n.Contains("maria") => "/images/profile/woman-posing-black-dress-medium-shot.jpg",
                    var n when n.Contains("david") => "/images/profile/man-portrait.jpg",
                    _ => "/images/profile/cute-smiling-woman-outdoor-portrait.jpg"
                };
            }
            else
            {
                (hostName, hostRole, defaultHostThumb, hostVerified) = (episode.EpisodeNumber % 4) switch
                {
                    1 => ("Candice Wu", "Lead Host", "/images/profile/cute-smiling-woman-outdoor-portrait.jpg", true),
                    2 => ("Chan", "Sound Engineer & Host", "/images/profile/handsome-asian-man-listening-music-through-headphones.jpg", true),
                    3 => ("Elsa", "Creative Director", "/images/profile/smiling-business-woman-with-folded-hands-against-white-wall-toothy-smile-crossed-arms.jpg", true),
                    _ => ("William", "Culture Producer", "/images/profile/smart-attractive-asian-glasses-male-standing-smile-with-freshness-joyful-casual-blue-shirt-portrait-white-background.jpg", true)
                };
            }

            string hostImage = host?.HostPhoto?.Url ?? defaultHostThumb;

            string slug = !string.IsNullOrWhiteSpace(episode.EpisodeSlug)
                ? episode.EpisodeSlug
                : (!string.IsNullOrWhiteSpace(episode.EpisodeTopicSlug)
                    ? episode.EpisodeTopicSlug
                    : (episode.SystemFields != null && !string.IsNullOrWhiteSpace(episode.SystemFields.ContentItemName)
                        ? episode.SystemFields.ContentItemName
                        : $"episode-{episode.EpisodeNumber}"));

            string defaultEpisodeImage = slug switch
            {
                "modern-apartment" => "/images/podcast/11683425_4790593.jpg",
                "vintage-timeless" => "/images/podcast/12577967_02.jpg",
                "creative-mindset" => "/images/podcast/27376480_7326766.jpg",
                "ai-future" => "/images/podcast/27670664_7369753.jpg",
                _ => (episode.EpisodeNumber switch
                {
                    1 => "/images/podcast/27376480_7326766.jpg",
                    2 => "/images/podcast/11683425_4790593.jpg",
                    3 => "/images/podcast/12577967_02.jpg",
                    4 => "/images/podcast/27670664_7369753.jpg",
                    _ => "/images/podcast/11683425_4790593.jpg"
                })
            };

            string thumbnailUrl = !string.IsNullOrEmpty(episode.EpisodeThumbnail?.Url)
                ? episode.EpisodeThumbnail.Url
                : defaultEpisodeImage;

            string audioUrl = !string.IsNullOrEmpty(episode.AudioUrl?.Url)
                ? episode.AudioUrl.Url
                : "#";

            string topic = !string.IsNullOrWhiteSpace(episode.EpisodeTopicName) 
                ? episode.EpisodeTopicName 
                : (!string.IsNullOrWhiteSpace(episode.EpisodeTopics) ? episode.EpisodeTopics : "General");

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

            string defaultTopicImage = slug switch
            {
                "productivity" => "/images/topics/physician-consulting-his-patient-clinic.jpg",
                "technic" => "/images/topics/repairman-doing-air-conditioner-service.jpg",
                "mindfullness" or "mindfulness" => "/images/topics/woman-practicing-yoga-mat-home.jpg",
                "design" => "/images/topics/delicious-meal-with-sambal-arrangement.jpg",
                "cooking" => "/images/topics/delicious-meal-with-sambal-arrangement.jpg",
                "health" => "/images/topics/physician-consulting-his-patient-clinic.jpg",
                "technology" => "/images/topics/repairman-doing-air-conditioner-service.jpg",
                _ => (topic.TopicTitle?.ToLowerInvariant() switch
                {
                    var t when t != null && t.Contains("product") => "/images/topics/physician-consulting-his-patient-clinic.jpg",
                    var t when t != null && (t.Contains("tech") || t.Contains("air")) => "/images/topics/repairman-doing-air-conditioner-service.jpg",
                    var t when t != null && (t.Contains("mind") || t.Contains("yoga")) => "/images/topics/woman-practicing-yoga-mat-home.jpg",
                    var t when t != null && (t.Contains("design") || t.Contains("cook")) => "/images/topics/delicious-meal-with-sambal-arrangement.jpg",
                    _ => "/images/topics/physician-consulting-his-patient-clinic.jpg"
                })
            };

            return new TopicViewModel
            {
                Name = topic.TopicTitle ?? "Topic",
                Title = topic.TopicTitle ?? "Topic",
                Description = topic.TopicDescription ?? "",
                Slug = slug,
                ImageUrl = !string.IsNullOrEmpty(topic.TopicImageUrl?.Url) 
                    ? topic.TopicImageUrl.Url 
                    : defaultTopicImage,
                EpisodeCount = episodeCount
            };
        }
    }
}
