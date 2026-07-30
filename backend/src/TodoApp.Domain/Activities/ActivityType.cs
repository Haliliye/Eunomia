namespace TodoApp.Domain.Activities;

/// <summary>US-133 AC: "filter by action type" needs a structured type — the
/// free-text Message alone couldn't be filtered on reliably.</summary>
public enum ActivityType
{
    Created,
    StatusChanged,
    Assigned,
    Archived,
    Commented
}
