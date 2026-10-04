# Security Policy

## Supported Versions

The following versions of TicketManager are currently supported with security updates:

| Version | Supported          |
| ------- | ------------------ |
| 0.0.x   | :white_check_mark: |

**Note:** This is an early-stage project.
Security updates will be provided for the latest release only.
Once the project reaches 1.0, we will maintain security support for the current major version and one previous major version.

## Security Features

TicketManager implements the following security measures:

### Authentication & Authorization

- **Auth0 sign-in (planned)** - Not implemented yet: the app registers no authentication handler or login endpoint, so every page is public.
  The `Auth0:Domain`, `Auth0:ClientId` and `Auth0:ClientSecret` settings are read only to decide whether to redirect to `/Account/Login`
- **GitHub token** - GitHub API access uses a token from configuration or user secrets, falling back to the `GITHUB_TOKEN` environment variable

### Data Protection

- **No local data store** - GitHub is the system of record; the app keeps no database or copy of issue data
- **XSS protection** - Blazor's automatic HTML encoding
- **CSRF protection** - Built-in anti-forgery tokens
- **HTTPS enforcement** - HTTPS redirection everywhere, and HSTS outside Development

## Reporting a Vulnerability

If you discover a security vulnerability in TicketManager, please report it responsibly:

### How to Report

**Email:** <matthew.paulosky@outlook.com>  
**Subject:** [SECURITY] TicketManager Vulnerability Report

**Please do NOT open a public GitHub issue for security vulnerabilities.**

### What to Include

When reporting a security vulnerability, please include:

1. **Description** - Clear description of the vulnerability
2. **Impact** - Potential security impact and severity
3. **Steps to Reproduce** - Detailed steps to reproduce the vulnerability
4. **Affected Versions** - Which versions are affected
5. **Suggested Fix** - If you have ideas for mitigation (optional)
6. **Your Contact Info** - How we can reach you for follow-up

### Response Timeline

- **Initial Response:** Within 48 hours of report submission
- **Status Update:** Within 7 days with assessment and timeline
- **Fix Timeline:**
  - Critical vulnerabilities: Within 7 days
  - High severity: Within 14 days
  - Medium/Low severity: Within 30 days

### Disclosure Policy

- We will work with you to understand and validate the vulnerability
- We will develop and test a fix before public disclosure
- We will credit you in the security advisory (unless you prefer anonymity)
- We request that you do not publicly disclose the vulnerability until we have released a fix

### Security Advisories

Security updates will be published:

- In the [GitHub Security Advisories](https://github.com/mpaulosky/TicketManager/security/advisories)
- In the project [CHANGELOG.md](../CHANGELOG.md) (if one exists)
- In release notes for security-related releases

## Security Best Practices for Contributors

When contributing to TicketManager, please follow these security guidelines:

### Code Review

- All code changes require review before merging
- Security-sensitive changes require additional scrutiny
- Never commit secrets, API keys, or passwords

### Testing

- Add security-focused tests for authorization checks
- Test boundary conditions and edge cases

### Dependencies

- Keep NuGet packages up to date
- Review dependency security advisories
- Use `dotnet list package --vulnerable` to check for known vulnerabilities

### Secrets Management

- Use **User Secrets** for local development (`dotnet user-secrets`)
- Use **Environment Variables** for production
- Never commit `appsettings.Production.json` with secrets
- Add sensitive files to `.gitignore`

### Data Validation

- Validate all user input before it is sent to the GitHub API
- Sanitize data before rendering in Blazor components (Blazor does this automatically)

## Known Security Considerations

### Current Limitations

- **GitHub token scope** - The app can do whatever its GitHub token allows, so give it only the scopes it needs
- **No rate limiting** - Consider implementing rate limiting for production
- **No audit logging** - User actions are not currently logged

### Recommendations for Production

1. **Use HTTPS** - Enable HTTPS and HSTS
2. **Secure secrets** - Keep the GitHub token and Auth0 client secret in Azure Key Vault or similar
3. **Enable logging** - Add security event logging
4. **Rate limiting** - Implement API rate limiting
5. **Regular updates** - Keep .NET and dependencies updated
6. **Security headers** - Add security headers (CSP, X-Frame-Options, etc.)
7. **Monitor dependencies** - Use GitHub Dependabot for security alerts

## Security Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [ASP.NET Core Security Best Practices](https://learn.microsoft.com/aspnet/core/security/)
- [Blazor Security](https://learn.microsoft.com/aspnet/core/blazor/security/)

---

Thank you for helping keep TicketManager secure!
