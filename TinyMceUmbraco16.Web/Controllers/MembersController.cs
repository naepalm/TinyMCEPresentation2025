using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace TinyMceUmbraco16.Web.Controllers
{
    [ApiController]
    [Route("umbraco/api/[controller]")]
    public class MembersController : ControllerBase
    {
        private readonly IMemberService _memberService;
        private readonly IMemberManager _memberManager;

        public MembersController(IMemberService memberService, IMemberManager memberManager)
        {
            _memberService = memberService;
            _memberManager = memberManager;
        }

        [HttpGet("getmembers")]
        public IActionResult GetMembers(string term = "")
        {
            var members = _memberService.GetAllMembers()
                .Where(m => string.IsNullOrEmpty(term) ||
                            m.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .Select(m => new
                {
                    id = m.Id,
                    name = m.Name,
                    description = m.GetValue<string>("description")
                });

            return Ok(members);
        }

        [HttpGet("getmember/{id}")]
        public async Task<IActionResult> GetMember(string id)
        {
            var member = await _memberManager.FindByIdAsync(id);

            if (member == null)
                return NotFound();

            var publishedMember = _memberManager.AsPublishedMember(member);

            var result = new
            {
                id = id,
                name = member.Name,
                description = publishedMember?.Value<string>("description"),
                image = publishedMember?.Value<MediaWithCrops>("image")?.GetCropUrl(100, 100)
            };

            return Ok(result);
        }
    }
}
