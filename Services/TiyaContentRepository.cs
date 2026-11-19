#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;
using Microsoft.Extensions.Logging;
using Podcast.Web.Components.Widgets.TiyaEvents;
using Podcast.Web.Models;
using TimeToTalk;

namespace Podcast.Web.Services
{
    public class TiyaContentRepository : ITiyaContentRepository
    {
        private readonly IContentQueryExecutor _queryExecutor;
        private readonly IContentItemManagerFactory _itemManagerFactory;
        private readonly IContentFolderManagerFactory _folderManagerFactory;
        private readonly IInfoProvider<UserInfo> _userInfoProvider;
        private readonly ILogger<TiyaContentRepository> _logger;

        public const string FOLDER_EVENTS = "Events";
        public const string FOLDER_MEMBERSHIPS = "Memberships";
        public const string FOLDER_TEAM = "Team";
        public const string FOLDER_SHARED = "SharedContent";

        public TiyaContentRepository(
            IContentQueryExecutor queryExecutor,
            IContentItemManagerFactory itemManagerFactory,
            IContentFolderManagerFactory folderManagerFactory,
            IInfoProvider<UserInfo> userInfoProvider,
            ILogger<TiyaContentRepository> logger)
        {
            _queryExecutor = queryExecutor;
            _itemManagerFactory = itemManagerFactory;
            _folderManagerFactory = folderManagerFactory;
            _userInfoProvider = userInfoProvider;
            _logger = logger;
        }

        private int GetAdminUserId()
        {
            try
            {
                var admin = _userInfoProvider.Get().GetEnumerableTypedResult().FirstOrDefault();
                return admin?.UserID ?? 53;
            }
            catch
            {
                return 53;
            }
        }

        private HashSet<int> GetFolderItemIds(string folderName)
        {
            var ids = new HashSet<int>();
            try
            {
                var ds = ConnectionHelper.ExecuteQuery(
                    $"SELECT CI.ContentItemID FROM CMS_ContentItem CI JOIN CMS_ContentFolder C ON CI.ContentItemContentFolderID = C.ContentFolderID WHERE C.ContentFolderName = '{folderName.Replace("'", "''")}'",
                    null, QueryTypeEnum.SQLQuery);

                if (ds != null && ds.Tables.Count > 0)
                {
                    foreach (DataRow r in ds.Tables[0].Rows)
                    {
                        ids.Add(Convert.ToInt32(r["ContentItemID"]));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query item IDs for folder '{FolderName}'.", folderName);
            }
            return ids;
        }

        // ==========================================
        // 1. DYNAMIC EVENTS FROM CONTENT HUB
        // ==========================================
        public async Task<List<EventItemModel>> GetEventsAsync(int? topN = null)
        {
            var eventList = new List<EventItemModel>();

            try
            {
                var builder = new ContentItemQueryBuilder()
                    .ForContentType(PodcastEpisode.CONTENT_TYPE_NAME, q =>
                    {
                        q.OrderBy(nameof(PodcastEpisode.EpisodeNumber), nameof(PodcastEpisode.PublishDate));
                    });

                var episodes = await _queryExecutor.GetMappedResult<PodcastEpisode>(builder);
                if (episodes != null)
                {
                    foreach (var ep in episodes)
                    {
                        var day = ep.PublishDate != default ? ep.PublishDate.Day.ToString() : "24";
                        var monthYear = ep.PublishDate != default ? ep.PublishDate.ToString("MMM yyyy", CultureInfo.InvariantCulture) : "Feb 2048";
                        var price = !string.IsNullOrWhiteSpace(ep.EpisodeListensCount) ? ep.EpisodeListensCount : "$250";
                        var location = !string.IsNullOrWhiteSpace(ep.EpisodeShortDescription) ? ep.EpisodeShortDescription : "National Center, NYC";
                        var img = !string.IsNullOrWhiteSpace(ep.EpisodeThumbnail?.Url) 
                            ? ep.EpisodeThumbnail.Url 
                            : GetFallbackEventImage(ep.EpisodeNumber);

                        var slug = !string.IsNullOrWhiteSpace(ep.EpisodeSlug) 
                            ? ep.EpisodeSlug 
                            : CleanSlug(ep.EpisodeTitle ?? "event");

                        eventList.Add(new EventItemModel
                        {
                            Id = ep.SystemFields.ContentItemID,
                            Title = ep.EpisodeTitle ?? "Tournament Event",
                            Day = day,
                            MonthYear = monthYear,
                            EventDate = ep.PublishDate,
                            ImageUrl = img,
                            Description = ep.EpisodeDescription ?? ep.EpisodeShortDescription ?? "Join us for our upcoming championship golf event.",
                            Location = location,
                            TicketPrice = price,
                            Url = "/detail-page?slug=" + slug,
                            Slug = slug,
                            Category = ep.EpisodeTopicName ?? "Tournament",
                            IsAltBackground = ep.EpisodeRequiresSubscription,
                            Order = ep.EpisodeNumber
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load events dynamically from Content Hub, using seed dynamic data.");
            }

            // Filter by Content Hub 'Events' folder if populated
            var folderIds = GetFolderItemIds(FOLDER_EVENTS);
            if (folderIds.Any())
            {
                var folderEvents = eventList.Where(e => folderIds.Contains(e.Id)).OrderBy(e => e.Order).ToList();
                if (folderEvents.Any())
                {
                    eventList = folderEvents;
                }
            }

            if (!eventList.Any())
            {
                eventList = GetInitialTiyaEvents();
            }

            if (topN.HasValue && topN.Value > 0)
            {
                eventList = eventList.Take(topN.Value).ToList();
            }

            return eventList;
        }

        public async Task<EventItemModel?> GetEventBySlugAsync(string slug)
        {
            var all = await GetEventsAsync();
            return all.FirstOrDefault(e => string.Equals(e.Slug, slug, StringComparison.OrdinalIgnoreCase))
                   ?? all.FirstOrDefault();
        }

        public async Task<EventItemModel?> GetEventByIdAsync(int id)
        {
            var all = await GetEventsAsync();
            return all.FirstOrDefault(e => e.Id == id);
        }

        public async Task<List<EventItemModel>> GetEventsByGuidsAsync(IEnumerable<Guid> guids)
        {
            var list = new List<EventItemModel>();
            var guidList = guids?.ToList() ?? new List<Guid>();
            if (!guidList.Any()) return list;

            try
            {
                var builder = new ContentItemQueryBuilder()
                    .ForContentType(PodcastEpisode.CONTENT_TYPE_NAME, q =>
                    {
                        q.Where(w => w.WhereIn(nameof(IContentItemFieldsSource.SystemFields.ContentItemGUID), guidList));
                    });

                var episodes = await _queryExecutor.GetMappedResult<PodcastEpisode>(builder);
                if (episodes != null)
                {
                    var epDict = episodes.ToDictionary(e => e.SystemFields.ContentItemGUID);
                    int idx = 0;
                    foreach (var g in guidList)
                    {
                        if (epDict.TryGetValue(g, out var ep))
                        {
                            var day = ep.PublishDate != default ? ep.PublishDate.Day.ToString() : "24";
                            var monthYear = ep.PublishDate != default ? ep.PublishDate.ToString("MMM yyyy", CultureInfo.InvariantCulture) : "Feb 2048";
                            var price = !string.IsNullOrWhiteSpace(ep.EpisodeListensCount) ? ep.EpisodeListensCount : "$250";
                            var location = !string.IsNullOrWhiteSpace(ep.EpisodeShortDescription) ? ep.EpisodeShortDescription : "National Center, NYC";
                            var img = !string.IsNullOrWhiteSpace(ep.EpisodeThumbnail?.Url) 
                                ? ep.EpisodeThumbnail.Url 
                                : GetFallbackEventImage(ep.EpisodeNumber);
                            var slug = !string.IsNullOrWhiteSpace(ep.EpisodeSlug) 
                                ? ep.EpisodeSlug 
                                : (ep.SystemFields.ContentItemName ?? $"event-{ep.EpisodeNumber}");

                            list.Add(new EventItemModel
                            {
                                Id = ep.SystemFields.ContentItemID,
                                Title = ep.EpisodeTitle ?? "Tournament Event",
                                Day = day,
                                MonthYear = monthYear,
                                EventDate = ep.PublishDate,
                                ImageUrl = img,
                                Description = ep.EpisodeDescription ?? ep.EpisodeShortDescription ?? "Join us for our upcoming championship golf event.",
                                Location = location,
                                TicketPrice = price,
                                Url = "/detail-page?slug=" + slug,
                                Slug = slug,
                                Category = ep.EpisodeTopicName ?? "Tournament",
                                IsAltBackground = ep.EpisodeRequiresSubscription,
                                Order = ep.EpisodeNumber
                            });
                            idx++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying events by GUIDs from Content Hub.");
            }

            return list;
        }

        // ==========================================
        // 2. DYNAMIC TEAM MEMBERS FROM CONTENT HUB
        // ==========================================
        public async Task<List<TeamMemberModel>> GetTeamMembersAsync()
        {
            var team = new List<TeamMemberModel>();

            try
            {
                var builder = new ContentItemQueryBuilder()
                    .ForContentType(PodcastHost.CONTENT_TYPE_NAME);

                var hosts = await _queryExecutor.GetMappedResult<PodcastHost>(builder);
                if (hosts != null && hosts.Any())
                {
                    int idx = 1;
                    foreach (var h in hosts)
                    {
                        var img = idx == 1 
                            ? "/images/members/portrait-young-handsome-businessman-wearing-suit-standing-with-crossed-arms-with-isolated-studio-white-background.jpg"
                            : "/images/members/successful-asian-lady-boss-red-blazer-holding-clipboard-with-documens-pen-working-looking-happy-white-background.jpg";

                        team.Add(new TeamMemberModel
                        {
                            Id = h.SystemFields.ContentItemID,
                            Name = h.HostName ?? "Club Staff",
                            Role = h.HostRole ?? "Management",
                            Biography = h.HostBiography ?? "",
                            ImageUrl = img,
                            TwitterUrl = !string.IsNullOrWhiteSpace(h.HostTwitterURL) ? h.HostTwitterURL : "https://twitter.com",
                            WhatsAppUrl = !string.IsNullOrWhiteSpace(h.HostFacebookURL) ? h.HostFacebookURL : "https://whatsapp.com",
                            LinkedInUrl = !string.IsNullOrWhiteSpace(h.HostLinkedInURL) ? h.HostLinkedInURL : "https://linkedin.com",
                            SocialLinks = ParseSocialLinks(h),
                            Order = idx++
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load team members dynamically from Content Hub.");
            }

            // Filter by Content Hub 'Team' folder if populated
            var folderIds = GetFolderItemIds(FOLDER_TEAM);
            if (folderIds.Any())
            {
                var folderTeam = team.Where(t => folderIds.Contains(t.Id)).OrderBy(t => t.Order).ToList();
                if (folderTeam.Any())
                {
                    team = folderTeam;
                }
            }

            if (!team.Any())
            {
                team = GetInitialTeamMembers();
            }

            return team;
        }

        public async Task<List<TeamMemberModel>> GetTeamMembersByGuidsAsync(IEnumerable<Guid> guids)
        {
            var list = new List<TeamMemberModel>();
            var guidList = guids?.ToList() ?? new List<Guid>();
            if (!guidList.Any()) return list;

            try
            {
                var builder = new ContentItemQueryBuilder()
                    .ForContentType(PodcastHost.CONTENT_TYPE_NAME, q =>
                    {
                        q.Where(w => w.WhereIn(nameof(IContentItemFieldsSource.SystemFields.ContentItemGUID), guidList));
                    });

                var hosts = await _queryExecutor.GetMappedResult<PodcastHost>(builder);
                if (hosts != null)
                {
                    var hostDict = hosts.ToDictionary(h => h.SystemFields.ContentItemGUID);
                    int order = 1;
                    foreach (var g in guidList)
                    {
                        if (hostDict.TryGetValue(g, out var h))
                        {
                            var img = order == 1 
                                ? "/images/members/portrait-young-handsome-businessman-wearing-suit-standing-with-crossed-arms-with-isolated-studio-white-background.jpg"
                                : "/images/members/successful-asian-lady-boss-red-blazer-holding-clipboard-with-documens-pen-working-looking-happy-white-background.jpg";

                            list.Add(new TeamMemberModel
                            {
                                Id = h.SystemFields.ContentItemID,
                                Name = h.HostName ?? "Club Staff",
                                Role = h.HostRole ?? "Management",
                                Biography = h.HostBiography ?? "",
                                ImageUrl = img,
                                TwitterUrl = !string.IsNullOrWhiteSpace(h.HostTwitterURL) ? h.HostTwitterURL : "https://twitter.com",
                                WhatsAppUrl = !string.IsNullOrWhiteSpace(h.HostFacebookURL) ? h.HostFacebookURL : "https://whatsapp.com",
                                LinkedInUrl = !string.IsNullOrWhiteSpace(h.HostLinkedInURL) ? h.HostLinkedInURL : "https://linkedin.com",
                                SocialLinks = ParseSocialLinks(h),
                                Order = order++
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load team members by GUIDs.");
            }

            return list;
        }

        // ==========================================
        // 3. DYNAMIC MEMBERSHIP TIERS & FEATURES
        // ==========================================
        public async Task<List<MembershipTierModel>> GetMembershipTiersAsync()
        {
            var tiers = new List<MembershipTierModel>();

            try
            {
                var builder = new ContentItemQueryBuilder()
                    .ForContentType(WebsiteContentBlock.CONTENT_TYPE_NAME, q => q.Where(w => w.WhereEquals(nameof(WebsiteContentBlock.BlockType), "MembershipTier")));

                var blocks = await _queryExecutor.GetMappedResult<WebsiteContentBlock>(builder);
                if (blocks != null && blocks.Any())
                {
                    foreach (var b in blocks.OrderBy(x => x.DisplayOrder))
                    {
                        tiers.Add(MapBlockToTier(b));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query membership tiers from Content Hub.");
            }

            var folderIds = GetFolderItemIds(FOLDER_MEMBERSHIPS);
            if (folderIds.Any())
            {
                var folderTiers = tiers.Where(t => folderIds.Contains(t.Id)).OrderBy(t => t.Order).ToList();
                if (folderTiers.Any())
                {
                    tiers = folderTiers;
                }
            }

            if (!tiers.Any())
            {
                tiers = GetInitialMembershipTiers();
            }

            return tiers;
        }

        public async Task<List<MembershipTierModel>> GetMembershipTiersByGuidsAsync(IEnumerable<Guid> guids)
        {
            var list = new List<MembershipTierModel>();
            var guidList = guids?.ToList() ?? new List<Guid>();
            if (!guidList.Any()) return list;

            try
            {
                var builder = new ContentItemQueryBuilder()
                    .ForContentType(WebsiteContentBlock.CONTENT_TYPE_NAME, q =>
                    {
                        q.Where(w => w.WhereIn(nameof(IContentItemFieldsSource.SystemFields.ContentItemGUID), guidList));
                    });

                var blocks = await _queryExecutor.GetMappedResult<WebsiteContentBlock>(builder);
                if (blocks != null)
                {
                    var blockDict = blocks.ToDictionary(b => b.SystemFields.ContentItemGUID);
                    foreach (var g in guidList)
                    {
                        if (blockDict.TryGetValue(g, out var b))
                        {
                            list.Add(MapBlockToTier(b));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query membership tiers by GUIDs from Content Hub.");
            }

            return list;
        }

        public async Task<List<MembershipFeatureRowModel>> GetMembershipFeaturesAsync()
        {
            var features = new List<MembershipFeatureRowModel>();

            try
            {
                var builder = new ContentItemQueryBuilder()
                    .ForContentType(WebsiteContentBlock.CONTENT_TYPE_NAME, q => q.Where(w => w.WhereEquals(nameof(WebsiteContentBlock.BlockType), "MembershipFeature")));

                var blocks = await _queryExecutor.GetMappedResult<WebsiteContentBlock>(builder);
                if (blocks != null && blocks.Any())
                {
                    foreach (var b in blocks.OrderBy(x => x.DisplayOrder))
                    {
                        features.Add(MapBlockToFeature(b));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query membership features from Content Hub.");
            }

            var folderIds = GetFolderItemIds(FOLDER_MEMBERSHIPS);
            if (folderIds.Any())
            {
                var folderFeatures = features.Where(f => folderIds.Contains(f.Id)).ToList();
                if (folderFeatures.Any())
                {
                    features = folderFeatures;
                }
            }

            if (!features.Any())
            {
                features = GetInitialMembershipFeatures();
            }

            return features;
        }

        public async Task<List<MembershipFeatureRowModel>> GetMembershipFeaturesByGuidsAsync(IEnumerable<Guid> guids)
        {
            var list = new List<MembershipFeatureRowModel>();
            var guidList = guids?.ToList() ?? new List<Guid>();
            if (!guidList.Any()) return list;

            try
            {
                var builder = new ContentItemQueryBuilder()
                    .ForContentType(WebsiteContentBlock.CONTENT_TYPE_NAME, q =>
                    {
                        q.Where(w => w.WhereIn(nameof(IContentItemFieldsSource.SystemFields.ContentItemGUID), guidList));
                    });

                var blocks = await _queryExecutor.GetMappedResult<WebsiteContentBlock>(builder);
                if (blocks != null)
                {
                    var blockDict = blocks.ToDictionary(b => b.SystemFields.ContentItemGUID);
                    foreach (var g in guidList)
                    {
                        if (blockDict.TryGetValue(g, out var b))
                        {
                            list.Add(MapBlockToFeature(b));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query membership features by GUIDs from Content Hub.");
            }

            return list;
        }

        private static MembershipTierModel MapBlockToTier(WebsiteContentBlock b)
        {
            var price = !string.IsNullOrWhiteSpace(b.PrimaryButtonText) ? b.PrimaryButtonText.Trim() : "$250";
            var tierCode = !string.IsNullOrWhiteSpace(b.IconName) ? b.IconName.Trim() : "T1";
            var name = !string.IsNullOrWhiteSpace(b.Heading) ? b.Heading.Trim() : (b.BlockName ?? "Tier");

            return new MembershipTierModel
            {
                Id = b.SystemFields.ContentItemID,
                TierCode = tierCode,
                Name = name,
                Price = price,
                Description = b.Description ?? "",
                Order = b.DisplayOrder
            };
        }

        private static MembershipFeatureRowModel MapBlockToFeature(WebsiteContentBlock b)
        {
            var raw = b.PrimaryButtonURL ?? b.Description ?? "1,1,1";
            var flags = raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var availList = new List<bool>();
            foreach (var f in flags)
            {
                var trimmed = f.Trim();
                availList.Add(trimmed == "1" || trimmed.Equals("true", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("yes", StringComparison.OrdinalIgnoreCase));
            }

            return new MembershipFeatureRowModel
            {
                Id = b.SystemFields.ContentItemID,
                FeatureName = !string.IsNullOrWhiteSpace(b.Heading) ? b.Heading.Trim() : (b.BlockName ?? "Feature"),
                RawFlags = raw,
                TierAvailabilityList = availList
            };
        }

        public async Task<List<FormFieldModel>> GetMembershipFormFieldsAsync()
        {
            var fields = new List<FormFieldModel>();
            try
            {
                var block = await GetContentBlockAsync("membership-form-fields");
                if (block != null && !string.IsNullOrWhiteSpace(block.Description))
                {
                    var lines = block.Description.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 2)
                        {
                            var name = parts[0].Trim();
                            var label = parts[1].Trim();
                            var type = parts.Length > 2 ? parts[2].Trim().ToLowerInvariant() : "text";
                            var placeholder = parts.Length > 3 ? parts[3].Trim() : label;
                            var required = parts.Length > 4 ? (parts[4].Trim() == "1" || parts[4].Trim().Equals("true", StringComparison.OrdinalIgnoreCase)) : true;
                            var options = parts.Length > 5 
                                ? parts[5].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).ToList()
                                : new List<string>();

                            fields.Add(new FormFieldModel
                            {
                                Name = name,
                                Label = label,
                                Type = type,
                                Placeholder = placeholder,
                                IsRequired = required,
                                Options = options
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load membership form fields from Content Hub.");
            }

            if (!fields.Any())
            {
                fields = GetDefaultMembershipFormFields();
            }

            return fields;
        }

        private static List<FormFieldModel> GetDefaultMembershipFormFields()
        {
            return new List<FormFieldModel>
            {
                new FormFieldModel { Name = "fullName", Label = "Full Name", Type = "text", Placeholder = "Full Name", IsRequired = true },
                new FormFieldModel { Name = "email", Label = "Email address", Type = "email", Placeholder = "Email address", IsRequired = true },
                new FormFieldModel { Name = "phone", Label = "Phone Number", Type = "phone", Placeholder = "010-020-0340", IsRequired = false },
                new FormFieldModel { Name = "tier", Label = "Membership Tier", Type = "dropdown", Placeholder = "Select Plan", IsRequired = false, Options = new List<string> { "Standard Access ($600)", "Country Club Pro ($850)", "Platinum Masters ($1,100)" } },
                new FormFieldModel { Name = "comments", Label = "Comments", Type = "textarea", Placeholder = "Describe message here", IsRequired = false }
            };
        }

        private static List<SocialMediaLinkModel> ParseSocialLinks(PodcastHost h)
        {
            var links = new List<SocialMediaLinkModel>();
            if (!string.IsNullOrWhiteSpace(h.HostTwitterURL))
            {
                links.Add(new SocialMediaLinkModel { Platform = "Twitter", IconClass = "bi-twitter", Url = h.HostTwitterURL.Trim() });
            }
            if (!string.IsNullOrWhiteSpace(h.HostFacebookURL))
            {
                var u = h.HostFacebookURL.Trim();
                var isWa = u.Contains("whatsapp", StringComparison.OrdinalIgnoreCase) || u.Contains("wa.me", StringComparison.OrdinalIgnoreCase);
                links.Add(new SocialMediaLinkModel { Platform = isWa ? "WhatsApp" : "Facebook", IconClass = isWa ? "bi-whatsapp" : "bi-facebook", Url = u });
            }
            if (!string.IsNullOrWhiteSpace(h.HostLinkedInURL))
            {
                links.Add(new SocialMediaLinkModel { Platform = "LinkedIn", IconClass = "bi-linkedin", Url = h.HostLinkedInURL.Trim() });
            }

            var extra = $"{h.HostSpecialties} {h.HostBiography}";
            if (!string.IsNullOrWhiteSpace(extra) && extra.Contains("http", StringComparison.OrdinalIgnoreCase))
            {
                var tokens = extra.Split(new[] { ' ', ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var token in tokens)
                {
                    if (token.Contains("instagram.com", StringComparison.OrdinalIgnoreCase) && !links.Any(l => l.Platform.Equals("Instagram", StringComparison.OrdinalIgnoreCase)))
                    {
                        links.Add(new SocialMediaLinkModel { Platform = "Instagram", IconClass = "bi-instagram", Url = token.Trim() });
                    }
                    else if (token.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) && !links.Any(l => l.Platform.Equals("YouTube", StringComparison.OrdinalIgnoreCase)))
                    {
                        links.Add(new SocialMediaLinkModel { Platform = "YouTube", IconClass = "bi-youtube", Url = token.Trim() });
                    }
                    else if (token.Contains("facebook.com", StringComparison.OrdinalIgnoreCase) && !links.Any(l => l.Platform.Equals("Facebook", StringComparison.OrdinalIgnoreCase)))
                    {
                        links.Add(new SocialMediaLinkModel { Platform = "Facebook", IconClass = "bi-facebook", Url = token.Trim() });
                    }
                }
            }

            return links;
        }

        public async Task<WebsiteContentBlock?> GetContentBlockAsync(string blockName)
        {
            try
            {
                var builder = new ContentItemQueryBuilder()
                    .ForContentType(WebsiteContentBlock.CONTENT_TYPE_NAME, q => q.Where(w => w.WhereEquals(nameof(WebsiteContentBlock.BlockName), blockName)));

                var blocks = await _queryExecutor.GetMappedResult<WebsiteContentBlock>(builder);
                return blocks?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load content block '{BlockName}' from Content Hub.", blockName);
                return null;
            }
        }

        public async Task<WebsiteContentBlock?> GetContentBlockByGuidAsync(Guid guid)
        {
            try
            {
                var builder = new ContentItemQueryBuilder()
                    .ForContentType(WebsiteContentBlock.CONTENT_TYPE_NAME, q => q.Where(w => w.WhereEquals(nameof(IContentItemFieldsSource.SystemFields.ContentItemGUID), guid)));

                var blocks = await _queryExecutor.GetMappedResult<WebsiteContentBlock>(builder);
                return blocks?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load content block with GUID '{Guid}' from Content Hub.", guid);
                return null;
            }
        }

        // ==========================================
        // DYNAMIC WEBSITE SETTINGS & HOME PAGE
        // ==========================================
        public async Task<TiyaWebsiteSettings> GetWebsiteSettingsAsync()
        {
            try
            {
                var builder = new ContentItemQueryBuilder()
                    .ForContentType(TiyaWebsiteSettings.CONTENT_TYPE_NAME);

                var items = await _queryExecutor.GetMappedResult<TiyaWebsiteSettings>(builder);
                var settings = items?.FirstOrDefault();
                if (settings != null)
                {
                    return settings;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load TiyaWebsiteSettings from Content Hub. Using default settings.");
            }

            return new TiyaWebsiteSettings
            {
                WebsiteName = "Tiya Golf Club",
                HeaderLogoUrl = "/images/logo.png",
                FooterLogoUrl = "/images/logo.png",
                FaviconUrl = "/favicon.ico",
                MemberLoginButtonText = "Member Login",
                MemberLoginButtonUrl = "#offcanvasExample",
                FooterDescription = "Tiya Golf Club is a modern sports club offering championship courses, professional coaching, premium clubhouse facilities, and a thriving community for golf enthusiasts.",
                ContactEmail = "info@tiyagolf.com",
                ContactPhone = "(010) 020-0340",
                FooterAddress = "1200 Grand Avenue, Tiya Valley, CA 90210",
                CopyrightText = "Copyright © 2026 Tiya Golf Club. All rights reserved.",
                FacebookUrl = "https://facebook.com",
                TwitterUrl = "https://twitter.com",
                InstagramUrl = "https://instagram.com",
                LinkedInUrl = "https://linkedin.com",
                WhatsAppUrl = "https://whatsapp.com",
                YouTubeUrl = "https://youtube.com"
            };
        }

        public async Task<HomePage?> GetHomePageContentAsync()
        {
            try
            {
                var builder = new ContentItemQueryBuilder()
                    .ForContentType(HomePage.CONTENT_TYPE_NAME);

                var pages = await _queryExecutor.GetMappedResult<HomePage>(builder);
                return pages?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load HomePage content from CMS.");
                return null;
            }
        }

        // ==========================================
        // DYNAMIC HEADER AND FOOTER (CONTENT HUB)
        // ==========================================
        public async Task<TiyaHeaderModel> GetHeaderConfigAsync()
        {
            var header = new TiyaHeaderModel();
            try
            {
                var settings = await GetWebsiteSettingsAsync();
                if (settings != null)
                {
                    if (!string.IsNullOrWhiteSpace(settings.WebsiteName))
                    {
                        header.BrandTitle = settings.WebsiteName;
                        header.BrandLogoAlt = settings.WebsiteName;
                    }
                    if (!string.IsNullOrWhiteSpace(settings.HeaderLogoUrl))
                    {
                        header.BrandLogoUrl = settings.HeaderLogoUrl;
                    }
                    if (!string.IsNullOrWhiteSpace(settings.MemberLoginButtonText))
                    {
                        header.ButtonText = settings.MemberLoginButtonText;
                    }
                    if (!string.IsNullOrWhiteSpace(settings.MemberLoginButtonUrl))
                    {
                        header.ButtonUrl = settings.MemberLoginButtonUrl;
                    }
                    if (!string.IsNullOrWhiteSpace(settings.ContactPhone))
                    {
                        header.QuestionsPhone = settings.ContactPhone;
                    }
                }

                var block = await GetContentBlockAsync("tiya-header-config");
                if (block != null)
                {
                    header.Id = block.SystemFields.ContentItemID;
                    if (!string.IsNullOrWhiteSpace(block.Heading)) header.BrandTitle = block.Heading.Trim();
                    if (!string.IsNullOrWhiteSpace(block.IconName)) header.BrandSubtitle = block.IconName.Trim();
                    if (!string.IsNullOrWhiteSpace(block.PrimaryButtonText)) header.ButtonText = block.PrimaryButtonText.Trim();
                    if (!string.IsNullOrWhiteSpace(block.PrimaryButtonURL)) header.ButtonUrl = block.PrimaryButtonURL.Trim();

                    if (!string.IsNullOrWhiteSpace(block.Description))
                    {
                        var parts = block.Description.Split('|', StringSplitOptions.TrimEntries);
                        if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0])) header.QuestionsTitle = parts[0];
                        if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])) header.QuestionsPhone = parts[1];
                        if (parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2])) header.SearchPlaceholder = parts[2];
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load header config from Content Hub.");
            }
            return header;
        }

        public async Task<int> CreateOrUpdateHeaderConfigAsync(TiyaHeaderModel model, CancellationToken cancellationToken = default)
        {
            int folderId = await EnsureFolderAsync(FOLDER_SHARED, "Shared Content", cancellationToken);
            string codeName = "tiya-header-config";

            var data = new Dictionary<string, object>
            {
                { nameof(WebsiteContentBlock.BlockName), codeName },
                { nameof(WebsiteContentBlock.Heading), model.BrandTitle },
                { nameof(WebsiteContentBlock.IconName), model.BrandSubtitle },
                { nameof(WebsiteContentBlock.Description), $"{model.QuestionsTitle}|{model.QuestionsPhone}|{model.SearchPlaceholder}" },
                { nameof(WebsiteContentBlock.PrimaryButtonText), model.ButtonText },
                { nameof(WebsiteContentBlock.PrimaryButtonURL), model.ButtonUrl },
                { nameof(WebsiteContentBlock.BlockType), "HeaderConfig" },
                { nameof(WebsiteContentBlock.IsActive), true }
            };

            int itemId = await SaveOrUpdateItemAsync(
                WebsiteContentBlock.CONTENT_TYPE_NAME,
                codeName,
                "Tiya Header Configuration",
                data,
                model.Id > 0 ? model.Id : null,
                folderId,
                cancellationToken);

            _logger.LogInformation("Saved dynamic header configuration ID {Id} in Content Hub folder '{Folder}'.", itemId, FOLDER_SHARED);
            return itemId;
        }

        public async Task<TiyaFooterModel> GetFooterConfigAsync()
        {
            var footer = new TiyaFooterModel();
            try
            {
                var settings = await GetWebsiteSettingsAsync();
                if (settings != null)
                {
                    if (!string.IsNullOrWhiteSpace(settings.WebsiteName)) footer.BrandTitle = settings.WebsiteName;
                    if (!string.IsNullOrWhiteSpace(settings.FooterLogoUrl)) footer.BrandLogoUrl = settings.FooterLogoUrl;
                    if (!string.IsNullOrWhiteSpace(settings.FooterDescription)) footer.Description = settings.FooterDescription;
                    if (!string.IsNullOrWhiteSpace(settings.ContactEmail)) footer.EmailAddress = settings.ContactEmail;
                    if (!string.IsNullOrWhiteSpace(settings.ContactPhone)) footer.PhoneNumber = settings.ContactPhone;
                    if (!string.IsNullOrWhiteSpace(settings.CopyrightText)) footer.CopyrightText = settings.CopyrightText;
                    if (!string.IsNullOrWhiteSpace(settings.FacebookUrl)) footer.FacebookUrl = settings.FacebookUrl;
                    if (!string.IsNullOrWhiteSpace(settings.TwitterUrl)) footer.TwitterUrl = settings.TwitterUrl;
                    if (!string.IsNullOrWhiteSpace(settings.InstagramUrl)) footer.InstagramUrl = settings.InstagramUrl;
                    if (!string.IsNullOrWhiteSpace(settings.LinkedInUrl)) footer.LinkedInUrl = settings.LinkedInUrl;
                    if (!string.IsNullOrWhiteSpace(settings.WhatsAppUrl)) footer.WhatsAppUrl = settings.WhatsAppUrl;
                }

                var block = await GetContentBlockAsync("tiya-footer-config");
                if (block != null)
                {
                    footer.Id = block.SystemFields.ContentItemID;
                    if (!string.IsNullOrWhiteSpace(block.Description)) footer.Description = block.Description.Trim();
                    if (!string.IsNullOrWhiteSpace(block.Heading)) footer.CopyrightText = block.Heading.Trim();
                    if (!string.IsNullOrWhiteSpace(block.PrimaryButtonText)) footer.DesignerCredit = block.PrimaryButtonText.Trim();

                    if (!string.IsNullOrWhiteSpace(block.IconName))
                    {
                        var hourParts = block.IconName.Split('|', StringSplitOptions.TrimEntries);
                        if (hourParts.Length >= 5)
                        {
                            if (!string.IsNullOrWhiteSpace(hourParts[0])) footer.ScheduleTitle = hourParts[0];
                            if (!string.IsNullOrWhiteSpace(hourParts[1])) footer.WeekdayLabel = hourParts[1];
                            if (!string.IsNullOrWhiteSpace(hourParts[2])) footer.WeekdayHours = hourParts[2];
                            if (!string.IsNullOrWhiteSpace(hourParts[3])) footer.WeekendLabel = hourParts[3];
                            if (!string.IsNullOrWhiteSpace(hourParts[4])) footer.WeekendHours = hourParts[4];
                        }
                        else if (hourParts.Length >= 2)
                        {
                            footer.ScheduleTitle = "Join Us";
                            footer.WeekdayLabel = "Mon-Fri";
                            footer.WeekdayHours = hourParts[0];
                            footer.WeekendLabel = "Sat-Sun";
                            footer.WeekendHours = hourParts[1];
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(block.PrimaryButtonURL))
                    {
                        var socialParts = block.PrimaryButtonURL.Split('|', StringSplitOptions.TrimEntries);
                        if (socialParts.Length > 0 && !string.IsNullOrWhiteSpace(socialParts[0])) footer.InstagramUrl = socialParts[0];
                        if (socialParts.Length > 1 && !string.IsNullOrWhiteSpace(socialParts[1])) footer.TwitterUrl = socialParts[1];
                        if (socialParts.Length > 2 && !string.IsNullOrWhiteSpace(socialParts[2])) footer.WhatsAppUrl = socialParts[2];
                        if (socialParts.Length > 3 && !string.IsNullOrWhiteSpace(socialParts[3])) footer.FacebookUrl = socialParts[3];
                        if (socialParts.Length > 4 && !string.IsNullOrWhiteSpace(socialParts[4])) footer.LinkedInUrl = socialParts[4];
                    }

                    if (!string.IsNullOrWhiteSpace(block.SecondaryButtonText))
                    {
                        footer.PhoneNumber = block.SecondaryButtonText.Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(block.SecondaryButtonURL))
                    {
                        var extraParts = block.SecondaryButtonURL.Split('|', StringSplitOptions.TrimEntries);
                        if (extraParts.Length > 0 && !string.IsNullOrWhiteSpace(extraParts[0])) footer.EmailAddress = extraParts[0];
                        if (extraParts.Length > 1 && !string.IsNullOrWhiteSpace(extraParts[1])) footer.BrandLogoUrl = extraParts[1];
                        if (extraParts.Length > 2 && !string.IsNullOrWhiteSpace(extraParts[2])) footer.DesignerUrl = extraParts[2];
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load footer config from Content Hub.");
            }
            return footer;
        }

        public async Task<int> CreateOrUpdateFooterConfigAsync(TiyaFooterModel model, CancellationToken cancellationToken = default)
        {
            int folderId = await EnsureFolderAsync(FOLDER_SHARED, "Shared Content", cancellationToken);
            string codeName = "tiya-footer-config";

            var data = new Dictionary<string, object>
            {
                { nameof(WebsiteContentBlock.BlockName), codeName },
                { nameof(WebsiteContentBlock.Heading), model.CopyrightText },
                { nameof(WebsiteContentBlock.Description), model.Description },
                { nameof(WebsiteContentBlock.IconName), $"{model.ScheduleTitle}|{model.WeekdayLabel}|{model.WeekdayHours}|{model.WeekendLabel}|{model.WeekendHours}" },
                { nameof(WebsiteContentBlock.PrimaryButtonText), model.DesignerCredit },
                { nameof(WebsiteContentBlock.PrimaryButtonURL), $"{model.InstagramUrl}|{model.TwitterUrl}|{model.WhatsAppUrl}|{model.FacebookUrl}|{model.LinkedInUrl}" },
                { nameof(WebsiteContentBlock.SecondaryButtonText), model.PhoneNumber },
                { nameof(WebsiteContentBlock.SecondaryButtonURL), $"{model.EmailAddress}|{model.BrandLogoUrl}|{model.DesignerUrl}" },
                { nameof(WebsiteContentBlock.BlockType), "FooterConfig" },
                { nameof(WebsiteContentBlock.IsActive), true }
            };

            int itemId = await SaveOrUpdateItemAsync(
                WebsiteContentBlock.CONTENT_TYPE_NAME,
                codeName,
                "Tiya Footer Configuration",
                data,
                model.Id > 0 ? model.Id : null,
                folderId,
                cancellationToken);

            _logger.LogInformation("Saved dynamic footer configuration ID {Id} in Content Hub folder '{Folder}'.", itemId, FOLDER_SHARED);
            return itemId;
        }

        // ==========================================
        // DYNAMIC NAVIGATION ITEMS (CONTENT HUB)
        // ==========================================
        public async Task<List<NavigationItemViewModel>> GetContentHubNavigationItemsAsync()
        {
            var list = new List<NavigationItemViewModel>();
            try
            {
                var builder = new ContentItemQueryBuilder()
                    .ForContentType(WebsiteContentBlock.CONTENT_TYPE_NAME, q =>
                    {
                        q.Where(w => w.WhereEquals(nameof(WebsiteContentBlock.BlockType), "NavigationItem")
                                      .WhereTrue(nameof(WebsiteContentBlock.IsActive)))
                         .OrderBy(nameof(WebsiteContentBlock.DisplayOrder));
                    });

                var blocks = await _queryExecutor.GetMappedResult<WebsiteContentBlock>(builder);
                if (blocks != null)
                {
                    foreach (var b in blocks)
                    {
                        var url = !string.IsNullOrWhiteSpace(b.PrimaryButtonURL) ? b.PrimaryButtonURL.Trim() : "/";
                        list.Add(new NavigationItemViewModel
                        {
                            WebPageItemId = b.SystemFields.ContentItemID,
                            Title = !string.IsNullOrWhiteSpace(b.Heading) ? b.Heading.Trim() : (b.BlockName ?? "Navigation Link"),
                            Url = url,
                            IsClickScroll = url.StartsWith("#") || url.Contains("/#"),
                            IsActive = false
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load dynamic navigation items from Content Hub.");
            }
            return list;
        }

        public async Task<int> CreateOrUpdateNavigationItemAsync(NavigationItemViewModel item, CancellationToken cancellationToken = default)
        {
            int folderId = await EnsureFolderAsync("Navigation", "Navigation", cancellationToken);
            string codeName = "nav-" + CleanSlug(item.Title);

            var data = new Dictionary<string, object>
            {
                { nameof(WebsiteContentBlock.BlockName), codeName },
                { nameof(WebsiteContentBlock.Heading), item.Title },
                { nameof(WebsiteContentBlock.PrimaryButtonURL), item.Url },
                { nameof(WebsiteContentBlock.BlockType), "NavigationItem" },
                { nameof(WebsiteContentBlock.DisplayOrder), item.WebPageItemId > 0 ? item.WebPageItemId : 1 },
                { nameof(WebsiteContentBlock.IsActive), true }
            };

            int itemId = await SaveOrUpdateItemAsync(
                WebsiteContentBlock.CONTENT_TYPE_NAME,
                codeName,
                item.Title,
                data,
                item.WebPageItemId > 0 ? item.WebPageItemId : null,
                folderId,
                cancellationToken);

            _logger.LogInformation("Saved navigation item '{Title}' ID {Id} in Content Hub folder 'Navigation'.", item.Title, itemId);
            return itemId;
        }

        public async Task<bool> DeleteNavigationItemAsync(string codeName, CancellationToken cancellationToken = default)
        {
            var block = await GetContentBlockAsync(codeName);
            if (block == null) return false;
            try
            {
                var itemManager = _itemManagerFactory.Create(GetAdminUserId());
                await itemManager.Delete(block.SystemFields.ContentItemID, "en", cancellationToken);
                _logger.LogInformation("Deleted navigation item '{CodeName}'.", codeName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete navigation item '{CodeName}'.", codeName);
                return false;
            }
        }

        // ==========================================
        // SHARED CONTENT ITEMS
        // ==========================================
        public async Task<List<SharedContentItemModel>> GetSharedContentAsync()
        {
            var list = new List<SharedContentItemModel>();
            try
            {
                var builder = new ContentItemQueryBuilder()
                    .ForContentType(WebsiteContentBlock.CONTENT_TYPE_NAME, q => q.Where(w => w.WhereNotIn(nameof(WebsiteContentBlock.BlockType), new[] { "MembershipTier", "MembershipFeature" })));

                var blocks = await _queryExecutor.GetMappedResult<WebsiteContentBlock>(builder);
                if (blocks != null)
                {
                    foreach (var b in blocks)
                    {
                        list.Add(new SharedContentItemModel
                        {
                            Id = b.SystemFields.ContentItemID,
                            BlockName = b.BlockName ?? "",
                            Heading = b.Heading ?? "",
                            Description = b.Description ?? "",
                            IconName = b.IconName ?? "",
                            PrimaryButtonText = b.PrimaryButtonText ?? "",
                            PrimaryButtonURL = b.PrimaryButtonURL ?? "",
                            BlockType = b.BlockType ?? "",
                            DisplayOrder = b.DisplayOrder,
                            IsActive = b.IsActive
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load shared content items.");
            }
            return list;
        }

        public async Task<SharedContentItemModel?> GetSharedContentItemAsync(string blockName)
        {
            var b = await GetContentBlockAsync(blockName);
            if (b == null) return null;

            return new SharedContentItemModel
            {
                Id = b.SystemFields.ContentItemID,
                BlockName = b.BlockName ?? "",
                Heading = b.Heading ?? "",
                Description = b.Description ?? "",
                IconName = b.IconName ?? "",
                PrimaryButtonText = b.PrimaryButtonText ?? "",
                PrimaryButtonURL = b.PrimaryButtonURL ?? "",
                BlockType = b.BlockType ?? "",
                DisplayOrder = b.DisplayOrder,
                IsActive = b.IsActive
            };
        }

        public async Task<int> CreateOrUpdateSharedContentItemAsync(SharedContentItemModel model, CancellationToken cancellationToken = default)
        {
            int folderId = await EnsureFolderAsync(FOLDER_SHARED, "Shared Content", cancellationToken);
            string codeName = !string.IsNullOrWhiteSpace(model.BlockName) ? model.BlockName : CleanSlug(model.Heading);

            var data = new Dictionary<string, object>
            {
                { nameof(WebsiteContentBlock.BlockName), codeName },
                { nameof(WebsiteContentBlock.Heading), model.Heading },
                { nameof(WebsiteContentBlock.Description), model.Description },
                { nameof(WebsiteContentBlock.IconName), model.IconName },
                { nameof(WebsiteContentBlock.PrimaryButtonText), model.PrimaryButtonText },
                { nameof(WebsiteContentBlock.PrimaryButtonURL), model.PrimaryButtonURL },
                { nameof(WebsiteContentBlock.BlockType), string.IsNullOrWhiteSpace(model.BlockType) ? "SharedBlock" : model.BlockType },
                { nameof(WebsiteContentBlock.DisplayOrder), model.DisplayOrder },
                { nameof(WebsiteContentBlock.IsActive), model.IsActive }
            };

            int itemId = await SaveOrUpdateItemAsync(
                WebsiteContentBlock.CONTENT_TYPE_NAME,
                codeName,
                string.IsNullOrWhiteSpace(model.Heading) ? codeName : model.Heading,
                data,
                model.Id > 0 ? model.Id : null,
                folderId,
                cancellationToken);

            _logger.LogInformation("Saved dynamic shared content block '{CodeName}' (ID {Id}) in Content Hub folder '{Folder}'.", codeName, itemId, FOLDER_SHARED);
            return itemId;
        }

        public async Task<bool> DeleteSharedContentItemAsync(string blockName, CancellationToken cancellationToken = default)
        {
            try
            {
                var ds = ConnectionHelper.ExecuteQuery(
                    $"SELECT ContentItemID FROM CMS_ContentItem WHERE ContentItemName = '{blockName.Replace("'", "''")}'",
                    null, QueryTypeEnum.SQLQuery);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    int itemId = Convert.ToInt32(ds.Tables[0].Rows[0]["ContentItemID"]);
                    int userId = GetAdminUserId();
                    var itemManager = _itemManagerFactory.Create(userId);
                    await itemManager.Delete(itemId, "en", cancellationToken);
                    _logger.LogInformation("Deleted shared content item '{BlockName}' (ID {Id}) from Content Hub.", blockName, itemId);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete shared content item '{BlockName}'.", blockName);
            }
            return false;
        }

        private async Task<int> SaveOrUpdateItemAsync(
            string contentTypeName,
            string codeName,
            string displayName,
            Dictionary<string, object> data,
            int? contentItemId,
            int folderId,
            CancellationToken cancellationToken)
        {
            int userId = GetAdminUserId();
            var itemManager = _itemManagerFactory.Create(userId);
            var folderManager = _folderManagerFactory.Create(userId);

            int existingId = contentItemId ?? 0;
            if (existingId <= 0)
            {
                try
                {
                    var ds = ConnectionHelper.ExecuteQuery(
                        $"SELECT ContentItemID FROM CMS_ContentItem WHERE ContentItemName = '{codeName.Replace("'", "''")}'",
                        null, QueryTypeEnum.SQLQuery);
                    if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        existingId = Convert.ToInt32(ds.Tables[0].Rows[0]["ContentItemID"]);
                    }
                }
                catch { }
            }

            if (existingId > 0)
            {
                try
                {
                    await itemManager.TryCreateDraft(existingId, "en", cancellationToken);
                    await itemManager.TryUpdateDraft(existingId, "en", new ContentItemData(data), cancellationToken);
                    await itemManager.TryPublish(existingId, "en", cancellationToken);
                    return existingId;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update draft for '{CodeName}', will attempt create.", codeName);
                }
            }

            var createParams = new CreateContentItemParameters(contentTypeName, codeName, displayName, "en");
            var itemData = new ContentItemData(data);
            int itemId = await itemManager.Create(createParams, itemData, cancellationToken);
            try { await itemManager.TryPublish(itemId, "en", cancellationToken); } catch { }

            if (folderId > 0)
            {
                try { await folderManager.MoveItems(folderId, new[] { itemId }, cancellationToken); } catch { }
            }

            return itemId;
        }

        // ==========================================
        // 4. CRUD OPERATIONS ON CONTENT HUB
        // ==========================================
        public async Task<int> CreateOrUpdateEventAsync(EventItemModel model, CancellationToken cancellationToken = default)
        {
            int userId = GetAdminUserId();
            var itemManager = _itemManagerFactory.Create(userId);
            var folderManager = _folderManagerFactory.Create(userId);

            int folderId = await EnsureFolderAsync(FOLDER_EVENTS, "Events", cancellationToken);
            string codeName = !string.IsNullOrWhiteSpace(model.Slug) ? model.Slug : CleanSlug(model.Title);

            var eventDate = model.EventDate ?? ParseDate(model.Day, model.MonthYear);

            var data = new Dictionary<string, object>
            {
                { nameof(PodcastEpisode.EpisodeTitle), model.Title },
                { nameof(PodcastEpisode.EpisodeShortDescription), model.Location },
                { nameof(PodcastEpisode.EpisodeDescription), model.Description },
                { nameof(PodcastEpisode.EpisodeListensCount), model.TicketPrice },
                { nameof(PodcastEpisode.PublishDate), eventDate },
                { nameof(PodcastEpisode.EpisodeRequiresSubscription), model.IsAltBackground },
                { nameof(PodcastEpisode.EpisodeSlug), codeName },
                { nameof(PodcastEpisode.EpisodeTopicName), model.Category },
                { nameof(PodcastEpisode.EpisodeNumber), model.Order }
            };

            if (model.Id > 0)
            {
                try
                {
                    await itemManager.TryCreateDraft(model.Id, "en", cancellationToken);
                    await itemManager.TryUpdateDraft(model.Id, "en", new ContentItemData(data), cancellationToken);
                    await itemManager.TryPublish(model.Id, "en", cancellationToken);
                    _logger.LogInformation("Updated dynamic event ID {Id} ('{Title}') in Content Hub.", model.Id, model.Title);
                    return model.Id;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update existing event ID {Id}.", model.Id);
                }
            }

            var createParams = new CreateContentItemParameters(PodcastEpisode.CONTENT_TYPE_NAME, codeName, model.Title, "en");
            var itemData = new ContentItemData(data);
            int itemId = await itemManager.Create(createParams, itemData, cancellationToken);
            await itemManager.TryPublish(itemId, "en", cancellationToken);

            if (folderId > 0)
            {
                try
                {
                    await folderManager.MoveItems(folderId, new[] { itemId }, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Warning moving event ID {Id} to folder {FolderId}.", itemId, folderId);
                }
            }

            _logger.LogInformation("Created new dynamic event ID {Id} ('{Title}') in Content Hub folder '{Folder}'.", itemId, model.Title, FOLDER_EVENTS);
            return itemId;
        }

        public async Task<bool> DeleteEventAsync(int contentItemId, CancellationToken cancellationToken = default)
        {
            try
            {
                var itemManager = _itemManagerFactory.Create(GetAdminUserId());
                await itemManager.Delete(contentItemId, "en", cancellationToken);
                _logger.LogInformation("Deleted event ID {Id} from Content Hub.", contentItemId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete event ID {Id} from Content Hub.", contentItemId);
                return false;
            }
        }

        public async Task<int> CreateOrUpdateTeamMemberAsync(TeamMemberModel model, CancellationToken cancellationToken = default)
        {
            int userId = GetAdminUserId();
            var itemManager = _itemManagerFactory.Create(userId);
            var folderManager = _folderManagerFactory.Create(userId);

            int folderId = await EnsureFolderAsync(FOLDER_TEAM, "Club Staff / Team", cancellationToken);
            string codeName = CleanSlug(model.Name);

            var data = new Dictionary<string, object>
            {
                { nameof(PodcastHost.HostName), model.Name },
                { nameof(PodcastHost.HostRole), model.Role },
                { nameof(PodcastHost.HostBiography), model.Biography },
                { nameof(PodcastHost.HostTwitterURL), model.TwitterUrl },
                { nameof(PodcastHost.HostFacebookURL), model.WhatsAppUrl },
                { nameof(PodcastHost.HostLinkedInURL), model.LinkedInUrl },
                { nameof(PodcastHost.HostSlug), codeName },
                { nameof(PodcastHost.HostIsVerified), true }
            };

            if (model.Id > 0)
            {
                try
                {
                    await itemManager.TryCreateDraft(model.Id, "en", cancellationToken);
                    await itemManager.TryUpdateDraft(model.Id, "en", new ContentItemData(data), cancellationToken);
                    await itemManager.TryPublish(model.Id, "en", cancellationToken);
                    return model.Id;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update team member ID {Id}.", model.Id);
                }
            }

            var createParams = new CreateContentItemParameters(PodcastHost.CONTENT_TYPE_NAME, codeName, model.Name, "en");
            var itemData = new ContentItemData(data);
            int itemId = await itemManager.Create(createParams, itemData, cancellationToken);
            await itemManager.TryPublish(itemId, "en", cancellationToken);

            if (folderId > 0)
            {
                try { await folderManager.MoveItems(folderId, new[] { itemId }, cancellationToken); } catch { }
            }

            return itemId;
        }

        public async Task<bool> DeleteTeamMemberAsync(int contentItemId, CancellationToken cancellationToken = default)
        {
            try
            {
                var itemManager = _itemManagerFactory.Create(GetAdminUserId());
                await itemManager.Delete(contentItemId, "en", cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete team member ID {Id}.", contentItemId);
                return false;
            }
        }

        public async Task<int> CreateOrUpdateMembershipTierAsync(MembershipTierModel model, CancellationToken cancellationToken = default)
        {
            int userId = GetAdminUserId();
            var itemManager = _itemManagerFactory.Create(userId);
            var folderManager = _folderManagerFactory.Create(userId);

            int folderId = await EnsureFolderAsync(FOLDER_MEMBERSHIPS, "Memberships", cancellationToken);
            string codeName = "tier-" + CleanSlug(model.TierCode + "-" + model.Name);

            var data = new Dictionary<string, object>
            {
                { nameof(WebsiteContentBlock.BlockName), codeName },
                { nameof(WebsiteContentBlock.Heading), model.Name },
                { nameof(WebsiteContentBlock.Description), model.Description },
                { nameof(WebsiteContentBlock.IconName), model.TierCode },
                { nameof(WebsiteContentBlock.BlockType), "MembershipTier" },
                { nameof(WebsiteContentBlock.PrimaryButtonText), model.Price },
                { nameof(WebsiteContentBlock.DisplayOrder), model.Order },
                { nameof(WebsiteContentBlock.IsActive), true }
            };

            if (model.Id > 0)
            {
                try
                {
                    await itemManager.TryCreateDraft(model.Id, "en", cancellationToken);
                    await itemManager.TryUpdateDraft(model.Id, "en", new ContentItemData(data), cancellationToken);
                    await itemManager.TryPublish(model.Id, "en", cancellationToken);
                    return model.Id;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update membership tier ID {Id}.", model.Id);
                }
            }

            var createParams = new CreateContentItemParameters(WebsiteContentBlock.CONTENT_TYPE_NAME, codeName, model.Name, "en");
            var itemData = new ContentItemData(data);
            int itemId = await itemManager.Create(createParams, itemData, cancellationToken);
            await itemManager.TryPublish(itemId, "en", cancellationToken);

            if (folderId > 0)
            {
                try { await folderManager.MoveItems(folderId, new[] { itemId }, cancellationToken); } catch { }
            }

            return itemId;
        }

        public async Task<bool> DeleteMembershipTierAsync(int contentItemId, CancellationToken cancellationToken = default)
        {
            try
            {
                var itemManager = _itemManagerFactory.Create(GetAdminUserId());
                await itemManager.Delete(contentItemId, "en", cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete membership tier ID {Id}.", contentItemId);
                return false;
            }
        }

        // ==========================================
        // 5. CONTENT FOLDERS MANAGEMENT
        // ==========================================
        public async Task<List<ContentFolderModel>> GetFoldersAsync(CancellationToken cancellationToken = default)
        {
            var folders = new List<ContentFolderModel>();

            try
            {
                var ds = ConnectionHelper.ExecuteQuery(
                    "SELECT ContentFolderID, ContentFolderName, ContentFolderDisplayName, ContentFolderTreePath FROM CMS_ContentFolder ORDER BY ContentFolderTreePath ASC",
                    null, QueryTypeEnum.SQLQuery);

                if (ds != null && ds.Tables.Count > 0)
                {
                    foreach (DataRow r in ds.Tables[0].Rows)
                    {
                        folders.Add(new ContentFolderModel
                        {
                            FolderId = Convert.ToInt32(r["ContentFolderID"]),
                            Name = r["ContentFolderName"].ToString() ?? "",
                            DisplayName = r["ContentFolderDisplayName"].ToString() ?? "",
                            TreePath = r["ContentFolderTreePath"].ToString() ?? ""
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query folders from database directly.");
            }

            return await Task.FromResult(folders);
        }

        public async Task<int> EnsureFolderAsync(string name, string displayName, CancellationToken cancellationToken = default)
        {
            try
            {
                var folderManager = _folderManagerFactory.Create(GetAdminUserId());
                var existing = await folderManager.Get(name, cancellationToken);
                if (existing != null)
                {
                    return existing.ContentFolderID;
                }

                var root = await folderManager.GetRoot(cancellationToken);
                int createdId = await folderManager.Create(
                    root.ContentFolderID,
                    new CreateContentFolderParameters(displayName, name),
                    cancellationToken);

                _logger.LogInformation("Created Content Hub folder '{Name}' (ID {Id}).", name, createdId);
                return createdId;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error creating folder '{Name}': {Message}", name, ex.Message);
                return 0;
            }
        }

        public async Task<bool> DeleteFolderAsync(int folderId, CancellationToken cancellationToken = default)
        {
            try
            {
                var folderManager = _folderManagerFactory.Create(GetAdminUserId());
                await folderManager.Delete(folderId, cancellationToken);
                _logger.LogInformation("Deleted Content Hub folder ID {Id}.", folderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete Content Hub folder ID {Id}.", folderId);
                return false;
            }
        }

        // ==========================================
        // HELPERS
        // ==========================================
        private static string CleanSlug(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "item";
            var slug = raw.ToLowerInvariant().Trim();
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]+", "-");
            return slug.Trim('-');
        }

        private static DateTime ParseDate(string day, string monthYear)
        {
            try
            {
                var s = $"{day} {monthYear}";
                if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    return dt;
                }
            }
            catch { }
            return DateTime.UtcNow;
        }

        private static string GetFallbackEventImage(int index)
        {
            return (index % 4) switch
            {
                1 => "/images/professional-golf-player.jpg",
                2 => "/images/girl-taking-selfie-with-friends-golf-field.jpg",
                3 => "/images/anna-rosar-ew-olGvgCCs-unsplash.jpg",
                _ => "/images/frederik-rosar-NDSZcCfnsbY-unsplash.jpg"
            };
        }

        private static List<EventItemModel> GetInitialTiyaEvents()
        {
            return new List<EventItemModel>
            {
                new()
                {
                    Id = 1,
                    Title = "Private activities",
                    Day = "24",
                    MonthYear = "Feb 2048",
                    EventDate = new DateTime(2048, 2, 24),
                    ImageUrl = "/images/professional-golf-player.jpg",
                    Description = "Exclusive VIP private golf tournament on our 18-hole championship green with personal caddie service and premium club lounge access.",
                    Location = "National Center, NYC",
                    TicketPrice = "$250",
                    Url = "/detail-page?slug=private-activities",
                    Slug = "private-activities",
                    Category = "VIP / Private",
                    IsAltBackground = false,
                    Order = 1
                },
                new()
                {
                    Id = 2,
                    Title = "Group tournament activities",
                    Day = "28",
                    MonthYear = "Feb 2048",
                    EventDate = new DateTime(2048, 2, 28),
                    ImageUrl = "/images/girl-taking-selfie-with-friends-golf-field.jpg",
                    Description = "Bring your team or friends for our seasonal weekend scramble. Features live scoring, complimentary luncheon, and trophy ceremony.",
                    Location = "National Center, NYC",
                    TicketPrice = "$350",
                    Url = "/detail-page?slug=group-tournament-activities",
                    Slug = "group-tournament-activities",
                    Category = "Group Scramble",
                    IsAltBackground = true,
                    Order = 2
                },
                new()
                {
                    Id = 3,
                    Title = "Single Tournament Championship",
                    Day = "18",
                    MonthYear = "Mar 2048",
                    EventDate = new DateTime(2048, 3, 18),
                    ImageUrl = "/images/anna-rosar-ew-olGvgCCs-unsplash.jpg",
                    Description = "Individual stroke play championship for golfers of all skill levels. Cash prizes, custom trophies, and clubhouse banquet dinner included.",
                    Location = "Tiya Championship Links",
                    TicketPrice = "$150",
                    Url = "/detail-page?slug=single-tournament-championship",
                    Slug = "single-tournament-championship",
                    Category = "Stroke Play",
                    IsAltBackground = false,
                    Order = 3
                },
                new()
                {
                    Id = 4,
                    Title = "Junior & Master Golf Showcase",
                    Day = "25",
                    MonthYear = "Mar 2048",
                    EventDate = new DateTime(2048, 3, 25),
                    ImageUrl = "/images/frederik-rosar-NDSZcCfnsbY-unsplash.jpg",
                    Description = "Mentor and youth pairs tournament celebrating the future of golf. PGA coaching clinics and demo club fittings throughout the day.",
                    Location = "Orlando Country Club",
                    TicketPrice = "$180",
                    Url = "/detail-page?slug=junior-master-golf-showcase",
                    Slug = "junior-master-golf-showcase",
                    Category = "Youth / Showcase",
                    IsAltBackground = true,
                    Order = 4
                }
            };
        }

        private static List<TeamMemberModel> GetInitialTeamMembers()
        {
            return new List<TeamMemberModel>
            {
                new()
                {
                    Id = 1,
                    Name = "Michael",
                    Role = "Founder",
                    ImageUrl = "/images/members/portrait-young-handsome-businessman-wearing-suit-standing-with-crossed-arms-with-isolated-studio-white-background.jpg",
                    Biography = "Founder of Tiya Golf Club with over 20 years of course development and PGA tournament leadership experience.",
                    TwitterUrl = "https://twitter.com",
                    WhatsAppUrl = "https://whatsapp.com",
                    LinkedInUrl = "https://linkedin.com",
                    Order = 1
                },
                new()
                {
                    Id = 2,
                    Name = "Sandy",
                    Role = "Co-Founder",
                    ImageUrl = "/images/members/successful-asian-lady-boss-red-blazer-holding-clipboard-with-documens-pen-working-looking-happy-white-background.jpg",
                    Biography = "Co-Founder and Director of Hospitality, overseeing club member experiences and premier dining facilities.",
                    TwitterUrl = "https://twitter.com",
                    WhatsAppUrl = "https://whatsapp.com",
                    LinkedInUrl = "https://linkedin.com",
                    Order = 2
                }
            };
        }

        private static List<MembershipTierModel> GetInitialMembershipTiers()
        {
            return new List<MembershipTierModel>
            {
                new() { Id = 1, TierCode = "T1", Name = "Standard Access", Price = "T1 $420", Order = 1, Description = "Full access to driving ranges and weekday green tee times." },
                new() { Id = 2, TierCode = "T2", Name = "Country Club Pro", Price = "T2 $640", Order = 2, Description = "Weekend access, country club facilities, and seasonal tournament entry." },
                new() { Id = 3, TierCode = "T3", Name = "Platinum Masters", Price = "T3 $860", Order = 3, Description = "VIP course priority, pro networking, personalized locker, and club lounge access." }
            };
        }

        private static List<MembershipFeatureRowModel> GetInitialMembershipFeatures()
        {
            return new List<MembershipFeatureRowModel>
            {
                new() { FeatureName = "Golf Insurance", Tier1Available = true, Tier2Available = true, Tier3Available = true },
                new() { FeatureName = "Club Facilities", Tier1Available = true, Tier2Available = true, Tier3Available = true },
                new() { FeatureName = "Country Club", Tier1Available = false, Tier2Available = true, Tier3Available = true },
                new() { FeatureName = "Weekend Seasonal", Tier1Available = false, Tier2Available = true, Tier3Available = true },
                new() { FeatureName = "Premium Courses", Tier1Available = false, Tier2Available = false, Tier3Available = true },
                new() { FeatureName = "Pro's Networking", Tier1Available = false, Tier2Available = false, Tier3Available = true }
            };
        }
    }
}
