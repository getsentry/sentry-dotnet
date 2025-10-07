namespace Sentry.Tests.Internals;

#if NET9_0_OR_GREATER
using TBool = bool;
#else
using TBool = int;
#endif

public class InterlockedBooleanTests
{
#if NET9_0_OR_GREATER
    private const TBool True = true;
    private const TBool False = false;
#else
    private const TBool True = 1;
    private const TBool False = 0;
#endif

    private TBool ToTBool(bool value) => value ? True : False;
    private bool FromTBool(TBool value) => (value != False);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InterlockedBoolean_Constructor_ConstructsExpected(bool value)
    {
        // Arrange
        var tboolValue = ToTBool(value);

        // Act
        var actual = new InterlockedBoolean(value);

        // Assert
        actual._value.Should().Be(tboolValue);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InterlockedBoolean_ImplicitToBool_ReturnsExpected(bool value)
    {
        // Arrange
        var sut = new InterlockedBoolean(value);

        // Act
        bool actual = sut;

        // Assert
        actual.Should().Be(value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InterlockedBoolean_ImplicitFromBool_ReturnsExpected(bool value)
    {
        // Arrange
        var tboolValue = ToTBool(value);

        // Act
        InterlockedBoolean actual = value;

        // Assert
        actual._value.Should().Be(tboolValue);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void InterlockedBoolean_Exchange_ReturnsExpected(bool initialState, bool newValue)
    {
        // Arrange
        var sut = new InterlockedBoolean(initialState);

        // Act
        var result = sut.Exchange(newValue);

        // Assert
        result.Should().Be(initialState);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void InterlockedBoolean_Exchange_SetsExpectedNewState(bool initialState, bool newValue)
    {
        // Arrange
        var sut = new InterlockedBoolean(initialState);

        var tboolNewValue = ToTBool(newValue);

        // Act
        var result = sut.Exchange(newValue);

        // Assert
        sut._value.Should().Be(tboolNewValue);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void InterlockedBoolean_CompareExchange_ReturnsExpected(bool initialState, bool comparand, bool newValue)
    {
        // Arrange
        var sut = new InterlockedBoolean(initialState);

        // Act
        var result = sut.CompareExchange(newValue, comparand);

        // Assert
        result.Should().Be(initialState);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void InterlockedBoolean_CompareExchange_SetsExpectedNewState(bool initialState, bool comparand, bool newValue)
    {
        // Arrange
        var sut = new InterlockedBoolean(initialState);

        var tboolExpected = ToTBool(
            initialState == comparand
            ? newValue
            : initialState);

        // Act
        sut.CompareExchange(newValue, comparand);

        // Assert
        sut._value.Should().Be(tboolExpected);
    }
}
