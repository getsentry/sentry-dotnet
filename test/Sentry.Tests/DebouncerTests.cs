namespace Sentry.Tests;

    public class DebouncerTests
    {
        [Fact]
        public void PerMinute_InitialisedCorrectly()
        {
            // Act
            var debouncer = Debouncer.PerMinute();

            // Assert
            debouncer.Should().NotBeNull();
            debouncer._intervalType.Should().Be(Debouncer.DebouncerInterval.Minute);
            debouncer._eventMaximum.Should().Be(1);
        }

        [Fact]
        public void PerHour_InitialisedCorrectly()
        {
            // Act
            var debouncer = Debouncer.PerHour();

            // Assert
            debouncer.Should().NotBeNull();
            debouncer._intervalType.Should().Be(Debouncer.DebouncerInterval.Hour);
            debouncer._eventMaximum.Should().Be(1);
        }

        [Fact]
        public void PerDay_InitialisedCorrectly()
        {
            // Act
            var debouncer = Debouncer.PerDay();

            // Assert
            debouncer.Should().NotBeNull();
            debouncer._intervalType.Should().Be(Debouncer.DebouncerInterval.Day);
            debouncer._eventMaximum.Should().Be(1);
        }

        [Fact]
        public void PerApplicationLifetime_InitialisedCorrectly()
        {
            // Act
            var debouncer = Debouncer.PerApplicationLifetime();

            // Assert
            debouncer.Should().NotBeNull();
            debouncer._intervalType.Should().Be(Debouncer.DebouncerInterval.ApplicationLifetime);
            debouncer._eventMaximum.Should().Be(1);
        }

        [Fact]
        public void CanProcess_WithCooldown_RespectsCooldown()
        {
            // Arrange
            var debouncer = Debouncer.PerMinute(10, TimeSpan.FromMinutes(1));
            var initialTime = DateTimeOffset.UtcNow;
            debouncer.RecordOccurence(initialTime);

            // Act & Assert
            debouncer.CanProcess(initialTime.AddSeconds(59)).Should().BeFalse();

            // Act & Assert
            debouncer.CanProcess(initialTime.AddSeconds(60)).Should().BeTrue();

            // Act & Assert
            debouncer.CanProcess(initialTime.AddSeconds(61)).Should().BeTrue();
        }

        [Fact]
        public void CanProcess_NoCooldown_IgnoresCooldown()
        {
            // Arrange
            var debouncer = Debouncer.PerMinute(10);
            var initialTime = DateTimeOffset.UtcNow;
            debouncer.RecordOccurence(initialTime);

            // Act & Assert
            debouncer.CanProcess(initialTime.AddSeconds(59)).Should().BeTrue();

            // Act & Assert
            debouncer.CanProcess(initialTime.AddSeconds(60)).Should().BeTrue();

            // Act & Assert
            debouncer.CanProcess(initialTime.AddSeconds(61)).Should().BeTrue();
        }

        [Fact]
        public void CanProcess_RespectsMaximum()
        {
            // Arrange
            var debouncer = Debouncer.PerApplicationLifetime(1);

            // Act
            var canProcess = debouncer.CanProcess();

            // Assert
            canProcess.Should().BeTrue();

            // Act
            debouncer.RecordOccurence();
            canProcess = debouncer.CanProcess();

            // Assert
            canProcess.Should().BeFalse();
        }

        [Fact]
        public void RecordOccurence_WithinInterval_IncrementsOccurrences()
        {
            // Arrange
            var debouncer = Debouncer.PerMinute(10);
            var initialTime = DateTimeOffset.UtcNow;
            var secondEventTime = initialTime.AddSeconds(10);

            // Act
            debouncer.RecordOccurence(initialTime);
            debouncer.RecordOccurence(secondEventTime);

            // Assert
            debouncer._occurrences.Should().Be(2);
            debouncer._intervalStart.Should().Be(initialTime);
            debouncer._lastEvent.Should().Be(secondEventTime);
        }

        [Fact]
        public void RecordOccurence_AfterInterval_ResetsInterval()
        {
            // Arrange
            var debouncer = Debouncer.PerMinute(10);
            var initialTime = DateTimeOffset.UtcNow;
            var secondEventTime = initialTime.AddSeconds(70);

            // Act
            debouncer.RecordOccurence(initialTime);
            debouncer.RecordOccurence(secondEventTime);

            // Assert
            debouncer._occurrences.Should().Be(1);
            debouncer._intervalStart.Should().Be(secondEventTime);
            debouncer._lastEvent.Should().Be(secondEventTime);
        }
    }
