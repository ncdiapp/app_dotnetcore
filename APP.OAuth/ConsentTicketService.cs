using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace APP.OAuth;

/// <summary>
/// A short-lived, tamper-proof proof that a signed-in user approved one specific authorization request.
/// Issued by the consent API, presented back to /oauth/authorize. Bound to the exact request (client, redirect URI,
/// scope, resource, PKCE challenge, state) so it cannot be replayed for a different request. Stateless: protected with
/// ASP.NET Data Protection, so every node must share the key ring.
/// </summary>
public sealed class ConsentTicketService
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(3);

    /// <summary>The query keys that identify an authorization request; the ticket is bound to their values.</summary>
    private static readonly string[] BoundKeys = { "client_id", "redirect_uri", "scope", "resource", "code_challenge", "state" };

    private readonly ITimeLimitedDataProtector _protector;

    public ConsentTicketService(IDataProtectionProvider provider) =>
        _protector = provider.CreateProtector("AppAI.OAuth.ConsentTicket.v1").ToTimeLimitedDataProtector();

    /// <summary>SHA-256 over the bound request values, looked up through <paramref name="get"/>.</summary>
    public static string HashRequest(Func<string, string?> get)
    {
        var text = string.Join("\n", BoundKeys.Select(k => k + "=" + (get(k) ?? string.Empty)));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private sealed record Payload(int UserId, int CompanyId, string UserName, string RequestHash);

    public string Create(OAuthWebUser user, string requestHash) =>
        _protector.Protect(JsonSerializer.Serialize(new Payload(user.UserId, user.CompanyId, user.UserName, requestHash)), Lifetime);

    /// <summary>False for a forged, expired or other-request ticket.</summary>
    public bool TryValidate(string? ticket, string requestHash, out OAuthWebUser user)
    {
        user = default!;
        if (string.IsNullOrWhiteSpace(ticket)) return false;
        try
        {
            var payload = JsonSerializer.Deserialize<Payload>(_protector.Unprotect(ticket));
            if (payload is null || payload.UserId <= 0 || payload.CompanyId <= 0) return false;
            if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(payload.RequestHash), Encoding.ASCII.GetBytes(requestHash)))
                return false;
            user = new OAuthWebUser(payload.UserId, payload.CompanyId, payload.UserName);
            return true;
        }
        catch (CryptographicException)
        {
            return false;   // expired, tampered or protected with another key: all mean "not a valid ticket"
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
