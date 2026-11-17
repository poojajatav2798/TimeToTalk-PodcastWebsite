using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Podcast.Web.Components.Widgets.TiyaMembership
{
    public class TiyaMembershipProperties : IWidgetProperties
    {
        [TextInputComponent(Label = "Section Title", Order = 1)]
        public string SectionTitle { get; set; } = "Membership at Tiya";

        [TextInputComponent(Label = "Table Heading", Order = 2)]
        public string TableHeading { get; set; } = "Membership Fees";

        [TextInputComponent(Label = "T1 Price", Order = 3)]
        public string T1Price { get; set; } = "T1 $420";

        [TextInputComponent(Label = "T2 Price", Order = 4)]
        public string T2Price { get; set; } = "T2 $640";

        [TextInputComponent(Label = "T3 Price", Order = 5)]
        public string T3Price { get; set; } = "T3 $860";

        [TextInputComponent(Label = "Form Subheading", Order = 6)]
        public string FormSubheading { get; set; } = "Please join us!";

        [TextInputComponent(Label = "Form Title", Order = 7)]
        public string FormTitle { get; set; } = "Become a member";
    }
}
