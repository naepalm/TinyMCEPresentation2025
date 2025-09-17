using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Web;
using TinyMceUmbraco16.Web.Forms.Models;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.Email;
using Umbraco.Forms.Core;
using Umbraco.Forms.Core.Enums;

namespace TinyMceUmbraco16.Forms.Workflows
{
    public class SendEmailWithPickedTemplate : WorkflowType
    {
        private readonly IEmailSender _emailSender;
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SendEmailWithPickedTemplate> _logger;

        public SendEmailWithPickedTemplate(
            IEmailSender emailSender,
            IRazorViewEngine razorViewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider,
            ILogger<SendEmailWithPickedTemplate> logger)
        {
            _emailSender = emailSender;
            _razorViewEngine = razorViewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
            _logger = logger;

            Id = new Guid("f3b5e59a-1c21-4f2e-9f93-111111111111");
            Name = "Send Email With Picked Template";
            Description = "Sends an email using a picked Razor email template";
            Icon = "icon-message";
            Group = "Custom";
        }

        #region Settings

        [Umbraco.Forms.Core.Attributes.Setting("Content Node",
            Description = "Pick the TinyMCE Email Template you'd like to use from the templates inside of the Data Folder",
            View = "Umb.PropertyEditorUi.DocumentPicker")]
        public string? ContentNode { get; set; }

        [Umbraco.Forms.Core.Attributes.Setting("Recipient Email",
            Description = "Enter the recipient email address(es).",
            View = "Umb.PropertyEditorUi.TextBox")]
        public string? RecipientEmail { get; set; }

        [Umbraco.Forms.Core.Attributes.Setting("CC Email",
            Description = "Enter the CC email addresses (if required).",
            View = "Umb.PropertyEditorUi.TextBox")]
        public string? CCEmail { get; set; }

        [Umbraco.Forms.Core.Attributes.Setting("BCC Email",
            Description = "Enter the BCC email addresses (if required).",
            View = "Umb.PropertyEditorUi.TextBox")]
        public string? BCCEmail { get; set; }

        [Umbraco.Forms.Core.Attributes.Setting("Sender Email",
            Description = "Enter the sender email (if blank it will use the settings from configuration).",
            View = "Umb.PropertyEditorUi.TextBox")]
        public string? SenderEmail { get; set; }

        [Umbraco.Forms.Core.Attributes.Setting("Reply To Email",
            Description = "Enter the reply-to email (if required).",
            View = "Umb.PropertyEditorUi.TextBox")]
        public string? ReplyToEmail { get; set; }

        [Umbraco.Forms.Core.Attributes.Setting("Email Template",
            Description = "Path to Razor view to use for generating the email body (e.g. ~/Views/Partials/Forms/Emails/MyTemplate.cshtml).",
            View = "Forms.PropertyEditorUi.EmailTemplatePicker")]

        #endregion

        public string? EmailTemplate { get; set; }

        public override List<Exception> ValidateSettings()
        {
            var errors = new List<Exception>();
            if (string.IsNullOrWhiteSpace(ContentNode))
                errors.Add(new Exception("You must pick a content node."));
            if (string.IsNullOrWhiteSpace(RecipientEmail))
                errors.Add(new Exception("You must enter at least one recipient email address."));
            if (string.IsNullOrWhiteSpace(EmailTemplate))
                errors.Add(new Exception("You must select an email template."));
            return errors;
        }

        public override async Task<WorkflowExecutionStatus> ExecuteAsync(WorkflowExecutionContext context)
        {
            try
            {
                // Try to resolve ContentNode if one was picked
                using var scope = _serviceProvider.CreateScope();
                Umbraco.Cms.Core.Models.PublishedContent.IPublishedContent? contentNode = null;
                if (!string.IsNullOrWhiteSpace(ContentNode) && Guid.TryParse(ContentNode, out var guid))
                {
                    var pcq = scope.ServiceProvider.GetRequiredService<IPublishedContentQuery>();
                    contentNode = pcq.Content(guid);
                }

                // 1. Render Razor template
                var emailBody = await RenderRazorViewToStringAsync(EmailTemplate!, context, contentNode?.Value<IHtmlString>("template"));

                // 2. Create EmailMessage
                var email = new EmailMessage(
                    from: !string.IsNullOrWhiteSpace(SenderEmail) ? SenderEmail : "no-reply@yourdomain.com",
                    to: RecipientEmail?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>(),
                    cc: CCEmail?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>(),
                    bcc: BCCEmail?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>(),
                    replyTo: ReplyToEmail?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>(),
                    subject: contentNode?.Value<string>("subject") ?? $"Form submission: {context.Form.Name}",
                    body: emailBody,
                    attachments: null,
                    isBodyHtml: true
                );


                // 3. Send email (SMTP from appsettings.json)
                await _emailSender.SendAsync(email, "umbracoForm");
                _logger.LogInformation("Workflow '{Workflow}' sent email to {Recipients}", Name, string.Join(", ", email.To));
                return WorkflowExecutionStatus.Completed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Workflow '{Workflow}' failed to send email", Name);
                return WorkflowExecutionStatus.Failed;
            }
        }

        // Utility: render Razor view into a string
        private async Task<string> RenderRazorViewToStringAsync(string viewPath, WorkflowExecutionContext context, IHtmlString? templateContent)
        {
            using var scope = _serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            var actionContext = new ActionContext(
                new DefaultHttpContext { RequestServices = scopedProvider },
                new RouteData(),
                new ActionDescriptor());

            await using var sw = new StringWriter();

            // Try original path
            var viewResult = _razorViewEngine.GetView(null, viewPath, true);

            // Fallback to ~/Views/Partials if needed
            if (!viewResult.Success)
            {
                var fallbackPath = $"~/Views/Partials/{viewPath.TrimStart('~', '/')}";
                viewResult = _razorViewEngine.GetView(null, fallbackPath, true);

                if (!viewResult.Success)
                    throw new InvalidOperationException(
                        $"View not found. Tried '{viewPath}' and '{fallbackPath}'.");
            }

            // Build the model
            var model = new TinyMceEmailModel
            {
                FormName = context.Form.Name,
                Record = context.Record,
                Form = context.Form,
                TemplateContent = templateContent
            };

            var viewDictionary = new ViewDataDictionary(
                new EmptyModelMetadataProvider(),
                new ModelStateDictionary())
            {
                Model = model
            };

            var viewContext = new ViewContext(
                actionContext,
                viewResult.View,
                viewDictionary,
                new TempDataDictionary(actionContext.HttpContext, scopedProvider.GetRequiredService<ITempDataProvider>()),
                sw,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);
            return sw.ToString();
        }
    }
}
