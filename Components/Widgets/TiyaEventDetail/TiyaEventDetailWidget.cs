using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Podcast.Web.Components.Widgets.TiyaEventDetail;
using Podcast.Web.Services;
using System.Linq;
using System.Threading.Tasks;

[assembly: RegisterWidget(
    identifier: TiyaEventDetailWidgetViewComponent.IDENTIFIER,
    viewComponentType: typeof(TiyaEventDetailWidgetViewComponent),
    name: "Tiya Event Detail",
    propertiesType: typeof(TiyaEventDetailProperties),
    Description = "Event detail showcase fetched dynamically from Kentico Content Hub with banner, info, tickets, and location card",
    IconClass = "xp-article")]

[assembly: RegisterWidget(
    identifier: "PodTalk.TiyaEventDetailWidget",
    viewComponentType: typeof(TiyaEventDetailWidgetViewComponent),
    name: "Tiya Event Detail (Legacy Alias)",
    propertiesType: typeof(TiyaEventDetailProperties),
    Description = "Event detail showcase fetched dynamically from Kentico Content Hub with banner, info, tickets, and location card",
    IconClass = "xp-article")]

namespace Podcast.Web.Components.Widgets.TiyaEventDetail
{
    public class TiyaEventDetailWidgetViewComponent : ViewComponent
    {
        public const string IDENTIFIER = "TiyaGolf.TiyaEventDetailWidget";

        private readonly ITiyaContentRepository _contentRepo;

        public TiyaEventDetailWidgetViewComponent(ITiyaContentRepository contentRepo)
        {
            _contentRepo = contentRepo;
        }

        public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<TiyaEventDetailProperties> widgetModel)
        {
            var properties = widgetModel.Properties;

            try
            {
                var block = await _contentRepo.GetContentBlockAsync("detail-page-content");
                if (block != null && !string.IsNullOrWhiteSpace(block.PrimaryButtonText))
                {
                    properties.HeroTitle = block.PrimaryButtonText;
                }

                var slug = HttpContext.Request.Query["slug"].ToString();
                var eventItem = !string.IsNullOrWhiteSpace(slug)
                    ? await _contentRepo.GetEventBySlugAsync(slug)
                    : (await _contentRepo.GetEventsAsync(1)).FirstOrDefault();

                if (eventItem != null)
                {
                    if (!string.IsNullOrWhiteSpace(eventItem.Title)) properties.EventTitle = eventItem.Title;
                    if (!string.IsNullOrWhiteSpace(eventItem.Description)) properties.Description1 = eventItem.Description;
                    if (!string.IsNullOrWhiteSpace(eventItem.ImageUrl)) properties.ImageUrl = eventItem.ImageUrl;
                    if (!string.IsNullOrWhiteSpace(eventItem.Location)) properties.Location = eventItem.Location;
                    if (!string.IsNullOrWhiteSpace(eventItem.TicketPrice)) properties.TicketPrice = eventItem.TicketPrice;
                    if (!string.IsNullOrWhiteSpace(eventItem.Category)) properties.Subtitle = eventItem.Category;
                    if (!string.IsNullOrWhiteSpace(eventItem.Day) || !string.IsNullOrWhiteSpace(eventItem.MonthYear))
                    {
                        properties.EventDate = $"{eventItem.Day} {eventItem.MonthYear}".Trim();
                    }
                }
            }
            catch
            {
                // Graceful fallback to widget properties
            }

            return View("~/Components/Widgets/TiyaEventDetail/_TiyaEventDetail.cshtml", properties);
        }
    }
}
