using Microsoft.Extensions.Configuration;

namespace Web.Tests.Unit;

public class AppSettingsTests
{
	[Fact]
	public void AppSettings_AllowedHosts_IsWildcard()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
			.SetBasePath(AppContext.BaseDirectory)
			.AddJsonFile("appsettings.json")
			.Build();

		// Act
		var allowedHosts = configuration["AllowedHosts"];

		// Assert
		Assert.Equal("*", allowedHosts);
	}
}
