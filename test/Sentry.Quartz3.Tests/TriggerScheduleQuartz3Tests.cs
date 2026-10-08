namespace Sentry.Quartz.Tests;

// Quartz 3 needs exactly one day field to be '?', so expressions only Quartz 4 accepts aren't converted
public class TriggerScheduleQuartz3Tests
{
    [Theory]
    [InlineData("0 0 12 * * *")]
    [InlineData("0 0 12 1 * MON")]
    [InlineData("0 0 12 */2 * MON")]
    [InlineData("0 0 12 * * MON")]
    [InlineData("0 0 12 1 * *")]
    [InlineData("0 0 12 ? * ?")]
    public void ToCrontab_Quartz4DayFields_ReturnsNull(string cronExpression)
    {
        TriggerSchedule.ToCrontab(cronExpression).Should().BeNull();
    }

    [Theory]
    [InlineData("0 0 12 ? * MON", "0 12 * * 1")]
    [InlineData("0 0 12 1 * ?", "0 12 1 * *")]
    [InlineData("0 0 12 * * ?", "0 12 * * *")]
    [InlineData("0 0 12 ? * *", "0 12 * * *")]
    [InlineData("0 0 12 */2 * ?", "0 12 */2 * *")]
    public void ToCrontab_OneDayFieldIsQuestionMark_ReturnsEquivalentCrontab(string cronExpression, string expected)
    {
        TriggerSchedule.ToCrontab(cronExpression).Should().Be(expected);
    }
}
