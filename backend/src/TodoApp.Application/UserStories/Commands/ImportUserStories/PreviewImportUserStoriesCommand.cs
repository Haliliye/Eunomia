using MediatR;

namespace TodoApp.Application.UserStories.Commands.ImportUserStories;

public record PreviewImportUserStoriesCommand(string TeamId, string RequestingUserId, string CsvContent, CsvColumnMapping Mapping) : IRequest<IReadOnlyList<ImportRowDto>>;
