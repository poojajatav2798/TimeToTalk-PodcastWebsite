using System;

namespace Podcast.Web.Models;

public class EpisodeViewModel
{
    public int Id { get; set; }
    public int EpisodeNumber { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string ShortDescription { get; set; } = "";
    public string Description { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";
    public string AudioUrl { get; set; } = "";
    public int DurationMinutes { get; set; }
    public DateTime PublishDate { get; set; }
    public string HostName { get; set; } = "";
    public string HostTitle { get; set; } = "";
    public string HostImageUrl { get; set; } = "";
    public string Topic { get; set; } = "";
    public string ListensCount { get; set; } = "";
    public string LikesCount { get; set; } = "";
    public string CommentsCount { get; set; } = "";
    public string DownloadsCount { get; set; } = "";
    public bool IsTrending { get; set; }
    public bool HostIsVerified { get; set; }
}