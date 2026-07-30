using TodoApp.Domain.Common;

namespace TodoApp.Domain.Users;

/// <summary>
/// Aggregate root for a registered account. Kept intentionally minimal —
/// this is the piece of the skeleton that closes the "anyone can claim to
/// be any userId" gap the rest of the app had while there was no real auth.
/// </summary>
public class User : AggregateRoot
{
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedOn { get; private set; }

    // Default true for all — an account that's never touched these settings
    // should behave exactly like notifications did before this feature
    // existed (everything on), not go silent by default.
    public bool NotifyOnAssignment { get; private set; } = true;
    public bool NotifyOnMention { get; private set; } = true;
    public bool NotifyOnInvitation { get; private set; } = true;

    /// <summary>US-120: whether to notify the assignee before a story's due date.</summary>
    public bool NotifyOnDueSoon { get; private set; } = true;

    /// <summary>US-120 AC: "configurable per user in settings" — how many hours before the due date the reminder fires.</summary>
    public int ReminderLeadTimeHours { get; private set; } = 24;

    /// <summary>False until VerifyEmailCommand confirms a token sent to this address. Non-blocking — an unverified account can still use the app; the frontend just shows a reminder banner.</summary>
    public bool IsEmailVerified { get; private set; }

    private User() { }

    private User(string id, string email, string displayName, string passwordHash) : base(id)
    {
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        CreatedOn = DateTime.UtcNow;
    }

    public static User Create(string id, string email, string displayName, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.", nameof(displayName));

        return new User(id, email.Trim().ToLowerInvariant(), displayName.Trim(), passwordHash);
    }

    public static User Rehydrate(
        string id, string email, string displayName, string passwordHash, DateTime createdOn,
        bool notifyOnAssignment, bool notifyOnMention, bool notifyOnInvitation, bool isEmailVerified,
        bool notifyOnDueSoon, int reminderLeadTimeHours)
    {
        var user = new User(id, email, displayName, passwordHash)
        {
            CreatedOn = createdOn,
            NotifyOnAssignment = notifyOnAssignment,
            NotifyOnMention = notifyOnMention,
            NotifyOnInvitation = notifyOnInvitation,
            IsEmailVerified = isEmailVerified,
            NotifyOnDueSoon = notifyOnDueSoon,
            ReminderLeadTimeHours = reminderLeadTimeHours
        };
        return user;
    }

    /// <summary>Used by ResetPasswordCommandHandler once a valid reset token has been verified.</summary>
    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password hash is required.", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
    }

    public void UpdateNotificationPreferences(bool notifyOnAssignment, bool notifyOnMention, bool notifyOnInvitation, bool notifyOnDueSoon, int reminderLeadTimeHours)
    {
        if (reminderLeadTimeHours < 1)
            throw new ArgumentException("Reminder lead time must be at least 1 hour.", nameof(reminderLeadTimeHours));

        NotifyOnAssignment = notifyOnAssignment;
        NotifyOnMention = notifyOnMention;
        NotifyOnInvitation = notifyOnInvitation;
        NotifyOnDueSoon = notifyOnDueSoon;
        ReminderLeadTimeHours = reminderLeadTimeHours;
    }

    public void VerifyEmail() => IsEmailVerified = true;
}
