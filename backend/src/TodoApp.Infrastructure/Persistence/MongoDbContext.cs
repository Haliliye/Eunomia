using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence;

/// <summary>
/// Central place that exposes typed MongoDB collections. Each aggregate
/// root gets its own collection backed by a plain persistence document
/// (see Persistence/Documents) rather than the domain type directly —
/// this keeps MongoDB.Driver's serialization/LINQ conventions happy
/// without leaking persistence concerns into the Domain project.
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<TeamDocument> Teams => _database.GetCollection<TeamDocument>("teams");
    public IMongoCollection<UserStoryDocument> UserStories => _database.GetCollection<UserStoryDocument>("userStories");
    public IMongoCollection<CommentDocument> Comments => _database.GetCollection<CommentDocument>("comments");
    public IMongoCollection<NotificationDocument> Notifications => _database.GetCollection<NotificationDocument>("notifications");
    public IMongoCollection<UserDocument> Users => _database.GetCollection<UserDocument>("users");
    public IMongoCollection<InvitationDocument> Invitations => _database.GetCollection<InvitationDocument>("invitations");
    public IMongoCollection<RefreshTokenDocument> RefreshTokens => _database.GetCollection<RefreshTokenDocument>("refreshTokens");
    public IMongoCollection<PasswordResetTokenDocument> PasswordResetTokens => _database.GetCollection<PasswordResetTokenDocument>("passwordResetTokens");
    public IMongoCollection<EmailVerificationTokenDocument> EmailVerificationTokens => _database.GetCollection<EmailVerificationTokenDocument>("emailVerificationTokens");
    public IMongoCollection<ActivityDocument> Activities => _database.GetCollection<ActivityDocument>("activities");
    public IMongoCollection<SprintDocument> Sprints => _database.GetCollection<SprintDocument>("sprints");
    public IMongoCollection<PersonalTaskDocument> PersonalTasks => _database.GetCollection<PersonalTaskDocument>("personalTasks");
    public IMongoCollection<JiraConnectionDocument> JiraConnections => _database.GetCollection<JiraConnectionDocument>("jiraConnections");
    public IMongoCollection<GitHubConnectionDocument> GitHubConnections => _database.GetCollection<GitHubConnectionDocument>("gitHubConnections");
    public IMongoCollection<GitLabConnectionDocument> GitLabConnections => _database.GetCollection<GitLabConnectionDocument>("gitLabConnections");
    public IMongoCollection<EmailSignupInvitationDocument> EmailSignupInvitations => _database.GetCollection<EmailSignupInvitationDocument>("emailSignupInvitations");
    public IMongoCollection<JiraProjectSyncDocument> JiraProjectSyncs => _database.GetCollection<JiraProjectSyncDocument>("jiraProjectSyncs");
    public IMongoCollection<BoardDocument> Boards => _database.GetCollection<BoardDocument>("boards");
    public IMongoCollection<AzureDevOpsConnectionDocument> AzureDevOpsConnections => _database.GetCollection<AzureDevOpsConnectionDocument>("azureDevOpsConnections");
    public IMongoCollection<AzureDevOpsProjectSyncDocument> AzureDevOpsProjectSyncs => _database.GetCollection<AzureDevOpsProjectSyncDocument>("azureDevOpsProjectSyncs");
}
