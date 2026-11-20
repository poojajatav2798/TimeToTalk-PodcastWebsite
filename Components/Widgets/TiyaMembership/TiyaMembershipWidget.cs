using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Podcast.Web.Components.Widgets.TiyaMembership;
using Podcast.Web.Models;
using Podcast.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var props = widgetModel.Properties;

            var tierGuids = props.SelectedTiers?.Select(r => r.Identifier).ToList() ?? new List<Guid>();
            var featureGuids = props.SelectedFeatures?.Select(r => r.Identifier).ToList() ?? new List<Guid>();

            // Support legacy SelectedItems if specific selectors were not populated
            if (!tierGuids.Any() && !featureGuids.Any() && props.SelectedItems != null && props.SelectedItems.Any())
            {
                var legacyGuids = props.SelectedItems.Select(r => r.Identifier).ToList();
                tierGuids = legacyGuids;
                featureGuids = legacyGuids;
            }

            var allActiveTiers = await _contentRepo.GetMembershipTiersAsync();
            List<MembershipTierModel> tiers;
            if (tierGuids.Any())
            {
                var selectedTiers = await _contentRepo.GetMembershipTiersByGuidsAsync(tierGuids);
                tiers = (selectedTiers.Count >= allActiveTiers.Count || (!allActiveTiers.Any() && selectedTiers.Any()))
                    ? selectedTiers
                    : allActiveTiers;
            }
            else
            {
                tiers = allActiveTiers;
            }

            if (!tiers.Any())
            {
                tiers = allActiveTiers;
            }

            List<MembershipFeatureRowModel> features;
            if (featureGuids.Any())
            {
                var selectedFeatures = await _contentRepo.GetMembershipFeaturesByGuidsAsync(featureGuids);
                features = selectedFeatures.Any() ? selectedFeatures : await _contentRepo.GetMembershipFeaturesAsync();
            }
            else
            {
                features = await _contentRepo.GetMembershipFeaturesAsync();
            }

            var formFields = await _contentRepo.GetMembershipFormFieldsAsync();

            var model = new TiyaMembershipViewModel
            {
                Properties = props,
                Tiers = tiers,
                Features = features,
                FormFields = formFields
            };

            return View("~/Components/Widgets/TiyaMembership/_TiyaMembership.cshtml", model);
        }
    }
}
