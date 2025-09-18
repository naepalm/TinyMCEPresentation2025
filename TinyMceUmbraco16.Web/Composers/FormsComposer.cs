using TinyMceUmbraco16.Web.Services;
using Umbraco.Cms.Core.Composing;

namespace TinyMceUmbraco16.Composers
{
    public class FormsComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            // Register your service with DI
            builder.Services.AddSingleton<IFormsService, FormsService>();
        }
    }
}
