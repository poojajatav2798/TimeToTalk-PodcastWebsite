using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Podcast.Web.Components.Sections.TwoColumn;

[assembly: RegisterSection(
    identifier: TwoColumnSectionProperties.IDENTIFIER,
    name: "Two Column Section",
    propertiesType: typeof(TwoColumnSectionProperties),
    customViewName: "~/Components/Sections/TwoColumn/_TwoColumnSection.cshtml",
    Description = "Two equal columns zone for widgets",
    IconClass = "xp-columns-two")]

namespace Podcast.Web.Components.Sections.TwoColumn
{
    public class TwoColumnSectionProperties : ISectionProperties
    {
        public const string IDENTIFIER = "PodTalk.TwoColumnSection";

        [TextInputComponent(Label = "Custom CSS Class", Order = 1)]
        public string? CustomClass { get; set; }
    }
}
