using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using TimeToTalk;

[assembly: RegisterPageTemplate(
    identifier: "TimeToTalk.Contact",
    name: "Tiya Contact Page Template",
    customViewName: "~/PageTemplate/Contact/_ContactPageTemplate.cshtml",
    ContentTypeNames = new[] { ContactPage.CONTENT_TYPE_NAME },
    Description = "Contact page template for Tiya Golf Club with editable contact form and information areas",
    IconClass = "xp-envelope"
)]

namespace Podcast.Web.PageTemplate.Contact
{
    public class ContactPageTemplate
    {
        public const string IDENTIFIER = "TimeToTalk.Contact";
    }
}
