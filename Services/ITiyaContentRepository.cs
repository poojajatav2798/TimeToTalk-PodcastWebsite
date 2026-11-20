#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Podcast.Web.Components.Widgets.TiyaEvents;
using Podcast.Web.Models;
using TimeToTalk;

namespace Podcast.Web.Services
{
    public interface ITiyaContentRepository
    {
        // Querying Dynamic Data for UI
        Task<List<EventItemModel>> GetEventsAsync(int? topN = null);
        Task<List<EventItemModel>> GetEventsByGuidsAsync(IEnumerable<Guid> guids);
        Task<EventItemModel?> GetEventBySlugAsync(string slug);
        Task<EventItemModel?> GetEventByIdAsync(int id);
        Task<List<TeamMemberModel>> GetTeamMembersAsync();
        Task<List<TeamMemberModel>> GetTeamMembersByGuidsAsync(IEnumerable<System.Guid> guids);
        Task<List<MembershipTierModel>> GetMembershipTiersAsync();
        Task<List<MembershipTierModel>> GetMembershipTiersByGuidsAsync(IEnumerable<Guid> guids);
        Task<List<MembershipFeatureRowModel>> GetMembershipFeaturesAsync();
        Task<List<MembershipFeatureRowModel>> GetMembershipFeaturesByGuidsAsync(IEnumerable<Guid> guids);
        Task<List<FormFieldModel>> GetMembershipFormFieldsAsync();
        Task<WebsiteContentBlock?> GetContentBlockAsync(string blockName);
        Task<WebsiteContentBlock?> GetContentBlockByGuidAsync(System.Guid guid);

        // Dynamic Navigation Items in Content Hub
        Task<List<NavigationItemViewModel>> GetContentHubNavigationItemsAsync();
        Task<int> CreateOrUpdateNavigationItemAsync(NavigationItemViewModel item, CancellationToken cancellationToken = default);
        Task<bool> DeleteNavigationItemAsync(string codeName, CancellationToken cancellationToken = default);

        // CRUD Operations on Content Hub
        Task<int> CreateOrUpdateEventAsync(EventItemModel model, CancellationToken cancellationToken = default);
        Task<bool> DeleteEventAsync(int contentItemId, CancellationToken cancellationToken = default);

        Task<int> CreateOrUpdateTeamMemberAsync(TeamMemberModel model, CancellationToken cancellationToken = default);
        Task<bool> DeleteTeamMemberAsync(int contentItemId, CancellationToken cancellationToken = default);

        Task<int> CreateOrUpdateMembershipTierAsync(MembershipTierModel model, CancellationToken cancellationToken = default);
        Task<bool> DeleteMembershipTierAsync(int contentItemId, CancellationToken cancellationToken = default);

        // Dynamic Website Settings, Home, About, and Contact Pages
        Task<TiyaWebsiteSettings> GetWebsiteSettingsAsync();
        Task<HomePage?> GetHomePageContentAsync();
        Task<AboutPage?> GetAboutPageContentAsync();
        Task<ContactPage?> GetContactPageContentAsync();

        // Dynamic Header and Footer Configurations
        Task<TiyaHeaderModel> GetHeaderConfigAsync();
        Task<int> CreateOrUpdateHeaderConfigAsync(TiyaHeaderModel model, CancellationToken cancellationToken = default);

        Task<TiyaFooterModel> GetFooterConfigAsync();
        Task<int> CreateOrUpdateFooterConfigAsync(TiyaFooterModel model, CancellationToken cancellationToken = default);

        // Shared Content Items
        Task<List<SharedContentItemModel>> GetSharedContentAsync();
        Task<SharedContentItemModel?> GetSharedContentItemAsync(string blockName);
        Task<int> CreateOrUpdateSharedContentItemAsync(SharedContentItemModel model, CancellationToken cancellationToken = default);
        Task<bool> DeleteSharedContentItemAsync(string blockName, CancellationToken cancellationToken = default);

        // Content Folders
        Task<List<ContentFolderModel>> GetFoldersAsync(CancellationToken cancellationToken = default);
        Task<int> EnsureFolderAsync(string name, string displayName, CancellationToken cancellationToken = default);
        Task<bool> DeleteFolderAsync(int folderId, CancellationToken cancellationToken = default);
    }
}
