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
            var settings = await _contentRepo.GetWebsiteSettingsAsync();

            var model = new FooterViewModel
            {
                FooterItems = menuItems,
                BrandTitle = !string.IsNullOrWhiteSpace(settings?.WebsiteName) ? settings.WebsiteName : footerConfig.BrandTitle,
                BrandSubtitle = footerConfig.BrandSubtitle,
                BrandLogoUrl = !string.IsNullOrWhiteSpace(settings?.FooterLogoUrl) ? settings.FooterLogoUrl : (!string.IsNullOrWhiteSpace(footerConfig.BrandLogoUrl) ? footerConfig.BrandLogoUrl : "/images/logo.png"),
                Description = !string.IsNullOrWhiteSpace(settings?.FooterDescription) ? settings.FooterDescription : footerConfig.Description,
                ScheduleTitle = !string.IsNullOrWhiteSpace(footerConfig.ScheduleTitle) ? footerConfig.ScheduleTitle : "Join Us",
                WeekdayLabel = !string.IsNullOrWhiteSpace(footerConfig.WeekdayLabel) ? footerConfig.WeekdayLabel : "Mon-Fri",
                WeekdayHours = footerConfig.WeekdayHours,
                WeekendLabel = !string.IsNullOrWhiteSpace(footerConfig.WeekendLabel) ? footerConfig.WeekendLabel : "Sat-Sun",
                WeekendHours = footerConfig.WeekendHours,
                CopyrightText = !string.IsNullOrWhiteSpace(settings?.CopyrightText) ? settings.CopyrightText : footerConfig.CopyrightText,
                DesignerCredit = footerConfig.DesignerCredit,
                DesignerUrl = footerConfig.DesignerUrl,
                PhoneNumber = !string.IsNullOrWhiteSpace(settings?.ContactPhone) ? settings.ContactPhone : footerConfig.PhoneNumber,
                EmailAddress = !string.IsNullOrWhiteSpace(settings?.ContactEmail) ? settings.ContactEmail : footerConfig.EmailAddress,
                InstagramUrl = !string.IsNullOrWhiteSpace(settings?.InstagramUrl) ? settings.InstagramUrl : footerConfig.InstagramUrl,
                TwitterUrl = !string.IsNullOrWhiteSpace(settings?.TwitterUrl) ? settings.TwitterUrl : footerConfig.TwitterUrl,
                WhatsAppUrl = !string.IsNullOrWhiteSpace(settings?.WhatsAppUrl) ? settings.WhatsAppUrl : footerConfig.WhatsAppUrl,
                FacebookUrl = !string.IsNullOrWhiteSpace(settings?.FacebookUrl) ? settings.FacebookUrl : footerConfig.FacebookUrl,
                LinkedInUrl = !string.IsNullOrWhiteSpace(settings?.LinkedInUrl) ? settings.LinkedInUrl : footerConfig.LinkedInUrl
            };

            return View("~/Components/ViewComponents/Footer/_Footer.cshtml", model);
        }
    }
}
