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
                BrandLogoUrl = !string.IsNullOrWhiteSpace(footerConfig.BrandLogoUrl) ? footerConfig.BrandLogoUrl : "/images/logo.png",
                Description = footerConfig.Description,
                ScheduleTitle = !string.IsNullOrWhiteSpace(footerConfig.ScheduleTitle) ? footerConfig.ScheduleTitle : "Join Us",
                WeekdayLabel = !string.IsNullOrWhiteSpace(footerConfig.WeekdayLabel) ? footerConfig.WeekdayLabel : "Mon-Fri",
                WeekdayHours = footerConfig.WeekdayHours,
                WeekendLabel = !string.IsNullOrWhiteSpace(footerConfig.WeekendLabel) ? footerConfig.WeekendLabel : "Sat-Sun",
                WeekendHours = footerConfig.WeekendHours,
                CopyrightText = footerConfig.CopyrightText,
                DesignerCredit = footerConfig.DesignerCredit,
                DesignerUrl = footerConfig.DesignerUrl,
                PhoneNumber = footerConfig.PhoneNumber,
                EmailAddress = footerConfig.EmailAddress,
                InstagramUrl = footerConfig.InstagramUrl,
                TwitterUrl = footerConfig.TwitterUrl,
                WhatsAppUrl = footerConfig.WhatsAppUrl,
                FacebookUrl = footerConfig.FacebookUrl,
                LinkedInUrl = footerConfig.LinkedInUrl
            };

            return View("~/Components/ViewComponents/Footer/_Footer.cshtml", model);
        }
    }
}
