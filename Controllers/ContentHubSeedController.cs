using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using Microsoft.AspNetCore.Mvc;
using Podcast.Web.Components.Widgets.TiyaEvents;
using Podcast.Web.Models;
using Podcast.Web.Services;
using TimeToTalk;

namespace Podcast.Web.Controllers
{
    [Route("admin-tools")]
    public class ContentHubSeedController : Controller
    {
        private readonly IContentFolderManagerFactory _folderManagerFactory;
        private readonly IContentItemManagerFactory _itemManagerFactory;
        private readonly IWebPageManagerFactory _webPageManagerFactory;
        private readonly IInfoProvider<CMS.Membership.UserInfo> _userInfoProvider;
        private readonly ITiyaContentRepository _contentRepo;

        public ContentHubSeedController(
            IContentFolderManagerFactory folderManagerFactory,
            IContentItemManagerFactory itemManagerFactory,
            IWebPageManagerFactory webPageManagerFactory,
            IInfoProvider<CMS.Membership.UserInfo> userInfoProvider,
            ITiyaContentRepository contentRepo)
        {
            _folderManagerFactory = folderManagerFactory;
            _itemManagerFactory = itemManagerFactory;
            _webPageManagerFactory = webPageManagerFactory;
            _userInfoProvider = userInfoProvider;
            _contentRepo = contentRepo;
        }

        [HttpGet("seed-all")]
        public async Task<IActionResult> SeedAll(CancellationToken cancellationToken)
        {
            var logs = new List<string>();

            try
            {
                var adminUser = _userInfoProvider.Get().GetEnumerableTypedResult().FirstOrDefault();
                if (adminUser == null)
                {
                    return BadRequest("No users found in CMS_User");
                }

                int userId = adminUser.UserID;
                logs.Add($"Using UserID: {userId} ({adminUser.UserName})");

                var folderManager = _folderManagerFactory.Create(userId);
                var itemManager = _itemManagerFactory.Create(userId);
                var pageManager = _webPageManagerFactory.Create(1, userId);

                // 1. CONTENT HUB FOLDERS
                var rootFolder = await folderManager.GetRoot(cancellationToken);
                logs.Add($"Root folder ID: {rootFolder.ContentFolderID}");

                async Task<int> EnsureFolder(string name, string displayName)
                {
                    try
                    {
                        var existing = await folderManager.Get(name, cancellationToken);
                        if (existing != null)
                        {
                            logs.Add($"Folder '{name}' already exists (ID: {existing.ContentFolderID})");
                            return existing.ContentFolderID;
                        }
                    }
                    catch { }

                    try
                    {
                        int createdId = await folderManager.Create(
                            rootFolder.ContentFolderID,
                            new CreateContentFolderParameters(displayName, name),
                            cancellationToken);
                        logs.Add($"Created folder '{name}' (ID: {createdId})");
                        return createdId;
                    }
                    catch (Exception ex)
                    {
                        logs.Add($"Could not create folder '{name}': {ex.Message}");
                        return 0;
                    }
                }

                int eventsFolderId = await EnsureFolder(TiyaContentRepository.FOLDER_EVENTS, "Events");
                int membershipsFolderId = await EnsureFolder(TiyaContentRepository.FOLDER_MEMBERSHIPS, "Memberships");
                int teamFolderId = await EnsureFolder(TiyaContentRepository.FOLDER_TEAM, "Club Staff / Team");
                int sharedFolderId = await EnsureFolder(TiyaContentRepository.FOLDER_SHARED, "Shared Content");

                // Helper to create & publish reusable content item
                async Task<int> CreateAndPublishItem(string contentTypeName, string name, string displayName, Dictionary<string, object> data, int folderId)
                {
                    try
                    {
                        // Check if item already exists by name
                        var existingDs = ConnectionHelper.ExecuteQuery(
                            $"SELECT ContentItemID FROM CMS_ContentItem WHERE ContentItemName = '{name.Replace("'", "''")}'",
                            null, QueryTypeEnum.SQLQuery);

                        if (existingDs != null && existingDs.Tables.Count > 0 && existingDs.Tables[0].Rows.Count > 0)
                        {
                            int existingId = Convert.ToInt32(existingDs.Tables[0].Rows[0]["ContentItemID"]);
                            logs.Add($"Preserved dynamic Content Hub item '{displayName}' (ID: {existingId}) - already exists and managed via Admin UI.");
                            return existingId;
                        }

                        var createParams = new CreateContentItemParameters(contentTypeName, name, displayName, "en");
                        var itemData = new ContentItemData(data);
                        int itemId = await itemManager.Create(createParams, itemData, cancellationToken);
                        logs.Add($"Created {contentTypeName} '{displayName}' (ID: {itemId})");

                        try
                        {
                            await itemManager.TryPublish(itemId, "en", cancellationToken);
                            logs.Add($"Published '{displayName}'");
                        }
                        catch (Exception ex)
                        {
                            logs.Add($"Warning publishing '{displayName}': {ex.Message}");
                        }

                        if (folderId > 0)
                        {
                            try
                            {
                                await folderManager.MoveItems(folderId, new[] { itemId }, cancellationToken);
                                logs.Add($"Moved '{displayName}' to folder {folderId}");
                            }
                            catch (Exception ex)
                            {
                                logs.Add($"Warning moving '{displayName}' to folder: {ex.Message}");
                            }
                        }

                        return itemId;
                    }
                    catch (Exception ex)
                    {
                        logs.Add($"Error creating '{displayName}': {ex.Message}");
                        return 0;
                    }
                }

                // 2. SEED TIYA GOLF CLUB EVENTS (In 'Events' folder)
                logs.Add("--- Seeding Golf Events ---");
                await CreateAndPublishItem(
                    PodcastEpisode.CONTENT_TYPE_NAME,
                    "private-activities",
                    "Private activities",
                    new Dictionary<string, object>
                    {
                        { nameof(PodcastEpisode.EpisodeTitle), "Private activities" },
                        { nameof(PodcastEpisode.EpisodeShortDescription), "National Center, NYC" },
                        { nameof(PodcastEpisode.EpisodeDescription), "Exclusive VIP private golf tournament on our 18-hole championship green with personal caddie service and premium club lounge access." },
                        { nameof(PodcastEpisode.EpisodeListensCount), "$250" },
                        { nameof(PodcastEpisode.PublishDate), new DateTime(2048, 2, 24) },
                        { nameof(PodcastEpisode.EpisodeRequiresSubscription), false },
                        { nameof(PodcastEpisode.EpisodeSlug), "private-activities" },
                        { nameof(PodcastEpisode.EpisodeTopicName), "VIP / Private" },
                        { nameof(PodcastEpisode.EpisodeNumber), 1 }
                    },
                    eventsFolderId);

                await CreateAndPublishItem(
                    PodcastEpisode.CONTENT_TYPE_NAME,
                    "group-tournament-activities",
                    "Group tournament activities",
                    new Dictionary<string, object>
                    {
                        { nameof(PodcastEpisode.EpisodeTitle), "Group tournament activities" },
                        { nameof(PodcastEpisode.EpisodeShortDescription), "National Center, NYC" },
                        { nameof(PodcastEpisode.EpisodeDescription), "Bring your team or friends for our seasonal weekend scramble. Features live scoring, complimentary luncheon, and trophy ceremony." },
                        { nameof(PodcastEpisode.EpisodeListensCount), "$350" },
                        { nameof(PodcastEpisode.PublishDate), new DateTime(2048, 2, 28) },
                        { nameof(PodcastEpisode.EpisodeRequiresSubscription), true },
                        { nameof(PodcastEpisode.EpisodeSlug), "group-tournament-activities" },
                        { nameof(PodcastEpisode.EpisodeTopicName), "Group Scramble" },
                        { nameof(PodcastEpisode.EpisodeNumber), 2 }
                    },
                    eventsFolderId);

                await CreateAndPublishItem(
                    PodcastEpisode.CONTENT_TYPE_NAME,
                    "single-tournament-championship",
                    "Single Tournament Championship",
                    new Dictionary<string, object>
                    {
                        { nameof(PodcastEpisode.EpisodeTitle), "Single Tournament Championship" },
                        { nameof(PodcastEpisode.EpisodeShortDescription), "Tiya Championship Links" },
                        { nameof(PodcastEpisode.EpisodeDescription), "Individual stroke play championship for golfers of all skill levels. Cash prizes, custom trophies, and clubhouse banquet dinner included." },
                        { nameof(PodcastEpisode.EpisodeListensCount), "$150" },
                        { nameof(PodcastEpisode.PublishDate), new DateTime(2048, 3, 18) },
                        { nameof(PodcastEpisode.EpisodeRequiresSubscription), false },
                        { nameof(PodcastEpisode.EpisodeSlug), "single-tournament-championship" },
                        { nameof(PodcastEpisode.EpisodeTopicName), "Stroke Play" },
                        { nameof(PodcastEpisode.EpisodeNumber), 3 }
                    },
                    eventsFolderId);

                await CreateAndPublishItem(
                    PodcastEpisode.CONTENT_TYPE_NAME,
                    "junior-master-golf-showcase",
                    "Junior & Master Golf Showcase",
                    new Dictionary<string, object>
                    {
                        { nameof(PodcastEpisode.EpisodeTitle), "Junior & Master Golf Showcase" },
                        { nameof(PodcastEpisode.EpisodeShortDescription), "Orlando Country Club" },
                        { nameof(PodcastEpisode.EpisodeDescription), "Mentor and youth pairs tournament celebrating the future of golf. PGA coaching clinics and demo club fittings throughout the day." },
                        { nameof(PodcastEpisode.EpisodeListensCount), "$180" },
                        { nameof(PodcastEpisode.PublishDate), new DateTime(2048, 3, 25) },
                        { nameof(PodcastEpisode.EpisodeRequiresSubscription), true },
                        { nameof(PodcastEpisode.EpisodeSlug), "junior-master-golf-showcase" },
                        { nameof(PodcastEpisode.EpisodeTopicName), "Youth / Showcase" },
                        { nameof(PodcastEpisode.EpisodeNumber), 4 }
                    },
                    eventsFolderId);

                await CreateAndPublishItem(
                    PodcastEpisode.CONTENT_TYPE_NAME,
                    "pro-golf-invitational",
                    "Pro Golf Invitational",
                    new Dictionary<string, object>
                    {
                        { nameof(PodcastEpisode.EpisodeTitle), "Pro Golf Invitational" },
                        { nameof(PodcastEpisode.EpisodeShortDescription), "Tiya Championship Course" },
                        { nameof(PodcastEpisode.EpisodeDescription), "Watch top ranked regional golfers compete in an exhilarating 36-hole weekend showdown with VIP clubhouse hospitality." },
                        { nameof(PodcastEpisode.EpisodeListensCount), "$300" },
                        { nameof(PodcastEpisode.PublishDate), new DateTime(2023, 2, 28) },
                        { nameof(PodcastEpisode.EpisodeRequiresSubscription), false },
                        { nameof(PodcastEpisode.EpisodeSlug), "pro-golf-invitational" },
                        { nameof(PodcastEpisode.EpisodeTopicName), "Invitational" },
                        { nameof(PodcastEpisode.EpisodeNumber), 5 }
                    },
                    eventsFolderId);

                // 3. SEED CLUB STAFF / TEAM (In 'Team' folder)
                logs.Add("--- Seeding Club Team ---");
                await CreateAndPublishItem(
                    PodcastHost.CONTENT_TYPE_NAME,
                    "michael-founder",
                    "Michael",
                    new Dictionary<string, object>
                    {
                        { nameof(PodcastHost.HostName), "Michael" },
                        { nameof(PodcastHost.HostRole), "Founder" },
                        { nameof(PodcastHost.HostSpecialties), "Course Architecture & Championship Greens" },
                        { nameof(PodcastHost.HostBiography), "Founder of Tiya Golf Club with over 20 years of course development and PGA tournament leadership experience." },
                        { nameof(PodcastHost.HostIsVerified), true },
                        { nameof(PodcastHost.HostSlug), "michael-founder" },
                        { nameof(PodcastHost.HostTwitterURL), "https://twitter.com" },
                        { nameof(PodcastHost.HostFacebookURL), "https://whatsapp.com" },
                        { nameof(PodcastHost.HostLinkedInURL), "https://linkedin.com" }
                    },
                    teamFolderId);

                await CreateAndPublishItem(
                    PodcastHost.CONTENT_TYPE_NAME,
                    "sandy-cofounder",
                    "Sandy",
                    new Dictionary<string, object>
                    {
                        { nameof(PodcastHost.HostName), "Sandy" },
                        { nameof(PodcastHost.HostRole), "Co-Founder" },
                        { nameof(PodcastHost.HostSpecialties), "Member Hospitality & Executive Dining" },
                        { nameof(PodcastHost.HostBiography), "Co-Founder and Director of Member Hospitality, overseeing premier dining facilities and VIP member events." },
                        { nameof(PodcastHost.HostIsVerified), true },
                        { nameof(PodcastHost.HostSlug), "sandy-cofounder" },
                        { nameof(PodcastHost.HostTwitterURL), "https://twitter.com" },
                        { nameof(PodcastHost.HostFacebookURL), "https://whatsapp.com" },
                        { nameof(PodcastHost.HostLinkedInURL), "https://linkedin.com" }
                    },
                    teamFolderId);

                // 4. SEED MEMBERSHIPS (In 'Memberships' folder)
                logs.Add("--- Seeding Memberships ---");
                await CreateAndPublishItem(
                    WebsiteContentBlock.CONTENT_TYPE_NAME,
                    "tier-1-standard",
                    "Standard Access",
                    new Dictionary<string, object>
                    {
                        { nameof(WebsiteContentBlock.BlockName), "tier-1-standard" },
                        { nameof(WebsiteContentBlock.Heading), "Standard Access" },
                        { nameof(WebsiteContentBlock.Description), "Full access to driving ranges and weekday green tee times." },
                        { nameof(WebsiteContentBlock.IconName), "T1" },
                        { nameof(WebsiteContentBlock.BlockType), "MembershipTier" },
                        { nameof(WebsiteContentBlock.PrimaryButtonText), "T1 $420" },
                        { nameof(WebsiteContentBlock.DisplayOrder), 1 },
                        { nameof(WebsiteContentBlock.IsActive), true }
                    },
                    membershipsFolderId);

                await CreateAndPublishItem(
                    WebsiteContentBlock.CONTENT_TYPE_NAME,
                    "tier-2-premium",
                    "Country Club Pro",
                    new Dictionary<string, object>
                    {
                        { nameof(WebsiteContentBlock.BlockName), "tier-2-premium" },
                        { nameof(WebsiteContentBlock.Heading), "Country Club Pro" },
                        { nameof(WebsiteContentBlock.Description), "Weekend access, country club facilities, and seasonal tournament entry." },
                        { nameof(WebsiteContentBlock.IconName), "T2" },
                        { nameof(WebsiteContentBlock.BlockType), "MembershipTier" },
                        { nameof(WebsiteContentBlock.PrimaryButtonText), "T2 $640" },
                        { nameof(WebsiteContentBlock.DisplayOrder), 2 },
                        { nameof(WebsiteContentBlock.IsActive), true }
                    },
                    membershipsFolderId);

                await CreateAndPublishItem(
                    WebsiteContentBlock.CONTENT_TYPE_NAME,
                    "tier-3-platinum",
                    "Platinum Masters",
                    new Dictionary<string, object>
                    {
                        { nameof(WebsiteContentBlock.BlockName), "tier-3-platinum" },
                        { nameof(WebsiteContentBlock.Heading), "Platinum Masters" },
                        { nameof(WebsiteContentBlock.Description), "VIP course priority, pro networking, personalized locker, and club lounge access." },
                        { nameof(WebsiteContentBlock.IconName), "T3" },
                        { nameof(WebsiteContentBlock.BlockType), "MembershipTier" },
                        { nameof(WebsiteContentBlock.PrimaryButtonText), "T3 $860" },
                        { nameof(WebsiteContentBlock.DisplayOrder), 3 },
                        { nameof(WebsiteContentBlock.IsActive), true }
                    },
                    membershipsFolderId);

                // Membership Feature Rows
                var featureData = new[]
                {
                    ("feat-golf-insurance", "Golf Insurance", "1,1,1", 1),
                    ("feat-club-facilities", "Club Facilities", "1,1,1", 2),
                    ("feat-country-club", "Country Club", "0,1,1", 3),
                    ("feat-weekend-seasonal", "Weekend Seasonal", "0,1,1", 4),
                    ("feat-premium-courses", "Premium Courses", "0,0,1", 5),
                    ("feat-pros-networking", "Pro's Networking", "0,0,1", 6)
                };

                foreach (var (code, title, flags, ord) in featureData)
                {
                    await CreateAndPublishItem(
                        WebsiteContentBlock.CONTENT_TYPE_NAME,
                        code,
                        title,
                        new Dictionary<string, object>
                        {
                            { nameof(WebsiteContentBlock.BlockName), code },
                            { nameof(WebsiteContentBlock.Heading), title },
                            { nameof(WebsiteContentBlock.BlockType), "MembershipFeature" },
                            { nameof(WebsiteContentBlock.PrimaryButtonURL), flags },
                            { nameof(WebsiteContentBlock.DisplayOrder), ord },
                            { nameof(WebsiteContentBlock.IsActive), true }
                        },
                        membershipsFolderId);
                }

                // 5. SEED SHARED CONTENT (In 'Shared Content' folder)
                logs.Add("--- Seeding Shared Content ---");
                // 5.1 Header Configuration
                await CreateAndPublishItem(
                    WebsiteContentBlock.CONTENT_TYPE_NAME,
                    "tiya-header-config",
                    "Tiya Header Configuration",
                    new Dictionary<string, object>
                    {
                        { nameof(WebsiteContentBlock.BlockName), "tiya-header-config" },
                        { nameof(WebsiteContentBlock.Heading), "Tiya" },
                        { nameof(WebsiteContentBlock.IconName), "Golf Club" },
                        { nameof(WebsiteContentBlock.Description), "Any Questions?|010-020-0340|Search events..." },
                        { nameof(WebsiteContentBlock.PrimaryButtonText), "Member Login" },
                        { nameof(WebsiteContentBlock.PrimaryButtonURL), "#offcanvasExample" },
                        { nameof(WebsiteContentBlock.BlockType), "HeaderConfig" },
                        { nameof(WebsiteContentBlock.IsActive), true }
                    },
                    sharedFolderId);

                // 5.2 Footer Configuration
                await CreateAndPublishItem(
                    WebsiteContentBlock.CONTENT_TYPE_NAME,
                    "tiya-footer-config",
                    "Tiya Footer Configuration",
                    new Dictionary<string, object>
                    {
                        { nameof(WebsiteContentBlock.BlockName), "tiya-footer-config" },
                        { nameof(WebsiteContentBlock.Heading), "Copyright © 2048 Tiya Golf Club" },
                        { nameof(WebsiteContentBlock.Description), "Since 1984, Tiya is ranked #8 in the top 10 golf courses in the world. Modern, creative and lifestyle golf community." },
                        { nameof(WebsiteContentBlock.IconName), "Join Us|Mon-Fri|6:00 AM - 6:00 PM|Sat-Sun|6:30 AM - 8:30 PM" },
                        { nameof(WebsiteContentBlock.PrimaryButtonText), "TemplateMo" },
                        { nameof(WebsiteContentBlock.PrimaryButtonURL), "https://instagram.com|https://twitter.com|https://whatsapp.com||" },
                        { nameof(WebsiteContentBlock.SecondaryButtonText), "(020) 010-020-0340" },
                        { nameof(WebsiteContentBlock.SecondaryButtonURL), "info@company.com|/images/logo.png|https://templatemo.com" },
                        { nameof(WebsiteContentBlock.BlockType), "FooterConfig" },
                        { nameof(WebsiteContentBlock.IsActive), true }
                    },
                    sharedFolderId);

                // 5.3 Home Hero Banner
                await CreateAndPublishItem(
                    WebsiteContentBlock.CONTENT_TYPE_NAME,
                    "home-hero-banner",
                    "Home Hero Banner",
                    new Dictionary<string, object>
                    {
                        { nameof(WebsiteContentBlock.BlockName), "home-hero-banner" },
                        { nameof(WebsiteContentBlock.Heading), "Tiya Golf Club" },
                        { nameof(WebsiteContentBlock.Description), "Modern, Creative, Lifestyle" },
                        { nameof(WebsiteContentBlock.PrimaryButtonText), "Our Story" },
                        { nameof(WebsiteContentBlock.PrimaryButtonURL), "#section_2" },
                        { nameof(WebsiteContentBlock.SecondaryButtonText), "Become a member" },
                        { nameof(WebsiteContentBlock.SecondaryButtonURL), "#section_3" },
                        { nameof(WebsiteContentBlock.IconName), "Welcome to the Club|is|https://www.youtube.com/embed/MGNgbAGK6s8" },
                        { nameof(WebsiteContentBlock.BlockType), "HeroBanner" },
                        { nameof(WebsiteContentBlock.IsActive), true }
                    },
                    sharedFolderId);

                // 5.4 Tiya Club History (About Section)
                await CreateAndPublishItem(
                    WebsiteContentBlock.CONTENT_TYPE_NAME,
                    "tiya-history-block",
                    "Tiya Club History",
                    new Dictionary<string, object>
                    {
                        { nameof(WebsiteContentBlock.BlockName), "tiya-history-block" },
                        { nameof(WebsiteContentBlock.Heading), "Tiya Club History" },
                        { nameof(WebsiteContentBlock.Description), "Since 1984, Tiya is ranked #8 in the top 10 golf courses in the world. Tiya is a modern golf club layout designed for champions, leisure, and memorable tournaments." },
                        { nameof(WebsiteContentBlock.PrimaryButtonText), "About Tiya" },
                        { nameof(WebsiteContentBlock.PrimaryButtonURL), "Our championship course offers picturesque landscapes, premier dining facilities, and a dedicated team of PGA instructors passionate about the sport." },
                        { nameof(WebsiteContentBlock.BlockType), "History" },
                        { nameof(WebsiteContentBlock.IsActive), true }
                    },
                    sharedFolderId);

                // 5.5 Home Newsletter
                await CreateAndPublishItem(
                    WebsiteContentBlock.CONTENT_TYPE_NAME,
                    "home-newsletter",
                    "Home Newsletter",
                    new Dictionary<string, object>
                    {
                        { nameof(WebsiteContentBlock.BlockName), "home-newsletter" },
                        { nameof(WebsiteContentBlock.Heading), "Subscribe to our newsletter" },
                        { nameof(WebsiteContentBlock.Description), "Get the latest news and tournament updates delivered directly to your inbox." },
                        { nameof(WebsiteContentBlock.BlockType), "Newsletter" },
                        { nameof(WebsiteContentBlock.IsActive), true }
                    },
                    sharedFolderId);

                // 5.6 Home Contact Info
                await CreateAndPublishItem(
                    WebsiteContentBlock.CONTENT_TYPE_NAME,
                    "home-contact-info",
                    "Home Contact Info",
                    new Dictionary<string, object>
                    {
                        { nameof(WebsiteContentBlock.BlockName), "home-contact-info" },
                        { nameof(WebsiteContentBlock.Heading), "Say Hello to Tiya" },
                        { nameof(WebsiteContentBlock.Description), "London, United Kingdom" },
                        { nameof(WebsiteContentBlock.IconName), "(020) 010-020-0340" },
                        { nameof(WebsiteContentBlock.PrimaryButtonText), "info@company.com" },
                        { nameof(WebsiteContentBlock.BlockType), "Contact" },
                        { nameof(WebsiteContentBlock.IsActive), true }
                    },
                    sharedFolderId);

                // 5.7 Listing Page Content
                await CreateAndPublishItem(
                    WebsiteContentBlock.CONTENT_TYPE_NAME,
                    "listing-page-content",
                    "Listing Page Content",
                    new Dictionary<string, object>
                    {
                        { nameof(WebsiteContentBlock.BlockName), "listing-page-content" },
                        { nameof(WebsiteContentBlock.Heading), "Upcoming Golf Events" },
                        { nameof(WebsiteContentBlock.Description), "Explore tournaments, clinics, and member activities" },
                        { nameof(WebsiteContentBlock.PrimaryButtonText), "Event Listing" },
                        { nameof(WebsiteContentBlock.BlockType), "ListingPage" },
                        { nameof(WebsiteContentBlock.IsActive), true }
                    },
                    sharedFolderId);

                // 5.8 Detail Page Content
                await CreateAndPublishItem(
                    WebsiteContentBlock.CONTENT_TYPE_NAME,
                    "detail-page-content",
                    "Detail Page Content",
                    new Dictionary<string, object>
                    {
                        { nameof(WebsiteContentBlock.BlockName), "detail-page-content" },
                        { nameof(WebsiteContentBlock.Heading), "Championship Tournament" },
                        { nameof(WebsiteContentBlock.Description), "VIP / Private | National Center, NYC | $250" },
                        { nameof(WebsiteContentBlock.PrimaryButtonText), "Event Detail" },
                        { nameof(WebsiteContentBlock.BlockType), "DetailPage" },
                        { nameof(WebsiteContentBlock.IsActive), true }
                    },
                    sharedFolderId);

                // 5.8b Membership Form Dynamic Fields Configuration
                await CreateAndPublishItem(
                    WebsiteContentBlock.CONTENT_TYPE_NAME,
                    "membership-form-fields",
                    "Membership Form Fields",
                    new Dictionary<string, object>
                    {
                        { nameof(WebsiteContentBlock.BlockName), "membership-form-fields" },
                        { nameof(WebsiteContentBlock.Heading), "Membership Form Configuration" },
                        { nameof(WebsiteContentBlock.Description), "fullName|Full Name|text|Full Name|1\r\nemail|Email address|email|Email address|1\r\nphone|Phone Number|phone|010-020-0340|0\r\ntier|Membership Tier|dropdown|Select Plan|0|Standard Access ($600),Country Club Pro ($850),Platinum Masters ($1,100)\r\ncomments|Comments|textarea|Describe message here|0" },
                        { nameof(WebsiteContentBlock.BlockType), "MembershipConfig" },
                        { nameof(WebsiteContentBlock.IsActive), true }
                    },
                    sharedFolderId);

                // 5.9 Content Hub Dynamic Navigation Items (Navigation Folder)
                logs.Add("--- Seeding Dynamic Navigation Items in Navigation folder ---");
                int navFolderId = await EnsureFolder("Navigation", "Navigation");

                await CreateAndPublishItem(
                    WebsiteContentBlock.CONTENT_TYPE_NAME,
                    "nav-home",
                    "Home",
                    new Dictionary<string, object>
                    {
                        { nameof(WebsiteContentBlock.BlockName), "nav-home" },
                        { nameof(WebsiteContentBlock.Heading), "Home" },
                        { nameof(WebsiteContentBlock.PrimaryButtonURL), "/#section_1" },
                        { nameof(WebsiteContentBlock.BlockType), "NavigationItem" },
                        { nameof(WebsiteContentBlock.DisplayOrder), 1 },
                        { nameof(WebsiteContentBlock.IsActive), true }
                    },
                    navFolderId);

                await CreateAndPublishItem(
                    WebsiteContentBlock.CONTENT_TYPE_NAME,
                    "nav-about",
                    "About",
                    new Dictionary<string, object>
                    {
                        { nameof(WebsiteContentBlock.BlockName), "nav-about" },
                        { nameof(WebsiteContentBlock.Heading), "About" },
                        { nameof(WebsiteContentBlock.PrimaryButtonURL), "/#section_2" },
                        { nameof(WebsiteContentBlock.BlockType), "NavigationItem" },
                        { nameof(WebsiteContentBlock.DisplayOrder), 2 },
                        { nameof(WebsiteContentBlock.IsActive), true }
                    },
                    navFolderId);

                await CreateAndPublishItem(
                    WebsiteContentBlock.CONTENT_TYPE_NAME,
                    "nav-membership",
                    "Membership",
                    new Dictionary<string, object>
                    {
                        { nameof(WebsiteContentBlock.BlockName), "nav-membership" },
                        { nameof(WebsiteContentBlock.Heading), "Membership" },
                        { nameof(WebsiteContentBlock.PrimaryButtonURL), "/#section_3" },
                        { nameof(WebsiteContentBlock.BlockType), "NavigationItem" },
                        { nameof(WebsiteContentBlock.DisplayOrder), 3 },
                        { nameof(WebsiteContentBlock.IsActive), true }
                    },
                    navFolderId);

                await CreateAndPublishItem(
                    WebsiteContentBlock.CONTENT_TYPE_NAME,
                    "nav-events",
                    "Events",
                    new Dictionary<string, object>
                    {
                        { nameof(WebsiteContentBlock.BlockName), "nav-events" },
                        { nameof(WebsiteContentBlock.Heading), "Events" },
                        { nameof(WebsiteContentBlock.PrimaryButtonURL), "/#section_4" },
                        { nameof(WebsiteContentBlock.BlockType), "NavigationItem" },
                        { nameof(WebsiteContentBlock.DisplayOrder), 4 },
                        { nameof(WebsiteContentBlock.IsActive), true }
                    },
                    navFolderId);

                await CreateAndPublishItem(
                    WebsiteContentBlock.CONTENT_TYPE_NAME,
                    "nav-contact",
                    "Contact Us",
                    new Dictionary<string, object>
                    {
                        { nameof(WebsiteContentBlock.BlockName), "nav-contact" },
                        { nameof(WebsiteContentBlock.Heading), "Contact Us" },
                        { nameof(WebsiteContentBlock.PrimaryButtonURL), "/#section_5" },
                        { nameof(WebsiteContentBlock.BlockType), "NavigationItem" },
                        { nameof(WebsiteContentBlock.DisplayOrder), 5 },
                        { nameof(WebsiteContentBlock.IsActive), true }
                    },
                    navFolderId);

                // 6. ASSIGN TEMPLATES AND WIDGETS
                logs.Add("--- Assigning Page Templates & Widgets from Content Hub Saved Data ---");
                AssignTemplates();
                var widgetResult = await SeedPageWidgets();
                logs.Add("Dynamic Page Widgets synced from Content Hub saved data.");

                return Ok(new { success = true, logs });
            }
            catch (Exception ex)
            {
                logs.Add($"Fatal: {ex}");
                return StatusCode(500, new { success = false, error = ex.Message, logs });
            }
        }

        [HttpGet("assign-templates")]
        public IActionResult AssignTemplates()
        {
            var logs = new List<string>();
            try
            {
                var pagesDs = ConnectionHelper.ExecuteQuery(
                    @"SELECT W.WebPageItemID, W.WebPageItemName, C.ContentItemCommonDataID, C.ContentItemCommonDataPageTemplateConfiguration, CI.ContentItemContentTypeID, CT.ClassName
                      FROM CMS_WebPageItem W
                      JOIN CMS_ContentItem CI ON W.WebPageItemContentItemID = CI.ContentItemID
                      JOIN CMS_Class CT ON CI.ContentItemContentTypeID = CT.ClassID
                      JOIN CMS_ContentItemCommonData C ON CI.ContentItemID = C.ContentItemCommonDataContentItemID",
                    null,
                    QueryTypeEnum.SQLQuery);

                if (pagesDs != null && pagesDs.Tables.Count > 0)
                {
                    foreach (DataRow row in pagesDs.Tables[0].Rows)
                    {
                        int pageId = Convert.ToInt32(row["WebPageItemID"]);
                        string pageName = row["WebPageItemName"].ToString() ?? "";
                        string className = row["ClassName"].ToString() ?? "";
                        int commonDataId = Convert.ToInt32(row["ContentItemCommonDataID"]);

                        string targetTemplate = className switch
                        {
                            "TimeToTalk.HomePage" => "TimeToTalk.HomePage",
                            "TimeToTalk.AboutPage" => "TimeToTalk.About",
                            "TimeToTalk.ContactPage" => "TimeToTalk.Contact",
                            "TimeToTalk.DetailPage" => "TimeToTalk.Detail",
                            "TimeToTalk.ListingPage" => "TimeToTalk.Listing",
                            _ => ""
                        };

                        if (!string.IsNullOrEmpty(targetTemplate))
                        {
                            string newConfig = $"{{\"identifier\":\"{targetTemplate}\",\"properties\":{{}}}}";
                            ConnectionHelper.ExecuteQuery(
                                $"UPDATE CMS_ContentItemCommonData SET ContentItemCommonDataPageTemplateConfiguration = '{newConfig}' WHERE ContentItemCommonDataID = {commonDataId}",
                                null,
                                QueryTypeEnum.SQLQuery);
                            logs.Add($"Assigned template '{targetTemplate}' to Page ID {pageId} ('{pageName}')");
                        }
                    }
                }

                return Ok(new { success = true, logs });
            }
            catch (Exception ex)
            {
                logs.Add($"Error: {ex}");
                return StatusCode(500, new { success = false, error = ex.Message, logs });
            }
        }

        [HttpGet("seed-page-widgets")]
        [HttpGet("sync-widgets-from-content-hub")]
        [HttpPost("sync-widgets-from-content-hub")]
        public async Task<IActionResult> SeedPageWidgets()
        {
            var logs = new List<string>();
            try
            {
                // Fetch dynamic items directly from Content Hub saved data
                var heroBlock = await _contentRepo.GetContentBlockAsync("home-hero-banner");
                var historyBlock = await _contentRepo.GetContentBlockAsync("tiya-history-block");
                var newsletterBlock = await _contentRepo.GetContentBlockAsync("home-newsletter");
                var contactBlock = await _contentRepo.GetContentBlockAsync("home-contact-info");
                var listingBlock = await _contentRepo.GetContentBlockAsync("listing-page-content");
                var detailBlock = await _contentRepo.GetContentBlockAsync("detail-page-content");
                var featuredEvent = (await _contentRepo.GetEventsAsync(1)).FirstOrDefault();

                logs.Add($"Loaded dynamic Content Hub blocks: Hero='{heroBlock?.Heading}', History='{historyBlock?.Heading}', Contact='{contactBlock?.Heading}'");

                static object CreateArea(string areaId, string widgetType, object properties)
                {
                    return new
                    {
                        identifier = areaId,
                        sections = new[]
                        {
                            new
                            {
                                identifier = Guid.NewGuid().ToString(),
                                type = "Kentico.DefaultSection",
                                properties = (object?)null,
                                zones = new[]
                                {
                                    new
                                    {
                                        identifier = Guid.NewGuid().ToString(),
                                        widgets = new[]
                                        {
                                            new
                                            {
                                                identifier = Guid.NewGuid().ToString(),
                                                type = widgetType,
                                                variants = new[]
                                                {
                                                    new
                                                    {
                                                        identifier = Guid.NewGuid().ToString(),
                                                        properties = properties,
                                                        fieldIdentifiers = new { }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        fieldIdentifiers = new { }
                    };
                }

                // 1. HOME PAGE WIDGETS (Populated dynamically from Content Hub saved data)
                var homeWidgets = new
                {
                    editableAreas = new[]
                    {
                        CreateArea("hero-area", "TiyaGolf.HeroBannerWidget", new
                        {
                            heroTitle = heroBlock?.Heading ?? "Tiya Golf Club",
                            animatedWords = heroBlock?.Description ?? "Modern, Creative, Lifestyle",
                            button1Text = heroBlock?.PrimaryButtonText ?? "Our Story",
                            button1Link = "#section_2",
                            button2Text = heroBlock?.IconName ?? "Become a member",
                            button2Link = "#section_3",
                            videoUrl = "https://www.youtube.com/embed/MGNgbAGK6s8"
                        }),
                        CreateArea("about-area", "TiyaGolf.TiyaAboutWidget", new
                        {
                            sectionTitle = historyBlock?.PrimaryButtonText ?? "About Tiya",
                            historyHeading = historyBlock?.Heading ?? "Tiya Club History",
                            historyParagraph1 = historyBlock?.Description ?? "Since 1984, Tiya is ranked #8 in the top 10 golf courses in the world. Tiya is a modern golf club layout designed for champions, leisure, and memorable tournaments.",
                            historyParagraph2 = historyBlock?.PrimaryButtonURL ?? "Our championship course offers picturesque landscapes, premier dining facilities, and a dedicated team of PGA instructors passionate about the sport."
                        }),
                        CreateArea("newsletter-area", "TiyaGolf.NewsletterWidget", new
                        {
                            heading = newsletterBlock?.Heading ?? "Subscribe to our newsletter",
                            subtitle = newsletterBlock?.Description ?? "Get the latest news and tournament updates delivered directly to your inbox."
                        }),
                        CreateArea("membership-area", "TiyaGolf.TiyaMembershipWidget", new
                        {
                            sectionTitle = "Membership at Tiya",
                            tableHeading = "Membership Fees"
                        }),
                        CreateArea("events-area", "TiyaGolf.TiyaEventsWidget", new
                        {
                            sectionTitle = "Upcoming Events",
                            enablePagination = true,
                            eventsPerPage = 2
                        }),
                        CreateArea("contact-area", "TiyaGolf.TiyaContactWidget", new
                        {
                            sectionTitle = contactBlock?.Heading ?? "Say Hello to Tiya",
                            locationAddress = contactBlock?.Description ?? "London, United Kingdom",
                            phone = contactBlock?.IconName ?? "(020) 010-020-0340",
                            email = contactBlock?.PrimaryButtonText ?? "info@company.com"
                        })
                    }
                };

                // 2. LISTING PAGE WIDGETS (Populated dynamically from Content Hub saved data)
                var listingWidgets = new
                {
                    editableAreas = new[]
                    {
                        CreateArea("main-area", "TiyaGolf.TiyaEventListingWidget", new
                        {
                            heroTitle = listingBlock?.PrimaryButtonText ?? "Event Listing",
                            sectionTitle = listingBlock?.Heading ?? "Upcoming Golf Events",
                            eventsPerPage = 4
                        })
                    }
                };

                // 3. DETAIL PAGE WIDGETS (Populated dynamically from Content Hub saved data)
                var detailWidgets = new
                {
                    editableAreas = new[]
                    {
                        CreateArea("main-area", "TiyaGolf.TiyaEventDetailWidget", new
                        {
                            heroTitle = detailBlock?.PrimaryButtonText ?? "Event Detail",
                            eventTitle = featuredEvent?.Title ?? detailBlock?.Heading ?? "Championship Tournament",
                            subtitle = featuredEvent?.Category ?? "VIP / Private",
                            location = featuredEvent?.Location ?? "National Center, NYC",
                            ticketPrice = featuredEvent?.TicketPrice ?? "$250"
                        })
                    }
                };

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };

                void SaveWidgets(string pageClassName, object widgetObj)
                {
                    string json = System.Text.Json.JsonSerializer.Serialize(widgetObj, options);
                    string escaped = json.Replace("'", "''");
                    ConnectionHelper.ExecuteQuery(
                        $@"UPDATE C
                          SET ContentItemCommonDataVisualBuilderWidgets = '{escaped}'
                          FROM CMS_ContentItemCommonData C
                          JOIN CMS_ContentItem CI ON C.ContentItemCommonDataContentItemID = CI.ContentItemID
                          JOIN CMS_Class CT ON CI.ContentItemContentTypeID = CT.ClassID
                          WHERE CT.ClassName = '{pageClassName}' AND (C.ContentItemCommonDataIsLatest = 1 OR C.ContentItemCommonDataVersionStatus = 2)",
                        null,
                        QueryTypeEnum.SQLQuery);
                    logs.Add($"Saved widgets for page type '{pageClassName}' from dynamic Content Hub data");
                }

                SaveWidgets("TimeToTalk.HomePage", homeWidgets);
                SaveWidgets("TimeToTalk.ListingPage", listingWidgets);
                SaveWidgets("TimeToTalk.DetailPage", detailWidgets);

                return Ok(new { success = true, logs });
            }
            catch (Exception ex)
            {
                logs.Add($"Error: {ex}");
                return StatusCode(500, new { success = false, error = ex.Message, logs });
            }
        }
    }
}
