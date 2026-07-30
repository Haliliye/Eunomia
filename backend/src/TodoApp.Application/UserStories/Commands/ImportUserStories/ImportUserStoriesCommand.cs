using MediatR;

namespace TodoApp.Application.UserStories.Commands.ImportUserStories;

public record ImportUserStoriesCommand(string TeamId, string RequestingUserId, string CsvContent, CsvColumnMapping Mapping) : IRequest<ImportSummaryDto>;
