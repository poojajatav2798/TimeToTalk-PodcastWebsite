using System.Collections.Generic;

namespace Podcast.Web.Models;

public class HostViewModel
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Bio { get; set; } = "";
    public string Specialties { get; set; } = "";
    public List<string> Badges { get; set; } = new();
    public string ImageUrl { get; set; } = "";
    public string Slug { get; set; } = "";
    public int EpisodeCount { get; set; }
    public int ListenerCount { get; set; }
    public bool IsVerified { get; set; }
    public string TwitterUrl { get; set; } = "";
    public string FacebookUrl { get; set; } = "";
    public string InstagramUrl { get; set; } = "";
    public string LinkedInUrl { get; set; } = "";
    public string PinterestUrl { get; set; } = "";
    public string WhatsAppUrl { get; set; } = "";
    public string YouTubeUrl { get; set; } = "";
    public string PrimaryButtonText { get; set; } = "";
    public string SecondaryButtonText { get; set; } = "";
}
