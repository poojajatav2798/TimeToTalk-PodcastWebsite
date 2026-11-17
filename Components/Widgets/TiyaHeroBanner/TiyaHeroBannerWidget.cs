using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Podcast.Web.Components.Widgets.TiyaHeroBanner;
using Podcast.Web.Services;
using System.Linq;
using System.Threading.Tasks;

[assembly: RegisterWidget(
    identifier: TiyaHeroBannerWidgetViewComponent.IDENTIFIER,
    viewComponentType: typeof(TiyaHeroBannerWidgetViewComponent),
    name: "Tiya Hero Banner",
    propertiesType: typeof(TiyaHeroBannerProperties),
    Description = "Hero banner with animated headline, call-to-action buttons, and video modal",
    IconClass = "xp-ribbon")]

[assembly: RegisterWidget(
    identifier: "PodTalk.HeroBannerWidget",
    viewComponentType: typeof(TiyaHeroBannerWidgetViewComponent),
    name: "Tiya Hero Banner (Legacy Alias)",
    propertiesType: typeof(TiyaHeroBannerProperties),
    Description = "Hero banner with animated headline, call-to-action buttons, and video modal",
    IconClass = "xp-ribbon")]

namespace Podcast.Web.Components.Widgets.TiyaHeroBanner
{
    public class TiyaHeroBannerWidgetViewComponent : ViewComponent
    {
        public const string IDENTIFIER = "TiyaGolf.HeroBannerWidget";

        private readonly ITiyaContentRepository _contentRepo;

        public TiyaHeroBannerWidgetViewComponent(ITiyaContentRepository contentRepo)
        {
            _contentRepo = contentRepo;
        }

        public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<TiyaHeroBannerProperties> widgetModel)
        {
            var properties = widgetModel.Properties;

            // 1. If editor selected a specific banner block via ContentItemSelectorComponent
            TimeToTalk.WebsiteContentBlock? block = null;
            if (properties.SelectedBanner != null && properties.SelectedBanner.Any())
            {
                var bannerGuid = properties.SelectedBanner.First().Identifier;
                block = await _contentRepo.GetContentBlockByGuidAsync(bannerGuid);
            }

            // 2. Fallback to default Content Hub item 'home-hero-banner'
            if (block == null)
            {
                block = await _contentRepo.GetContentBlockAsync("home-hero-banner");
            }

            if (block != null)
            {
                if (!string.IsNullOrWhiteSpace(block.Heading)) properties.HeroTitle = block.Heading.Trim();
                if (!string.IsNullOrWhiteSpace(block.Description)) properties.AnimatedWords = block.Description.Trim();
                if (!string.IsNullOrWhiteSpace(block.PrimaryButtonText)) properties.Button1Text = block.PrimaryButtonText.Trim();
                
                if (!string.IsNullOrWhiteSpace(block.PrimaryButtonURL))
                {
                    var pUrl = block.PrimaryButtonURL.Trim();
                    if (pUrl.Contains("youtube.com", System.StringComparison.OrdinalIgnoreCase) || pUrl.Contains("youtu.be", System.StringComparison.OrdinalIgnoreCase))
                    {
                        properties.VideoUrl = pUrl;
                    }
                    else
                    {
                        properties.Button1Link = pUrl;
                    }
                }

                if (!string.IsNullOrWhiteSpace(block.SecondaryButtonText)) properties.Button2Text = block.SecondaryButtonText.Trim();
                if (!string.IsNullOrWhiteSpace(block.SecondaryButtonURL)) properties.Button2Link = block.SecondaryButtonURL.Trim();
                
                // IconName can hold subtitle, title connector, and video url (separated by '|')
                if (!string.IsNullOrWhiteSpace(block.IconName))
                {
                    var parts = block.IconName.Split('|', System.StringSplitOptions.TrimEntries);
                    if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0])) properties.Subtitle = parts[0];
                    if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])) properties.TitleConnector = parts[1];
                    if (parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2])) properties.VideoUrl = parts[2];
                }

                if (!string.IsNullOrEmpty(block.Image?.Url))
                {
                    properties.BackgroundImageUrl = block.Image.Url;
                }
            }

            properties.VideoUrl = NormalizeYouTubeUrl(properties.VideoUrl);

            return View("~/Components/Widgets/TiyaHeroBanner/_TiyaHeroBanner.cshtml", properties);
        }

        private static string NormalizeYouTubeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "https://www.youtube.com/embed/MGNgbAGK6s8";
            url = url.Trim();
            if (url.Contains("watch?v="))
            {
                var vIndex = url.IndexOf("watch?v=") + 8;
                var ampIndex = url.IndexOf('&', vIndex);
                var id = ampIndex > vIndex ? url.Substring(vIndex, ampIndex - vIndex) : url.Substring(vIndex);
                return $"https://www.youtube.com/embed/{id}";
            }
            if (url.Contains("youtu.be/"))
            {
                var id = url.Substring(url.IndexOf("youtu.be/") + 9).Split('?')[0];
                return $"https://www.youtube.com/embed/{id}";
            }
            return url;
        }
    }
}
