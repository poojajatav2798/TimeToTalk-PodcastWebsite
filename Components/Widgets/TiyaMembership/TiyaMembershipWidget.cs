using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Podcast.Web.Components.Widgets.TiyaMembership;
using Podcast.Web.Models;
using Podcast.Web.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

[assembly: RegisterWidget(
    identifier: TiyaMembershipWidgetViewComponent.IDENTIFIER,
    viewComponentType: typeof(TiyaMembershipWidgetViewComponent),
    name: "Tiya Membership & Pricing",
    propertiesType: typeof(TiyaMembershipProperties),
    Description = "Tiya Golf Club membership pricing tiers comparison table and join form fetched dynamically from Kentico Content Hub",
    IconClass = "xp-dollar")]

[assembly: RegisterWidget(
    identifier: "PodTalk.TiyaMembershipWidget",
    viewComponentType: typeof(TiyaMembershipWidgetViewComponent),
    name: "Tiya Membership & Pricing (Legacy Alias)",
    propertiesType: typeof(TiyaMembershipProperties),
    Description = "Tiya Golf Club membership pricing tiers comparison table and join form fetched dynamically from Kentico Content Hub",
    IconClass = "xp-dollar")]

namespace Podcast.Web.Components.Widgets.TiyaMembership
{
    public class TiyaMembershipViewModel
    {
        public TiyaMembershipProperties Properties { get; set; } = new();
        public List<MembershipTierModel> Tiers { get; set; } = new();
        public List<MembershipFeatureRowModel> Features { get; set; } = new();
        public List<FormFieldModel> FormFields { get; set; } = new();
    }

    public class TiyaMembershipWidgetViewComponent : ViewComponent
    {
        public const string IDENTIFIER = "TiyaGolf.TiyaMembershipWidget";

        private readonly ITiyaContentRepository _contentRepo;

        public TiyaMembershipWidgetViewComponent(ITiyaContentRepository contentRepo)
        {
            _contentRepo = contentRepo;
        }

        public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<TiyaMembershipProperties> widgetModel)
        {
            var tiers = await _contentRepo.GetMembershipTiersAsync();
            var features = await _contentRepo.GetMembershipFeaturesAsync();
            var formFields = await _contentRepo.GetMembershipFormFieldsAsync();

            var model = new TiyaMembershipViewModel
            {
                Properties = widgetModel.Properties,
                Tiers = tiers,
                Features = features,
                FormFields = formFields
            };

            return View("~/Components/Widgets/TiyaMembership/_TiyaMembership.cshtml", model);
        }
    }
}
