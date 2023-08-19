using Sentry.Tests.Helpers.Reflection;

namespace Sentry.Tests.Internals;

public class SparseArrayTests
{
    private void Test(SparseScalarArray<int> sut, int setDefault)
    {
        sut.ContainsKey(0).Should().BeFalse();
        sut.ContainsKey(1).Should().BeFalse();
        sut[0] = setDefault;
        sut.ContainsKey(0).Should().BeFalse();
        sut.ContainsKey(1).Should().BeFalse();
        sut[0] = setDefault + 1;
        sut.ContainsKey(0).Should().BeTrue();
        sut.ContainsKey(1).Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    public void ContainsKey_WhenInitializedEmpty_Works(int defaultValue)
    {
        var sut = new SparseScalarArray<int>(defaultValue);
        Test(sut, defaultValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    public void ContainsKey_WhenGivenCapacity_Works(int defaultValue)
    {
        var sut = new SparseScalarArray<int>(defaultValue, 10);
        Test(sut, defaultValue);
    }
}
