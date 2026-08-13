using System.Linq;
using TodoApp.Domain.UserStories;
using Xunit;

namespace TodoApp.UnitTests.UserStories;

/// <summary>
/// Pure domain coverage for UserStory's checklist/label/link/attachment
/// behavior — previously untested at the domain level at all (only hit
/// indirectly through handler tests), per the 2026-08-11 review's "pure
/// domain tests: none" finding. UserStory is the most complex aggregate in
/// the codebase, so this focuses on the parts with real invariants (order
/// assignment, size limits, self-link prevention) rather than simple
/// property setters.
/// </summary>
public class UserStoryTests
{
    private static UserStory CreateStory() =>
        UserStory.Create(Guid.NewGuid().ToString(), "team-1", "Some story", null);

    // --- Checklist ---

    [Fact]
    public void AddChecklistItem_AssignsIncrementingOrder()
    {
        var story = CreateStory();

        var first = story.AddChecklistItem(Guid.NewGuid().ToString(), "First");
        var second = story.AddChecklistItem(Guid.NewGuid().ToString(), "Second");

        Assert.Equal(0, first.Order);
        Assert.Equal(1, second.Order);
    }

    [Fact]
    public void AddChecklistItem_BlankText_ThrowsArgumentException()
    {
        var story = CreateStory();

        Assert.Throws<ArgumentException>(() => story.AddChecklistItem(Guid.NewGuid().ToString(), "   "));
    }

    [Fact]
    public void ToggleChecklistItem_FlipsIsCompleted()
    {
        var story = CreateStory();
        var item = story.AddChecklistItem(Guid.NewGuid().ToString(), "Do the thing");

        story.ToggleChecklistItem(item.Id);
        Assert.True(story.ChecklistItems.Single(i => i.Id == item.Id).IsCompleted);

        story.ToggleChecklistItem(item.Id);
        Assert.False(story.ChecklistItems.Single(i => i.Id == item.Id).IsCompleted);
    }

    [Fact]
    public void ToggleChecklistItem_UnknownId_ThrowsKeyNotFoundException()
    {
        var story = CreateStory();

        Assert.Throws<KeyNotFoundException>(() => story.ToggleChecklistItem("does-not-exist"));
    }

    [Fact]
    public void RemoveChecklistItem_RemovesIt()
    {
        var story = CreateStory();
        var item = story.AddChecklistItem(Guid.NewGuid().ToString(), "Remove me");

        story.RemoveChecklistItem(item.Id);

        Assert.Empty(story.ChecklistItems);
    }

    [Fact]
    public void ReorderChecklistItems_ReassignsOrderToMatchGivenSequence()
    {
        var story = CreateStory();
        var a = story.AddChecklistItem(Guid.NewGuid().ToString(), "A");
        var b = story.AddChecklistItem(Guid.NewGuid().ToString(), "B");
        var c = story.AddChecklistItem(Guid.NewGuid().ToString(), "C");

        story.ReorderChecklistItems(new[] { c.Id, a.Id, b.Id });

        Assert.Equal(new[] { c.Id, a.Id, b.Id }, story.ChecklistItems.Select(i => i.Id));
    }

    // --- Labels ---

    [Fact]
    public void AddLabel_Twice_IsIdempotent()
    {
        var story = CreateStory();

        story.AddLabel("label-1");
        story.AddLabel("label-1");

        Assert.Single(story.LabelIds);
    }

    [Fact]
    public void RemoveLabel_NotPresent_DoesNotThrow()
    {
        var story = CreateStory();

        var exception = Record.Exception(() => story.RemoveLabel("never-added"));

        Assert.Null(exception);
    }

    // --- Attachments ---

    [Fact]
    public void AddAttachment_OverSizeLimit_ThrowsArgumentException()
    {
        var story = CreateStory();

        Assert.Throws<ArgumentException>(() =>
            story.AddAttachment(Guid.NewGuid().ToString(), "huge.zip", "application/zip", UserStory.MaxAttachmentSizeBytes + 1, "storage-key", "user-1"));
    }

    [Fact]
    public void AddAttachment_AtExactSizeLimit_Succeeds()
    {
        var story = CreateStory();

        var attachment = story.AddAttachment(Guid.NewGuid().ToString(), "exact.zip", "application/zip", UserStory.MaxAttachmentSizeBytes, "storage-key", "user-1");

        Assert.Contains(attachment, story.Attachments);
    }

    [Fact]
    public void RemoveAttachment_UnknownId_ThrowsKeyNotFoundException()
    {
        var story = CreateStory();

        Assert.Throws<KeyNotFoundException>(() => story.RemoveAttachment("does-not-exist"));
    }

    // --- Links ---

    [Fact]
    public void AddLink_ToSelf_ThrowsArgumentException()
    {
        var story = CreateStory();

        Assert.Throws<ArgumentException>(() => story.AddLink(story.Id, StoryLinkType.Blocks));
    }

    [Fact]
    public void AddLink_CalledTwiceForSameTarget_ReplacesRatherThanDuplicates()
    {
        var story = CreateStory();
        var targetId = Guid.NewGuid().ToString();

        story.AddLink(targetId, StoryLinkType.RelatesTo);
        story.AddLink(targetId, StoryLinkType.Blocks);

        var link = Assert.Single(story.Links);
        Assert.Equal(StoryLinkType.Blocks, link.LinkType);
    }

    [Fact]
    public void RemoveLink_RemovesTheMatchingOne()
    {
        var story = CreateStory();
        var targetId = Guid.NewGuid().ToString();
        story.AddLink(targetId, StoryLinkType.RelatesTo);

        story.RemoveLink(targetId);

        Assert.Empty(story.Links);
    }

    // --- Time tracking ---

    [Fact]
    public void SetEstimate_Negative_ThrowsArgumentException()
    {
        var story = CreateStory();

        Assert.Throws<ArgumentException>(() => story.SetEstimate(-1));
    }

    [Fact]
    public void LogTime_ZeroOrNegativeHours_ThrowsArgumentException()
    {
        var story = CreateStory();

        Assert.Throws<ArgumentException>(() => story.LogTime(Guid.NewGuid().ToString(), 0, null, "user-1"));
        Assert.Throws<ArgumentException>(() => story.LogTime(Guid.NewGuid().ToString(), -2, null, "user-1"));
    }

    [Fact]
    public void LogTime_PositiveHours_AddsEntry()
    {
        var story = CreateStory();

        story.LogTime(Guid.NewGuid().ToString(), 2.5, "Investigated the bug", "user-1");

        var entry = Assert.Single(story.TimeLogEntries);
        Assert.Equal(2.5, entry.Hours);
    }

    // --- Basic creation ---

    [Fact]
    public void Create_BlankTitle_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => UserStory.Create(Guid.NewGuid().ToString(), "team-1", "   ", null));
    }

    [Fact]
    public void Create_DefaultsToToDoStatus()
    {
        var story = CreateStory();

        Assert.Equal("ToDo", story.Status);
    }
}
