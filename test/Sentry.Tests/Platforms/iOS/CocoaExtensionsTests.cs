using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Foundation;
using Sentry.Cocoa.Extensions;
using Xunit;

namespace Sentry.Tests.Platforms.iOS;

public class CocoaExtensionsTests
{
    [Fact]
    public void CopyToCocoaSentryEvent_CopiesProperties()
    {
        var evt = new SentryEvent(new Exception("Test Exception"));

        evt.Level = SentryLevel.Debug;
        evt.ServerName = "test server name";
        evt.Distribution = "test distribution";
        evt.Logger = "test logger";
        evt.Release = "test release";
        evt.Environment = "test environment";
        evt.TransactionName = "test transaction name";
        evt.Message = new SentryMessage { Params = ["Test"] };
        evt.SetTag("TestTagKey", "TestTagValue");
        evt.AddBreadcrumb(new Breadcrumb("test breadcrumb", "test type"));
        evt.SetExtra("TestExtraKey", "TestExtraValue");
        evt.User = new SentryUser
        {
            Id = "user id",
            Username = "test",
            Email = "test@sentry.io",
            IpAddress = "127.0.0.1"
        };

        var native = new CocoaSdk.SentryEvent();
        evt.CopyToCocoaSentryEvent(native);

        AssertEqual(evt, native);

        // message - native does not copy this over to dotnet
        native.Message.Should().NotBeNull("Message should not be null");
        native.Message.Params.Should().NotBeNull("Message params should not be null");
        native.Message.Params.First().Should().Be(evt.Message!.Params!.First().ToString());
    }


    [Fact]
    public void ToSentryEvent_ConvertToManaged()
    {
        var native = new CocoaSdk.SentryEvent();

        native.Timestamp = DateTimeOffset.UtcNow.ToNSDate();
        native.Level = Sentry.CocoaSdk.SentryLevel.Debug;
        native.ServerName = "native server name";
        native.Dist = "native dist";
        native.Logger = "native logger";
        native.ReleaseName = "native release";
        native.Environment = "native env";
        native.Transaction = "native transaction";
        native.Message = new SentryMessage { Params = ["Test"] }.ToCocoaSentryMessage();
        native.Tags = new Dictionary<string, string> { { "TestTagKey", "TestTagValue" } }.ToNSDictionaryStrings();
        native.Extra = new Dictionary<string, string> { { "TestExtraKey", "TestExtraValue" } }.ToNSDictionary();
        native.Error = new NSError(new NSString("Test Error"), IntPtr.Zero);
        native.Breadcrumbs =
        [
            new CocoaSdk.SentryBreadcrumb(CocoaSdk.SentryLevel.Debug, "category")
        ];
        native.User = new CocoaSdk.SentryUser
        {
            UserId = "user id",
            Username = "test",
            Email = "test@sentry.io",
            IpAddress = "127.0.0.1"
        };
        var managed = native.ToSentryEvent();
        AssertEqual(managed, native);
    }

    private static void AssertEqual(SentryEvent managed, CocoaSdk.SentryEvent native)
    {
        native.ServerName.Should().Be(managed.ServerName, "Server Name");
        native.Dist.Should().Be(managed.Distribution, "Distribution");
        native.Logger.Should().Be(managed.Logger, "Logger");
        native.ReleaseName.Should().Be(managed.Release, "Release");
        native.Environment.Should().Be(managed.Environment, "Environment");
        native.Transaction.Should().Be(managed.TransactionName!, "Transaction");
        native.Level!.ToString().Should().Be(managed.Level.ToString(), "Level");

        native.Extra.Should().NotBeNull("No extras found");
        native.Extra.Count.Should().Be(1, "Extras should have 1 item");
        native.Extra!.Keys![0]!.Should().Be(new NSString(managed.Extra.Keys.First()), "Extras key should match");
        native.Extra!.Values![0]!.Should().Be(NSObject.FromObject(managed.Extra.Values.First()), "Extra value should match");

        // tags
        native.Tags.Should().NotBeNull("No tags found");
        native.Tags.Count.Should().Be(1, "Tags should have 1 item");
        native.Tags!.Keys![0]!.Should().Be(new NSString(managed.Tags.Keys.First()));
        native.Tags!.Values![0]!.Should().Be(new NSString(managed.Tags.Values.First()));

        // breadcrumbs
        native.Breadcrumbs.Should().NotBeNull("No breadcrumbs found");
        var nb = native.Breadcrumbs!.First();
        var mb = managed.Breadcrumbs!.First();
        nb.Message.Should().Be(mb.Message, "Breadcrumb message");
        nb.Type.Should().Be(mb.Type, "Breadcrumb type");

        // user
        native.User!.UserId.Should().Be(managed.User.Id, "UserId should match");
        native.User.Email.Should().Be(managed.User.Email, "Email should match");
        native.User.Username.Should().Be(managed.User.Username, "Username should match");
        native.User.IpAddress.Should().Be(managed.User.IpAddress, "IpAddress should match");

        // check contains because ios/android dotnet tend to move how this works from time to time
        managed.Exception!.ToString().Contains(native.Error!.Domain!).Should().BeTrue("Domain message should be included in dotnet exception");
    }
}
