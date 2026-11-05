using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Podcast.Web.Components.HomePage;
using TimeToTalk;

[assembly: RegisterPageTemplate(
    identifier: HomePageTemplate.IDENTIFIER,
    name: "TimeToTalk Home Page",
    typeof(HomePageTemplate),
    customViewName: "~/Components/HomePage/_HomePageTemplate.cshtml",
    ContentTypeNames = new[]
    {
        HomePage.CONTENT_TYPE_NAME
    },
    IconClass = "xp-home")]

namespace Podcast.Web.Components.HomePage
{
    public class HomePageTemplate : IPageTemplateProperties
    {
        public const string IDENTIFIER = "Podcast.HomePageTemplate";

        [CheckBoxComponent(Label = "Show Wave Header", Order = 1)]
        public bool ShowWaveHeader { get; set; } = true;
    }
}
