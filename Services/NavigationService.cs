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
        private readonly ITiyaContentRepository _contentRepo;
        private readonly ILogger<NavigationService> _logger;

        public NavigationService(
            IContentQueryExecutor queryExecutor,
            IWebsiteChannelContext websiteChannelContext,
            ITiyaContentRepository contentRepo,
            ILogger<NavigationService> logger)
        {
            _queryExecutor = queryExecutor;
            _websiteChannelContext = websiteChannelContext;
            _contentRepo = contentRepo;
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
                // First check if Content Hub has active navigation items configured
                var contentHubItems = await _contentRepo.GetContentHubNavigationItemsAsync();

                var sql = @"
WITH RankedWebPages AS (
    SELECT 
        W.WebPageItemID, 
        W.WebPageItemName, 
        W.WebPageItemOrder,
        W.WebPageItemParentID,
        M.ContentItemLanguageMetadataDisplayName,
        CT.ClassName,
        U.WebPageUrlPath,
        COALESCE(H.ShowInHeaderNavigation, A.ShowInHeaderNavigation, L.ShowInHeaderNavigation, D.ShowInHeaderNavigation, CP.ShowInHeaderNavigation, 0) AS ShowInHeaderNavigation,
        COALESCE(H.NavigationTitle, A.NavigationTitle, L.NavigationTitle, D.NavigationTitle, CP.NavigationTitle, '') AS NavigationTitle,
        COALESCE(H.NavigationOrder, A.NavigationOrder, L.NavigationOrder, D.NavigationOrder, CP.NavigationOrder, W.WebPageItemOrder) AS NavigationOrder,
        COALESCE(H.Title, A.Title, L.HeaderTitle, D.Title, CP.Title, '') AS PageTitle,
        ROW_NUMBER() OVER (
            PARTITION BY W.WebPageItemID 
            ORDER BY 
                CASE WHEN C.ContentItemCommonDataVersionStatus = 2 THEN 1 ELSE 2 END ASC,
                C.ContentItemCommonDataIsLatest DESC,
                C.ContentItemCommonDataID DESC
        ) AS rn
    FROM CMS_WebPageItem W
    JOIN CMS_ContentItem CI ON W.WebPageItemContentItemID = CI.ContentItemID
    JOIN CMS_Class CT ON CI.ContentItemContentTypeID = CT.ClassID
    JOIN CMS_ContentItemCommonData C ON CI.ContentItemID = C.ContentItemCommonDataContentItemID
    LEFT JOIN CMS_ContentItemLanguageMetadata M ON M.ContentItemLanguageMetadataContentItemID = CI.ContentItemID
    LEFT JOIN CMS_WebPageUrlPath U ON U.WebPageUrlPathWebPageItemID = W.WebPageItemID AND U.WebPageUrlPathIsLatest = 1
    LEFT JOIN Podcastweb_HomePage H ON H.ContentItemDataCommonDataID = C.ContentItemCommonDataID
    LEFT JOIN TimeToTalk_AboutPage A ON A.ContentItemDataCommonDataID = C.ContentItemCommonDataID
    LEFT JOIN TimeToTalk_ListingPage L ON L.ContentItemDataCommonDataID = C.ContentItemCommonDataID
    LEFT JOIN TimeToTalk_DetailPage D ON D.ContentItemDataCommonDataID = C.ContentItemCommonDataID
    LEFT JOIN TimeToTalk_ContactPage CP ON CP.ContentItemDataCommonDataID = C.ContentItemCommonDataID
    WHERE C.ContentItemCommonDataVersionStatus IN (1, 2)
)
SELECT * FROM RankedWebPages WHERE rn = 1
ORDER BY NavigationOrder ASC, WebPageItemID ASC";

                var ds = ConnectionHelper.ExecuteQuery(sql, null, QueryTypeEnum.SQLQuery);
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    return BuildNavigationFromDataSet(ds.Tables[0], normalizedCurrent, contentHubItems);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query navigation from database directly, falling back to CMS ContentItemQueryBuilder.");
            }

            return await FallbackGetNavigationTreeAsync(normalizedCurrent);
        }

        private List<NavigationItemViewModel> BuildNavigationFromDataSet(DataTable table, string normalizedCurrent, List<NavigationItemViewModel>? contentHubItems = null)
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
            DataRow? homeRow = rows.FirstOrDefault(r => r["ClassName"].ToString() == HomePage.CONTENT_TYPE_NAME && (r["WebPageItemName"].ToString()!.StartsWith("Home", StringComparison.OrdinalIgnoreCase) || CleanUrl(r["WebPageUrlPath"]?.ToString()) == "/" || CleanUrl(r["WebPageUrlPath"]?.ToString()) == "/home"))
                              ?? rows.FirstOrDefault(r => r["WebPageItemName"].ToString()!.StartsWith("Home", StringComparison.OrdinalIgnoreCase))
                              ?? rows.FirstOrDefault(r => CleanUrl(r["WebPageUrlPath"]?.ToString()) == "/" || CleanUrl(r["WebPageUrlPath"]?.ToString()) == "/home")
                              ?? rows.FirstOrDefault(r => r["ClassName"].ToString() == HomePage.CONTENT_TYPE_NAME);

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
                if (r == null) return false;
                var val = r["ShowInHeaderNavigation"];
                if (val == null || val == DBNull.Value) return false;
                return Convert.ToBoolean(val);
            }

            string RowTitle(DataRow? r, string fallback)
            {
                if (r == null) return fallback;
                string custom = r["NavigationTitle"]?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(custom)) return custom.Trim();
                
                string displayName = r.Table.Columns.Contains("ContentItemLanguageMetadataDisplayName")
                    ? r["ContentItemLanguageMetadataDisplayName"]?.ToString() ?? ""
                    : "";
                if (!string.IsNullOrWhiteSpace(displayName)) return CleanTitle(displayName, fallback);

                string pageTitle = r.Table.Columns.Contains("PageTitle")
                    ? r["PageTitle"]?.ToString() ?? ""
                    : "";
                if (!string.IsNullOrWhiteSpace(pageTitle)) return CleanTitle(pageTitle, fallback);

                string name = r["WebPageItemName"]?.ToString() ?? fallback;
                return CleanTitle(name, fallback);
            }

            string RowUrl(DataRow? r, string fallback)
            {
                if (r == null) return fallback;
                string path = r["WebPageUrlPath"]?.ToString() ?? "";
                return string.IsNullOrWhiteSpace(path) ? fallback : CleanUrl(path);
            }

            var disabledKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Record all explicitly disabled channel pages
            foreach (var r in rows)
            {
                if (!RowShowsInHeader(r))
                {
                    var dTitle = RowTitle(r, "");
                    var dNavTitle = r["NavigationTitle"]?.ToString() ?? "";
                    var dPath = r["WebPageUrlPath"]?.ToString() ?? "";
                    var dName = r["WebPageItemName"]?.ToString() ?? "";
                    var cls = r["ClassName"]?.ToString() ?? "";

                    if (!string.IsNullOrWhiteSpace(dTitle)) disabledKeywords.Add(dTitle.Trim());
                    if (!string.IsNullOrWhiteSpace(dNavTitle)) disabledKeywords.Add(dNavTitle.Trim());
                    if (!string.IsNullOrWhiteSpace(dPath))
                    {
                        disabledKeywords.Add(dPath.Trim());
                        disabledKeywords.Add(CleanUrl(dPath));
                    }
                    if (!string.IsNullOrWhiteSpace(dName)) disabledKeywords.Add(dName.Trim());

                    if (cls == AboutPage.CONTENT_TYPE_NAME || dName.Contains("about", StringComparison.OrdinalIgnoreCase) || dPath.Contains("about", StringComparison.OrdinalIgnoreCase))
                    {
                        disabledKeywords.Add("About");
                        disabledKeywords.Add("About Us");
                        disabledKeywords.Add("#section_2");
                        disabledKeywords.Add("/#section_2");
                    }
                    if (cls == ContactPage.CONTENT_TYPE_NAME || dName.Contains("contact", StringComparison.OrdinalIgnoreCase) || dPath.Contains("contact", StringComparison.OrdinalIgnoreCase))
                    {
                        disabledKeywords.Add("Contact");
                        disabledKeywords.Add("Contact Us");
                        disabledKeywords.Add("#section_5");
                        disabledKeywords.Add("/#section_5");
                    }
                    if (dName.Contains("membership", StringComparison.OrdinalIgnoreCase) || dPath.Contains("membership", StringComparison.OrdinalIgnoreCase))
                    {
                        disabledKeywords.Add("Membership");
                        disabledKeywords.Add("Membership at Tiya");
                        disabledKeywords.Add("#section_3");
                        disabledKeywords.Add("/#section_3");
                    }
                    if (dName.Contains("event", StringComparison.OrdinalIgnoreCase) || dPath.Contains("event", StringComparison.OrdinalIgnoreCase))
                    {
                        disabledKeywords.Add("Events");
                        disabledKeywords.Add("Upcoming Events");
                        disabledKeywords.Add("#section_4");
                        disabledKeywords.Add("/#section_4");
                    }
                    if (cls == HomePage.CONTENT_TYPE_NAME || dName.StartsWith("Home", StringComparison.OrdinalIgnoreCase) || dPath.Equals("Home", StringComparison.OrdinalIgnoreCase) || dPath == "/" || dPath == "/home")
                    {
                        disabledKeywords.Add("Home");
                        disabledKeywords.Add("#section_1");
                        disabledKeywords.Add("/#section_1");
                    }
                    if (cls == ListingPage.CONTENT_TYPE_NAME || dPath.Contains("listing", StringComparison.OrdinalIgnoreCase))
                    {
                        disabledKeywords.Add("Event Listing");
                        disabledKeywords.Add("Listing");
                        disabledKeywords.Add("/listing-page");
                    }
                    if (cls == DetailPage.CONTENT_TYPE_NAME || dPath.Contains("detail", StringComparison.OrdinalIgnoreCase))
                    {
                        disabledKeywords.Add("Event Detail");
                        disabledKeywords.Add("Detail");
                        disabledKeywords.Add("/detail-page");
                    }
                }
            }

            // 1. Home -> #section_1
            if (!disabledKeywords.Contains("Home") && !disabledKeywords.Contains("#section_1"))
            {
                if (homeRow != null && RowShowsInHeader(homeRow))
                {
                    menuItems.Add(new NavigationItemViewModel
                    {
                        WebPageItemId = Convert.ToInt32(homeRow["WebPageItemID"]),
                        Title = RowTitle(homeRow, "Home"),
                        Url = sectionPrefix + "#section_1",
                        IsClickScroll = true,
                        IsActive = isHomePage,
                        Order = Convert.ToInt32(homeRow["NavigationOrder"])
                    });
                }
                else if (homeRow == null)
                {
                    menuItems.Add(new NavigationItemViewModel
                    {
                        WebPageItemId = 1,
                        Title = "Home",
                        Url = sectionPrefix + "#section_1",
                        IsClickScroll = true,
                        IsActive = isHomePage,
                        Order = 1
                    });
                }
            }

            // 2. About -> #section_2
            if (!disabledKeywords.Contains("About") && !disabledKeywords.Contains("About Us") && !disabledKeywords.Contains("#section_2"))
            {
                if (aboutRow != null && RowShowsInHeader(aboutRow))
                {
                    menuItems.Add(new NavigationItemViewModel
                    {
                        WebPageItemId = Convert.ToInt32(aboutRow["WebPageItemID"]),
                        Title = RowTitle(aboutRow, "About"),
                        Url = sectionPrefix + "#section_2",
                        IsClickScroll = true,
                        IsActive = false,
                        Order = Convert.ToInt32(aboutRow["NavigationOrder"])
                    });
                }
                else if (aboutRow == null)
                {
                    menuItems.Add(new NavigationItemViewModel
                    {
                        WebPageItemId = 2,
                        Title = "About",
                        Url = sectionPrefix + "#section_2",
                        IsClickScroll = true,
                        IsActive = false,
                        Order = 2
                    });
                }
            }

            // 3. Membership -> #section_3
            if (!disabledKeywords.Contains("Membership") && !disabledKeywords.Contains("Membership at Tiya") && !disabledKeywords.Contains("#section_3"))
            {
                if (membershipRow != null && RowShowsInHeader(membershipRow))
                {
                    menuItems.Add(new NavigationItemViewModel
                    {
                        WebPageItemId = Convert.ToInt32(membershipRow["WebPageItemID"]),
                        Title = RowTitle(membershipRow, "Membership"),
                        Url = sectionPrefix + "#section_3",
                        IsClickScroll = true,
                        IsActive = false,
                        Order = Convert.ToInt32(membershipRow["NavigationOrder"])
                    });
                }
                else if (membershipRow == null)
                {
                    menuItems.Add(new NavigationItemViewModel
                    {
                        WebPageItemId = 300,
                        Title = "Membership",
                        Url = sectionPrefix + "#section_3",
                        IsClickScroll = true,
                        IsActive = false,
                        Order = 3
                    });
                }
            }

            // 4. Events -> #section_4
            if (!disabledKeywords.Contains("Events") && !disabledKeywords.Contains("Upcoming Events") && !disabledKeywords.Contains("#section_4"))
            {
                if (eventsNavRow != null && RowShowsInHeader(eventsNavRow))
                {
                    menuItems.Add(new NavigationItemViewModel
                    {
                        WebPageItemId = Convert.ToInt32(eventsNavRow["WebPageItemID"]),
                        Title = RowTitle(eventsNavRow, "Events"),
                        Url = sectionPrefix + "#section_4",
                        IsClickScroll = true,
                        IsActive = false,
                        Order = Convert.ToInt32(eventsNavRow["NavigationOrder"])
                    });
                }
                else if (eventsNavRow == null)
                {
                    menuItems.Add(new NavigationItemViewModel
                    {
                        WebPageItemId = 400,
                        Title = "Events",
                        Url = sectionPrefix + "#section_4",
                        IsClickScroll = true,
                        IsActive = false,
                        Order = 4
                    });
                }
            }

            // 5. Contact Us -> #section_5
            if (!disabledKeywords.Contains("Contact") && !disabledKeywords.Contains("Contact Us") && !disabledKeywords.Contains("#section_5"))
            {
                var activeContactRow = rows.FirstOrDefault(r => (r["ClassName"].ToString() == ContactPage.CONTENT_TYPE_NAME || r["WebPageItemName"].ToString()!.Contains("contact", StringComparison.OrdinalIgnoreCase)) && RowShowsInHeader(r));
                if (activeContactRow != null)
                {
                    menuItems.Add(new NavigationItemViewModel
                    {
                        WebPageItemId = Convert.ToInt32(activeContactRow["WebPageItemID"]),
                        Title = RowTitle(activeContactRow, "Contact Us"),
                        Url = sectionPrefix + "#section_5",
                        IsClickScroll = true,
                        IsActive = false,
                        Order = Convert.ToInt32(activeContactRow["NavigationOrder"])
                    });
                }
                else if (contactRow == null)
                {
                    menuItems.Add(new NavigationItemViewModel
                    {
                        WebPageItemId = 5,
                        Title = "Contact Us",
                        Url = sectionPrefix + "#section_5",
                        IsClickScroll = true,
                        IsActive = false,
                        Order = 5
                    });
                }
            }

            // 6. Pages Dropdown (Event Listing & Event Detail)
            var showListing = !disabledKeywords.Contains("Event Listing") && !disabledKeywords.Contains("Listing") && !disabledKeywords.Contains("/listing-page") && (listingRow == null || RowShowsInHeader(listingRow));
            var showDetail = !disabledKeywords.Contains("Event Detail") && !disabledKeywords.Contains("Detail") && !disabledKeywords.Contains("/detail-page") && (detailRow == null || RowShowsInHeader(detailRow));

            var listingUrl = RowUrl(listingRow, "/listing-page");
            var detailUrl = RowUrl(detailRow, "/detail-page");

            var children = new List<NavigationItemViewModel>();
            if (showListing && (listingRow != null || !disabledKeywords.Contains("Event Listing")))
            {
                children.Add(new NavigationItemViewModel
                {
                    WebPageItemId = listingRow != null ? Convert.ToInt32(listingRow["WebPageItemID"]) : 101,
                    Title = listingRow != null ? RowTitle(listingRow, "Event Listing") : "Event Listing",
                    Url = listingUrl,
                    IsClickScroll = false,
                    IsActive = IsUrlActive(normalizedCurrent, listingUrl),
                    Order = listingRow != null ? Convert.ToInt32(listingRow["NavigationOrder"]) : 1
                });
            }

            if (showDetail && (detailRow != null || !disabledKeywords.Contains("Event Detail")))
            {
                children.Add(new NavigationItemViewModel
                {
                    WebPageItemId = detailRow != null ? Convert.ToInt32(detailRow["WebPageItemID"]) : 102,
                    Title = detailRow != null ? RowTitle(detailRow, "Event Detail") : "Event Detail",
                    Url = detailUrl,
                    IsClickScroll = false,
                    IsActive = IsUrlActive(normalizedCurrent, detailUrl),
                    Order = detailRow != null ? Convert.ToInt32(detailRow["NavigationOrder"]) : 2
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
                    Children = children,
                    Order = 6
                });
            }

            // 7. Dynamic pages created in the channel (e.g. Testing, Blog, etc.)
            var knownIds = new HashSet<int>();
            if (homeRow != null) knownIds.Add(Convert.ToInt32(homeRow["WebPageItemID"]));
            if (aboutRow != null) knownIds.Add(Convert.ToInt32(aboutRow["WebPageItemID"]));
            if (contactRow != null) knownIds.Add(Convert.ToInt32(contactRow["WebPageItemID"]));
            if (listingRow != null) knownIds.Add(Convert.ToInt32(listingRow["WebPageItemID"]));
            if (detailRow != null) knownIds.Add(Convert.ToInt32(detailRow["WebPageItemID"]));

            // Legacy seeded items exclusion
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
                    path.Equals("membership", StringComparison.OrdinalIgnoreCase) ||
                    path.Equals("events", StringComparison.OrdinalIgnoreCase) ||
                    path.Equals("pages", StringComparison.OrdinalIgnoreCase) ||
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
                .Where(RowShowsInHeader) // STRICT ENABLE/DISABLE CHECK!
                .OrderBy(r => Convert.ToInt32(r["NavigationOrder"]))
                .ToList();

            foreach (var extraRow in otherRows)
            {
                var extraUrl = RowUrl(extraRow, "/");
                var itemTitle = RowTitle(extraRow, extraRow["WebPageItemName"]?.ToString() ?? "Page");

                if (menuItems.Any(m => m.Title.Equals(itemTitle, StringComparison.OrdinalIgnoreCase) ||
                                       m.Url.Equals(extraUrl, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                menuItems.Add(new NavigationItemViewModel
                {
                    WebPageItemId = Convert.ToInt32(extraRow["WebPageItemID"]),
                    Title = itemTitle,
                    Url = extraUrl,
                    IsClickScroll = false,
                    IsActive = IsUrlActive(normalizedCurrent, extraUrl),
                    Order = Convert.ToInt32(extraRow["NavigationOrder"])
                });
            }

            // 8. If Content Hub navigation items exist that are not yet in the menu, include them
            if (contentHubItems != null && contentHubItems.Any())
            {
                foreach (var chItem in contentHubItems)
                {
                    // If this title or URL was explicitly disabled by an editor, DO NOT RE-ADD IT!
                    if (disabledKeywords.Contains(chItem.Title) ||
                        disabledKeywords.Contains(chItem.Url) ||
                        disabledKeywords.Contains(CleanUrl(chItem.Url)) ||
                        (disabledKeywords.Contains("Home") && (chItem.Title.Equals("Home", StringComparison.OrdinalIgnoreCase) || chItem.Url.Contains("#section_1"))))
                    {
                        continue;
                    }

                    if (!menuItems.Any(m => m.Title.Equals(chItem.Title, StringComparison.OrdinalIgnoreCase) ||
                                            m.Url.Equals(chItem.Url, StringComparison.OrdinalIgnoreCase)))
                    {
                        menuItems.Add(new NavigationItemViewModel
                        {
                            WebPageItemId = chItem.WebPageItemId,
                            Title = chItem.Title,
                            Url = chItem.Url,
                            IsClickScroll = chItem.IsClickScroll,
                            IsActive = IsUrlActive(normalizedCurrent, chItem.Url),
                            Order = chItem.Order > 0 ? chItem.Order : 50
                        });
                    }
                }
            }

            return menuItems.OrderBy(m => m.Order).ToList();
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
