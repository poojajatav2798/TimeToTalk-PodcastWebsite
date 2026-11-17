using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Podcast.Web.Components.Widgets.TiyaNewsletter
{
    public class TiyaNewsletterProperties : IWidgetProperties
    {
        [TextInputComponent(Label = "Section Heading", Order = 1)]
        public string SectionHeading { get; set; } = "Get our newsletter";

        [TextAreaComponent(Label = "Description", Order = 2)]
        public string Description { get; set; } = "Sign up for tournament updates, exclusive golf course access, and seasonal clubhouse events.";

        [TextInputComponent(Label = "Button Text", Order = 3)]
        public string ButtonText { get; set; } = "Subscribe";

        [TextInputComponent(Label = "Email Placeholder", Order = 4)]
        public string PlaceholderText { get; set; } = "Email address";

        [TextInputComponent(Label = "Success Feedback Message", Order = 5)]
        public string SuccessMessage { get; set; } = "Thank you for subscribing to Tiya Golf Club newsletter!";
    }
}
