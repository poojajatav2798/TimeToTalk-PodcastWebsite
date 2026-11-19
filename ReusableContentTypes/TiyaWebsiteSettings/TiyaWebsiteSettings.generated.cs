#nullable enable
using System;
using CMS.ContentEngine;

namespace TimeToTalk
{
    /// <summary>
    /// Represents a reusable content item of type <see cref="TiyaWebsiteSettings"/>.
    /// </summary>
    [RegisterContentTypeMapping(CONTENT_TYPE_NAME)]
    public partial class TiyaWebsiteSettings : IContentItemFieldsSource
    {
        /// <summary>
        /// Code name of the content type.
        /// </summary>
        public const string CONTENT_TYPE_NAME = "TimeToTalk.TiyaWebsiteSettings";

        /// <summary>
        /// Represents system properties for a content item.
        /// </summary>
        [SystemField]
        public ContentItemFields SystemFields { get; set; } = null!;

        public string WebsiteName { get; set; } = "Tiya Golf Club";
        public string? HeaderLogoUrl { get; set; } = "/images/logo.png";
        public string? FooterLogoUrl { get; set; } = "/images/logo.png";
        public string? FaviconUrl { get; set; } = "/favicon.ico";
        public string? MemberLoginButtonText { get; set; } = "Member Login";
        public string? MemberLoginButtonUrl { get; set; } = "#offcanvasExample";
        public string? FooterDescription { get; set; }
        public string? ContactEmail { get; set; } = "info@tiyagolf.com";
        public string? ContactPhone { get; set; } = "010-020-0340";
        public string? FooterAddress { get; set; } = "1355 Market Street, San Francisco, CA 94103";
        public string? CopyrightText { get; set; } = "Copyright © 2048 Tiya Golf Club. All rights reserved.";
        public string? FacebookUrl { get; set; } = "https://facebook.com";
        public string? TwitterUrl { get; set; } = "https://twitter.com";
        public string? InstagramUrl { get; set; } = "https://instagram.com";
        public string? LinkedInUrl { get; set; } = "https://linkedin.com";
        public string? WhatsAppUrl { get; set; } = "https://whatsapp.com";
        public string? YouTubeUrl { get; set; } = "https://youtube.com";
    }
}
