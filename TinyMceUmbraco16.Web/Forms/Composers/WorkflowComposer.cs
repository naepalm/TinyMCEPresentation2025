using TinyMceUmbraco16.Forms.Workflows;
using Umbraco.Cms.Core.Composing;
using Umbraco.Forms.Core.Providers;

namespace TinyMceUmbraco16.Web.Forms.Composers
{
    public class WorkflowComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.WithCollectionBuilder<WorkflowCollectionBuilder>()
                   .Add<SendEmailWithPickedTemplate>();
        }
    }
}
