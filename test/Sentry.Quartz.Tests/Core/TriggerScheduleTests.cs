namespace Sentry.Quartz.Tests;

public class TriggerScheduleTests
{
    [Theory]
    [InlineData("0 0 12 * * ?", "0 12 * * *")]
    [InlineData("0 15 10 ? * *", "15 10 * * *")]
    [InlineData("30 0/5 * * * ?", "0-59/5 * * * *")]
    [InlineData("0 */15 * * * ?", "*/15 * * * *")]
    [InlineData("0 10-40/10 * * * ?", "10-40/10 * * * *")]
    [InlineData("0 0 9-17 ? * MON-FRI", "0 9-17 * * 1-5")]
    [InlineData("0 0 8 ? * 2,4,6", "0 8 * * 1,3,5")]
    [InlineData("0 0 0 ? * sun", "0 0 * * 0")]
    [InlineData("0 0 0 ? * 7", "0 0 * * 6")]
    [InlineData("0 0 0 ? * 2/2", "0 0 * * 1-6/2")]
    [InlineData("0 0 0 ? * */2", "0 0 * * */2")]
    [InlineData("0 0 0 ? * 1-7/3", "0 0 * * 0-6/3")]
    [InlineData("0 0 6 1,15 JAN-MAR ?", "0 6 1,15 1-3 *")]
    [InlineData("0 0 0 1/10 * ?", "0 0 1-31/10 * *")]
    [InlineData("0 0 0 * * ? *", "0 0 * * *")]
    [InlineData("00  05\t3 ? * 01", "5 3 * * 0")]
    public void ToCrontab_SupportedExpression_ReturnsEquivalentCrontab(string cronExpression, string expected)
    {
        var crontab = TriggerSchedule.ToCrontab(cronExpression);

        crontab.Should().Be(expected);
        var interval = () => new SentryMonitorOptions().Interval(crontab!);
        interval.Should().NotThrow();
    }

    [Theory]
    [InlineData("0 0 0 * * ? 2030")]
    [InlineData("0/30 * * * * ?")]
    [InlineData("* * * * * ?")]
    [InlineData("0 0 12 L * ?")]
    [InlineData("0 0 12 LW * ?")]
    [InlineData("0 0 12 15W * ?")]
    [InlineData("0 0 12 ? * 6L")]
    [InlineData("0 0 12 ? * 6#3")]
    [InlineData("0 0 12 ? * MON/2")]
    [InlineData("0 0 12 ? * MON-FRI/2")]
    [InlineData("0 0 12 1 JAN/2 ?")]
    [InlineData("0 0 22-2 * * ?")]
    [InlineData("0 0 5-5 * * ?")]
    [InlineData("0 0 12 ? * 7/2")]
    [InlineData("0 0 12 ? * ?")]
    [InlineData("0 60 12 * * ?")]
    [InlineData("0 0 12 ? * 0")]
    [InlineData("0 0 12 * *")]
    [InlineData("")]
    [InlineData(null)]
    public void ToCrontab_ExpressionSentryCantRepresent_ReturnsNull(string? cronExpression)
    {
        TriggerSchedule.ToCrontab(cronExpression).Should().BeNull();
    }

    [Theory]
    [InlineData(1, 1, SentryMonitorInterval.Minute)]
    [InlineData(90, 90, SentryMonitorInterval.Minute)]
    [InlineData(120, 2, SentryMonitorInterval.Hour)]
    [InlineData(36 * 60, 36, SentryMonitorInterval.Hour)]
    [InlineData(48 * 60, 2, SentryMonitorInterval.Day)]
    public void ToInterval_RepeatsForeverInWholeMinutes_ReturnsInterval(
        int minutes,
        int expectedInterval,
        SentryMonitorInterval expectedUnit)
    {
        var trigger = (ISimpleTrigger)TriggerBuilder.Create()
            .WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromMinutes(minutes)).RepeatForever())
            .Build();

        TriggerSchedule.ToInterval(trigger).Should().Be((expectedInterval, expectedUnit));
    }

    [Fact]
    public void ToInterval_IntervalNotWholeMinutes_ReturnsNull()
    {
        var trigger = (ISimpleTrigger)TriggerBuilder.Create()
            .WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromSeconds(90)).RepeatForever())
            .Build();

        TriggerSchedule.ToInterval(trigger).Should().BeNull();
    }

    [Fact]
    public void ToInterval_RepeatCountLimited_ReturnsNull()
    {
        var trigger = (ISimpleTrigger)TriggerBuilder.Create()
            .WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromHours(1)).WithRepeatCount(5))
            .Build();

        TriggerSchedule.ToInterval(trigger).Should().BeNull();
    }

    [Fact]
    public void ToIanaTimeZoneId_Utc_ReturnsUtc()
    {
        TriggerSchedule.ToIanaTimeZoneId(TimeZoneInfo.Utc).Should().Be("UTC");
    }

    [Fact]
    public void ToIanaTimeZoneId_CustomTimeZone_ReturnsNull()
    {
        var timeZone = TimeZoneInfo.CreateCustomTimeZone("Custom", TimeSpan.FromHours(3), "Custom", "Custom");

        TriggerSchedule.ToIanaTimeZoneId(timeZone).Should().BeNull();
    }

#if NET6_0_OR_GREATER
    [SkippableTheory]
    [InlineData("Europe/Berlin", "Europe/Berlin")]
    [InlineData("W. Europe Standard Time", "Europe/Berlin")]
    public void ToIanaTimeZoneId_SystemTimeZone_ReturnsIanaId(string timeZoneId, string expected)
    {
        Skip.If(!TestEnvironment.HasTimeZone(timeZoneId), "No time zone database");

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        TriggerSchedule.ToIanaTimeZoneId(timeZone).Should().Be(expected);
    }
#endif

    [Fact]
    public void ToMonitorConfig_CronTrigger_ReturnsCrontabAndTimeZone()
    {
        var trigger = TriggerBuilder.Create()
            .WithCronSchedule("0 30 6 ? * MON-FRI", schedule => schedule.InTimeZone(TimeZoneInfo.Utc))
            .Build();

        var monitorConfig = MonitorConfigJson.Render(TriggerSchedule.ToMonitorConfig(trigger)!);

        monitorConfig.GetProperty("schedule").GetProperty("type").GetString().Should().Be("crontab");
        monitorConfig.GetProperty("schedule").GetProperty("value").GetString().Should().Be("30 6 * * 1-5");
        monitorConfig.GetProperty("timezone").GetString().Should().Be("UTC");
    }

    [Fact]
    public void ToMonitorConfig_SimpleTrigger_ReturnsInterval()
    {
        var trigger = TriggerBuilder.Create()
            .WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromHours(2)).RepeatForever())
            .Build();

        var monitorConfig = MonitorConfigJson.Render(TriggerSchedule.ToMonitorConfig(trigger)!);

        monitorConfig.GetProperty("schedule").GetProperty("type").GetString().Should().Be("interval");
        monitorConfig.GetProperty("schedule").GetProperty("value").GetInt32().Should().Be(2);
        monitorConfig.GetProperty("schedule").GetProperty("unit").GetString().Should().Be("hour");
    }

    [Fact]
    public void ToMonitorConfig_TriggerWithCalendar_ReturnsNull()
    {
        var trigger = TriggerBuilder.Create()
            .WithCronSchedule("0 0 12 * * ?", schedule => schedule.InTimeZone(TimeZoneInfo.Utc))
            .WithCalendar("holidays")
            .Build();

        TriggerSchedule.ToMonitorConfig(trigger).Should().BeNull();
    }

    [Fact]
    public void ToMonitorConfig_CalendarIntervalTrigger_ReturnsNull()
    {
        var trigger = TriggerBuilder.Create()
            .WithCalendarIntervalSchedule(schedule => schedule.WithInterval(1, IntervalUnit.Day))
            .Build();

        TriggerSchedule.ToMonitorConfig(trigger).Should().BeNull();
    }
}
