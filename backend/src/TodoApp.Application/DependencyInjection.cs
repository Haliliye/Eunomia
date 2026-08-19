using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Application.Common.Behaviors;

namespace TodoApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<Integrations.Jira.JiraAccessTokenProvider>();
        services.AddScoped<Integrations.Jira.JiraProjectImportService>();
        services.AddScoped<Integrations.AzureDevOps.AzureDevOpsProjectImportService>();
        services.AddScoped<Integrations.GitHub.GitHubAccessTokenProvider>();
        services.AddScoped<Integrations.GitHub.GitHubProjectImportService>();

        return services;
    }
}
