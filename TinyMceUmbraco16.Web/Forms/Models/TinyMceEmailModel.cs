using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Forms.Core.Models;
using Umbraco.Forms.Core.Persistence.Dtos;

namespace TinyMceUmbraco16.Web.Forms.Models
{
    public class TinyMceEmailModel
    {
        public string FormName { get; set; } = string.Empty;
        public Record Record { get; set; } = default!;
        public Form Form { get; set; } = default!;
        public IPublishedContent? ContentNode { get; set; }
    }

}
