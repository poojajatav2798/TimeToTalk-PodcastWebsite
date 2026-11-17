using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Podcast.Web.Components.Widgets.TiyaContact
{
    public class TiyaContactProperties : IWidgetProperties
    {
        [TextInputComponent(Label = "Form Heading", Order = 1)]
        public string Heading { get; set; } = "Contact Tiya";

        [TextInputComponent(Label = "Location Name", Order = 2)]
        public string LocationName { get; set; } = "London, United Kingdom";

        [TextInputComponent(Label = "Phone Number", Order = 3)]
        public string PhoneNumber { get; set; } = "010-020-0340";

        [TextInputComponent(Label = "Phone Display Text", Order = 4)]
        public string PhoneDisplay { get; set; } = "(020) 010-020-0340";

        [TextInputComponent(Label = "Contact Email", Order = 5)]
        public string Email { get; set; } = "info@company.com";

        [TextInputComponent(Label = "Directions Map URL", Order = 6)]
        public string DirectionsUrl { get; set; } = "https://maps.google.com";
    }
}
