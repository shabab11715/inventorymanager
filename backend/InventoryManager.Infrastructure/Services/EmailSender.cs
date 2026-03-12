using InventoryManager.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace InventoryManager.Infrastructure.Services;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var apiKey = _configuration["SendGrid:ApiKey"];
        var fromEmail = _configuration["SendGrid:FromEmail"];
        var fromName = _configuration["SendGrid:FromName"];

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(fromEmail))
        {
            _logger.LogWarning("SendGrid is not configured. Verification email was not sent to {Email}. Subject: {Subject}", toEmail, subject);
            _logger.LogInformation("Email body for {Email}: {Body}", toEmail, htmlBody);
            return;
        }

        var client = new SendGridClient(apiKey);

        var from = new EmailAddress(fromEmail, fromName);
        var to = new EmailAddress(toEmail);

        var message = MailHelper.CreateSingleEmail(from, to, subject, null, htmlBody);

        await client.SendEmailAsync(message);
    }
}