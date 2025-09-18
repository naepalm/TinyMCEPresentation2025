using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Web;
using TinyMceUmbraco16.Web.Services;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Forms.Core;
using Umbraco.Forms.Core.Enums;

namespace TinyMceUmbraco16.Forms.Workflows
{
    public class SendEmailWithPickedTemplate : WorkflowType
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly ILogger<SendEmailWithPickedTemplate> _logger;
        private readonly IFormsService _formsService;

        public SendEmailWithPickedTemplate(
            IServiceProvider serviceProvider,
            IUmbracoContextAccessor umbracoContextAccessor,
            ILogger<SendEmailWithPickedTemplate> logger,
            IFormsService formsService)
        {
            _serviceProvider = serviceProvider;
            _umbracoContextAccessor = umbracoContextAccessor;
            _logger = logger;
            _formsService = formsService;

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
                IPublishedContent? contentNode = null;
                IPublishedContent? pageNode = null;

                if (!string.IsNullOrWhiteSpace(ContentNode) && Guid.TryParse(ContentNode, out var guid))
                {
                    using var scope = _serviceProvider.CreateScope();
                    var pcq = scope.ServiceProvider.GetRequiredService<IPublishedContentQuery>();
                    contentNode = pcq.Content(guid);
                }

                if (_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
                {
                    pageNode = umbracoContext.PublishedRequest?.PublishedContent;
                }

                // 1. Page specific overrides (if any) or fallbacks to the template node
                var fallbackSubject = contentNode?.Value<string>("subject") ?? $"Form submission: {context.Form.Name}";
                var fallbackTemplate = contentNode?.Value<string>("template") ?? string.Empty;

                var overrideSubject = pageNode?.Value<string>("subjectOverride");
                var overrideTemplate = pageNode?.Value<string>("templateOverride");

                var finalSubject = !string.IsNullOrWhiteSpace(overrideSubject) ? overrideSubject : fallbackSubject;
                var rawTemplate = !string.IsNullOrWhiteSpace(overrideTemplate) ? overrideTemplate : fallbackTemplate;

                // 2. Expand [Customer.*] and dictionary tokens
                var processedTemplate = await _formsService.ReplaceTemplateTokensAsync(rawTemplate, context);

                // 3. Render body
                var emailBody = await _formsService.RenderRazorViewToStringAsync(EmailTemplate!, context, processedTemplate);

                // 4. Build message
                var email = await _formsService.BuildEmailMessageAsync(context, finalSubject, SenderEmail, RecipientEmail, CCEmail, BCCEmail, ReplyToEmail, emailBody);

                // 5. Send
                await _formsService.SendEmailAsync(email, Name);

                return WorkflowExecutionStatus.Completed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Workflow '{Workflow}' failed to send email", Name);
                return WorkflowExecutionStatus.Failed;
            }
        }

    }
}
