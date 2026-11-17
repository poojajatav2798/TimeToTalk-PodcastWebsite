using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Podcast.Web.Components.Widgets.TiyaEvents
{
    public class TiyaEventsProperties : IWidgetProperties
    {
        [TextInputComponent(Label = "Section Title", Order = 1)]
        public string SectionTitle { get; set; } = "Upcoming Events";

        [CheckBoxComponent(Label = "Enable Pagination", Order = 2)]
        public bool EnablePagination { get; set; } = true;

        [NumberInputComponent(Label = "Events Per Page", Order = 3)]
        public int EventsPerPage { get; set; } = 2;
    }
}
