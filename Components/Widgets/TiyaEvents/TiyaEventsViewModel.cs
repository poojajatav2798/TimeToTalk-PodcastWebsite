using System;
using System.Collections.Generic;

namespace Podcast.Web.Components.Widgets.TiyaEvents
{
    public class EventItemModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Day { get; set; } = "24";
        public string MonthYear { get; set; } = "Feb 2048";
        public DateTime? EventDate { get; set; }
        public string ImageUrl { get; set; } = "";
        public string Description { get; set; } = "";
        public string Location { get; set; } = "National Center, NYC";
        public string TicketPrice { get; set; } = "$250";
        public string Url { get; set; } = "/detail-page";
        public string Slug { get; set; } = "";
        public string Category { get; set; } = "Tournament";
        public bool IsAltBackground { get; set; }
        public int Order { get; set; } = 1;
    }

    public class TiyaEventsViewModel
    {
        public TiyaEventsProperties Properties { get; set; } = new();
        public List<EventItemModel> Events { get; set; } = new();
    }
}
