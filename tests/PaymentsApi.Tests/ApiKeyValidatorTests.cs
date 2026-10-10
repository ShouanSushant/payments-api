using PaymentsApi.Infrastructure;

namespace PaymentsApi.Tests;

public class ApiKeyValidatorTests
{
    private static readonly byte[] ExpectedHash = ApiKeyValidator.Hash("correct-key");

    [Fact]
    public void Matches_CorrectKey_ReturnsTrue()
    {
        Assert.True(ApiKeyValidator.Matches("correct-key", ExpectedHash));
    }

    [Theory]
    [InlineData("wrong-key")]
    [InlineData("Correct-Key")]
    [InlineData("")]
    public void Matches_WrongOrEmptyKey_ReturnsFalse(string provided)
    {
        Assert.False(ApiKeyValidator.Matches(provided, ExpectedHash));
    }

    [Fact]
    public void Matches_NullKey_ReturnsFalse()
    {
        Assert.False(ApiKeyValidator.Matches(null, ExpectedHash));
    }
}
