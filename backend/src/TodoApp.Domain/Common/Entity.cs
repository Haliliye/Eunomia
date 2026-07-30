namespace TodoApp.Domain.Common;

/// <summary>
/// Base class for entities that are identified by an Id rather than by their attributes.
/// </summary>
public abstract class Entity
{
    public string Id { get; protected set; } = string.Empty;

    protected Entity() { }

    protected Entity(string id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return Id == other.Id;
    }

    public override int GetHashCode() => (GetType().ToString() + Id).GetHashCode();
}
