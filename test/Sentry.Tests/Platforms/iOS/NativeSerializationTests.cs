using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using Sentry.Cocoa.Extensions;
using Xunit;

namespace Sentry.Tests.Platforms.iOS;

public class NativeSerializationTests
{
    [Fact]
    public void Managed_To_Native()
    {
        var evt = new SentryEvent(new Exception("Test Exception"));

        evt.Level = SentryLevel.Debug;
        evt.ServerName = "test server name";
        evt.Distribution = "test distribution";
        evt.Logger = "test logger";
        evt.Release = "test release";
        evt.Environment = "test environment";
        evt.Platform = "test platform";
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
    }


    [Fact]
    public void Native_To_Managed()
    {
        var native = new CocoaSdk.SentryEvent();

        native.ServerName = "native server name";
        native.Dist = "native dist";
        native.Logger = "native logger";
        native.ReleaseName = "native release";
        native.Environment = "native env";
        native.Platform = "native platform";
        native.Transaction = "native transaction";
        native.Message = new SentryMessage { Params = ["Test"] }.ToCocoaSentryMessage();
        native.Tags = new Dictionary<string, string> { { "TestTagKey", "TestTagValue" } }.ToNSDictionaryStrings();
        native.Extra = new Dictionary<string, string> { { "TestExtraKey", "TestExtraValue" } }.ToNSDictionary();
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
        native.ServerName.Should().Be(managed.ServerName);
        native.Dist.Should().Be(managed.Distribution);
        native.Logger.Should().Be(managed.Logger);
        native.ReleaseName.Should().Be(managed.Release);
        native.Environment.Should().Be(managed.Environment);
        native.Platform.Should().Be(managed.Platform!);
        native.Transaction.Should().Be(managed.TransactionName!);

        native.Message.Params.First().Should().Be(managed.Message.Params.First().ToString());

        native.Extra.Keys[0].Should().Be(new NSString(managed.Extra.Keys.First()));
        native.Extra.Values[0].Should().Be(NSObject.FromObject(managed.Extra.Values.First()));

        native.Tags.Keys[0].Should().Be(new NSString(managed.Tags.Keys.First()));
        native.Tags.Values[0].Should().Be(new NSString(managed.Tags.Values.First()));

        var nb = native.Breadcrumbs.First();
        var mb = managed.Breadcrumbs.First();
        nb.Message.Should().Be(mb.Message);
        nb.Type.Should().Be(mb.Type);

        native.User.UserId.Should().Be(managed.User.Id);
        native.User.Email.Should().Be(managed.User.Email);
        native.User.Username.Should().Be(managed.User.Username);
        native.User.IpAddress.Should().Be(managed.User.IpAddress);

        native.Level.ToString().Equals(managed.Level.ToString());

        native.Error.Domain.Should().Be(managed.Exception.ToString());
    }
}
