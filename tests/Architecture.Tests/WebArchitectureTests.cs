using NetArchTest.Rules;

namespace Architecture.Tests;

public class WebArchitectureTests
{
	[Fact]
	public void Web_DoesNotReferenceSqlClientAssemblies()
	{
		// Arrange
		var webAssembly = typeof(Web.Components.App).Assembly;

		// Act
		var result = Types.InAssembly(webAssembly)
			.Should()
			.NotHaveDependencyOnAny("System.Data.SqlClient", "Microsoft.Data.SqlClient")
			.GetResult();

		// Assert
		Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
	}
}
