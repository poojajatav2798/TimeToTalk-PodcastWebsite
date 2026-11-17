using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Podcast.Web.Components.Widgets.TiyaNewsletter;
using Podcast.Web.Services;
using System.Threading.Tasks;

[assembly: RegisterWidget(
    identifier: TiyaNewsletterWidgetViewComponent.IDENTIFIER,
    viewComponentType: typeof(TiyaNewsletterWidgetViewComponent),
    name: "Tiya Newsletter",
    propertiesType: typeof(TiyaNewsletterProperties),
    Description = "Tiya Golf Club newsletter subscription form",
    IconClass = "xp-envelope")]

[assembly: RegisterWidget(
    identifier: "PodTalk.NewsletterWidget",
    viewComponentType: typeof(TiyaNewsletterWidgetViewComponent),
    name: "Tiya Newsletter (Legacy Alias)",
    propertiesType: typeof(TiyaNewsletterProperties),
    Description = "Tiya Golf Club newsletter subscription form",
    IconClass = "xp-envelope")]

namespace Podcast.Web.Components.Widgets.TiyaNewsletter
{
    public class TiyaNewsletterWidgetViewComponent : ViewComponent
    {
        public const string IDENTIFIER = "TiyaGolf.NewsletterWidget";

        private readonly ITiyaContentRepository _contentRepo;

        public TiyaNewsletterWidgetViewComponent(ITiyaContentRepository contentRepo)
        {
            _contentRepo = contentRepo;
        }

        public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<TiyaNewsletterProperties> widgetModel)
        {
            var properties = widgetModel.Properties;

            var block = await _contentRepo.GetContentBlockAsync("home-newsletter");
            if (block != null)
            {
                if (!string.IsNullOrWhiteSpace(block.Heading)) properties.SectionHeading = block.Heading;
                if (!string.IsNullOrWhiteSpace(block.Description)) properties.Description = block.Description;
                if (!string.IsNullOrWhiteSpace(block.PrimaryButtonText)) properties.ButtonText = block.PrimaryButtonText;
            }

            return View("~/Components/Widgets/TiyaNewsletter/_TiyaNewsletter.cshtml", properties);
        }
    }
}
