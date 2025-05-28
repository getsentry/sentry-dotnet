namespace Sentry.Tests.Internals;

public class FnvHashTests
{
    [Theory]
    [InlineData("", 2_166_136_261)]
    [InlineData("h", 3_977_000_791)]
    [InlineData("he", 1_547_363_254)]
    [InlineData("hel", 179_613_742)]
    [InlineData("hell", 477_198_310)]
    [InlineData("hello", 1_335_831_723)]
    [InlineData("hello ", 3_801_292_497)]
    [InlineData("hello w", 1_402_552_146)]
    [InlineData("hello wo", 3_611_200_775)]
    [InlineData("hello wor", 1_282_977_583)]
    [InlineData("hello worl", 2_767_971_961)]
    [InlineData("hello world", 3_582_672_807)]
    public void ComputeHash_WithString_ReturnsExpected(string input, uint expected)
    {
        var actual = FnvHash.ComputeHash(input);

        Assert.Equal(unchecked((int)expected), actual);
    }
}
