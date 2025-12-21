namespace BackendApi.Application.Services;

/// <summary>
/// Email service interface for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a verification email to the user
    /// </summary>
    Task SendVerificationEmailAsync(string to, string verificationToken, string userName);

    /// <summary>
    /// Sends a notification email (e.g., recipe created, recipe approved)
    /// </summary>
    Task SendNotificationEmailAsync(string to, string subject, string body, bool isHtml = true);

    /// <summary>
    /// Sends a password reset email
    /// </summary>
    Task SendPasswordResetEmailAsync(string to, string resetToken, string userName);

    /// <summary>
    /// Sends a recipe created notification to admins
    /// </summary>
    Task SendRecipeCreatedNotificationAsync(int recipeId, string recipeTitle, string? userEmail);
}

