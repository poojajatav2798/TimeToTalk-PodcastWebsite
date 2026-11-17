using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using TimeToTalk;

[assembly: RegisterPageTemplate(
    identifier: "TimeToTalk.DetailPage",
    name: "Tiya Event Detail Page Template",
    customViewName: "~/PageTemplate/DetailPage/_DetailPageTemplate.cshtml",
    ContentTypeNames = new[] { DetailPage.CONTENT_TYPE_NAME },
    Description = "Tiya Golf Club event detail page template with editable widget areas",
    IconClass = "xp-picture"
)]

namespace Podcast.Web.PageTemplate.DetailPage
{
    public class DetailPageTemplate
    {
        public const string IDENTIFIER = "TimeToTalk.DetailPage";
    }
}
