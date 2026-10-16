# APP.OAuth.E2E

A developer tool that drives the whole OAuth flow over real HTTP, so a change to `APP.OAuth` can be checked in about a
minute without the AppAI databases. It is **not** in `AppAI.Core.sln` and must never be deployed.

What it is: a small host with the same pipeline shape as `AppAI.Web` (host-root `/.well-known` rewrite, `UsePathBase("/appai")`,
authentication) running the real `APP.OAuth` code on SQL stores in LocalDB, with a **fake** session bridge in place of
AppAI's login (the "signed-in user" is an `X-Test-User: userId:companyId:name` header) and a stand-in `/mcp`.

## Run

1. Create an empty LocalDB database and the tables:
   ```
   sqlcmd -S "(localdb)\MSSQLLocalDB" -E -Q "CREATE DATABASE AppMasterDB"
   sqlcmd -S "(localdb)\MSSQLLocalDB" -E -b -i ..\Document\Design\MasterDB_OAuthTables.sql
   ```
   (Use a throwaway LocalDB instance, not a real AppMasterDB.)
2. `dotnet run --project APP.OAuth.E2E` (listens on http://localhost:5097)
3. In another shell: `bash APP.OAuth.E2E/flow.sh`. It prints PASS/FAIL per check and a total.

## What `flow.sh` checks (52 checks)

Dynamic client registration (allowed and refused redirect URIs, auth method, grant types); authorize without approval goes to
the consent page; `prompt=none`; the consent API (describe, approve, deny, foreign URL, client-supplied ticket, unregistered
redirect); code, token and `/mcp`; a ticket cannot be reused for another request or forged; refresh rotation and reuse
detection (whole chain revoked); admin list and revoke scoped to the company; a deactivated user's connection stops at
once; the in-app agent run token; discovery consistency; the maintenance round.

Not covered here (needs the real AppAI): AppAI's own login and session tables, `AppOAuthGrantSessionBL`, the React pages.
