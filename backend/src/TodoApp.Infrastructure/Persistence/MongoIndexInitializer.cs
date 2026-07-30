using MongoDB.Driver;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence;

/// <summary>
/// Creates the indexes the app's queries rely on. CreateOneAsync/CreateManyAsync
/// are idempotent — MongoDB no-ops if an equivalent index already exists — so
/// this is safe to call on every startup rather than requiring a manual
/// mongosh/migration step.
/// </summary>
public static class MongoIndexInitializer
{
    public static async Task EnsureIndexesAsync(MongoDbContext context, CancellationToken cancellationToken = default)
    {
        // Teams: looked up by member on every "my teams" / membership check.
        await context.Teams.Indexes.CreateOneAsync(
            new CreateIndexModel<TeamDocument>(Builders<TeamDocument>.IndexKeys.Ascending("Members.UserId")),
            cancellationToken: cancellationToken);

        // User stories: filtered by team on every list/board/dashboard load, by
        // assignee for the "assigned to me" filter, and searched by keyword —
        // the text index backs the US-116 keyword search (see UserStoryRepository.SearchAsync).
        await context.UserStories.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<UserStoryDocument>(Builders<UserStoryDocument>.IndexKeys.Ascending(s => s.TeamId)),
            new CreateIndexModel<UserStoryDocument>(Builders<UserStoryDocument>.IndexKeys.Ascending(s => s.AssigneeId)),
            new CreateIndexModel<UserStoryDocument>(
                Builders<UserStoryDocument>.IndexKeys.Combine(
                    Builders<UserStoryDocument>.IndexKeys.Text(s => s.Title),
                    Builders<UserStoryDocument>.IndexKeys.Text(s => s.Description))),
        }, cancellationToken);

        // Comments: always fetched by their parent story.
        await context.Comments.Indexes.CreateOneAsync(
            new CreateIndexModel<CommentDocument>(Builders<CommentDocument>.IndexKeys.Ascending(c => c.UserStoryId)),
            cancellationToken: cancellationToken);

        // Notifications: always fetched by recipient (the bell polls this per user).
        await context.Notifications.Indexes.CreateOneAsync(
            new CreateIndexModel<NotificationDocument>(Builders<NotificationDocument>.IndexKeys.Ascending(n => n.RecipientUserId)),
            cancellationToken: cancellationToken);

        // Users: email must be unique and is the lookup key for login.
        await context.Users.Indexes.CreateOneAsync(
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        // Invitations: "my pending invitations" is looked up by invitee, and
        // InviteTeamMemberCommandHandler checks for an existing pending
        // invitation per (team, invitee) before creating a new one.
        await context.Invitations.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<InvitationDocument>(Builders<InvitationDocument>.IndexKeys.Ascending(i => i.InvitedUserId)),
            new CreateIndexModel<InvitationDocument>(
                Builders<InvitationDocument>.IndexKeys
                    .Ascending(i => i.TeamId)
                    .Ascending(i => i.InvitedUserId)),
        }, cancellationToken);

        // Refresh tokens: looked up by hash on every refresh; the TTL index
        // auto-deletes documents once ExpiresOn is in the past, so expired/
        // rotated-away tokens don't accumulate forever.
        await context.RefreshTokens.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<RefreshTokenDocument>(Builders<RefreshTokenDocument>.IndexKeys.Ascending(t => t.TokenHash)),
            new CreateIndexModel<RefreshTokenDocument>(
                Builders<RefreshTokenDocument>.IndexKeys.Ascending(t => t.ExpiresOn),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }),
        }, cancellationToken);

        // Password reset tokens: same TTL cleanup pattern as refresh tokens.
        await context.PasswordResetTokens.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<PasswordResetTokenDocument>(Builders<PasswordResetTokenDocument>.IndexKeys.Ascending(t => t.TokenHash)),
            new CreateIndexModel<PasswordResetTokenDocument>(
                Builders<PasswordResetTokenDocument>.IndexKeys.Ascending(t => t.ExpiresOn),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }),
        }, cancellationToken);

        // Email verification tokens: same pattern again.
        await context.EmailVerificationTokens.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<EmailVerificationTokenDocument>(Builders<EmailVerificationTokenDocument>.IndexKeys.Ascending(t => t.TokenHash)),
            new CreateIndexModel<EmailVerificationTokenDocument>(
                Builders<EmailVerificationTokenDocument>.IndexKeys.Ascending(t => t.ExpiresOn),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }),
        }, cancellationToken);

        // Activities: the Summary tab's feed queries "most recent N for this team".
        await context.Activities.Indexes.CreateOneAsync(
            new CreateIndexModel<ActivityDocument>(
                Builders<ActivityDocument>.IndexKeys.Ascending(a => a.TeamId).Descending(a => a.CreatedOn)),
            cancellationToken: cancellationToken);

        // Sprints: listed by team, and "find the currently active one" is a
        // very common lookup (StartSprintCommandHandler, CompleteSprint's
        // unsprint-unfinished-work step).
        await context.Sprints.Indexes.CreateOneAsync(
            new CreateIndexModel<SprintDocument>(
                Builders<SprintDocument>.IndexKeys.Ascending(s => s.TeamId).Ascending(s => s.Status)),
            cancellationToken: cancellationToken);

        // Stories filtered by sprint (Board/Backlog "this sprint only" view).
        await context.UserStories.Indexes.CreateOneAsync(
            new CreateIndexModel<UserStoryDocument>(Builders<UserStoryDocument>.IndexKeys.Ascending(s => s.SprintId)),
            cancellationToken: cancellationToken);

        // Personal tasks: "my tasks" and "My Work" both look these up by owner.
        await context.PersonalTasks.Indexes.CreateOneAsync(
            new CreateIndexModel<PersonalTaskDocument>(Builders<PersonalTaskDocument>.IndexKeys.Ascending(t => t.OwnerUserId)),
            cancellationToken: cancellationToken);
    }
}
