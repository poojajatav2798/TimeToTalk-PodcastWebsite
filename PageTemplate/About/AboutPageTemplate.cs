using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using TimeToTalk;

[assembly: RegisterPageTemplate(
    identifier: "TimeToTalk.About",
    name: "Tiya About Page Template",
    customViewName: "~/PageTemplate/About/_AboutPageTemplate.cshtml",
    ContentTypeNames = new[] { AboutPage.CONTENT_TYPE_NAME },
    Description = "About page template for Tiya Golf Club with editable hero and about story areas",
    IconClass = "xp-home"
)]

namespace Podcast.Web.PageTemplate.About
{
    public class AboutPageTemplate
    {
        public const string IDENTIFIER = "TimeToTalk.About";
    }
}
