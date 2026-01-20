namespace BookingPlatform.Application.Services;

public interface IEmailService
{
    Task SendInvitationLinkEmailAsync(InvitationLinkEmailRequest request, CancellationToken cancellationToken = default);
    Task SendInvitationEmailAsync(InvitationEmailRequest request, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(PasswordResetEmailRequest request, CancellationToken cancellationToken = default);
    Task SendAppointmentConfirmationAsync(AppointmentEmailRequest request, CancellationToken cancellationToken = default);
    Task SendAppointmentReminderAsync(AppointmentEmailRequest request, CancellationToken cancellationToken = default);
    Task SendAppointmentCancellationAsync(AppointmentEmailRequest request, CancellationToken cancellationToken = default);
}

public record InvitationLinkEmailRequest(
    string ToEmail,
    string TenantName,
    string InvitedByName,
    string Role,
    string AcceptInvitationUrl
);

public record InvitationEmailRequest(
    string ToEmail,
    string ToName,
    string TenantName,
    string TemporaryPassword,
    string LoginUrl
);

public record PasswordResetEmailRequest(
    string ToEmail,
    string ToName,
    string ResetToken,
    string ResetUrl
);

public record AppointmentEmailRequest(
    string ToEmail,
    string ToName,
    string TenantName,
    string ServiceName,
    string StaffName,
    DateTime AppointmentDateTime,
    int DurationMinutes,
    decimal Price,
    string? Notes = null
);
