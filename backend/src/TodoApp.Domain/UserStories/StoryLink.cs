namespace TodoApp.Domain.UserStories;

/// <summary>
/// A relationship to another story (US: classic Jira "Linked Issues").
/// Always created/removed as a symmetric pair — when A "Blocks" B, B
/// automatically gets a "BlockedBy" A link, so either side can be queried
/// without a reverse lookup (see UserStory.AddLink).
/// </summary>
public class StoryLink
{
    public string LinkedStoryId { get; private set; } = string.Empty;
    public StoryLinkType LinkType { get; private set; }

    private StoryLink() { }

    public StoryLink(string linkedStoryId, StoryLinkType linkType)
    {
        LinkedStoryId = linkedStoryId;
        LinkType = linkType;
    }
}
