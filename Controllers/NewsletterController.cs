using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;

namespace Podcast.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsletterController : ControllerBase
    {
        [HttpPost("submit")]
        public IActionResult Submit([FromForm] NewsletterSubmissionData data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.Email) || !new EmailAddressAttribute().IsValid(data.Email))
                {
                    return BadRequest(new { success = false, message = "Please provide a valid email address." });
                }

                // In production, save to Kentico Contact Management or database here
                return Ok(new { success = true, message = $"Thank you! '{data.Email}' has been successfully subscribed to Pod Talk updates." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while processing your subscription. Please try again later." });
            }
        }
    }

    public class NewsletterSubmissionData
    {
        public string Email { get; set; } = string.Empty;
    }
}
