#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Podcast.Web.Components.Widgets.TiyaEvents;
using Podcast.Web.Models;
using Podcast.Web.Services;

namespace Podcast.Web.Controllers
{
    [ApiController]
    [Route("api/content-hub")]
    public class ContentHubApiController : ControllerBase
    {
        private readonly ITiyaContentRepository _contentRepo;

        public ContentHubApiController(ITiyaContentRepository contentRepo)
        {
            _contentRepo = contentRepo;
        }

        // ==========================================
        // 1. GOLF EVENTS CRUD
        // ==========================================
        [HttpGet("events")]
        public async Task<IActionResult> GetEvents([FromQuery] int? topN)
        {
            var events = await _contentRepo.GetEventsAsync(topN);
            return Ok(new { success = true, count = events.Count, data = events });
        }

        [HttpGet("events/{id:int}")]
        public async Task<IActionResult> GetEventById(int id)
        {
            var ev = await _contentRepo.GetEventByIdAsync(id);
            if (ev == null)
            {
                return NotFound(new { success = false, message = $"Event ID {id} not found." });
            }
            return Ok(new { success = true, data = ev });
        }

        [HttpGet("events/slug/{slug}")]
        public async Task<IActionResult> GetEventBySlug(string slug)
        {
            var ev = await _contentRepo.GetEventBySlugAsync(slug);
            if (ev == null)
            {
                return NotFound(new { success = false, message = $"Event with slug '{slug}' not found." });
            }
            return Ok(new { success = true, data = ev });
        }

        [HttpPost("events")]
        public async Task<IActionResult> CreateEvent([FromBody] EventItemModel model, CancellationToken ct)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Title))
            {
                return BadRequest(new { success = false, message = "Event title is required." });
            }

            model.Id = 0; // force creation
            int newId = await _contentRepo.CreateOrUpdateEventAsync(model, ct);
            return CreatedAtAction(nameof(GetEventById), new { id = newId }, new { success = true, id = newId, message = "Event created and published in Content Hub Events folder." });
        }

        [HttpPut("events/{id:int}")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventItemModel model, CancellationToken ct)
        {
            if (model == null)
            {
                return BadRequest(new { success = false, message = "Invalid event payload." });
            }

            model.Id = id;
            int updatedId = await _contentRepo.CreateOrUpdateEventAsync(model, ct);
            return Ok(new { success = true, id = updatedId, message = $"Event ID {id} updated and published in Content Hub." });
        }

        [HttpDelete("events/{id:int}")]
        public async Task<IActionResult> DeleteEvent(int id, CancellationToken ct)
        {
            bool deleted = await _contentRepo.DeleteEventAsync(id, ct);
            if (!deleted)
            {
                return StatusCode(500, new { success = false, message = $"Failed to delete event ID {id}." });
            }
            return Ok(new { success = true, message = $"Event ID {id} deleted from Content Hub." });
        }

        // ==========================================
        // 2. MEMBERSHIPS CRUD
        // ==========================================
        [HttpGet("memberships")]
        public async Task<IActionResult> GetMemberships()
        {
            var tiers = await _contentRepo.GetMembershipTiersAsync();
            var features = await _contentRepo.GetMembershipFeaturesAsync();
            return Ok(new { success = true, tiers, features });
        }

        [HttpPost("memberships/tiers")]
        public async Task<IActionResult> CreateMembershipTier([FromBody] MembershipTierModel model, CancellationToken ct)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
            {
                return BadRequest(new { success = false, message = "Tier name is required." });
            }

            model.Id = 0;
            int newId = await _contentRepo.CreateOrUpdateMembershipTierAsync(model, ct);
            return Ok(new { success = true, id = newId, message = "Membership tier created in Content Hub Memberships folder." });
        }

        [HttpPut("memberships/tiers/{id:int}")]
        public async Task<IActionResult> UpdateMembershipTier(int id, [FromBody] MembershipTierModel model, CancellationToken ct)
        {
            if (model == null)
            {
                return BadRequest(new { success = false, message = "Invalid tier payload." });
            }

            model.Id = id;
            int updatedId = await _contentRepo.CreateOrUpdateMembershipTierAsync(model, ct);
            return Ok(new { success = true, id = updatedId, message = $"Membership tier ID {id} updated in Content Hub." });
        }

        [HttpDelete("memberships/tiers/{id:int}")]
        public async Task<IActionResult> DeleteMembershipTier(int id, CancellationToken ct)
        {
            bool deleted = await _contentRepo.DeleteMembershipTierAsync(id, ct);
            if (!deleted)
            {
                return StatusCode(500, new { success = false, message = $"Failed to delete membership tier ID {id}." });
            }
            return Ok(new { success = true, message = $"Membership tier ID {id} deleted from Content Hub." });
        }

        // ==========================================
        // 3. TEAM MEMBERS CRUD
        // ==========================================
        [HttpGet("team")]
        public async Task<IActionResult> GetTeam()
        {
            var team = await _contentRepo.GetTeamMembersAsync();
            return Ok(new { success = true, count = team.Count, data = team });
        }

        [HttpPost("team")]
        public async Task<IActionResult> CreateTeamMember([FromBody] TeamMemberModel model, CancellationToken ct)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
            {
                return BadRequest(new { success = false, message = "Member name is required." });
            }

            model.Id = 0;
            int newId = await _contentRepo.CreateOrUpdateTeamMemberAsync(model, ct);
            return Ok(new { success = true, id = newId, message = "Team member created in Content Hub Team folder." });
        }

        [HttpPut("team/{id:int}")]
        public async Task<IActionResult> UpdateTeamMember(int id, [FromBody] TeamMemberModel model, CancellationToken ct)
        {
            if (model == null)
            {
                return BadRequest(new { success = false, message = "Invalid member payload." });
            }

            model.Id = id;
            int updatedId = await _contentRepo.CreateOrUpdateTeamMemberAsync(model, ct);
            return Ok(new { success = true, id = updatedId, message = $"Team member ID {id} updated in Content Hub." });
        }

        [HttpDelete("team/{id:int}")]
        public async Task<IActionResult> DeleteTeamMember(int id, CancellationToken ct)
        {
            bool deleted = await _contentRepo.DeleteTeamMemberAsync(id, ct);
            if (!deleted)
            {
                return StatusCode(500, new { success = false, message = $"Failed to delete team member ID {id}." });
            }
            return Ok(new { success = true, message = $"Team member ID {id} deleted from Content Hub." });
        }

        // ==========================================
        // 4. CONTENT FOLDERS CRUD
        // ==========================================
        [HttpGet("folders")]
        public async Task<IActionResult> GetFolders(CancellationToken ct)
        {
            var folders = await _contentRepo.GetFoldersAsync(ct);
            return Ok(new { success = true, count = folders.Count, data = folders });
        }

        [HttpPost("folders")]
        public async Task<IActionResult> CreateFolder([FromBody] ContentFolderModel model, CancellationToken ct)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
            {
                return BadRequest(new { success = false, message = "Folder name is required." });
            }

            var displayName = !string.IsNullOrWhiteSpace(model.DisplayName) ? model.DisplayName : model.Name;
            int folderId = await _contentRepo.EnsureFolderAsync(model.Name, displayName, ct);
            return Ok(new { success = true, folderId, message = $"Folder '{model.Name}' created in Content Hub." });
        }

        [HttpDelete("folders/{id:int}")]
        public async Task<IActionResult> DeleteFolder(int id, CancellationToken ct)
        {
            bool deleted = await _contentRepo.DeleteFolderAsync(id, ct);
            if (!deleted)
            {
                return StatusCode(500, new { success = false, message = $"Failed to delete folder ID {id}." });
            }
            return Ok(new { success = true, message = $"Folder ID {id} deleted from Content Hub." });
        }

        // ==========================================
        // 5. HEADER CONFIGURATION
        // ==========================================
        [HttpGet("header")]
        public async Task<IActionResult> GetHeader()
        {
            var header = await _contentRepo.GetHeaderConfigAsync();
            return Ok(new { success = true, data = header });
        }

        [HttpPut("header")]
        public async Task<IActionResult> UpdateHeader([FromBody] TiyaHeaderModel model, CancellationToken ct)
        {
            if (model == null) return BadRequest(new { success = false, message = "Invalid header payload." });
            int id = await _contentRepo.CreateOrUpdateHeaderConfigAsync(model, ct);
            return Ok(new { success = true, id, message = "Header configuration updated in Content Hub." });
        }

        // ==========================================
        // 6. FOOTER CONFIGURATION
        // ==========================================
        [HttpGet("footer")]
        public async Task<IActionResult> GetFooter()
        {
            var footer = await _contentRepo.GetFooterConfigAsync();
            return Ok(new { success = true, data = footer });
        }

        [HttpPut("footer")]
        public async Task<IActionResult> UpdateFooter([FromBody] TiyaFooterModel model, CancellationToken ct)
        {
            if (model == null) return BadRequest(new { success = false, message = "Invalid footer payload." });
            int id = await _contentRepo.CreateOrUpdateFooterConfigAsync(model, ct);
            return Ok(new { success = true, id, message = "Footer configuration updated in Content Hub." });
        }

        // ==========================================
        // 7. SHARED CONTENT ITEMS
        // ==========================================
        [HttpGet("shared-content")]
        public async Task<IActionResult> GetSharedContent()
        {
            var items = await _contentRepo.GetSharedContentAsync();
            return Ok(new { success = true, count = items.Count, data = items });
        }

        [HttpGet("shared-content/{name}")]
        public async Task<IActionResult> GetSharedContentItem(string name)
        {
            var item = await _contentRepo.GetSharedContentItemAsync(name);
            if (item == null) return NotFound(new { success = false, message = $"Shared content item '{name}' not found." });
            return Ok(new { success = true, data = item });
        }

        [HttpPost("shared-content")]
        public async Task<IActionResult> CreateSharedContentItem([FromBody] SharedContentItemModel model, CancellationToken ct)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.BlockName))
            {
                return BadRequest(new { success = false, message = "BlockName is required." });
            }
            int id = await _contentRepo.CreateOrUpdateSharedContentItemAsync(model, ct);
            return Ok(new { success = true, id, message = $"Shared content item '{model.BlockName}' created in Content Hub." });
        }

        [HttpPut("shared-content/{name}")]
        public async Task<IActionResult> UpdateSharedContentItem(string name, [FromBody] SharedContentItemModel model, CancellationToken ct)
        {
            if (model == null) return BadRequest(new { success = false, message = "Invalid payload." });
            model.BlockName = name;
            int id = await _contentRepo.CreateOrUpdateSharedContentItemAsync(model, ct);
            return Ok(new { success = true, id, message = $"Shared content item '{name}' updated in Content Hub." });
        }

        [HttpDelete("shared-content/{name}")]
        public async Task<IActionResult> DeleteSharedContentItem(string name, CancellationToken ct)
        {
            bool deleted = await _contentRepo.DeleteSharedContentItemAsync(name, ct);
            if (!deleted) return StatusCode(500, new { success = false, message = $"Failed to delete shared content item '{name}'." });
            return Ok(new { success = true, message = $"Shared content item '{name}' deleted from Content Hub." });
        }
    }
}
