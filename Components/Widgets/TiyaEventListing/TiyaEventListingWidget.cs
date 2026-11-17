using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Podcast.Web.Components.Widgets.TiyaEvents;
using Podcast.Web.Components.Widgets.TiyaEventListing;
using Podcast.Web.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

[assembly: RegisterWidget(
    identifier: TiyaEventListingWidgetViewComponent.IDENTIFIER,
    viewComponentType: typeof(TiyaEventListingWidgetViewComponent),
    name: "Tiya Event Listing",
    propertiesType: typeof(TiyaEventListingProperties),
    Description = "Full event listing directory fetched dynamically from Kentico Content Hub with cards, dates, ticket prices, and pagination",
    IconClass = "xp-list")]

[assembly: RegisterWidget(
    identifier: "PodTalk.TiyaEventListingWidget",
    viewComponentType: typeof(TiyaEventListingWidgetViewComponent),
    name: "Tiya Event Listing (Legacy Alias)",
    propertiesType: typeof(TiyaEventListingProperties),
    Description = "Full event listing directory fetched dynamically from Kentico Content Hub with cards, dates, ticket prices, and pagination",
    IconClass = "xp-list")]

namespace Podcast.Web.Components.Widgets.TiyaEventListing
{
    public class TiyaEventListingViewModel
    {
        public TiyaEventListingProperties Properties { get; set; } = new();
        public List<EventItemModel> Events { get; set; } = new();
    }

    public class TiyaEventListingWidgetViewComponent : ViewComponent
    {
        public const string IDENTIFIER = "TiyaGolf.TiyaEventListingWidget";

        private readonly ITiyaContentRepository _contentRepo;

        public TiyaEventListingWidgetViewComponent(ITiyaContentRepository contentRepo)
        {
            _contentRepo = contentRepo;
        }

        public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<TiyaEventListingProperties> widgetModel)
        {
            var properties = widgetModel.Properties;
            var dynamicEvents = await _contentRepo.GetEventsAsync();

            var block = await _contentRepo.GetContentBlockAsync("listing-page-content");
            if (block != null)
            {
                if (!string.IsNullOrWhiteSpace(block.PrimaryButtonText)) properties.HeroTitle = block.PrimaryButtonText;
                if (!string.IsNullOrWhiteSpace(block.Heading)) properties.SectionTitle = block.Heading;
            }

            var model = new TiyaEventListingViewModel
            {
                Properties = properties,
                Events = dynamicEvents
            };

            return View("~/Components/Widgets/TiyaEventListing/_TiyaEventListing.cshtml", model);
        }
    }
}
