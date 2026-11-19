using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Podcast.Web.Models;
using Podcast.Web.Services;

namespace Podcast.Web.Components.ViewComponents.Header
{
    public class HeaderViewComponent : ViewComponent
    {
        private readonly INavigationService _navigationService;
        private readonly ITiyaContentRepository _contentRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HeaderViewComponent(
            INavigationService navigationService,
            ITiyaContentRepository contentRepo,
            IHttpContextAccessor httpContextAccessor)
        {
            _navigationService = navigationService;
            _contentRepo = contentRepo;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var currentPath = _httpContextAccessor.HttpContext?.Request.Path.Value ?? "/";
            var menuItems = await _navigationService.GetNavigationTreeAsync(currentPath);
            var headerConfig = await _contentRepo.GetHeaderConfigAsync();
            var settings = await _contentRepo.GetWebsiteSettingsAsync();

            var model = new HeaderViewModel
            {
                MenuItems = menuItems,
                BrandTitle = !string.IsNullOrWhiteSpace(settings?.WebsiteName) ? settings.WebsiteName : headerConfig.BrandTitle,
                BrandSubtitle = headerConfig.BrandSubtitle,
                BrandLogoUrl = !string.IsNullOrWhiteSpace(settings?.HeaderLogoUrl) ? settings.HeaderLogoUrl : headerConfig.BrandLogoUrl,
                BrandLogoAlt = !string.IsNullOrWhiteSpace(settings?.WebsiteName) ? settings.WebsiteName : headerConfig.BrandLogoAlt,
                SearchPlaceholder = headerConfig.SearchPlaceholder,
                SearchActionUrl = "/listing-page",
                ButtonText = !string.IsNullOrWhiteSpace(settings?.MemberLoginButtonText) ? settings.MemberLoginButtonText : headerConfig.ButtonText,
                ButtonUrl = !string.IsNullOrWhiteSpace(settings?.MemberLoginButtonUrl) ? settings.MemberLoginButtonUrl : headerConfig.ButtonUrl,
                QuestionsTitle = headerConfig.QuestionsTitle,
                QuestionsPhone = !string.IsNullOrWhiteSpace(settings?.ContactPhone) ? settings.ContactPhone : headerConfig.QuestionsPhone
            };

            return View("~/Components/ViewComponents/Header/_Header.cshtml", model);
        }
    }
}
