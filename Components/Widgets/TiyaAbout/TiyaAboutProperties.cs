using System.Collections.Generic;
using System.Linq;
using CMS.ContentEngine;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Podcast.Web.Components.Widgets.TiyaAbout
{
    public class TiyaAboutProperties : IWidgetProperties
    {
        [ContentItemSelectorComponent("TimeToTalk.WebsiteContentBlock", Label = "Select History Block (Content Hub)", Order = 0)]
        public IEnumerable<ContentItemReference> SelectedHistoryBlock { get; set; } = Enumerable.Empty<ContentItemReference>();

        [TextInputComponent(Label = "Section Title", Order = 1)]
        public string SectionTitle { get; set; } = "About Tiya";

        [TextInputComponent(Label = "History Heading", Order = 2)]
        public string HistoryHeading { get; set; } = "Tiya Club History";

        [TextAreaComponent(Label = "History Paragraph 1", Order = 3)]
        public string HistoryParagraph1 { get; set; } = "Since 1984, Tiya is ranked #8 in the top 10 golf courses in the world. Tiya is a modern golf club layout designed for champions, leisure, and memorable tournaments.";

        [TextAreaComponent(Label = "History Paragraph 2", Order = 4)]
        public string HistoryParagraph2 { get; set; } = "Our championship course offers picturesque landscapes, premier dining facilities, and a dedicated team of PGA instructors passionate about the sport.";

        [ContentItemSelectorComponent("TimeToTalk.PodcastHost", Label = "Select Team Cards (Content Hub)", Order = 5)]
        public IEnumerable<ContentItemReference> SelectedCards { get; set; } = Enumerable.Empty<ContentItemReference>();
    }
}
