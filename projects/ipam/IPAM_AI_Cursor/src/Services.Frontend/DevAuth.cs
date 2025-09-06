using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Services.Frontend;

public sealed class DevAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
	public const string Scheme = "Dev";

	public DevAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
		: base(options, logger, encoder, clock) { }

	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		var claims = new List<Claim>
		{
			new Claim(ClaimTypes.NameIdentifier, "dev-user"),
			new Claim(ClaimTypes.Name, "DevUser"),
			new Claim("role", "SystemAdmin"),
			new Claim("role", "AddressSpaceAdmin"),
			new Claim("role", "AddressSpaceViewer")
		};
		var identity = new ClaimsIdentity(claims, Scheme);
		var principal = new ClaimsPrincipal(identity);
		var ticket = new AuthenticationTicket(principal, Scheme);
		return Task.FromResult(AuthenticateResult.Success(ticket));
	}
}
