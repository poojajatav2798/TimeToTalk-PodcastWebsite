using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Podcast.Web.Components.Widgets.TiyaAbout;
using Podcast.Web.Models;
using Podcast.Web.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[assembly: RegisterWidget(
    identifier: TiyaAboutWidgetViewComponent.IDENTIFIER,
    viewComponentType: typeof(TiyaAboutWidgetViewComponent),
    name: "Tiya About & History",
    propertiesType: typeof(TiyaAboutProperties),
    Description = "Tiya Golf Club history and founder/co-founder leadership team cards fetched dynamically from Kentico Content Hub",
    IconClass = "xp-users")]

[assembly: RegisterWidget(
    identifier: "PodTalk.TiyaAboutWidget",
    viewComponentType: typeof(TiyaAboutWidgetViewComponent),
    name: "Tiya About & History (Legacy Alias)",
    propertiesType: typeof(TiyaAboutProperties),
    Description = "Tiya Golf Club history and founder/co-founder leadership team cards fetched dynamically from Kentico Content Hub",
    IconClass = "xp-users")]

namespace Podcast.Web.Components.Widgets.TiyaAbout
{
    public class TiyaAboutViewModel
    {
        public TiyaAboutProperties Properties { get; set; } = new();
        public List<TeamMemberModel> TeamMembers { get; set; } = new();
    }

    public class TiyaAboutWidgetViewComponent : ViewComponent
    {
        public const string IDENTIFIER = "TiyaGolf.TiyaAboutWidget";

        private readonly ITiyaContentRepository _contentRepo;

        public TiyaAboutWidgetViewComponent(ITiyaContentRepository contentRepo)
        {
            _contentRepo = contentRepo;
        }

        public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<TiyaAboutProperties> widgetModel)
        {
            var properties = widgetModel.Properties;
            var team = await _contentRepo.GetTeamMembersAsync();

            var historyBlock = await _contentRepo.GetContentBlockAsync("tiya-history-block");
            if (historyBlock != null)
            {
                if (!string.IsNullOrWhiteSpace(historyBlock.Heading)) properties.HistoryHeading = historyBlock.Heading;
                if (!string.IsNullOrWhiteSpace(historyBlock.Description)) properties.HistoryParagraph1 = historyBlock.Description;
                if (!string.IsNullOrWhiteSpace(historyBlock.PrimaryButtonURL)) properties.HistoryParagraph2 = historyBlock.PrimaryButtonURL;
                if (!string.IsNullOrWhiteSpace(historyBlock.PrimaryButtonText)) properties.SectionTitle = historyBlock.PrimaryButtonText;
            }

            if (team.Any())
            {
                var m1 = team.ElementAtOrDefault(0);
                if (m1 != null)
                {
                    if (!string.IsNullOrWhiteSpace(m1.Name)) properties.Member1Name = m1.Name;
                    if (!string.IsNullOrWhiteSpace(m1.Role)) properties.Member1Role = m1.Role;
                    if (!string.IsNullOrWhiteSpace(m1.ImageUrl)) properties.Member1ImageUrl = m1.ImageUrl;
                    if (!string.IsNullOrWhiteSpace(m1.TwitterUrl)) properties.Member1TwitterUrl = m1.TwitterUrl;
                    if (!string.IsNullOrWhiteSpace(m1.WhatsAppUrl)) properties.Member1WhatsAppUrl = m1.WhatsAppUrl;
                }

                var m2 = team.ElementAtOrDefault(1);
                if (m2 != null)
                {
                    if (!string.IsNullOrWhiteSpace(m2.Name)) properties.Member2Name = m2.Name;
                    if (!string.IsNullOrWhiteSpace(m2.Role)) properties.Member2Role = m2.Role;
                    if (!string.IsNullOrWhiteSpace(m2.ImageUrl)) properties.Member2ImageUrl = m2.ImageUrl;
                    if (!string.IsNullOrWhiteSpace(m2.LinkedInUrl)) properties.Member2LinkedInUrl = m2.LinkedInUrl;
                    if (!string.IsNullOrWhiteSpace(m2.TwitterUrl)) properties.Member2TwitterUrl = m2.TwitterUrl;
                }
            }

            var model = new TiyaAboutViewModel
            {
                Properties = properties,
                TeamMembers = team
            };

            return View("~/Components/Widgets/TiyaAbout/_TiyaAbout.cshtml", model);
        }
    }
}
