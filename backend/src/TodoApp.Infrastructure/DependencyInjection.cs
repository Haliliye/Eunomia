using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Application.Common;
using TodoApp.Domain.Activities;
using TodoApp.Domain.Auth;
using TodoApp.Domain.Comments;
using TodoApp.Domain.Invitations;
using TodoApp.Domain.Notifications;
using TodoApp.Domain.PersonalTasks;
using TodoApp.Domain.Sprints;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;
using TodoApp.Domain.Users;
using TodoApp.Domain.Integrations;
using TodoApp.Infrastructure.Attachments;
using TodoApp.Infrastructure.Email;
using TodoApp.Infrastructure.Integrations.Jira;
using TodoApp.Infrastructure.Integrations.AzureDevOps;
using TodoApp.Infrastructure.Integrations.GitHub;
using TodoApp.Infrastructure.Persistence;
using TodoApp.Infrastructure.Persistence.Repositories;
using TodoApp.Infrastructure.Security;

namespace TodoApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(configuration.GetSection(MongoDbSettings.SectionName));
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.Configure<BrevoApiSettings>(configuration.GetSection(BrevoApiSettings.SectionName));
        services.Configure<AttachmentStorageSettings>(configuration.GetSection(AttachmentStorageSettings.SectionName));
        services.Configure<R2StorageSettings>(configuration.GetSection(R2StorageSettings.SectionName));
        services.Configure<JiraSettings>(configuration.GetSection(JiraSettings.SectionName));
        services.Configure<GitHubSettings>(configuration.GetSection(GitHubSettings.SectionName));
        services.Configure<TokenEncryptionSettings>(configuration.GetSection(TokenEncryptionSettings.SectionName));
        services.AddSingleton<MongoDbContext>();

        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IUserStoryRepository, UserStoryRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<ISprintRepository, SprintRepository>();
        services.AddScoped<IPersonalTaskRepository, PersonalTaskRepository>();
        services.AddScoped<IJiraConnectionRepository, JiraConnectionRepository>();
        services.AddScoped<IGitHubConnectionRepository, GitHubConnectionRepository>();
        services.AddScoped<IAzureDevOpsConnectionRepository, AzureDevOpsConnectionRepository>();
        services.AddScoped<IAzureDevOpsProjectSyncRepository, AzureDevOpsProjectSyncRepository>();
        services.AddScoped<IEmailSignupInvitationRepository, EmailSignupInvitationRepository>();
        services.AddScoped<IJiraProjectSyncRepository, JiraProjectSyncRepository>();
        services.AddScoped<TodoApp.Domain.Boards.IBoardRepository, BoardRepository>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        // BrevoApi:ApiKey configured wins over SMTP — Render's free tier blocks
        // outbound traffic to SMTP ports (25/465/587) entirely, but ordinary
        // HTTPS to Brevo's REST API isn't affected. SMTP stays as the
        // zero-extra-setup path for local dev / any host that doesn't block it.
        var brevoSettings = configuration.GetSection(BrevoApiSettings.SectionName).Get<BrevoApiSettings>();
        if (brevoSettings?.IsConfigured == true)
        {
            services.AddHttpClient<IEmailSender, BrevoApiEmailSender>();
            services.AddScoped<IEmailSettingsProvider, BrevoEmailSettingsProvider>();
        }
        else
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
            services.AddScoped<IEmailSettingsProvider, EmailSettingsProvider>();
        }
        // R2Storage:Enabled=true swaps in the Cloudflare R2-backed implementation
        // (needed for any deployment whose filesystem doesn't persist between
        // restarts/redeploys — e.g. Render's free tier); local disk remains the
        // zero-config default for local development.
        var r2Settings = configuration.GetSection(R2StorageSettings.SectionName).Get<R2StorageSettings>();
        if (r2Settings?.Enabled == true)
        {
            services.AddSingleton<Amazon.S3.IAmazonS3>(_ => new Amazon.S3.AmazonS3Client(
                r2Settings.AccessKeyId,
                r2Settings.SecretAccessKey,
                new Amazon.S3.AmazonS3Config
                {
                    ServiceURL = r2Settings.ServiceUrl,
                    ForcePathStyle = true, // required by R2's S3-compatible API
                    AuthenticationRegion = "auto", // Cloudflare's documented value for R2 — without an explicit region, SigV4 signing can mismatch what R2 expects
                }));
            services.AddSingleton<IAttachmentStorage, R2AttachmentStorage>();
        }
        else
        {
            services.AddSingleton<IAttachmentStorage, LocalDiskAttachmentStorage>();
        }

        services.AddSingleton<ITokenCipher, AesTokenCipher>();

        // Only registered when a Jira OAuth app is actually configured — the
        // Jira endpoints simply aren't usable (clear DI error rather than a
        // silent no-op) until Jira:ClientId/ClientSecret are set, same as
        // Brevo above.
        var jiraSettings = configuration.GetSection(JiraSettings.SectionName).Get<JiraSettings>();
        if (jiraSettings?.IsConfigured == true)
        {
            services.AddHttpClient<IJiraClient, JiraApiClient>();
        }

        // Same gating reasoning as Jira above — only usable once GitHub:ClientId/ClientSecret are set.
        var gitHubSettings = configuration.GetSection(GitHubSettings.SectionName).Get<GitHubSettings>();
        if (gitHubSettings?.IsConfigured == true)
        {
            services.AddHttpClient<IGitHubClient, GitHubApiClient>();
        }

        // PAT-based (see AzureDevOpsConnection) — no client id/secret to gate
        // registration on, unlike Jira's OAuth client.
        services.AddHttpClient<IAzureDevOpsClient, AzureDevOpsApiClient>();

        return services;
    }
}
