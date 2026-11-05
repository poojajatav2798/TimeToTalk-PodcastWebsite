using System;
using System.Collections.Generic;

namespace Podcast.Web.Models
{
    public static class SampleData
    {
        public static List<EpisodeViewModel> GetEpisodes() => new()
        {
            new()
            {
                Id = 1, EpisodeNumber = 1,
                Title = "Getting Started with Mindfulness",
                ShortDescription = "A beginner's guide to mindfulness and meditation.",
                Description = "In this episode, Anna walks you through the basics of mindfulness, breathing techniques, and how to build a daily meditation habit — even if you only have 5 minutes a day.",
                ThumbnailUrl = "/images/podcast/27376480_7326766.jpg",
                AudioUrl = "#",
                DurationMinutes = 32,
                PublishDate = new DateTime(2024, 10, 1),
                HostName = "Anna Torres",
                HostTitle = "Host",
                HostImageUrl = "/images/profile/cute-smiling-woman-outdoor-portrait.jpg",
                HostIsVerified = true,
                Topic = "Mindfulness",
                IsTrending = true,
                ListensCount = "120k",
                LikesCount = "4.2k",
                CommentsCount = "148",
                DownloadsCount = "35k"
            },
            new()
            {
                Id = 2, EpisodeNumber = 2,
                Title = "The Future of AI in Everyday Life",
                ShortDescription = "How artificial intelligence is reshaping our daily routines.",
                Description = "James Lee explores how AI tools are changing how we work, communicate, and create — and what skills you'll need to stay relevant in the next decade.",
                ThumbnailUrl = "/images/podcast/11683425_4790593.jpg",
                AudioUrl = "#",
                DurationMinutes = 45,
                PublishDate = new DateTime(2024, 10, 8),
                HostName = "James Lee",
                HostTitle = "Creator",
                HostImageUrl = "/images/profile/handsome-asian-man-listening-music-through-headphones.jpg",
                HostIsVerified = true,
                Topic = "Technology",
                IsTrending = true,
                ListensCount = "95k",
                LikesCount = "3.8k",
                CommentsCount = "112",
                DownloadsCount = "28k"
            },
            new()
            {
                Id = 3, EpisodeNumber = 3,
                Title = "Cooking for the Soul",
                ShortDescription = "Simple recipes that bring joy and health to your kitchen.",
                Description = "Maria shares her favourite comfort recipes and the stories behind them, proving that great food does not need to be complicated.",
                ThumbnailUrl = "/images/podcast/12577967_02.jpg",
                AudioUrl = "#",
                DurationMinutes = 28,
                PublishDate = new DateTime(2024, 10, 15),
                HostName = "Maria Chen",
                HostTitle = "Chef & Host",
                HostImageUrl = "/images/profile/woman-posing-black-dress-medium-shot.jpg",
                HostIsVerified = false,
                Topic = "Cooking",
                IsTrending = false,
                ListensCount = "64k",
                LikesCount = "2.1k",
                CommentsCount = "89",
                DownloadsCount = "19k"
            },
            new()
            {
                Id = 4, EpisodeNumber = 4,
                Title = "Productivity Hacks for Remote Workers",
                ShortDescription = "Proven strategies to stay focused while working from home.",
                Description = "David Kim breaks down the systems, tools, and mindset shifts that the most productive remote workers use every single day.",
                ThumbnailUrl = "/images/podcast/27670664_7369753.jpg",
                AudioUrl = "#",
                DurationMinutes = 38,
                PublishDate = new DateTime(2024, 10, 22),
                HostName = "David Kim",
                HostTitle = "Strategist",
                HostImageUrl = "/images/profile/man-portrait.jpg",
                HostIsVerified = true,
                Topic = "Productivity",
                IsTrending = true,
                ListensCount = "142k",
                LikesCount = "5.6k",
                CommentsCount = "230",
                DownloadsCount = "48k"
            },
        };

        public static List<HostViewModel> GetHosts() => new()
        {
            new() { Name = "Anna Torres",  Category = "Mindfulness", EpisodeCount = 18, ListenerCount = 4200, IsVerified = true,
                    Badges = new() { "Mindfulness", "Yoga" },
                    ImageUrl = "/images/profile/cute-smiling-woman-outdoor-portrait.jpg" },
            new() { Name = "James Lee",    Category = "Technology",  EpisodeCount = 24, ListenerCount = 7100, IsVerified = true,
                    Badges = new() { "Technology", "AI" },
                    ImageUrl = "/images/profile/handsome-asian-man-listening-music-through-headphones.jpg" },
            new() { Name = "Maria Chen",   Category = "Cooking",     EpisodeCount = 12, ListenerCount = 3400, IsVerified = false,
                    Badges = new() { "Cooking", "Culinary" },
                    ImageUrl = "/images/profile/woman-posing-black-dress-medium-shot.jpg" },
            new() { Name = "David Kim",    Category = "Productivity",EpisodeCount = 30, ListenerCount = 9800, IsVerified = true,
                    Badges = new() { "Productivity", "Strategy" },
                    ImageUrl = "/images/profile/man-portrait.jpg" },
        };

        public static List<TopicViewModel> GetTopics() => new()
        {
            new() { Name = "Mindfulness", Title = "Mindfulness", Slug = "mindfulness", EpisodeCount = 6,
                    ImageUrl = "/images/topics/woman-practicing-yoga-mat-home.jpg" },
            new() { Name = "Cooking",     Title = "Cooking",     Slug = "cooking",     EpisodeCount = 4,
                    ImageUrl = "/images/topics/delicious-meal-with-sambal-arrangement.jpg" },
            new() { Name = "Health",      Title = "Health",      Slug = "health",      EpisodeCount = 8,
                    ImageUrl = "/images/topics/physician-consulting-his-patient-clinic.jpg" },
            new() { Name = "Technology",  Title = "Technology",  Slug = "technology",  EpisodeCount = 12,
                    ImageUrl = "/images/topics/repairman-doing-air-conditioner-service.jpg" },
        };
    }
}
