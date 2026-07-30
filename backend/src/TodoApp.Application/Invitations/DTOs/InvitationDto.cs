namespace TodoApp.Application.Invitations.DTOs;

public record InvitationDto(
    string Id,
    string TeamId,
    string TeamName,
    string InvitedUserId,
    string InvitedByUserId,
    DateTime CreatedOn);
