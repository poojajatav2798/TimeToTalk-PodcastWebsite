using System.Collections.Generic;
using System.Threading.Tasks;
using Podcast.Web.Models;

namespace Podcast.Web.Services
{
    public interface INavigationService
    {
        Task<List<NavigationItemViewModel>> GetNavigationTreeAsync(string currentPath);
    }
}
