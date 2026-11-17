using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Podcast.Web.Components.Widgets.TiyaAbout
{
    public class TiyaAboutProperties : IWidgetProperties
    {
        [TextInputComponent(Label = "Section Title", Order = 1)]
        public string SectionTitle { get; set; } = "About Tiya";

        [TextInputComponent(Label = "History Heading", Order = 2)]
        public string HistoryHeading { get; set; } = "Tiya Club History";

        [TextAreaComponent(Label = "History Paragraph 1", Order = 3)]
        public string HistoryParagraph1 { get; set; } = "Since 1984, Tiya is ranked #8 in the top 10 golf courses in the world. Tiya is a modern golf club layout designed for champions, leisure, and memorable tournaments.";

        [TextAreaComponent(Label = "History Paragraph 2", Order = 4)]
        public string HistoryParagraph2 { get; set; } = "Our championship course offers picturesque landscapes, premier dining facilities, and a dedicated team of PGA instructors passionate about the sport.";

        [TextInputComponent(Label = "Member 1 Name", Order = 5)]
        public string Member1Name { get; set; } = "Michael";

        [TextInputComponent(Label = "Member 1 Role", Order = 6)]
        public string Member1Role { get; set; } = "Founder";

        [TextInputComponent(Label = "Member 1 Image URL", Order = 7)]
        public string Member1ImageUrl { get; set; } = "/images/members/portrait-young-handsome-businessman-wearing-suit-standing-with-crossed-arms-with-isolated-studio-white-background.jpg";

        [TextInputComponent(Label = "Member 1 Twitter URL", Order = 8)]
        public string Member1TwitterUrl { get; set; } = "https://twitter.com";

        [TextInputComponent(Label = "Member 1 WhatsApp URL", Order = 9)]
        public string Member1WhatsAppUrl { get; set; } = "https://whatsapp.com";

        [TextInputComponent(Label = "Member 2 Name", Order = 10)]
        public string Member2Name { get; set; } = "Sandy";

        [TextInputComponent(Label = "Member 2 Role", Order = 11)]
        public string Member2Role { get; set; } = "Co-Founder";

        [TextInputComponent(Label = "Member 2 Image URL", Order = 12)]
        public string Member2ImageUrl { get; set; } = "/images/members/successful-asian-lady-boss-red-blazer-holding-clipboard-with-documens-pen-working-looking-happy-white-background.jpg";

        [TextInputComponent(Label = "Member 2 LinkedIn URL", Order = 13)]
        public string Member2LinkedInUrl { get; set; } = "https://linkedin.com";

        [TextInputComponent(Label = "Member 2 Twitter URL", Order = 14)]
        public string Member2TwitterUrl { get; set; } = "https://twitter.com";
    }
}
