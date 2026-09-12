// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     Auth0ConfigurationHelper.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web
// =============================================

namespace Web.Security;

public static class Auth0ConfigurationHelper
{
	public static bool IsAuthenticationEnabled(string? domain, string? clientId, string? clientSecret)
	{
		return !string.IsNullOrWhiteSpace(domain)
		       && !string.IsNullOrWhiteSpace(clientId)
		       && !string.IsNullOrWhiteSpace(clientSecret);
	}
}
