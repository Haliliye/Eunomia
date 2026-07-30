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
using TodoApp.Infrastructure.Attachments;
using TodoApp.Infrastructure.Email;
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
        services.Configure<AttachmentStorageSettings>(configuration.GetSection(AttachmentStorageSettings.SectionName));
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

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IEmailSettingsProvider, EmailSettingsProvider>();
        services.AddSingleton<IAttachmentStorage, LocalDiskAttachmentStorage>();

        return services;
    }
}
