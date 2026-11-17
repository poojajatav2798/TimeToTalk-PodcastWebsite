using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Podcast.Web.Components.Sections.SingleColumn;

[assembly: RegisterSection(
    identifier: SingleColumnSectionProperties.IDENTIFIER,
    name: "Single Column Section",
    propertiesType: typeof(SingleColumnSectionProperties),
    customViewName: "~/Components/Sections/SingleColumn/_SingleColumnSection.cshtml",
    Description = "Single full-width column zone for widgets",
    IconClass = "xp-column-one")]

namespace Podcast.Web.Components.Sections.SingleColumn
{
    public class SingleColumnSectionProperties : ISectionProperties
    {
        public const string IDENTIFIER = "PodTalk.SingleColumnSection";

        [TextInputComponent(Label = "Custom CSS Class", Order = 1)]
        public string? CustomClass { get; set; }
    }
}
