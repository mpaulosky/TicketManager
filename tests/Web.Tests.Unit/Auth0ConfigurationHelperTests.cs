using Web.Security;

namespace Web.Tests.Unit;

public class Auth0ConfigurationHelperTests
{
	[Fact]
	public void IsAuthenticationEnabled_AllValuesPresent_ReturnsTrue()
	{
		// Act
		var result = Auth0ConfigurationHelper.IsAuthenticationEnabled("domain", "clientId", "clientSecret");

		// Assert
		Assert.True(result);
	}

	[Theory]
	[InlineData(null, "clientId", "clientSecret")]
	[InlineData("domain", null, "clientSecret")]
	[InlineData("domain", "clientId", null)]
	[InlineData("", "clientId", "clientSecret")]
	[InlineData("domain", "", "clientSecret")]
	[InlineData("domain", "clientId", "")]
	[InlineData("  ", "clientId", "clientSecret")]
	public void IsAuthenticationEnabled_AnyValueMissing_ReturnsFalse(string? domain, string? clientId,
		string? clientSecret)
	{
		// Act
		var result = Auth0ConfigurationHelper.IsAuthenticationEnabled(domain, clientId, clientSecret);

		// Assert
		Assert.False(result);
	}
}
