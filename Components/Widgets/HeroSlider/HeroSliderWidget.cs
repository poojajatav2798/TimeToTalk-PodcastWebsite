using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using Podcast.Web.Components.Widgets.HeroSlider;
using Podcast.Web.Models;
using Podcast.Web.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[assembly: RegisterWidget(
    HeroSliderWidgetViewComponent.IDENTIFIER,
    typeof(HeroSliderWidgetViewComponent),
    "Hero Banner & Slider",
    typeof(HeroSliderWidgetProperties),
    Description = "Displays the hero headline and creators carousel at the top of the home page.",
    IconClass = "icon-carousel")]

namespace Podcast.Web.Components.Widgets.HeroSlider
{
    public class HeroSliderWidgetProperties : IWidgetProperties
    {
        [TextInputComponent(Label = "Headline", Order = 1)]
        public string Headline { get; set; } = "Listen to Pod Talk";

        [TextInputComponent(Label = "Subheadline", Order = 2)]
        public string Subheadline { get; set; } = "Listen it everywhere. Explore your fav podcasts.";

        [TextInputComponent(Label = "Button Text", Order = 3)]
        public string ButtonText { get; set; } = "Start listening";

        [TextInputComponent(Label = "Button Target URL", Order = 4)]
        public string ButtonUrl { get; set; } = "#section_2";
    }

    public class HeroSliderWidgetViewModel
    {
        public HeroSliderWidgetProperties Properties { get; set; } = new();
        public IEnumerable<HostViewModel> Hosts { get; set; } = new List<HostViewModel>();
    }

    public class HeroSliderWidgetViewComponent : ViewComponent
    {
        public const string IDENTIFIER = "PodTalk.HeroSliderWidget";
        private readonly IHostRepository _hostRepo;

        public HeroSliderWidgetViewComponent(IHostRepository hostRepo)
        {
            _hostRepo = hostRepo;
        }

        public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<HeroSliderWidgetProperties> widgetModel)
        {
            var hostEntities = await _hostRepo.GetAllHostsAsync();
            var hosts = hostEntities?.Select(h => h.ToViewModel()).ToList() ?? new List<HostViewModel>();
            if (!hosts.Any())
            {
                hosts = SampleData.GetHosts();
            }

            var model = new HeroSliderWidgetViewModel
            {
                Properties = widgetModel.Properties,
                Hosts = hosts
            };

            return View("~/Components/Widgets/HeroSlider/_HeroSliderWidget.cshtml", model);
        }
    }
}
