#nullable enable
namespace TimeToTalk
{
    public interface IHeaderNavigationItem
    {
        bool ShowInHeaderNavigation { get; set; }
        string? NavigationTitle { get; set; }
        int NavigationOrder { get; set; }
    }

    public partial class HomePage : IHeaderNavigationItem
    {
        public bool ShowInHeaderNavigation { get; set; } = true;
        public string? NavigationTitle { get; set; }
        public int NavigationOrder { get; set; } = 1;
    }

    public partial class AboutPage : IHeaderNavigationItem
    {
        public bool ShowInHeaderNavigation { get; set; } = true;
        public string? NavigationTitle { get; set; }
        public int NavigationOrder { get; set; } = 2;
    }

    public partial class ListingPage : IHeaderNavigationItem
    {
        public bool ShowInHeaderNavigation { get; set; } = true;
        public string? NavigationTitle { get; set; }
        public int NavigationOrder { get; set; } = 3;
    }

    public partial class DetailPage : IHeaderNavigationItem
    {
        public bool ShowInHeaderNavigation { get; set; } = true;
        public string? NavigationTitle { get; set; }
        public int NavigationOrder { get; set; } = 4;
    }

    public partial class ContactPage : IHeaderNavigationItem
    {
        public bool ShowInHeaderNavigation { get; set; } = true;
        public string? NavigationTitle { get; set; }
        public int NavigationOrder { get; set; } = 5;
    }
}
