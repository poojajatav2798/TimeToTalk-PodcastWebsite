using System.Collections.Generic;
using System.Linq;
using CMS.ContentEngine;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Podcast.Web.Components.Widgets.TiyaHeroBanner
{
    public class TiyaHeroBannerProperties : IWidgetProperties
    {
        [ContentItemSelectorComponent("TimeToTalk.WebsiteContentBlock", Label = "Select Banner Item (Content Hub)", Order = 1)]
        public IEnumerable<ContentItemReference> SelectedBanner { get; set; } = Enumerable.Empty<ContentItemReference>();

        [TextInputComponent(Label = "Subtitle (above title)", Order = 2)]
        public string Subtitle { get; set; } = "Welcome to the Club";

        [TextInputComponent(Label = "Hero Title", Order = 3)]
        public string HeroTitle { get; set; } = "Tiya Golf Club";

        [TextInputComponent(Label = "Title Connector (e.g. 'is')", Order = 4)]
        public string TitleConnector { get; set; } = "is";

        [TextInputComponent(Label = "Animated Rotating Words (comma-separated)", Order = 5)]
        public string AnimatedWords { get; set; } = "Modern, Creative, Lifestyle";

        [TextInputComponent(Label = "Button 1 Text", Order = 6)]
        public string Button1Text { get; set; } = "Our Story";

        [TextInputComponent(Label = "Button 1 Link", Order = 7)]
        public string Button1Link { get; set; } = "#section_2";

        [TextInputComponent(Label = "Button 2 Text", Order = 8)]
        public string Button2Text { get; set; } = "Become a member";

        [TextInputComponent(Label = "Button 2 Link", Order = 9)]
        public string Button2Link { get; set; } = "#section_3";

        [TextInputComponent(Label = "YouTube Video URL", Order = 10)]
        public string VideoUrl { get; set; } = "https://www.youtube.com/embed/MGNgbAGK6s8?si=Z6Wpbg4e88P-5k5s";

        [TextInputComponent(Label = "Background Image URL (optional)", Order = 11)]
        public string BackgroundImageUrl { get; set; } = "";
    }
}
