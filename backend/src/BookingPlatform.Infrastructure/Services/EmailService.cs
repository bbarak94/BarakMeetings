using System.Net;
using System.Net.Mail;
using BookingPlatform.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookingPlatform.Infrastructure.Services;

public class EmailSettings
{
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Booking Platform";
    public bool EnableSsl { get; set; } = true;
    public bool IsEnabled { get; set; } = false; // Disabled by default for development
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendInvitationLinkEmailAsync(InvitationLinkEmailRequest request, CancellationToken cancellationToken = default)
    {
        var subject = $"You've been invited to join {request.TenantName}";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #4F46E5;'>You're Invited!</h2>
                    <p>Hi there,</p>
                    <p><strong>{request.InvitedByName}</strong> has invited you to join <strong>{request.TenantName}</strong> as a <strong>{request.Role}</strong>.</p>
                    <p>Click the button below to accept the invitation and set up your account:</p>
                    <p style='margin: 30px 0;'>
                        <a href='{request.AcceptInvitationUrl}' style='display: inline-block; background-color: #4F46E5; color: white; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: bold;'>
                            Accept Invitation
                        </a>
                    </p>
                    <p style='color: #6B7280; font-size: 14px;'>This invitation will expire in 7 days.</p>
                    <p style='color: #6B7280; font-size: 14px;'>If you didn't expect this invitation, you can safely ignore this email.</p>
                    <hr style='border: none; border-top: 1px solid #E5E7EB; margin: 20px 0;'>
                    <p style='color: #9CA3AF; font-size: 12px;'>This email was sent by {request.TenantName} via Booking Platform.</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(request.ToEmail, "New User", subject, body, cancellationToken);
    }

    public async Task SendInvitationEmailAsync(InvitationEmailRequest request, CancellationToken cancellationToken = default)
    {
        var subject = $"You've been invited to {request.TenantName}";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #4F46E5;'>Welcome to {request.TenantName}!</h2>
                    <p>Hi {request.ToName},</p>
                    <p>You've been invited to join <strong>{request.TenantName}</strong> on our booking platform.</p>
                    <p>Here are your login credentials:</p>
                    <div style='background-color: #F3F4F6; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 5px 0;'><strong>Email:</strong> {request.ToEmail}</p>
                        <p style='margin: 5px 0;'><strong>Temporary Password:</strong> <code style='background: #E5E7EB; padding: 2px 6px; border-radius: 4px;'>{request.TemporaryPassword}</code></p>
                    </div>
                    <p>
                        <a href='{request.LoginUrl}' style='display: inline-block; background-color: #4F46E5; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin: 10px 0;'>
                            Sign In Now
                        </a>
                    </p>
                    <p style='color: #6B7280; font-size: 14px;'>For security reasons, please change your password after your first login.</p>
                    <hr style='border: none; border-top: 1px solid #E5E7EB; margin: 20px 0;'>
                    <p style='color: #9CA3AF; font-size: 12px;'>This email was sent by {request.TenantName} via Booking Platform.</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(request.ToEmail, request.ToName, subject, body, cancellationToken);
    }

    public async Task SendPasswordResetEmailAsync(PasswordResetEmailRequest request, CancellationToken cancellationToken = default)
    {
        var subject = "Reset Your Password";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #4F46E5;'>Password Reset Request</h2>
                    <p>Hi {request.ToName},</p>
                    <p>We received a request to reset your password. Click the button below to create a new password:</p>
                    <p>
                        <a href='{request.ResetUrl}' style='display: inline-block; background-color: #4F46E5; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin: 10px 0;'>
                            Reset Password
                        </a>
                    </p>
                    <p style='color: #6B7280; font-size: 14px;'>If you didn't request this, you can safely ignore this email.</p>
                    <p style='color: #6B7280; font-size: 14px;'>This link will expire in 1 hour.</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(request.ToEmail, request.ToName, subject, body, cancellationToken);
    }

    public async Task SendAppointmentConfirmationAsync(AppointmentEmailRequest request, CancellationToken cancellationToken = default)
    {
        var subject = $"Appointment Confirmed - {request.TenantName}";
        var body = BuildAppointmentEmailBody(request, "Your Appointment is Confirmed", "#10B981");

        await SendEmailAsync(request.ToEmail, request.ToName, subject, body, cancellationToken);
    }

    public async Task SendAppointmentReminderAsync(AppointmentEmailRequest request, CancellationToken cancellationToken = default)
    {
        var subject = $"Appointment Reminder - {request.TenantName}";
        var body = BuildAppointmentEmailBody(request, "Appointment Reminder", "#F59E0B");

        await SendEmailAsync(request.ToEmail, request.ToName, subject, body, cancellationToken);
    }

    public async Task SendAppointmentCancellationAsync(AppointmentEmailRequest request, CancellationToken cancellationToken = default)
    {
        var subject = $"Appointment Cancelled - {request.TenantName}";
        var body = BuildAppointmentEmailBody(request, "Appointment Cancelled", "#EF4444");

        await SendEmailAsync(request.ToEmail, request.ToName, subject, body, cancellationToken);
    }

    private string BuildAppointmentEmailBody(AppointmentEmailRequest request, string title, string headerColor)
    {
        return $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: {headerColor};'>{title}</h2>
                    <p>Hi {request.ToName},</p>
                    <div style='background-color: #F3F4F6; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='margin-top: 0; color: #374151;'>{request.ServiceName}</h3>
                        <p style='margin: 8px 0;'><strong>Date:</strong> {request.AppointmentDateTime:dddd, MMMM d, yyyy}</p>
                        <p style='margin: 8px 0;'><strong>Time:</strong> {request.AppointmentDateTime:h:mm tt}</p>
                        <p style='margin: 8px 0;'><strong>Duration:</strong> {request.DurationMinutes} minutes</p>
                        <p style='margin: 8px 0;'><strong>Staff:</strong> {request.StaffName}</p>
                        <p style='margin: 8px 0;'><strong>Price:</strong> ${request.Price:N2}</p>
                        {(string.IsNullOrEmpty(request.Notes) ? "" : $"<p style='margin: 8px 0;'><strong>Notes:</strong> {request.Notes}</p>")}
                    </div>
                    <hr style='border: none; border-top: 1px solid #E5E7EB; margin: 20px 0;'>
                    <p style='color: #9CA3AF; font-size: 12px;'>This email was sent by {request.TenantName} via Booking Platform.</p>
                </div>
            </body>
            </html>";
    }

    private async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email service called. IsEnabled={IsEnabled}, SmtpHost={SmtpHost}, SmtpUser={SmtpUser}, FromEmail={FromEmail}",
            _settings.IsEnabled,
            _settings.SmtpHost,
            string.IsNullOrEmpty(_settings.SmtpUser) ? "(not set)" : _settings.SmtpUser,
            string.IsNullOrEmpty(_settings.FromEmail) ? "(not set)" : _settings.FromEmail);

        if (!_settings.IsEnabled)
        {
            _logger.LogWarning(
                "EMAIL DISABLED: Would have sent to {ToEmail}. Set Email__IsEnabled=true in environment.",
                toEmail);
            return;
        }

        if (string.IsNullOrEmpty(_settings.SmtpUser) || string.IsNullOrEmpty(_settings.SmtpPassword))
        {
            _logger.LogWarning("Email credentials not configured. SmtpUser or SmtpPassword empty. Skipping email to {ToEmail}", toEmail);
            return;
        }

        if (string.IsNullOrEmpty(_settings.FromEmail))
        {
            _logger.LogWarning("FromEmail not configured. Skipping email to {ToEmail}", toEmail);
            return;
        }

        try
        {
            _logger.LogInformation("Attempting to send email via {SmtpHost}:{SmtpPort} to {ToEmail}",
                _settings.SmtpHost, _settings.SmtpPort, toEmail);

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPassword),
                EnableSsl = _settings.EnableSsl,
                Timeout = 30000 // 30 second timeout
            };

            var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(toEmail, toName));

            // Use a timeout for the send operation
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            await client.SendMailAsync(message, cts.Token);

            _logger.LogInformation("Email sent successfully to {ToEmail} with subject: {Subject}", toEmail, subject);
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Email send timed out for {ToEmail} with subject: {Subject}", toEmail, subject);
            throw new TimeoutException($"Email send timed out for {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail} with subject: {Subject}. Error: {Error}", toEmail, subject, ex.Message);
            throw;
        }
    }
}
