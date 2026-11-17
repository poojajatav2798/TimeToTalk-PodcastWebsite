using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using TimeToTalk;

[assembly: RegisterPageTemplate(
    identifier: "TimeToTalk.Listing",
    name: "Tiya Event Listing Page Template",
    customViewName: "~/PageTemplate/Listing/_ListingPageTemplate.cshtml",
    ContentTypeNames = new[] { ListingPage.CONTENT_TYPE_NAME },
    Description = "Tiya Golf Club event listing page template with editable widget areas",
    IconClass = "xp-list"
)]

namespace Podcast.Web.PageTemplate.Listing
{
    public class ListingPageTemplate
    {
        public const string IDENTIFIER = "TimeToTalk.Listing";
    }
}
