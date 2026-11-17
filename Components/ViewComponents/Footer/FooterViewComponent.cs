using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Podcast.Web.Models;
using Podcast.Web.Services;

namespace Podcast.Web.Components.ViewComponents.Footer
{
    public class FooterViewComponent : ViewComponent
    {
        private readonly INavigationService _navigationService;
        private readonly ITiyaContentRepository _contentRepo;

        public FooterViewComponent(
            INavigationService navigationService,
            ITiyaContentRepository contentRepo)
        {
            _navigationService = navigationService;
            _contentRepo = contentRepo;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var menuItems = await _navigationService.GetNavigationTreeAsync(string.Empty);
            var footerConfig = await _contentRepo.GetFooterConfigAsync();

            var model = new FooterViewModel
            {
                FooterItems = menuItems,
                BrandTitle = footerConfig.BrandTitle,
                BrandSubtitle = footerConfig.BrandSubtitle,
                Description = footerConfig.Description,
                WeekdayHours = footerConfig.WeekdayHours,
                WeekendHours = footerConfig.WeekendHours,
                CopyrightText = footerConfig.CopyrightText,
                DesignerCredit = footerConfig.DesignerCredit,
                DesignerUrl = footerConfig.DesignerUrl,
                SubscribeTitle = "Subscribe to our Newsletter",
                SubscribePlaceholder = "Enter your email address",
                SubscribeButtonText = "Subscribe",
                PhoneNumber = "(020) 010-020-0340",
                EmailAddress = "info@company.com",
                DownloadTitle = "Download Mobile App",
                AppStoreUrl = "#",
                PlayStoreUrl = "#",
                SocialTitle = "Social",
                InstagramUrl = footerConfig.InstagramUrl,
                TwitterUrl = footerConfig.TwitterUrl,
                WhatsAppUrl = footerConfig.WhatsAppUrl
            };

            return View("~/Components/ViewComponents/Footer/_Footer.cshtml", model);
        }
    }
}
