using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Podcast.Web.Components.Widgets.TiyaEvents;
using Podcast.Web.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

[assembly: RegisterWidget(
    identifier: TiyaEventsWidgetViewComponent.IDENTIFIER,
    viewComponentType: typeof(TiyaEventsWidgetViewComponent),
    name: "Tiya Upcoming Events",
    propertiesType: typeof(TiyaEventsProperties),
    Description = "Upcoming golf tournaments and events fetched dynamically from Kentico Content Hub with dates, locations, ticket pricing, and pagination",
    IconClass = "xp-calendar")]

[assembly: RegisterWidget(
    identifier: "PodTalk.TiyaEventsWidget",
    viewComponentType: typeof(TiyaEventsWidgetViewComponent),
    name: "Tiya Upcoming Events (Legacy Alias)",
    propertiesType: typeof(TiyaEventsProperties),
    Description = "Upcoming golf tournaments and events fetched dynamically from Kentico Content Hub with dates, locations, ticket pricing, and pagination",
    IconClass = "xp-calendar")]

namespace Podcast.Web.Components.Widgets.TiyaEvents
{
    public class TiyaEventsWidgetViewComponent : ViewComponent
    {
        public const string IDENTIFIER = "TiyaGolf.TiyaEventsWidget";

        private readonly ITiyaContentRepository _contentRepo;

        public TiyaEventsWidgetViewComponent(ITiyaContentRepository contentRepo)
        {
            _contentRepo = contentRepo;
        }

        public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<TiyaEventsProperties> widgetModel)
        {
            var properties = widgetModel.Properties;
            List<EventItemModel> dynamicEvents;

            if (properties.SelectedEvents != null && System.Linq.Enumerable.Any(properties.SelectedEvents))
            {
                var guids = System.Linq.Enumerable.Select(properties.SelectedEvents, e => e.Identifier);
                dynamicEvents = await _contentRepo.GetEventsByGuidsAsync(guids);
            }
            else
            {
                dynamicEvents = await _contentRepo.GetEventsAsync();
            }

            var model = new TiyaEventsViewModel
            {
                Properties = properties,
                Events = dynamicEvents
            };

            return View("~/Components/Widgets/TiyaEvents/_TiyaEvents.cshtml", model);
        }
    }
}
