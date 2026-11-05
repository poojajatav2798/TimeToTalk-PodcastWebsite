namespace Podcast.Web.Models;

public class TopicViewModel
{
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public int EpisodeCount { get; set; }
    public string Slug { get; set; } = "";
}
