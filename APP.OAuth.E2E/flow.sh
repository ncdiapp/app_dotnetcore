#!/bin/bash
# End-to-end flow against the e2e host: DCR -> authorize -> consent -> code -> token -> /mcp -> refresh -> revoke.
H=http://localhost:5097; B=$H/appai; R=https://claude.ai/api/mcp/auth_callback; RES=$B/mcp
ALICE='X-Test-User: 42:7:alice'; BOB='X-Test-User: 43:8:bob'
j(){ sed -n "s/.*\"$1\": *\"\([^\"]*\)\".*/\1/p"; }
enc(){ printf %s "$1" | sed 's/:/%3A/g;s#/#%2F#g'; }
mk(){ V=$(openssl rand -base64 48 | tr -d '=+/\n' | head -c 64); C=$(printf %s "$V" | openssl dgst -sha256 -binary | openssl base64 -A | tr '+/' '-_' | tr -d '='); }
pass=0; fail=0
check(){ # name expected actual
  if [ "$2" = "$3" ]; then echo "PASS  $1"; pass=$((pass+1)); else echo "FAIL  $1  (expected: $2 | got: $3)"; fail=$((fail+1)); fi; }
has(){ # name needle haystack
  if echo "$3" | grep -q -- "$2"; then echo "PASS  $1"; pass=$((pass+1)); else echo "FAIL  $1  (missing: $2 | in: $(echo "$3" | head -c 200))"; fail=$((fail+1)); fi; }
authurl(){ echo "$B/oauth/authorize?client_id=$1&response_type=code&redirect_uri=$(enc $R)&scope=mcp%20offline_access&state=${2:-xyz}&code_challenge=$C&code_challenge_method=S256&resource=$(enc $RES)"; }
loc(){ curl -s -i -m 10 "$1" | tr -d '\r' | grep -i '^location:' | sed 's/^[Ll]ocation: //'; }
code_of(){ echo "$1" | sed -n 's/.*[?&]code=\([^&]*\).*/\1/p'; }
post(){ curl -s -m 10 -X POST "$1" -H 'Content-Type: application/json' ${3:+-H "$3"} -d "$2"; }

echo "=== dynamic client registration"
D=$(post $B/oauth/register '{"client_name":"Claude","redirect_uris":["https://claude.ai/api/mcp/auth_callback"],"token_endpoint_auth_method":"none","grant_types":["authorization_code","refresh_token"],"response_types":["code"]}')
CID=$(echo "$D" | j client_id); has "register returns a client id" "dcr-" "$CID"
has "register response echoes redirect" "claude.ai" "$D"
check "register: evil redirect host refused" "invalid_redirect_uri" "$(post $B/oauth/register '{"redirect_uris":["https://evil.example/cb"]}' | j error)"
check "register: plain http non-local refused" "invalid_redirect_uri" "$(post $B/oauth/register '{"redirect_uris":["http://claude.ai/cb"]}' | j error)"
check "register: loopback http accepted" "dcr-" "$(post $B/oauth/register '{"redirect_uris":["http://127.0.0.1:3118/callback"]}' | j client_id | cut -c1-4)"
check "register: confidential method refused" "invalid_client_metadata" "$(post $B/oauth/register '{"redirect_uris":["https://claude.ai/cb"],"token_endpoint_auth_method":"client_secret_post"}' | j error)"
check "register: no redirect refused" "invalid_redirect_uri" "$(post $B/oauth/register '{"client_name":"x"}' | j error)"
check "register: bad grant refused" "invalid_client_metadata" "$(post $B/oauth/register '{"redirect_uris":["https://claude.ai/cb"],"grant_types":["client_credentials"]}' | j error)"

echo "=== authorize without approval goes to the consent page"
mk; AU=$(authurl $CID); L1=$(loc "$AU")
has "redirects to consent page" "/appai/consent-page?authorize_url=" "$L1"
has "prompt=none cannot show UI" "error=interaction_required" "$(loc "$AU&prompt=none")"

echo "=== consent API (re-validates, never trusts the page)"
DESC=$(post $H/webapi/OAuthConsent/Describe "{\"authorizeUrl\":\"$AU\"}" "$ALICE"); has "describe shows the client name" "Claude" "$DESC"; has "describe shows the redirect host" "claude.ai" "$DESC"
check "describe needs a signed-in user" "401" "$(curl -s -o /dev/null -w '%{http_code}' -X POST $H/webapi/OAuthConsent/Describe -H 'Content-Type: application/json' -d "{\"authorizeUrl\":\"$AU\"}")"
check "describe rejects a foreign URL" "400" "$(curl -s -o /dev/null -w '%{http_code}' -X POST $H/webapi/OAuthConsent/Describe -H 'Content-Type: application/json' -H "$ALICE" -d '{"authorizeUrl":"https://evil.example/oauth/authorize?client_id=x"}')"
check "describe rejects a client-supplied ticket" "400" "$(curl -s -o /dev/null -w '%{http_code}' -X POST $H/webapi/OAuthConsent/Describe -H 'Content-Type: application/json' -H "$ALICE" -d "{\"authorizeUrl\":\"$AU&consent_ticket=abc\"}")"
check "describe rejects an unregistered redirect uri" "400" "$(curl -s -o /dev/null -w '%{http_code}' -X POST $H/webapi/OAuthConsent/Describe -H 'Content-Type: application/json' -H "$ALICE" -d "{\"authorizeUrl\":\"$(echo "$AU" | sed 's|claude.ai%2Fapi%2Fmcp%2Fauth_callback|claude.ai%2Fother|')\"}")"

echo "=== approve -> code -> token -> /mcp"
APP=$(post $H/webapi/OAuthConsent/Approve "{\"authorizeUrl\":\"$AU\"}" "$ALICE"); URL=$(echo "$APP" | j redirectUrl); has "approve returns a ticketed URL" "consent_ticket=" "$URL"
L2=$(loc "$URL"); CODE=$(code_of "$L2"); has "authorize with ticket redirects to the client with a code" "claude.ai/api/mcp/auth_callback?code=" "$L2"; has "redirect carries iss" "iss=" "$L2"; has "redirect carries state" "state=xyz" "$L2"
T=$(curl -s -m 10 -X POST $B/oauth/token -d grant_type=authorization_code -d client_id=$CID -d code=$CODE -d redirect_uri=$R -d code_verifier=$V -d resource=$RES)
AT=$(echo "$T" | j access_token); RT=$(echo "$T" | j refresh_token); check "access token issued" "43" "${#AT}"; check "refresh token issued" "43" "${#RT}"
ME=$(curl -s -m 5 -H "Authorization: Bearer $AT" $B/mcp); has "mcp resolves alice and her company" '"user":42,"company":7' "$ME"
has "mcp registered the identity" "42/7" "$ME"
check "mcp: garbage token refused" "401" "$(curl -s -o /dev/null -w '%{http_code}' -H 'Authorization: Bearer garbage' $B/mcp)"
has "mcp: 401 points at resource metadata" "resource_metadata=" "$(curl -s -i -H 'Authorization: Bearer garbage' $B/mcp | tr -d '\r' | grep -i '^www-authenticate')"

echo "=== a ticket cannot be reused for a different request or forged"
mk; AU2=$(authurl $CID statetwo); T2URL=$(post $H/webapi/OAuthConsent/Approve "{\"authorizeUrl\":\"$AU2\"}" "$ALICE" | j redirectUrl)
TK=$(echo "$T2URL" | sed -n 's/.*consent_ticket=\([^&]*\).*/\1/p')
mk; AU3=$(authurl $CID statethree)   # different state AND different PKCE challenge
has "ticket for another request is refused" "error=access_denied" "$(loc "$AU3&consent_ticket=$TK")"
has "forged ticket is refused" "error=access_denied" "$(loc "$AU3&consent_ticket=Zm9yZ2Vk")"
has "an empty ticket is no approval (goes to consent page)" "/appai/consent-page?authorize_url=" "$(loc "$AU3&consent_ticket=")"

echo "=== deny"
mk; AU4=$(authurl $CID); DURL=$(post $H/webapi/OAuthConsent/Deny "{\"authorizeUrl\":\"$AU4\"}" "$ALICE" | j redirectUrl)
has "deny sends the client access_denied" "error=access_denied" "$(loc "$DURL")"

echo "=== refresh rotation and reuse detection"
TR=$(curl -s -m 10 -X POST $B/oauth/token -d grant_type=refresh_token -d client_id=$CID -d refresh_token=$RT); RT2=$(echo "$TR" | j refresh_token); AT2=$(echo "$TR" | j access_token)
check "refresh issues a new refresh token" "yes" "$([ -n "$RT2" ] && [ "$RT" != "$RT2" ] && echo yes || echo no)"
check "refresh: new access token works" "42" "$(curl -s -H "Authorization: Bearer $AT2" $B/mcp | sed -n 's/.*"user":\([0-9]*\).*/\1/p')"
check "replaying the old refresh token fails" "invalid_grant" "$(curl -s -X POST $B/oauth/token -d grant_type=refresh_token -d client_id=$CID -d refresh_token=$RT | j error)"
check "...and ends the whole chain (new refresh)" "invalid_grant" "$(curl -s -X POST $B/oauth/token -d grant_type=refresh_token -d client_id=$CID -d refresh_token=$RT2 | j error)"
check "...and its access token" "401" "$(curl -s -o /dev/null -w '%{http_code}' -H "Authorization: Bearer $AT2" $B/mcp)"

echo "=== admin: list and revoke a connection (company scoped)"
mk; U5=$(post $H/webapi/OAuthConsent/Approve "{\"authorizeUrl\":\"$(authurl $CID)\"}" "$ALICE" | j redirectUrl); C5=$(code_of "$(loc "$U5")")
T5=$(curl -s -X POST $B/oauth/token -d grant_type=authorization_code -d client_id=$CID -d code=$C5 -d redirect_uri=$R -d code_verifier=$V -d resource=$RES); AT5=$(echo "$T5" | j access_token); RT5=$(echo "$T5" | j refresh_token)
CONN=$(curl -s -H "$ALICE" $H/webapi/OAuthAdmin/Connections); has "company 7 admin sees the connection" '"clientName":"Claude"' "$CONN"; AID=$(echo "$CONN" | grep -o '"authorizationId":"[^"]*"' | head -1 | cut -d'"' -f4)
check "company 8 admin sees none of company 7's" "[]" "$(curl -s -H "$BOB" $H/webapi/OAuthAdmin/Connections)"
check "company 8 admin cannot revoke company 7's connection" "false" "$(curl -s -X POST -H "$BOB" "$H/webapi/OAuthAdmin/Revoke?id=$AID" | sed -n 's/.*"revoked":\([a-z]*\).*/\1/p')"
check "token still works before revoke" "42" "$(curl -s -H "Authorization: Bearer $AT5" $B/mcp | sed -n 's/.*"user":\([0-9]*\).*/\1/p')"
check "company 7 admin revokes it" "true" "$(curl -s -X POST -H "$ALICE" "$H/webapi/OAuthAdmin/Revoke?id=$AID" | sed -n 's/.*"revoked":\([a-z]*\).*/\1/p')"
check "revoked: access token refused" "401" "$(curl -s -o /dev/null -w '%{http_code}' -H "Authorization: Bearer $AT5" $B/mcp)"
check "revoked: refresh refused" "invalid_grant" "$(curl -s -X POST $B/oauth/token -d grant_type=refresh_token -d client_id=$CID -d refresh_token=$RT5 | j error)"

echo "=== a deactivated user's connection stops at once"
mk; U6=$(post $H/webapi/OAuthConsent/Approve "{\"authorizeUrl\":\"$(authurl $CID)\"}" "$BOB" | j redirectUrl); C6=$(code_of "$(loc "$U6")")
T6=$(curl -s -X POST $B/oauth/token -d grant_type=authorization_code -d client_id=$CID -d code=$C6 -d redirect_uri=$R -d code_verifier=$V -d resource=$RES); AT6=$(echo "$T6" | j access_token); RT6=$(echo "$T6" | j refresh_token)
check "bob's token works" "43" "$(curl -s -H "Authorization: Bearer $AT6" $B/mcp | sed -n 's/.*"user":\([0-9]*\).*/\1/p')"
curl -s -o /dev/null -X POST "$H/fake/deactivate?u=43"
check "after deactivation the access token is refused" "401" "$(curl -s -o /dev/null -w '%{http_code}' -H "Authorization: Bearer $AT6" $B/mcp)"
check "after deactivation refresh is refused" "invalid_grant" "$(curl -s -X POST $B/oauth/token -d grant_type=refresh_token -d client_id=$CID -d refresh_token=$RT6 | j error)"

echo "=== in-app agent run token"
AG=$(curl -s -X POST -H "$ALICE" $H/webapi/Agent/MintToken | j token); has "agent token has its prefix" "aat." "$AG"
check "agent token authenticates as the user" "42" "$(curl -s -H "Authorization: Bearer $AG" $B/mcp | sed -n 's/.*"user":\([0-9]*\).*/\1/p')"
check "tampered agent token is refused" "401" "$(curl -s -o /dev/null -w '%{http_code}' -H "Authorization: Bearer ${AG}x" $B/mcp)"
check "agent token is not an OAuth token elsewhere" "invalid_grant" "$(curl -s -X POST $B/oauth/token -d grant_type=refresh_token -d client_id=$CID -d refresh_token=$AG | j error)"

echo "=== discovery still consistent"
check "issuer equals PRM authorization_servers" "$(curl -s $B/.well-known/openid-configuration | tr -d '\n ' | sed -n 's/.*"issuer":"\([^"]*\)".*/\1/p')" "$(curl -s $B/.well-known/oauth-protected-resource | tr -d '\n ' | sed -n 's/.*"authorization_servers":\["\([^"]*\)"\].*/\1/p')"
has "registration endpoint advertised" "oauth/register" "$(curl -s $B/.well-known/openid-configuration)"

echo "=== maintenance round runs"
check "maintenance ok" "200" "$(curl -s -o /dev/null -w '%{http_code}' -X POST $H/maintenance)"

echo; echo "RESULT: $pass passed, $fail failed"
