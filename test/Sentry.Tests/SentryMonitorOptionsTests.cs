namespace Sentry.Tests;

public class SentryMonitorOptionsTests
{
    /*
        The time and date fields are:
              field          allowed values
              -----          --------------
              minute         0-59
              hour           0-23
              day of month   1-31
              month          1-12 (or names, see below)
              day of week    0-7 (0 or 7 is Sunday, or use names)

        Special characters are:
            *	any value
            ,	value list separator
            -	range of values
            /	step values
    */
    [Theory]
    [InlineData("* * * * *")]
    [InlineData("0 0 1 1 *")]
    [InlineData("0 0 1 * 0")]
    [InlineData("59 23 31 12 7")]
    [InlineData("0 */2 * * *")]
    [InlineData("0 8-10 * * *")]
    [InlineData("0 6,8,9 * * *")]
    // Step values (*/n)
    [InlineData("*/15 * * * *")]        // Every 15 minutes
    [InlineData("0 */6 * * *")]         // Every 6 hours
    [InlineData("0 0 */2 * *")]         // Every 2 days
    [InlineData("0 0 1 */3 *")]         // Every 3 months
    [InlineData("*/100 * * * *")]       // Step value 100 for minutes
    [InlineData("* */25 * * *")]        // Step value 25 for hours
    [InlineData("* * */32 * *")]        // Step value 32 for days
    [InlineData("* * * */13 *")]        // Step value 13 for months
    [InlineData("* * * * */8")]         // Step value 8 for weekdays
    [InlineData("*/60 * * * *")]        // Step value 60 for minutes
    [InlineData("* */24 * * *")]        // Step value 24 for hours
    // Complex ranges
    [InlineData("1-15 * * * *")]        // Minutes 1 through 15
    [InlineData("* 9-17 * * *")]        // Business hours
    [InlineData("* * 1-15,16-31 * *")]  // Split day ranges
    // Multiple comma-separated values
    [InlineData("1,15,30,45 * * * *")]  // Specific minutes
    [InlineData("* 9,12,15,17 * * *")]  // Specific hours
    [InlineData("0 0 * * 1,3,5")]       // Monday, Wednesday, Friday
    // Combinations of special characters
    [InlineData("*/15 9-17 * * 1-5")]   // Every 15 min during business hours on weekdays
    [InlineData("0 8-10,13-15 * * *")]  // Morning and afternoon ranges
    // Edge cases
    [InlineData("0 0 1 1 0")]           // Minimum values
    [InlineData("*/1 * * * *")]         // Step of 1
    [InlineData("* * 31 */2 *")]        // 31st of every other month
    // Weekday names
    [InlineData("0 0 * * MON")]
    [InlineData("0 9 * * MON-FRI")]
    [InlineData("0 18 * * MON-FRI")]
    [InlineData("0 0 * * MON-FRI")]
    [InlineData("0 20 * * MON-FRI")]
    // Step values with ranges 
    [InlineData("0-30/15 * * * *")]     // Every 15 minutes from 0-30
    [InlineData("0-59/10 * * * *")]     // Every 10 minutes from 0-59
    [InlineData("0-45/5 * * * *")]      // Every 5 minutes from 0-45
    [InlineData("* 8-18/2 * * *")]      // Every 2 hours from 8-18
    [InlineData("* 0-23/6 * * *")]      // Every 6 hours from 0-23
    [InlineData("* 9-17/1 * * *")]      // Every hour from 9-17
    [InlineData("* * 1-15/3 * *")]      // Every 3 days from 1-15
    [InlineData("* * 1-31/7 * *")]      // Every 7 days from 1-31
    [InlineData("* * 10-20/2 * *")]     // Every 2 days from 10-20
    [InlineData("* * * 1-6/2 *")]       // Every 2 months from 1-6
    [InlineData("* * * 1-12/3 *")]      // Every 3 months from 1-12
    [InlineData("* * * 3-9/1 *")]       // Every month from 3-9
    [InlineData("* * * * 1-5/2")]       // Every 2 weekdays from 1-5 (Mon-Fri)
    [InlineData("* * * * 0-6/3")]       // Every 3 days of week from 0-6
    [InlineData("* * * * MON-FRI/2")]   // Every 2 weekdays from Mon-Fri
    [InlineData("* * * * MON-SUN/3")]   // Every 3 days from Mon-Sun
    // Complex combinations with step values and ranges
    [InlineData("0-30/15 8-18/2 * * *")] // Every 15 min from 0-30, every 2 hours from 8-18
    [InlineData("0-45/5 9-17/1 1-15/3 * *")] // Complex combination
    [InlineData("*/10 8-18/4 1-31/7 1-12/3 MON-FRI/2")] // All fields with step values and ranges
    public void Interval_ValidCrontab_DoesNotThrow(string crontab)
    {
        // Arrange
        var options = new SentryMonitorOptions();

        // Act
        options.Interval(crontab);
    }

    [Fact]
    public void Interval_SetMoreThanOnce_Throws()
    {
        // Arrange
        var options = new SentryMonitorOptions();

        // Act
        options.Interval(1, SentryMonitorInterval.Month);
        Assert.Throws<ArgumentException>(() => options.Interval(2, SentryMonitorInterval.Day));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a crontab")]
    [InlineData("* * a * *")]
    [InlineData("60 * * * *")]
    [InlineData("* 24 * * *")]
    [InlineData("* * 32 * *")]
    [InlineData("* * * 13 *")]
    [InlineData("* * * * 8")]
    // Invalid step values
    [InlineData("*/0 * * * *")]         // Step value cannot be 0
    // Invalid ranges
    [InlineData("5-60 * * * *")]        // Minute range exceeds 59
    [InlineData("* 5-24 * * *")]        // Hour range exceeds 23
    [InlineData("* * 0-31 * *")]        // Day cannot be 0
    [InlineData("* * * 0-12 *")]        // Month cannot be 0
    // Invalid combinations
    [InlineData("*-5 * * * *")]         // Invalid range with asterisk
    [InlineData("1,2,60 * * * *")]      // Invalid value in list
    [InlineData("1-5-10 * * * *")]      // Multiple ranges
    [InlineData("*/2/3 * * * *")]       // Multiple steps
    // Malformed expressions
    [InlineData("* * * *")]             // Too few fields
    [InlineData("* * * * * *")]         // Too many fields
    [InlineData("** * * * *")]          // Double asterisk
    [InlineData("*/* * * * *")]         // Invalid step format
    [InlineData(",1,2 * * * *")]        // Leading comma
    [InlineData("1,2, * * * *")]        // Trailing comma
    // Invalid step values with ranges
    [InlineData("0-60/15 * * * *")]     // Minute range exceeds 59
    [InlineData("0-30/0 * * * *")]      // Step value cannot be 0
    [InlineData("0-30/-5 * * * *")]     // Negative step value
    [InlineData("* 8-25/2 * * *")]      // Hour range exceeds 23
    [InlineData("* 8-18/0 * * *")]      // Step value cannot be 0
    [InlineData("* * 0-31/3 * *")]      // Day cannot be 0
    [InlineData("* * 1-32/3 * *")]      // Day range exceeds 31
    [InlineData("* * * 0-12/2 *")]      // Month cannot be 0
    [InlineData("* * * 1-13/2 *")]      // Month range exceeds 12
    [InlineData("* * * * 0-8/2")]       // Weekday range exceeds 7
    [InlineData("* * * * MON-FRI/0")]   // Step value cannot be 0
    [InlineData("0-30//15 * * * *")]    // Double slash
    [InlineData("0-30/15/ * * * *")]    // Trailing slash
    [InlineData("0-30/15- * * * *")]    // Incomplete range
    // Invalid single value with step (should only allow step with * or ranges)
    [InlineData("30/5 * * * *")]        // Single minute with step
    [InlineData("* 8/2 * * *")]         // Single hour with step
    [InlineData("* * 15/3 * *")]        // Single day with step
    [InlineData("* * * 6/2 *")]         // Single month with step
    [InlineData("* * * * 3/2")]         // Single weekday with step
    [InlineData("* * * * MON/2")]       // Single weekday name with step
    public void CaptureCheckIn_InvalidCrontabSet_Throws(string crontab)
    {
        // Arrange
        var options = new SentryMonitorOptions();

        // Act
        Assert.Throws<ArgumentException>(() => options.Interval(crontab));
    }
}
