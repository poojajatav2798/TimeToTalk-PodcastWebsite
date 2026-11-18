using System.Collections.Generic;
using System.Linq;
using CMS.ContentEngine;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Podcast.Web.Components.Widgets.TiyaMembership
{
    public class TiyaMembershipProperties : IWidgetProperties
    {
        [TextInputComponent(Label = "Section Title", Order = 1)]
        public string SectionTitle { get; set; } = "Membership at Tiya";

        [TextInputComponent(Label = "Table Heading", Order = 2)]
        public string TableHeading { get; set; } = "Membership Fees";

        [TextInputComponent(Label = "Billing Cycle Heading", Order = 3)]
        public string CycleHeading { get; set; } = "Yearly Access";

        [ContentItemSelectorComponent("TimeToTalk.WebsiteContentBlock", Label = "Select Membership Plans / Tiers (Columns in Order)", Order = 4)]
        public IEnumerable<ContentItemReference> SelectedTiers { get; set; } = Enumerable.Empty<ContentItemReference>();

        [ContentItemSelectorComponent("TimeToTalk.WebsiteContentBlock", Label = "Select Membership Features (Rows in Order)", Order = 5)]
        public IEnumerable<ContentItemReference> SelectedFeatures { get; set; } = Enumerable.Empty<ContentItemReference>();

        [TextInputComponent(Label = "Form Subheading", Order = 6)]
        public string FormSubheading { get; set; } = "Please join us!";

        [TextInputComponent(Label = "Form Title", Order = 7)]
        public string FormTitle { get; set; } = "Become a member";

        [TextInputComponent(Label = "T1 Price (fallback)", Order = 8)]
        public string T1Price { get; set; } = "T1 $420";

        [TextInputComponent(Label = "T2 Price (fallback)", Order = 9)]
        public string T2Price { get; set; } = "T2 $640";

        [TextInputComponent(Label = "T3 Price (fallback)", Order = 10)]
        public string T3Price { get; set; } = "T3 $860";

        [ContentItemSelectorComponent("TimeToTalk.WebsiteContentBlock", Label = "All Membership Items (Legacy Selector)", Order = 11)]
        public IEnumerable<ContentItemReference> SelectedItems { get; set; } = Enumerable.Empty<ContentItemReference>();
    }
}
