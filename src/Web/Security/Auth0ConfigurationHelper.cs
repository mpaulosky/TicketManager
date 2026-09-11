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
