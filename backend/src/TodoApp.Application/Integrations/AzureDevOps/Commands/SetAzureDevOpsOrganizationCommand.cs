using MediatR;

namespace TodoApp.Application.Integrations.AzureDevOps.Commands;

/// <summary>A Microsoft account can belong to several Azure DevOps organizations — this is the follow-up step after connecting where the person picks which one Eunomia should use.</summary>
public record SetAzureDevOpsOrganizationCommand(string RequestingUserId, string OrganizationName) : IRequest;
