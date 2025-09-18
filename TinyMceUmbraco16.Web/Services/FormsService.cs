using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using TinyMceUmbraco16.Web.Forms.Models;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.Email;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common;
using Umbraco.Forms.Core;

namespace TinyMceUmbraco16.Web.Services
{
    public class FormsService : IFormsService
    {
        private readonly IEmailSender _emailSender;
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<FormsService> _logger;
        private readonly IUmbracoHelperAccessor _umbracoHelperAccessor;

        public FormsService(
            IEmailSender emailSender,
            IRazorViewEngine razorViewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider,
            ILogger<FormsService> logger,
            IUmbracoHelperAccessor umbracoHelperAccessor)
        {
            _emailSender = emailSender;
            _razorViewEngine = razorViewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _umbracoHelperAccessor = umbracoHelperAccessor;
        }

        public async Task<string> RenderRazorViewToStringAsync(string viewPath, WorkflowExecutionContext context, string? templateContent)
        {
            using var scope = _serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            var actionContext = new ActionContext(
                new DefaultHttpContext { RequestServices = scopedProvider },
                new RouteData(),
                new ActionDescriptor());

            await using var sw = new StringWriter();

            var viewResult = _razorViewEngine.GetView(null, viewPath, true);
            if (!viewResult.Success)
            {
                var fallbackPath = $"~/Views/Partials/{viewPath.TrimStart('~', '/')}";
                viewResult = _razorViewEngine.GetView(null, fallbackPath, true);

                if (!viewResult.Success)
                    throw new InvalidOperationException($"View not found. Tried '{viewPath}' and '{fallbackPath}'.");
            }

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

        public Task<EmailMessage> BuildEmailMessageAsync(
            WorkflowExecutionContext context,
            string? subject,
            string? senderEmail,
            string? recipientEmail,
            string? ccEmail,
            string? bccEmail,
            string? replyToEmail,
            string emailBody)
        {
            var email = new EmailMessage(
                from: !string.IsNullOrWhiteSpace(senderEmail) ? senderEmail : "no-reply@yourdomain.com",
                to: recipientEmail?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>(),
                cc: ccEmail?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>(),
                bcc: bccEmail?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>(),
                replyTo: replyToEmail?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>(),
                subject: subject ?? $"Form submission: {context.Form.Name}",
                body: emailBody,
                attachments: null,
                isBodyHtml: true
            );

            return Task.FromResult(email);
        }

        public async Task SendEmailAsync(EmailMessage email, string workflowName)
        {
            await _emailSender.SendAsync(email, "umbracoForm");
            _logger.LogInformation("Workflow '{Workflow}' sent email to {Recipients}", workflowName, string.Join(", ", email.To));
        }

        public Task<string> ReplaceTemplateTokensAsync(string? template, WorkflowExecutionContext context, string? culture = null)
        {
            if (string.IsNullOrWhiteSpace(template))
                return Task.FromResult(string.Empty);

            _umbracoHelperAccessor.TryGetUmbracoHelper(out var umbracoHelper);

            var rx = new Regex(@"\[\s*(?<token>[A-Za-z0-9_.]+)\s*\]",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

            string replaced = rx.Replace(template, m =>
            {
                var token = m.Groups["token"].Value;

                // 1. Customer.* → Umbraco Forms field alias
                if (token.StartsWith("Customer.", StringComparison.OrdinalIgnoreCase))
                {
                    var alias = token.Substring("Customer.".Length);
                    var value = GetFormValueByAlias(context, alias);

                    if (!string.IsNullOrEmpty(value))
                        return WebUtility.HtmlEncode(value).Replace("\r\n", "<br/>").Replace("\n", "<br/>");

                    return string.Empty;
                }

                // 2. Dictionary item
                if (umbracoHelper != null)
                {
                    var dict = umbracoHelper.GetDictionaryValue(token, CultureInfo.CurrentCulture);
                    if (!string.IsNullOrEmpty(dict))
                        return dict;
                }

                // 3. Unknown → leave as-is
                return m.Value;
            });

            return Task.FromResult(replaced);
        }

        private static string? GetFormValueByAlias(WorkflowExecutionContext context, string alias)
        {
            if (context?.Form?.AllFields == null || context.Record == null)
                return null;

            // Normalize alias: remove spaces, underscores, make lowercase
            string Normalize(string input) =>
                new string(input.Where(c => !char.IsWhiteSpace(c) && c != '_').ToArray()).ToLowerInvariant();

            var normalizedAlias = Normalize(alias);

            // Try to find field by alias OR caption (label)
            var field = context.Form.AllFields.FirstOrDefault(f =>
                Normalize(f.Alias).Equals(normalizedAlias, StringComparison.OrdinalIgnoreCase) ||
                Normalize(f.Caption ?? string.Empty).Equals(normalizedAlias, StringComparison.OrdinalIgnoreCase));

            if (field == null)
                return null;

            if (context.Record.RecordFields != null &&
                context.Record.RecordFields.TryGetValue(field.Id, out var recordField) &&
                recordField != null)
            {
                if (recordField.Values is IEnumerable<object> vals && vals.Any())
                    return string.Join(", ", vals.Where(v => v != null).Select(v => v.ToString()));

                if (recordField.Values is IEnumerable<string> svals && svals.Any())
                    return string.Join(", ", svals.Where(s => !string.IsNullOrWhiteSpace(s)));
            }

            return null;
        }

    }
}
