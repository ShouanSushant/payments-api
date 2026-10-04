using PaymentsApi.Infrastructure;

namespace PaymentsApi.Tests;

[TestClass]
public class ApiKeyValidatorTests
{
    private static readonly byte[] ExpectedHash = ApiKeyValidator.Hash("correct-key");

    [TestMethod]
    public void Matches_CorrectKey_ReturnsTrue()
    {
        Assert.IsTrue(ApiKeyValidator.Matches("correct-key", ExpectedHash));
    }

    [DataTestMethod]
    [DataRow("wrong-key")]
    [DataRow("Correct-Key")]
    [DataRow("")]
    public void Matches_WrongOrEmptyKey_ReturnsFalse(string provided)
    {
        Assert.IsFalse(ApiKeyValidator.Matches(provided, ExpectedHash));
    }

    [TestMethod]
    public void Matches_NullKey_ReturnsFalse()
    {
        Assert.IsFalse(ApiKeyValidator.Matches(null, ExpectedHash));
    }
}
