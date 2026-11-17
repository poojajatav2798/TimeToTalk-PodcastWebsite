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

            var block = await _contentRepo.GetContentBlockAsync("home-hero-banner");
            if (block != null)
            {
                if (!string.IsNullOrWhiteSpace(block.Heading)) properties.HeroTitle = block.Heading;
                if (!string.IsNullOrWhiteSpace(block.Description)) properties.AnimatedWords = block.Description;
                if (!string.IsNullOrWhiteSpace(block.PrimaryButtonText)) properties.Button1Text = block.PrimaryButtonText;
                if (!string.IsNullOrWhiteSpace(block.PrimaryButtonURL)) properties.VideoUrl = block.PrimaryButtonURL;
                if (!string.IsNullOrWhiteSpace(block.IconName)) properties.Button2Text = block.IconName;
            }

            return View("~/Components/Widgets/TiyaHeroBanner/_TiyaHeroBanner.cshtml", properties);
        }
    }
}
