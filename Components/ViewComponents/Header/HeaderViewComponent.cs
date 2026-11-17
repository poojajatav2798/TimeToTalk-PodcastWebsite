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

            var model = new HeaderViewModel
            {
                MenuItems = menuItems,
                BrandTitle = headerConfig.BrandTitle,
                BrandSubtitle = headerConfig.BrandSubtitle,
                BrandLogoUrl = headerConfig.BrandLogoUrl,
                BrandLogoAlt = headerConfig.BrandLogoAlt,
                SearchPlaceholder = headerConfig.SearchPlaceholder,
                SearchActionUrl = "/listing-page",
                ButtonText = headerConfig.ButtonText,
                ButtonUrl = headerConfig.ButtonUrl,
                QuestionsTitle = headerConfig.QuestionsTitle,
                QuestionsPhone = headerConfig.QuestionsPhone
            };

            return View("~/Components/ViewComponents/Header/_Header.cshtml", model);
        }
    }
}
