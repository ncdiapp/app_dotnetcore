# Setting up OAuth sign-in for the AppAI MCP gateway

For: whoever deploys AppAI and the administrators who connect Claude, ChatGPT and in-house agents to it.
Design and status: `mcp-oauth-plan.md` (section 7g says what is verified). How the gateway works: `mcp-gateway-architecture.md`.

> **Status:** implemented and tested against a fake session layer; **not yet run on a real AppAI instance, nor with real
> Claude or ChatGPT.** Treat the first deployment as a dev rehearsal. ChatGPT details come from OpenAI's public documentation
> and are untested here.

## 1. What you get

| Who | Credential | Acts as |
|---|---|---|
| A person using Claude Desktop, claude.ai, Claude Code or ChatGPT | They sign in to AppAI in their browser and approve once (OAuth) | **That person**, with their own security groups |
| An in-house automation or unattended agent | An Integration token (Company security settings, "Integration Tokens") | The service account |
| The in-app agent started by a signed-in user | A short-lived token minted per run | **That user** |

An AI client only ever sees an opaque OAuth token. It never receives a login session id.

## 2. Before you start (these are not optional)

1. **A public HTTPS address** for the app, reachable by the AI vendors. Claude connects from Anthropic's cloud
   (`160.79.104.0/21`), so `http://localhost` or an internal-only name cannot work. If you cannot publish it, Anthropic's
   MCP tunnels are an option (Enterprise plan, by request); OpenAI's equivalent was not researched.
2. **Only publish what is needed:** `/appai/mcp`, `/appai/oauth/*`, `/appai/.well-known/*`, the host-root `/.well-known/*`, and
   the React page `/mcp-authorize`. Keep `/webapi`, admin screens and `/swagger` off the public route.
3. **A shared, persisted ASP.NET Data Protection key ring** if more than one server node runs AppAI (consent tickets and agent
   tokens are protected with it; the default key location is per machine).
4. **Trusted forwarded headers** if a reverse proxy is in front (the per-IP rate limits and the loopback-only `/swagger` rule
   need the real client address; the app has no forwarded-header handling today).
5. **Two certificates** (signing, encryption) installed in the LocalMachine or CurrentUser store; you need their thumbprints.
   Outside Development the app refuses to start OAuth without them.
6. The React app must be reachable at the host root so `https://<host>/mcp-authorize` works.

## 3. Install

1. **Database.** Run `Document/Design/MasterDB_OAuthTables.sql` once against `AppMasterDB` (SSMS or sqlcmd). It is safe to run again.
2. **Configuration** (`appsettings.json`; the agent that wrote this does not edit that file):

```json
"OAuth": {
  "Enabled": true,
  "PublicBaseUrl": "https://mcp.example.com/appai",
  "ConsentPageUrl": "https://mcp.example.com/mcp-authorize",
  "McpPath": "/mcp",
  "Scope": "mcp",
  "TlsTerminatedUpstream": true,
  "RegistrationEnabled": true,
  "SigningCertificateThumbprint": "<thumbprint>",
  "EncryptionCertificateThumbprint": "<thumbprint>",
  "AllowedRedirectHosts": [ "claude.ai", "claude.com", "chatgpt.com", "chat.openai.com" ],
  "AccessTokenMinutes": 60,
  "RefreshTokenDays": 14,
  "AgentTokenMinutes": 60,
  "AgentTokenServerUrls": [ "https://mcp.example.com/appai/mcp" ]
}
```

| Setting | Meaning |
|---|---|
| `PublicBaseUrl` | The public address including `/appai`. It is the OAuth issuer and the base of every metadata URL. https only (http only for localhost). |
| `ConsentPageUrl` | The React page that signs the user in and asks for approval. |
| `TlsTerminatedUpstream` | `true` when TLS ends at a proxy, so the app sees http (OpenIddict refuses http otherwise). |
| `RegistrationEnabled` | Lets Claude/ChatGPT register themselves. Turn off to allow only clients you register by hand. |
| `AllowedRedirectHosts` | Hosts an automatically registered client may redirect to (https), plus `http://localhost` / `127.0.0.1` for desktop tools. |
| `RefreshTokenDays` | Absolute lifetime of a connection: after this the user must connect again. |
| `AgentTokenServerUrls` | Exact MCP server URLs the in-app agent calls **as the signed-in user** (see section 6). Empty = none. |

3. **AppAI as an API source** (so tools can call this instance as the user). In `ApiSources:Sources` add:

```json
{ "Name": "AppAI", "BaseUrl": "http://localhost:52740/appai/", "SwaggerJsonPath": "swagger/v1/swagger.json",
  "AccessTokenHeaderName": "CurrentUserSessionId", "ForwardCallerToken": true }
```

   The header is **`CurrentUserSessionId`**: that is what `/webapi` reads. (An earlier note said `IntergrationAccessToken`; `/webapi`
   ignores it.) Turn `ForwardCallerToken` on **only** for this instance: it sends the caller's session id to that source.
4. **Restart.** The log should say `OAuth scope 'mcp' created for resource https://.../appai/mcp`. If it logs
   `OAuth could not prepare its 'mcp' scope`, the tables are missing; fix and restart.
5. **Check the metadata** (replace the host):
   `https://<host>/appai/.well-known/oauth-protected-resource` and `https://<host>/appai/.well-known/openid-configuration`
   must both answer 200, and the `issuer` in the second must equal `authorization_servers[0]` in the first, character for character.
   A request to `/appai/mcp` without a token must answer **401** with a `WWW-Authenticate` header containing `resource_metadata=`.

## 4. Connect an AI application

**Claude (claude.ai, Claude Desktop):** Settings > Connectors > Add custom connector. URL `https://<host>/appai/mcp`. Leave the OAuth
client on Claude's default (it registers itself). Sign in to AppAI when the browser opens, press **Allow**.
To use a client you registered yourself, open "Advanced" and enter its client id (section 5).

**Claude Code:** `claude mcp add --transport http appai https://<host>/appai/mcp`, then authenticate in the browser.

**ChatGPT:** needs Developer mode (Business or Enterprise/Edu workspaces; an admin turns it on). Create a custom MCP connector
with the same URL and OAuth authentication. If ChatGPT's redirect host is not in `AllowedRedirectHosts`, registration is refused
with `invalid_redirect_uri`: add the host from the error in the log.

After approval the AI application can only use the APIs allowed for that user's security groups
(Company security settings > **MCP API Access**), and everything is recorded under the user's name.

## 5. Administration

| Task | Where | Who |
|---|---|---|
| Choose which APIs external users may use | Company security settings > MCP API Access | Company admin |
| See and disconnect AI applications of your company's users | Company security settings > **AI Connections** | Company admin |
| See and disconnect your own AI applications | page `my-ai-connections` (no menu entry; open the URL) | Any user |
| Register or delete OAuth clients (for an application that cannot register itself) | page `oauth-clients` (no menu entry) | System administrator |
| Revoke a person's access at once | disconnect the connection, or deactivate/delete the user (their grant session goes with them) | Admin |

A disconnected connection stops immediately: its tokens are revoked, so the application must ask the user to sign in again.

## 6. In-house agents

- **Unattended jobs:** create one Integration token (service account) per job, each in its own security groups, and give that
  to the job. No OAuth involved.
- **The in-app agent run by a signed-in user:** add the gateway's MCP server URL to `OAuth:AgentTokenServerUrls`. For exactly
  those URLs the agent sends a token minted for the user who started the run (expires in `AgentTokenMinutes`) and **drops every
  stored header** of that server registration, so the user's own permissions apply. A run with no signed-in user skips that
  server instead of borrowing a shared identity.

## 7. Limits and lifetimes

Access token 60 min, connection 14 days (absolute), consent approval valid 3 min, refresh tokens rotate and a replayed one
ends the whole connection. Per client IP per minute: `/mcp` 120, `/oauth/token` 30, `/oauth/authorize` 60, `/oauth/register` 10.
Automatically registered clients that nobody used are deleted after 7 days; housekeeping runs hourly.

## 8. Troubleshooting

| Symptom | Likely cause |
|---|---|
| Claude: "Couldn't reach the MCP server" | `/appai/mcp` not reachable from the internet, or the 401 has no `resource_metadata`, or a firewall/WAF blocks `160.79.104.0/21` (also for the metadata URLs). |
| `invalid_target` at authorize | The URL typed into the connector differs from `PublicBaseUrl` + `McpPath` (including a trailing slash or `http` vs `https`). |
| Browser shows "not valid. Start again" on the consent page | The page was opened by hand, the request is for another resource/redirect, or it was altered. Start from the application. |
| "approval expired" | More than 3 minutes between pressing Allow and the redirect, or the server nodes do not share Data Protection keys. |
| Authorize returns 500 | The OAuth tables are missing, or the session table refuses a second row per user (see plan section 7g). Check the log. |
| `invalid_grant` on refresh | The connection ended (user signed out of that session row, deactivated, disconnected, or 14 days passed). Connect again. |
| `invalid_redirect_uri` on registration | The client's redirect host is not in `AllowedRedirectHosts`. |
| Tools list is empty | The user's security groups have no API enabled in MCP API Access. |
| Tool calls fail with 401 from AppAI | The AppAI source uses `IntergrationAccessToken` instead of `CurrentUserSessionId`, or `ForwardCallerToken` is off. |
