using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Podcast.Web.Components.Widgets.TiyaEventDetail
{
    public class TiyaEventDetailProperties : IWidgetProperties
    {
        [TextInputComponent(Label = "Hero Title", Order = 1)]
        public string HeroTitle { get; set; } = "Event Detail.";

        [TextInputComponent(Label = "Event Title", Order = 2)]
        public string EventTitle { get; set; } = "Tiya Club's Group Tournaments";

        [TextInputComponent(Label = "Image URL", Order = 3)]
        public string ImageUrl { get; set; } = "/images/professional-golf-player.jpg";

        [TextInputComponent(Label = "Subtitle", Order = 4)]
        public string Subtitle { get; set; } = "Golf Club based in Orlando";

        [TextAreaComponent(Label = "Description Paragraph 1", Order = 5)]
        public string Description1 { get; set; } = "Tiya Golf Club is Bootstrap v5.3.0 HTML CSS template for your golf related websites. Anyone can download, edit and use this layout for commercial purposes.";

        [TextAreaComponent(Label = "Description Paragraph 2", Order = 6)]
        public string Description2 { get; set; } = "Tiya is 100% free CSS template provided by TemplateMo website. Please tell your friends about our website. Thank you for visiting.";

        [TextInputComponent(Label = "Event Date", Order = 7)]
        public string EventDate { get; set; } = "18 Mar 2048";

        [TextInputComponent(Label = "Location", Order = 8)]
        public string Location { get; set; } = "Tiya Golf Club";

        [TextInputComponent(Label = "Ticket Price", Order = 9)]
        public string TicketPrice { get; set; } = "$150";

        [TextInputComponent(Label = "Contact City / Header", Order = 10)]
        public string ContactCity { get; set; } = "London, United Kingdom";

        [TextInputComponent(Label = "Contact Phone", Order = 11)]
        public string ContactPhone { get; set; } = "(020) 010-020-0340";

        [TextInputComponent(Label = "Contact Email", Order = 12)]
        public string ContactEmail { get; set; } = "info@company.com";
    }
}
