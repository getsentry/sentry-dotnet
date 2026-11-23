#if __IOS__
namespace Sentry.Tests.Platforms.iOS;

public class SentrySdkTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ApplyDataCollection_NotConfigured_ForwardsSendDefaultPii(bool sendDefaultPii)
    {
        var options = new SentryOptions { SendDefaultPii = sendDefaultPii };
        var nativeOptions = new CocoaSdk.SentryObjCOptions();

        SentrySdk.ApplyDataCollection(options, nativeOptions);

        nativeOptions.SendDefaultPii.Should().Be(sendDefaultPii);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void ApplyDataCollection_Configured_ForwardsUserInfoAsSendDefaultPii(bool userInfo, bool sendDefaultPii)
    {
        var options = new SentryOptions { SendDefaultPii = sendDefaultPii };
        options.ConfigureDataCollection(dataCollection => dataCollection.UserInfo = userInfo);
        var nativeOptions = new CocoaSdk.SentryObjCOptions();

        SentrySdk.ApplyDataCollection(options, nativeOptions);

        nativeOptions.SendDefaultPii.Should().Be(userInfo);
    }

    [Fact]
    public void ApplyDataCollection_Configured_LeavesSessionReplayUntouched()
    {
        var options = new SentryOptions();
        options.ConfigureDataCollection(dataCollection =>
        {
            dataCollection.UserInfo = false;
            dataCollection.Cookies = KeyValueCollectionBehavior.Off;
            dataCollection.UrlQueryParams = KeyValueCollectionBehavior.Off;
            dataCollection.HttpHeaders.Request = KeyValueCollectionBehavior.Off;
            dataCollection.HttpHeaders.Response = KeyValueCollectionBehavior.Off;
            dataCollection.HttpBodies = HttpBodyTypes.None;
            dataCollection.GraphQL.Document = false;
            dataCollection.GraphQL.Variables = false;
        });
        var nativeOptions = new CocoaSdk.SentryObjCOptions
        {
            SessionReplay = new CocoaSdk.SentryObjCReplayOptions
            {
                SessionSampleRate = 1,
                OnErrorSampleRate = 1,
                MaskAllText = false,
                MaskAllImages = false,
                NetworkCaptureBodies = true,
                NetworkRequestHeaders = ["Authorization"],
                NetworkResponseHeaders = ["Set-Cookie"]
            }
        };
        var expected = ReplaySnapshot(nativeOptions.SessionReplay);

        SentrySdk.ApplyDataCollection(options, nativeOptions);

        ReplaySnapshot(nativeOptions.SessionReplay).Should().BeEquivalentTo(expected);
    }

    private static object ReplaySnapshot(CocoaSdk.SentryObjCReplayOptions replay) => new
    {
        replay.SessionSampleRate,
        replay.OnErrorSampleRate,
        replay.MaskAllText,
        replay.MaskAllImages,
        replay.NetworkCaptureBodies,
        NetworkRequestHeaders = replay.NetworkRequestHeaders.ToList(),
        NetworkResponseHeaders = replay.NetworkResponseHeaders.ToList()
    };
}
#endif
