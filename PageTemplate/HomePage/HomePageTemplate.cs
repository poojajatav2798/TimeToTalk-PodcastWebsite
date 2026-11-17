using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using TimeToTalk;

[assembly: RegisterPageTemplate(
    identifier: "TimeToTalk.HomePage",
    name: "Tiya Home Page Template",
    customViewName: "~/PageTemplate/HomePage/_HomePageTemplate.cshtml",
    ContentTypeNames = new[] { HomePage.CONTENT_TYPE_NAME },
    Description = "Home page template for Tiya Golf Club with editable hero, about, newsletter, membership, events, and contact areas",
    IconClass = "xp-home"
)]

namespace Podcast.Web.PageTemplate.HomePage
{
    public class HomePageTemplate
    {
        public const string IDENTIFIER = "TimeToTalk.HomePage";
    }
}
