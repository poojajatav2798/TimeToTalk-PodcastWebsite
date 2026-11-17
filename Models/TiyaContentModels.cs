#nullable enable
using System;
using System.Collections.Generic;
using Podcast.Web.Components.Widgets.TiyaEvents;

namespace Podcast.Web.Models
{
    public class MembershipTierModel
    {
        public int Id { get; set; }
        public string TierCode { get; set; } = "T1";
        public string Name { get; set; } = "Standard Access";
        public string Price { get; set; } = "$250";
        public string BillingPeriod { get; set; } = "Yearly";
        public string Description { get; set; } = string.Empty;
        public List<string> IncludedFeatures { get; set; } = new();
        public int Order { get; set; } = 1;
    }

    public class MembershipFeatureRowModel
    {
        public string FeatureName { get; set; } = string.Empty;
        public List<bool> TierAvailabilityList { get; set; } = new();

        public bool Tier1Available
        {
            get => IsAvailableForTier(0);
            set => SetTierAvailability(0, value);
        }

        public bool Tier2Available
        {
            get => IsAvailableForTier(1);
            set => SetTierAvailability(1, value);
        }

        public bool Tier3Available
        {
            get => IsAvailableForTier(2);
            set => SetTierAvailability(2, value);
        }

        public bool IsAvailableForTier(int tierIndex)
        {
            if (tierIndex >= 0 && tierIndex < TierAvailabilityList.Count)
            {
                return TierAvailabilityList[tierIndex];
            }
            return false;
        }

        public void SetTierAvailability(int tierIndex, bool available)
        {
            while (TierAvailabilityList.Count <= tierIndex)
            {
                TierAvailabilityList.Add(false);
            }
            TierAvailabilityList[tierIndex] = available;
        }
    }

    public class FormFieldModel
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = "text"; // text, email, phone, dropdown, textarea
        public string Placeholder { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = true;
        public List<string> Options { get; set; } = new();
    }

    public class SocialMediaLinkModel
    {
        public string Platform { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class TeamMemberModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Biography { get; set; } = string.Empty;
        public string TwitterUrl { get; set; } = "https://twitter.com";
        public string WhatsAppUrl { get; set; } = "https://whatsapp.com";
        public string LinkedInUrl { get; set; } = "https://linkedin.com";
        public List<SocialMediaLinkModel> SocialLinks { get; set; } = new();
        public int Order { get; set; } = 1;
    }

    public class ContentFolderModel
    {
        public int FolderId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string TreePath { get; set; } = string.Empty;
        public int ItemCount { get; set; }
    }

    public class TiyaHeaderModel
    {
        public int Id { get; set; }
        public string BrandTitle { get; set; } = "Tiya";
        public string BrandSubtitle { get; set; } = "Golf Club";
        public string BrandLogoAlt { get; set; } = "Tiya Golf Club";
        public string BrandLogoUrl { get; set; } = "/images/logo.png";
        public string ButtonText { get; set; } = "Member Login";
        public string ButtonUrl { get; set; } = "#offcanvasExample";
        public string QuestionsTitle { get; set; } = "Any Questions?";
        public string QuestionsPhone { get; set; } = "010-020-0340";
        public string SearchPlaceholder { get; set; } = "Search events...";
    }

    public class TiyaFooterModel
    {
        public int Id { get; set; }
        public string BrandTitle { get; set; } = "Tiya";
        public string BrandSubtitle { get; set; } = "Golf Club";
        public string BrandLogoUrl { get; set; } = "/images/logo.png";
        public string Description { get; set; } = "Since 1984, Tiya is ranked #8 in the top 10 golf courses in the world. Modern, creative and lifestyle golf community.";
        public string ScheduleTitle { get; set; } = "Join Us";
        public string WeekdayLabel { get; set; } = "Mon-Fri";
        public string WeekdayHours { get; set; } = "6:00 AM - 6:00 PM";
        public string WeekendLabel { get; set; } = "Sat-Sun";
        public string WeekendHours { get; set; } = "6:30 AM - 8:30 PM";
        public string CopyrightText { get; set; } = "Copyright © 2048 Tiya Golf Club";
        public string DesignerCredit { get; set; } = "TemplateMo";
        public string DesignerUrl { get; set; } = "https://templatemo.com";
        public string PhoneNumber { get; set; } = "010-020-0340";
        public string EmailAddress { get; set; } = "info@company.com";
        public string InstagramUrl { get; set; } = "https://instagram.com";
        public string TwitterUrl { get; set; } = "https://twitter.com";
        public string WhatsAppUrl { get; set; } = "https://whatsapp.com";
        public string FacebookUrl { get; set; } = "";
        public string LinkedInUrl { get; set; } = "";
    }

    public class SharedContentItemModel
    {
        public int Id { get; set; }
        public string BlockName { get; set; } = string.Empty;
        public string Heading { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconName { get; set; } = string.Empty;
        public string PrimaryButtonText { get; set; } = string.Empty;
        public string PrimaryButtonURL { get; set; } = string.Empty;
        public string BlockType { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }
}
