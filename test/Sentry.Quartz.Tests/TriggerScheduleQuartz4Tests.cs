namespace Sentry.Quartz.Tests;

// Quartz 4 reads '*' and '?' alike in the day fields, and runs on either day when both restrict
public class TriggerScheduleQuartz4Tests
{
    [Theory]
    [InlineData("0 0 12 * * *", "0 12 * * *")]
    [InlineData("0 0 12 * * MON", "0 12 * * 1")]
    [InlineData("0 0 12 1 * *", "0 12 1 * *")]
    [InlineData("0 0 12 1 * MON", "0 12 1 * 1")]
    [InlineData("0 0 12 1,15 * MON-FRI", "0 12 1,15 * 1-5")]
    [InlineData("0 0 12 */2 * MON", "0 12 1-31/2 * 1")]
    [InlineData("0 0 12 1 * */2", "0 12 1 * 0-6/2")]
    [InlineData("0 0 12 */10,5 * */3", "0 12 1-31/10,5 * 0-6/3")]
    [InlineData("0 0 12 */2 * ?", "0 12 */2 * *")]
    [InlineData("0 0 12 */2 * *", "0 12 */2 * *")]
    [InlineData("0 0 12 5,* * MON", "0 12 * * 1")]
    [InlineData("0 0 12 ? * MON", "0 12 * * 1")]
    [InlineData("0 0 12 1 * ?", "0 12 1 * *")]
    public void ToCrontab_Quartz4DayFields_ReturnsEquivalentCrontab(string cronExpression, string expected)
    {
        var crontab = TriggerSchedule.ToCrontab(cronExpression);

        crontab.Should().Be(expected);
        var interval = () => new SentryMonitorOptions().Interval(crontab!);
        interval.Should().NotThrow();
    }

    [Theory]
    [InlineData("0 0 12 ? * ?")]
    [InlineData("0 0 12 5,? * MON")]
    [InlineData("0 0 12 *,L * MON")]
    public void ToCrontab_DayFieldsSentryCantRepresent_ReturnsNull(string cronExpression)
    {
        TriggerSchedule.ToCrontab(cronExpression).Should().BeNull();
    }
}
