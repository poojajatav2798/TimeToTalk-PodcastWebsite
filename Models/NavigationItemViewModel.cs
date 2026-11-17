using System.Collections.Generic;

namespace Podcast.Web.Models
{
    public class NavigationItemViewModel
    {
        public int WebPageItemId { get; set; }
        public int? ParentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsClickScroll { get; set; }
        public int Order { get; set; }
        public List<NavigationItemViewModel> Children { get; set; } = new();
    }

    public class HeaderViewModel
    {
        public List<NavigationItemViewModel> MenuItems { get; set; } = new();
        public string BrandTitle { get; set; } = "Tiya";
        public string BrandSubtitle { get; set; } = "Golf Club";
        public string BrandLogoUrl { get; set; } = "/images/logo.png";
        public string BrandLogoAlt { get; set; } = "Tiya Golf Club";
        public string SearchPlaceholder { get; set; } = "Search events...";
        public string SearchActionUrl { get; set; } = "/listing-page";
        public string ButtonText { get; set; } = "Member Login";
        public string ButtonUrl { get; set; } = "#offcanvasExample";
        public string QuestionsTitle { get; set; } = "Any Questions?";
        public string QuestionsPhone { get; set; } = "010-020-0340";
    }

    public class FooterViewModel
    {
        public List<NavigationItemViewModel> FooterItems { get; set; } = new();
        public string BrandTitle { get; set; } = "Tiya";
        public string BrandSubtitle { get; set; } = "Golf Club";
        public string Description { get; set; } = "Since 1984, Tiya is ranked #8 in the top 10 golf courses in the world. Modern, creative and lifestyle golf community.";
        public string WeekdayHours { get; set; } = "6:00 AM - 6:00 PM";
        public string WeekendHours { get; set; } = "6:30 AM - 8:30 PM";
        public string CopyrightText { get; set; } = "Copyright © 2048 Tiya Golf Club";
        public string DesignerCredit { get; set; } = "TemplateMo";
        public string DesignerUrl { get; set; } = "https://templatemo.com";
        public string SubscribeTitle { get; set; } = "Subscribe to our Newsletter";
        public string SubscribePlaceholder { get; set; } = "Enter your email address";
        public string SubscribeButtonText { get; set; } = "Subscribe";
        public string PhoneNumber { get; set; } = "(020) 010-020-0340";
        public string EmailAddress { get; set; } = "info@company.com";
        public string DownloadTitle { get; set; } = "Download Mobile App";
        public string AppStoreUrl { get; set; } = "#";
        public string PlayStoreUrl { get; set; } = "#";
        public string SocialTitle { get; set; } = "Social";
        public string InstagramUrl { get; set; } = "https://instagram.com";
        public string TwitterUrl { get; set; } = "https://twitter.com";
        public string WhatsAppUrl { get; set; } = "https://whatsapp.com";
    }
}
