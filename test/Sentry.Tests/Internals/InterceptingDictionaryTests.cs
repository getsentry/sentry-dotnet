namespace Sentry.Tests.Internals;

using NSubstitute;
using System.Collections.Generic;
using Xunit;

public class InterceptingDictionaryTests
{
    [Fact]
    public void Add_Item_ShouldInvokeBeforeAndAfterSet()
    {
        // Arrange
        var innerDictionary = Substitute.For<IDictionary<string, string>>();
        var afterSetInvoked = false;

        var dictionary = new InterceptingDictionary<string, string>(
            innerDictionary,
            (key, value) => true,
            null,
            (key, value) => afterSetInvoked = true,
            null);

        // Act
        dictionary.Add("key", "value");

        // Assert
        innerDictionary.Received(1).Add("key", "value");
        Assert.True(afterSetInvoked);
    }

    [Fact]
    public void Add_Item_BeforeSetReturnsFalse_ShouldNotAdd()
    {
        // Arrange
        var innerDictionary = Substitute.For<IDictionary<string, string>>();

        var dictionary = new InterceptingDictionary<string, string>(
            innerDictionary,
            beforeSet: (key, value) => false
            );

        // Act
        dictionary.Add("key", "value");

        // Assert
        innerDictionary.DidNotReceive().Add("key", "value");
    }

    [Fact]
    public void Remove_Item_ShouldInvokeBeforeAndAfterRemove()
    {
        // Arrange
        var innerDictionary = Substitute.For<IDictionary<string, string>>();
        var afterRemoveInvoked = false;

        var dictionary = new InterceptingDictionary<string, string>(
            innerDictionary,
            null,
            key => true,
            null,
            key => afterRemoveInvoked = true);

        innerDictionary.Remove("key").Returns(true);

        // Act
        var result = dictionary.Remove("key");

        // Assert
        Assert.True(result);
        Assert.True(afterRemoveInvoked);
        innerDictionary.Received(1).Remove("key");
    }

    [Fact]
    public void Remove_Item_BeforeRemoveReturnsFalse_ShouldNotRemove()
    {
        // Arrange
        var innerDictionary = Substitute.For<IDictionary<string, string>>();

        var dictionary = new InterceptingDictionary<string, string>(
            innerDictionary,
            null,
            key => false, // BeforeRemove returns false
            null,
            null);

        // Act
        var result = dictionary.Remove("key");

        // Assert
        Assert.False(result);
        innerDictionary.DidNotReceive().Remove("key");
    }

    [Fact]
    public void Indexer_SetValue_ShouldInvokeBeforeAndAfterSet()
    {
        // Arrange
        var innerDictionary = Substitute.For<IDictionary<string, string>>();
        var afterSetInvoked = false;

        var dictionary = new InterceptingDictionary<string, string>(
            innerDictionary,
            (key, value) => true,
            null,
            (key, value) => afterSetInvoked = true,
            null);

        // Act
        dictionary["key"] = "value";

        // Assert
        innerDictionary.Received()[Arg.Is("key")] = "value";
        Assert.True(afterSetInvoked);
    }

    [Fact]
    public void Indexer_SetValue_BeforeSetReturnsFalse_ShouldNotSet()
    {
        // Arrange
        var innerDictionary = Substitute.For<IDictionary<string, string>>();

        var dictionary = new InterceptingDictionary<string, string>(
            innerDictionary,
            (key, value) => false, // BeforeSet returns false
            null,
            null,
            null);

        // Act
        dictionary["key"] = "value";

        // Assert
        innerDictionary.DidNotReceive()[Arg.Any<string>()] = Arg.Any<string>();
    }
}
