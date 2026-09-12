// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ErrorPageTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Bunit
// =============================================

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
		cut.Markup.Should().Contain("An error occurred while processing your request.");
	}

	[Fact]
	public void Error_NoHttpContext_DoesNotShowRequestId()
	{
		// Arrange & Act
		var cut = Render<Error>();

		// Assert
		cut.Markup.Should().NotContain("Request ID");
	}

	[Fact]
	public void Error_HttpContextWithTraceIdentifier_ShowsRequestId()
	{
		// Arrange
		var httpContext = new DefaultHttpContext { TraceIdentifier = "trace-123" };

		// Act
		var cut = Render<Error>(parameters => parameters.AddCascadingValue(httpContext));

		// Assert
		cut.Markup.Should().Contain("Request ID");
		cut.Markup.Should().Contain("trace-123");
	}
}
