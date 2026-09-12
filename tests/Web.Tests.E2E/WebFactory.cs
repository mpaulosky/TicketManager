// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     WebFactory.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.E2E
// =============================================

namespace Web.Tests.E2E;

/// <summary>
///   Hosts <c>Web</c> on a real Kestrel TCP port (not the in-memory TestServer) so an
///   out-of-process Playwright browser can navigate to it over real HTTP.
/// </summary>
public sealed class WebFactory : WebApplicationFactory<Program>
{
	private bool _started;

	public string ServerAddress { get; private set; } = string.Empty;

	public void EnsureStarted()
	{
		if (_started)
		{
			return;
		}

		UseKestrel(0);
		StartServer();

		var server = Services.GetRequiredService<IServer>();
		ServerAddress = server.Features.Get<IServerAddressesFeature>()!.Addresses.First();
		_started = true;
	}
}
