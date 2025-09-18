using System.Web;
using Umbraco.Cms.Core.Models.Email;
using Umbraco.Forms.Core;

namespace TinyMceUmbraco16.Web.Services
{
    public interface IFormsService
    {
        Task<string> RenderRazorViewToStringAsync(string viewPath, WorkflowExecutionContext context, string? templateContent);

        Task<EmailMessage> BuildEmailMessageAsync(
            WorkflowExecutionContext context,
            string? subject,
            string? senderEmail,
            string? recipientEmail,
            string? ccEmail,
            string? bccEmail,
            string? replyToEmail,
            string emailBody);

        Task SendEmailAsync(EmailMessage email, string workflowName);

        Task<string> ReplaceTemplateTokensAsync(string? template, WorkflowExecutionContext context, string? culture = null);
    }
}
