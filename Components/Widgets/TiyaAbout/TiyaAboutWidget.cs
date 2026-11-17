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
    Description = "Tiya Golf Club history and leadership team cards fetched dynamically from Kentico Content Hub",
    IconClass = "xp-users")]

[assembly: RegisterWidget(
    identifier: "PodTalk.TiyaAboutWidget",
    viewComponentType: typeof(TiyaAboutWidgetViewComponent),
    name: "Tiya About & History (Legacy Alias)",
    propertiesType: typeof(TiyaAboutProperties),
    Description = "Tiya Golf Club history and leadership team cards fetched dynamically from Kentico Content Hub",
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

            // 1. Load History text from Content Hub block if available
            TimeToTalk.WebsiteContentBlock? historyBlock = null;
            if (properties.SelectedHistoryBlock != null && properties.SelectedHistoryBlock.Any())
            {
                var blockGuid = properties.SelectedHistoryBlock.First().Identifier;
                historyBlock = await _contentRepo.GetContentBlockByGuidAsync(blockGuid);
            }
            if (historyBlock == null)
            {
                historyBlock = await _contentRepo.GetContentBlockAsync("tiya-history-block");
            }

            if (historyBlock != null)
            {
                if (!string.IsNullOrWhiteSpace(historyBlock.Heading)) properties.HistoryHeading = historyBlock.Heading.Trim();
                if (!string.IsNullOrWhiteSpace(historyBlock.Description)) properties.HistoryParagraph1 = historyBlock.Description.Trim();
                if (!string.IsNullOrWhiteSpace(historyBlock.PrimaryButtonURL)) properties.HistoryParagraph2 = historyBlock.PrimaryButtonURL.Trim();
                if (!string.IsNullOrWhiteSpace(historyBlock.PrimaryButtonText)) properties.SectionTitle = historyBlock.PrimaryButtonText.Trim();
            }

            // 2. Load Team Members: either from ContentItemSelectorComponent or all from Content Hub
            List<TeamMemberModel> team;
            if (properties.SelectedCards != null && properties.SelectedCards.Any())
            {
                var cardGuids = properties.SelectedCards.Select(c => c.Identifier);
                team = await _contentRepo.GetTeamMembersByGuidsAsync(cardGuids);
            }
            else
            {
                team = await _contentRepo.GetTeamMembersAsync();
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
