using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using Microsoft.Extensions.Logging;
using Podcast.Web.Models;
using TimeToTalk;

namespace Podcast.Web.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IContentQueryExecutor _queryExecutor;
        private readonly IWebsiteChannelContext _websiteChannelContext;
        private readonly ILogger<NavigationService> _logger;

        public NavigationService(
            IContentQueryExecutor queryExecutor,
            IWebsiteChannelContext websiteChannelContext,
            ILogger<NavigationService> logger)
        {
            _queryExecutor = queryExecutor;
            _websiteChannelContext = websiteChannelContext;
            _logger = logger;
        }

        public async Task<List<NavigationItemViewModel>> GetNavigationTreeAsync(string currentPath)
        {
            var normalizedCurrent = CleanUrl(currentPath).TrimEnd('/');
            if (string.IsNullOrEmpty(normalizedCurrent))
            {
                normalizedCurrent = string.Empty;
            }

            try
            {
                var sql = @"
SELECT 
    W.WebPageItemID, 
    W.WebPageItemName, 
    W.WebPageItemOrder,
    CT.ClassName,
    U.WebPageUrlPath,
    COALESCE(H.ShowInHeaderNavigation, A.ShowInHeaderNavigation, L.ShowInHeaderNavigation, D.ShowInHeaderNavigation, CP.ShowInHeaderNavigation, 1) AS ShowInHeaderNavigation,
    COALESCE(H.NavigationTitle, A.NavigationTitle, L.NavigationTitle, D.NavigationTitle, CP.NavigationTitle, '') AS NavigationTitle,
    COALESCE(H.NavigationOrder, A.NavigationOrder, L.NavigationOrder, D.NavigationOrder, CP.NavigationOrder, W.WebPageItemOrder) AS NavigationOrder
FROM CMS_WebPageItem W
JOIN CMS_ContentItem CI ON W.WebPageItemContentItemID = CI.ContentItemID
JOIN CMS_Class CT ON CI.ContentItemContentTypeID = CT.ClassID
JOIN CMS_ContentItemCommonData C ON CI.ContentItemID = C.ContentItemCommonDataContentItemID
LEFT JOIN CMS_WebPageUrlPath U ON U.WebPageUrlPathWebPageItemID = W.WebPageItemID AND U.WebPageUrlPathIsLatest = 1
LEFT JOIN Podcastweb_HomePage H ON H.ContentItemDataCommonDataID = C.ContentItemCommonDataID
LEFT JOIN TimeToTalk_AboutPage A ON A.ContentItemDataCommonDataID = C.ContentItemCommonDataID
LEFT JOIN TimeToTalk_ListingPage L ON L.ContentItemDataCommonDataID = C.ContentItemCommonDataID
LEFT JOIN TimeToTalk_DetailPage D ON D.ContentItemDataCommonDataID = C.ContentItemCommonDataID
LEFT JOIN TimeToTalk_ContactPage CP ON CP.ContentItemDataCommonDataID = C.ContentItemCommonDataID
WHERE C.ContentItemCommonDataIsLatest = 1
ORDER BY NavigationOrder ASC, W.WebPageItemID ASC";

                var ds = ConnectionHelper.ExecuteQuery(sql, null, QueryTypeEnum.SQLQuery);
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    return BuildNavigationFromDataSet(ds.Tables[0], normalizedCurrent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query navigation from database directly, falling back to CMS ContentItemQueryBuilder.");
            }

            return await FallbackGetNavigationTreeAsync(normalizedCurrent);
        }

        private List<NavigationItemViewModel> BuildNavigationFromDataSet(DataTable table, string normalizedCurrent)
        {
            var rows = new List<DataRow>();
            var seenIds = new HashSet<int>();

            foreach (DataRow row in table.Rows)
            {
                int pageId = Convert.ToInt32(row["WebPageItemID"]);
                if (!seenIds.Add(pageId)) continue;
                rows.Add(row);
            }

            var isHomePage = string.IsNullOrEmpty(normalizedCurrent) || normalizedCurrent == "/" || normalizedCurrent.Equals("/home", StringComparison.OrdinalIgnoreCase);
            var sectionPrefix = isHomePage ? "" : "/";

            // Identify standard pages (specifically Home by url path '/' or item name 'Home')
            DataRow? homeRow = rows.FirstOrDefault(r => CleanUrl(r["WebPageUrlPath"]?.ToString()) == "/" || (r["WebPageItemName"].ToString()!.Equals("home", StringComparison.OrdinalIgnoreCase) && r["ClassName"].ToString() == HomePage.CONTENT_TYPE_NAME))
                              ?? rows.FirstOrDefault(r => r["WebPageItemName"].ToString()!.Equals("home", StringComparison.OrdinalIgnoreCase))
                              ?? rows.FirstOrDefault(r => CleanUrl(r["WebPageUrlPath"]?.ToString()) == "/");

            DataRow? aboutRow = rows.FirstOrDefault(r => r["ClassName"].ToString() == AboutPage.CONTENT_TYPE_NAME);

            DataRow? listingRow = rows.FirstOrDefault(r => r["ClassName"].ToString() == ListingPage.CONTENT_TYPE_NAME
                                                          && !r["WebPageItemName"].ToString()!.Contains("topic", StringComparison.OrdinalIgnoreCase))
                                  ?? rows.FirstOrDefault(r => r["ClassName"].ToString() == ListingPage.CONTENT_TYPE_NAME);

            DataRow? detailRow = rows.FirstOrDefault(r => r["ClassName"].ToString() == DetailPage.CONTENT_TYPE_NAME);

            DataRow? contactRow = rows.FirstOrDefault(r => r["ClassName"].ToString() == ContactPage.CONTENT_TYPE_NAME);

            DataRow? membershipRow = rows.FirstOrDefault(r => r["WebPageItemName"].ToString()!.Contains("membership", StringComparison.OrdinalIgnoreCase)
                                                           || r["NavigationTitle"].ToString()!.Contains("membership", StringComparison.OrdinalIgnoreCase));

            DataRow? eventsNavRow = rows.FirstOrDefault(r => r["WebPageItemName"].ToString()!.Equals("events", StringComparison.OrdinalIgnoreCase)
                                                          || r["NavigationTitle"].ToString()!.Equals("events", StringComparison.OrdinalIgnoreCase));

            var menuItems = new List<NavigationItemViewModel>();

            bool RowShowsInHeader(DataRow? r)
            {
                if (r == null) return true;
                var val = r["ShowInHeaderNavigation"];
                if (val == null || val == DBNull.Value) return true;
                return Convert.ToBoolean(val);
            }

            string RowTitle(DataRow? r, string fallback)
            {
                if (r == null) return fallback;
                string custom = r["NavigationTitle"]?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(custom)) return custom.Trim();
                string name = r["WebPageItemName"]?.ToString() ?? fallback;
                return CleanTitle(name, fallback);
            }

            string RowUrl(DataRow? r, string fallback)
            {
                if (r == null) return fallback;
                string path = r["WebPageUrlPath"]?.ToString() ?? "";
                return string.IsNullOrWhiteSpace(path) ? fallback : CleanUrl(path);
            }

            // 1. Home -> #section_1
            if (homeRow == null || RowShowsInHeader(homeRow))
            {
                menuItems.Add(new NavigationItemViewModel
                {
                    WebPageItemId = homeRow != null ? Convert.ToInt32(homeRow["WebPageItemID"]) : 1,
                    Title = RowTitle(homeRow, "Home"),
                    Url = sectionPrefix + "#section_1",
                    IsClickScroll = true,
                    IsActive = isHomePage
                });
            }

            // 2. About -> #section_2
            if (aboutRow == null || RowShowsInHeader(aboutRow))
            {
                menuItems.Add(new NavigationItemViewModel
                {
                    WebPageItemId = aboutRow != null ? Convert.ToInt32(aboutRow["WebPageItemID"]) : 2,
                    Title = RowTitle(aboutRow, "About"),
                    Url = sectionPrefix + "#section_2",
                    IsClickScroll = true,
                    IsActive = false
                });
            }

            // 3. Membership -> #section_3
            if (membershipRow == null || RowShowsInHeader(membershipRow))
            {
                menuItems.Add(new NavigationItemViewModel
                {
                    WebPageItemId = membershipRow != null ? Convert.ToInt32(membershipRow["WebPageItemID"]) : 300,
                    Title = RowTitle(membershipRow, "Membership"),
                    Url = sectionPrefix + "#section_3",
                    IsClickScroll = true,
                    IsActive = false
                });
            }

            // 4. Events -> #section_4
            if (eventsNavRow == null || RowShowsInHeader(eventsNavRow))
            {
                menuItems.Add(new NavigationItemViewModel
                {
                    WebPageItemId = eventsNavRow != null ? Convert.ToInt32(eventsNavRow["WebPageItemID"]) : 400,
                    Title = RowTitle(eventsNavRow, "Events"),
                    Url = sectionPrefix + "#section_4",
                    IsClickScroll = true,
                    IsActive = false
                });
            }

            // 5. Contact Us -> #section_5
            if (contactRow == null || RowShowsInHeader(contactRow))
            {
                menuItems.Add(new NavigationItemViewModel
                {
                    WebPageItemId = contactRow != null ? Convert.ToInt32(contactRow["WebPageItemID"]) : 5,
                    Title = RowTitle(contactRow, "Contact Us"),
                    Url = sectionPrefix + "#section_5",
                    IsClickScroll = true,
                    IsActive = false
                });
            }

            // 6. Pages Dropdown (Event Listing & Event Detail)
            var showListing = listingRow == null || RowShowsInHeader(listingRow);
            var showDetail = detailRow == null || RowShowsInHeader(detailRow);

            var listingUrl = RowUrl(listingRow, "/listing-page");
            var detailUrl = RowUrl(detailRow, "/detail-page");

            var children = new List<NavigationItemViewModel>();
            if (showListing && listingRow != null)
            {
                children.Add(new NavigationItemViewModel
                {
                    WebPageItemId = Convert.ToInt32(listingRow["WebPageItemID"]),
                    Title = RowTitle(listingRow, "Event Listing"),
                    Url = listingUrl,
                    IsClickScroll = false,
                    IsActive = IsUrlActive(normalizedCurrent, listingUrl)
                });
            }

            if (showDetail && detailRow != null)
            {
                children.Add(new NavigationItemViewModel
                {
                    WebPageItemId = Convert.ToInt32(detailRow["WebPageItemID"]),
                    Title = RowTitle(detailRow, "Event Detail"),
                    Url = detailUrl,
                    IsClickScroll = false,
                    IsActive = IsUrlActive(normalizedCurrent, detailUrl)
                });
            }

            if (children.Any())
            {
                menuItems.Add(new NavigationItemViewModel
                {
                    WebPageItemId = 100,
                    Title = "Pages",
                    Url = "#",
                    IsActive = children.Any(c => c.IsActive),
                    Children = children
                });
            }

            // 7. Any other dynamically added pages in the website channel (e.g. Testing, Blog, Gallery, etc.)
            var knownIds = new HashSet<int>();

            // Exclude the specific standard template pages already mapped above
            if (homeRow != null) knownIds.Add(Convert.ToInt32(homeRow["WebPageItemID"]));
            if (aboutRow != null) knownIds.Add(Convert.ToInt32(aboutRow["WebPageItemID"]));
            if (contactRow != null) knownIds.Add(Convert.ToInt32(contactRow["WebPageItemID"]));
            if (listingRow != null) knownIds.Add(Convert.ToInt32(listingRow["WebPageItemID"]));
            if (detailRow != null) knownIds.Add(Convert.ToInt32(detailRow["WebPageItemID"]));

            // Also exclude legacy duplicate seeded items by path or name
            foreach (var r in rows)
            {
                var name = r["WebPageItemName"]?.ToString() ?? "";
                var path = r["WebPageUrlPath"]?.ToString() ?? "";

                if (path.Equals("about", StringComparison.OrdinalIgnoreCase) ||
                    path.Equals("contact", StringComparison.OrdinalIgnoreCase) ||
                    path.Equals("contact-us", StringComparison.OrdinalIgnoreCase) ||
                    path.Equals("episodes", StringComparison.OrdinalIgnoreCase) ||
                    path.Equals("topics", StringComparison.OrdinalIgnoreCase) ||
                    path.Equals("listing-page", StringComparison.OrdinalIgnoreCase) ||
                    path.Equals("detail-page", StringComparison.OrdinalIgnoreCase) ||
                    path.Equals("home", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("AboutPage-", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("ContactUs-", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("ListingPage-", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("DetailPage-", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("Home-ts", StringComparison.OrdinalIgnoreCase))
                {
                    knownIds.Add(Convert.ToInt32(r["WebPageItemID"]));
                }
            }

            var otherRows = rows
                .Where(r => !knownIds.Contains(Convert.ToInt32(r["WebPageItemID"])))
                .Where(RowShowsInHeader)
                .OrderBy(r => Convert.ToInt32(r["NavigationOrder"]))
                .ToList();

            foreach (var extraRow in otherRows)
            {
                var extraUrl = RowUrl(extraRow, "/");
                menuItems.Add(new NavigationItemViewModel
                {
                    WebPageItemId = Convert.ToInt32(extraRow["WebPageItemID"]),
                    Title = RowTitle(extraRow, extraRow["WebPageItemName"]?.ToString() ?? "Page"),
                    Url = extraUrl,
                    IsClickScroll = false,
                    IsActive = IsUrlActive(normalizedCurrent, extraUrl)
                });
            }

            return menuItems;
        }

        private async Task<List<NavigationItemViewModel>> FallbackGetNavigationTreeAsync(string normalizedCurrent)
        {
            var isHomePage = string.IsNullOrEmpty(normalizedCurrent) || normalizedCurrent == "/" || normalizedCurrent.Equals("/home", StringComparison.OrdinalIgnoreCase);
            var prefix = isHomePage ? "" : "/";

            var menuItems = new List<NavigationItemViewModel>
            {
                new() { WebPageItemId = 1, Title = "Home", Url = prefix + "#section_1", IsClickScroll = true, IsActive = isHomePage },
                new() { WebPageItemId = 2, Title = "About", Url = prefix + "#section_2", IsClickScroll = true },
                new() { WebPageItemId = 3, Title = "Membership", Url = prefix + "#section_3", IsClickScroll = true },
                new() { WebPageItemId = 4, Title = "Events", Url = prefix + "#section_4", IsClickScroll = true },
                new() { WebPageItemId = 5, Title = "Contact Us", Url = prefix + "#section_5", IsClickScroll = true },
                new()
                {
                    WebPageItemId = 100,
                    Title = "Pages",
                    Url = "#",
                    IsActive = !isHomePage,
                    Children = new List<NavigationItemViewModel>
                    {
                        new() { WebPageItemId = 101, Title = "Event Listing", Url = "/listing-page", IsActive = normalizedCurrent.Contains("listing") },
                        new() { WebPageItemId = 102, Title = "Event Detail", Url = "/detail-page", IsActive = normalizedCurrent.Contains("detail") }
                    }
                }
            };

            return await Task.FromResult(menuItems);
        }

        private static string CleanUrl(string? rawPath)
        {
            var path = (rawPath ?? "/").Trim();

            if (path.StartsWith("/~"))
            {
                path = path.Substring(2);
            }
            else if (path.StartsWith("~"))
            {
                path = path.TrimStart('~');
            }

            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }

            if (path.Equals("/home", StringComparison.OrdinalIgnoreCase))
            {
                return "/";
            }

            return path.ToLowerInvariant();
        }

        private static bool IsUrlActive(string current, string target)
        {
            var normCurrent = current.Trim().TrimEnd('/');
            var normTarget = CleanUrl(target).TrimEnd('/');

            if (string.IsNullOrEmpty(normTarget))
            {
                return string.IsNullOrEmpty(normCurrent);
            }

            return normCurrent.Equals(normTarget, StringComparison.OrdinalIgnoreCase) ||
                   normCurrent.StartsWith(normTarget + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static string CleanTitle(string? raw, string fallback)
        {
            if (string.IsNullOrWhiteSpace(raw)) return fallback;

            var cleaned = raw.Trim();

            // Strip random Kentico hash suffixes e.g. -ts730nq3, _ts730nq3, Ts730nq3, -k81a...
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"[-_\s]+[a-zA-Z0-9]{6,12}$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

            if (string.IsNullOrWhiteSpace(cleaned)) return fallback;

            // Normalize code names and technical page names to human titles
            if (cleaned.Equals("HomePage", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("Home Page", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("home-page", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("home", StringComparison.OrdinalIgnoreCase))
            {
                return "Home";
            }

            if (cleaned.Equals("AboutPage", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("About Page", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("about-page", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("about", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("About Us", StringComparison.OrdinalIgnoreCase))
            {
                return "About";
            }

            if (cleaned.Equals("ContactPage", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("Contact Page", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("contact-page", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("contact", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("ContactUs", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("Contact-Us", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("Contact Us", StringComparison.OrdinalIgnoreCase))
            {
                return "Contact Us";
            }

            if (cleaned.Equals("ListingPage", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("Listing Page", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("episodes-page", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("episodes", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("Event Listing", StringComparison.OrdinalIgnoreCase))
            {
                return "Event Listing";
            }

            if (cleaned.Equals("DetailPage", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("Detail Page", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("Event Detail", StringComparison.OrdinalIgnoreCase))
            {
                return "Event Detail";
            }

            // Convert kebab-case / snake_case into Title Case
            if (cleaned.Contains("-") || cleaned.Contains("_"))
            {
                cleaned = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
                    cleaned.Replace("-", " ").Replace("_", " ").Trim());
            }

            // If it still ends with a random hash in title case (e.g. 'Home Ts730nq3'), clean it again
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+[A-Za-z0-9]{6,12}$", "").Trim();

            return string.IsNullOrWhiteSpace(cleaned) ? fallback : cleaned;
        }
    }
}
