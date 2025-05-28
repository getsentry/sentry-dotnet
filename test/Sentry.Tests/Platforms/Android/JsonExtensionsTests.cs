using System;
using System.Linq;
using FluentAssertions;
using Sentry.Android.Extensions;
using Xunit;

namespace Sentry.Tests.Platforms.Android;

public class JsonExtensionsTests

{
    [Fact]
    public void ToJavaSentryEvent_Success()

    {
        var evt = new SentryEvent(new Exception("Test Exception"));

        evt.Level = SentryLevel.Debug;
        evt.ServerName = "test server name";
        evt.Distribution = "test distribution";
        evt.Logger = "test logger";
        evt.Release = "test release";
        evt.Environment = "test environment";
        evt.TransactionName = "test transaction name";
        evt.Message = new SentryMessage
        {
            Params = ["Test"]
        };

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

        var native = evt.ToJavaSentryEvent(new SentryOptions(), new JavaSdk.SentryOptions());

        AssertEqual(evt, native);
    }


    [Fact]
    public void ToSentryEvent_ConvertToManaged()
    {
        var native = new JavaSdk.SentryEvent();

        native.Throwable = new Exception("Test Exception").ToThrowable();
        native.Timestamp = DateTimeOffset.UtcNow.ToJavaDate();
        native.Level = JavaSdk.SentryLevel.Debug;
        native.ServerName = "native server name";
        native.Dist = "native dist";
        native.Logger = "native logger";
        native.Release = "native release";
        native.Environment = "native env";
        native.Transaction = "native transaction";
        native.Message = new JavaSdk.Protocol.Message
        {
            Params = ["Test"]
        };
        native.SetTag("TestTagKey", "TestTagValue");
        native.SetExtra("TestExtraKey", "TestExtraValue");
        native.Breadcrumbs =
        [
            new JavaSdk.Breadcrumb
            {
                Category = "category",
                Level = JavaSdk.SentryLevel.Debug
            }
        ];

        native.User = new JavaSdk.Protocol.User
        {
            Id = "user id",
            Username = "test",
            Email = "test@sentry.io",
            IpAddress = "127.0.0.1"
        };

        var managed = native.ToSentryEvent(new JavaSdk.SentryOptions());

        AssertEqual(managed, native);
    }


    private static void AssertEqual(SentryEvent managed, JavaSdk.SentryEvent native)
    {
        native.ServerName.Should().Be(managed.ServerName, "Server Name");
        native.Dist.Should().Be(managed.Distribution, "Distribution");
        native.Logger.Should().Be(managed.Logger, "Logger");
        native.Release.Should().Be(managed.Release, "Release");
        native.Environment.Should().Be(managed.Environment, "Environment");
        native.Transaction.Should().Be(managed.TransactionName!, "Transaction");
        native.Level!.ToString().ToUpper().Should().Be(managed.Level!.ToString()!.ToUpper(), "Level");
        // native.Throwable.Message.Should().Be(managed.Exception!.Message, "Message should match");

        // extras
        native.Extras.Should().NotBeNull("No extras found");
        native.Extras!.Count.Should().Be(1, "Extras should have 1 item");
        native.Extras!.Keys!.First().Should().Be(managed.Extra.Keys.First(), "Extras key should match");
        native.Extras!.Values!.First().ToString().Should().Be(managed.Extra.Values.First().ToString(), "Extra value should match");

        // tags
        native.Tags.Should().NotBeNull("No tags found");
        native.Tags!.Count.Should().Be(1, "Tags should have 1 item");
        native.Tags!.Keys!.First().Should().Be(managed.Tags.Keys.First());
        native.Tags!.Values!.First().Should().Be(managed.Tags.Values.First());

        // breadcrumbs
        native.Breadcrumbs.Should().NotBeNull("No breadcrumbs found");
        var nb = native.Breadcrumbs!.First();
        var mb = managed.Breadcrumbs!.First();
        nb.Message.Should().Be(mb.Message, "Breadcrumb message");
        nb.Type.Should().Be(mb.Type, "Breadcrumb type");

        // user
        native.User!.Id.Should().Be(managed.User.Id, "UserId should match");
        native.User.Email.Should().Be(managed.User.Email, "Email should match");
        native.User.Username.Should().Be(managed.User.Username, "Username should match");
        native.User.IpAddress.Should().Be(managed.User.IpAddress, "IpAddress should match");
    }
}
