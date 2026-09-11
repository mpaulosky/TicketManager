using Microsoft.AspNetCore.Http;
using Web.Components.Pages;

namespace Web.Tests.Bunit;

public class ErrorPageTests : BunitContext
{
	[Fact]
	public void Error_Rendered_ShowsGenericErrorHeading()
	{
		// Arrange & Act
		var cut = Render<Error>();

		// Assert
		Assert.Contains("An error occurred while processing your request.", cut.Markup);
	}

	[Fact]
	public void Error_NoHttpContext_DoesNotShowRequestId()
	{
		// Arrange & Act
		var cut = Render<Error>();

		// Assert
		Assert.DoesNotContain("Request ID", cut.Markup);
	}

	[Fact]
	public void Error_HttpContextWithTraceIdentifier_ShowsRequestId()
	{
		// Arrange
		var httpContext = new DefaultHttpContext { TraceIdentifier = "trace-123" };

		// Act
		var cut = Render<Error>(parameters => parameters.AddCascadingValue(httpContext));

		// Assert
		Assert.Contains("Request ID", cut.Markup);
		Assert.Contains("trace-123", cut.Markup);
	}
}
