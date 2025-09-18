using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;

namespace TinyMceUmbraco16.Web.Controllers
{
    [ApiController]
    [Route("umbraco/api/[controller]")]
    public class AdvancedTemplatesController : ControllerBase
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IDocumentNavigationQueryService _nav;

        public AdvancedTemplatesController(
            IUmbracoContextAccessor umbracoContextAccessor,
            IDocumentNavigationQueryService nav)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
            _nav = nav;
        }

        [HttpGet("gettemplate/{id}")]
        public IActionResult GetTemplate(string id)
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var ctx))
                return NotFound();

            if (!Guid.TryParse(id, out var key))
                return BadRequest("Invalid template id.");

            var node = ctx.Content?.GetById(key);
            if (node is null)
                return NotFound();

            var result = new
            {
                id = node.Key.ToString(),
                name = node.Name,
                content = node.Value<string>("template")
            };

            return Ok(result);
        }

        [HttpGet("gettemplates")]
        public IActionResult GetTemplates()
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var ctx))
                return NotFound();

            // 1) Get keys of all root nodes (modern, non-obsolete API)
            if (!_nav.TryGetRootKeys(out var rootKeys) || rootKeys is null)
                return Ok(Enumerable.Empty<object>());

            // 2) Resolve keys -> IPublishedContent via UmbracoContext
            var rootNodes = rootKeys
                .Select(k => ctx.Content?.GetById(k))
                .WhereNotNull()
                .ToList();

            var dataFolder = rootNodes.FirstOrDefault(x => x.ContentType.Alias == "dataFolder");
            if (dataFolder is null)
                return Ok(Enumerable.Empty<object>());

            var categories = dataFolder.Children()
                .Where(x => x.ContentType.Alias == "templatesFolder")
                .Select(category => new
                {
                    id = category.Key.ToString(),
                    name = category.Name,
                    items = category.Children()
                        .Where(x => x.ContentType.Alias == "emailTemplate")
                        .Select(t => new
                        {
                            id = t.Key.ToString(),
                            name = t.Name,
                            content = t.Value<string>("template")
                        })
                });

            return Ok(categories);
        }
    }
}
