using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Sentry.Maui.Tests;

public class BreadcrumbEventTests
{
    [Fact]
    public void BreadcrumbEvent_OldConstructor_EquivalentToNewConstructor()
    {
        // Arrange
        var sender = new object();
        var eventName = "TestEvent";

        // Act
        IEnumerable<(string Key, string Value)> extraData = [("key1", "value1"), ("key2", "value2")];
        var oldEvent = new BreadcrumbEvent(sender, eventName, extraData);
        var newEvent = new BreadcrumbEvent(sender, eventName, ("key1", "value1"), ("key2", "value2"));

        // Assert
        oldEvent.Sender.Should().Be(newEvent.Sender);
        oldEvent.EventName.Should().Be(newEvent.EventName);
        oldEvent.ExtraData.Should().BeEquivalentTo(newEvent.ExtraData);
    }
}
