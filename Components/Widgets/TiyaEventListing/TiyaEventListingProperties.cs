using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Podcast.Web.Components.Widgets.TiyaEventListing
{
    public class TiyaEventListingProperties : IWidgetProperties
    {
        [TextInputComponent(Label = "Hero Title", Order = 1)]
        public string HeroTitle { get; set; } = "Event Listing";

        [TextInputComponent(Label = "Section Title", Order = 2)]
        public string SectionTitle { get; set; } = "Latest events";

        [CheckBoxComponent(Label = "Enable Pagination", Order = 3)]
        public bool EnablePagination { get; set; } = true;

        [NumberInputComponent(Label = "Events Per Page", Order = 4)]
        public int EventsPerPage { get; set; } = 4;
    }
}
