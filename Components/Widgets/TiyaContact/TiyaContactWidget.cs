using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Podcast.Web.Components.Widgets.TiyaContact;
using Podcast.Web.Services;
using System.Threading.Tasks;

[assembly: RegisterWidget(
    identifier: TiyaContactWidgetViewComponent.IDENTIFIER,
    viewComponentType: typeof(TiyaContactWidgetViewComponent),
    name: "Tiya Contact & Map",
    propertiesType: typeof(TiyaContactProperties),
    Description = "Tiya Golf Club contact form with address card, phone, directions link, and WorldMap SVG",
    IconClass = "xp-envelope")]

[assembly: RegisterWidget(
    identifier: "PodTalk.TiyaContactWidget",
    viewComponentType: typeof(TiyaContactWidgetViewComponent),
    name: "Tiya Contact & Map (Legacy Alias)",
    propertiesType: typeof(TiyaContactProperties),
    Description = "Tiya Golf Club contact form with address card, phone, directions link, and WorldMap SVG",
    IconClass = "xp-envelope")]

namespace Podcast.Web.Components.Widgets.TiyaContact
{
    public class TiyaContactViewModel
    {
        public TiyaContactProperties Properties { get; set; } = new();
    }

    public class TiyaContactWidgetViewComponent : ViewComponent
    {
        public const string IDENTIFIER = "TiyaGolf.TiyaContactWidget";

        private readonly ITiyaContentRepository _contentRepo;

        public TiyaContactWidgetViewComponent(ITiyaContentRepository contentRepo)
        {
            _contentRepo = contentRepo;
        }

        public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<TiyaContactProperties> widgetModel)
        {
            var properties = widgetModel.Properties;

            var block = await _contentRepo.GetContentBlockAsync("home-contact-info");
            if (block != null)
            {
                if (!string.IsNullOrWhiteSpace(block.Heading)) properties.Heading = block.Heading;
                if (!string.IsNullOrWhiteSpace(block.Description)) properties.LocationName = block.Description;
                if (!string.IsNullOrWhiteSpace(block.IconName))
                {
                    properties.PhoneDisplay = block.IconName;
                    properties.PhoneNumber = block.IconName;
                }
                if (!string.IsNullOrWhiteSpace(block.PrimaryButtonText)) properties.Email = block.PrimaryButtonText;
            }

            var model = new TiyaContactViewModel
            {
                Properties = properties
            };

            return View("~/Components/Widgets/TiyaContact/_TiyaContact.cshtml", model);
        }
    }
}
