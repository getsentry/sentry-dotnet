namespace Sentry.Tests.Internals;

public class InitCounterTests
{
    [Fact]
    public void Count_StartsAtZero()
    {
        // Arrange
        var intCounter = new InitCounter();

        // Act & Assert
        Assert.Equal(0, intCounter.Count);
    }

    [Fact]
    public void Increment_IncreasesByOne()
    {
        // Arrange
        var intCounter = new InitCounter();

        // Act
        intCounter.Increment();

        // Assert
        intCounter.Count.Should().Be(1);
    }

    [Fact]
    public async Task Increment_IsThreadsSafe()
    {
        // Arrange
        const int incrementsPerTask = 1000;
        const int numberOfTasks = 10;
        var intCounter = new InitCounter();

        // Act
        // Spawn multiple threads to increment the counter then wait for all the threads to complete and verify the expected number of increments has been made
        var tasks = new List<Task>();
        for (var i = 0; i < numberOfTasks; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < incrementsPerTask; j++)
                {
                    intCounter.Increment();
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(incrementsPerTask * numberOfTasks, intCounter.Count);
    }
}
