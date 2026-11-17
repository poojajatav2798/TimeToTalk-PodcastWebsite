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
                        tiers.Add(new MembershipTierModel
                        {
                            Id = b.SystemFields.ContentItemID,
                            TierCode = b.IconName ?? "T1",
                            Name = b.Heading ?? "Tier",
                            Price = b.PrimaryButtonText ?? "$250",
                            Description = b.Description ?? "",
                            Order = b.DisplayOrder
                        });
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
                        var flags = (b.PrimaryButtonURL ?? "1,1,1").Split(',');
                        features.Add(new MembershipFeatureRowModel
                        {
                            FeatureName = b.Heading ?? "Feature",
                            Tier1Available = flags.Length > 0 && flags[0].Trim() == "1",
                            Tier2Available = flags.Length > 1 && flags[1].Trim() == "1",
                            Tier3Available = flags.Length > 2 && flags[2].Trim() == "1"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query membership features from Content Hub.");
            }

            if (!features.Any())
            {
                features = GetInitialMembershipFeatures();
            }

            return features;
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

        // ==========================================
        // DYNAMIC HEADER AND FOOTER (CONTENT HUB)
        // ==========================================
        public async Task<TiyaHeaderModel> GetHeaderConfigAsync()
        {
            var header = new TiyaHeaderModel();
            try
            {
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
                        if (hourParts.Length > 0 && !string.IsNullOrWhiteSpace(hourParts[0])) footer.WeekdayHours = hourParts[0];
                        if (hourParts.Length > 1 && !string.IsNullOrWhiteSpace(hourParts[1])) footer.WeekendHours = hourParts[1];
                    }

                    if (!string.IsNullOrWhiteSpace(block.PrimaryButtonURL))
                    {
                        var socialParts = block.PrimaryButtonURL.Split('|', StringSplitOptions.TrimEntries);
                        if (socialParts.Length > 0 && !string.IsNullOrWhiteSpace(socialParts[0])) footer.InstagramUrl = socialParts[0];
                        if (socialParts.Length > 1 && !string.IsNullOrWhiteSpace(socialParts[1])) footer.TwitterUrl = socialParts[1];
                        if (socialParts.Length > 2 && !string.IsNullOrWhiteSpace(socialParts[2])) footer.WhatsAppUrl = socialParts[2];
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
                { nameof(WebsiteContentBlock.IconName), $"{model.WeekdayHours}|{model.WeekendHours}" },
                { nameof(WebsiteContentBlock.PrimaryButtonText), model.DesignerCredit },
                { nameof(WebsiteContentBlock.PrimaryButtonURL), $"{model.InstagramUrl}|{model.TwitterUrl}|{model.WhatsAppUrl}" },
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
