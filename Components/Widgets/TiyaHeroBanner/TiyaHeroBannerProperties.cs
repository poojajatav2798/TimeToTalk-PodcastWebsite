using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Podcast.Web.Components.Widgets.TiyaHeroBanner
{
    public class TiyaHeroBannerProperties : IWidgetProperties
    {
        [TextInputComponent(Label = "Hero Title", Order = 1)]
        public string HeroTitle { get; set; } = "Tiya Golf Club";

        [TextInputComponent(Label = "Animated Rotating Words (comma-separated)", Order = 2)]
        public string AnimatedWords { get; set; } = "Modern, Creative, Lifestyle";

        [TextInputComponent(Label = "Button 1 Text", Order = 3)]
        public string Button1Text { get; set; } = "Our Story";

        [TextInputComponent(Label = "Button 1 Link", Order = 4)]
        public string Button1Link { get; set; } = "#section_2";

        [TextInputComponent(Label = "Button 2 Text", Order = 5)]
        public string Button2Text { get; set; } = "Become a member";

        [TextInputComponent(Label = "Button 2 Link", Order = 6)]
        public string Button2Link { get; set; } = "#section_3";

        [TextInputComponent(Label = "YouTube Video URL", Order = 7)]
        public string VideoUrl { get; set; } = "https://www.youtube.com/embed/MGNgbAGK6s8?si=Z6Wpbg4e88P-5k5s";
    }
}
