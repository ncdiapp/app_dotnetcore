import React, { useCallback, useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { oauthSvc, OAuthConsentInfo } from '../../webapi/oauthSvc';
import { useTheme } from '../../redux/hooks/useTheme';

// Opened by /oauth/authorize when an AI application (Claude, ChatGPT, ...) asks to connect. The user is signed in to
// AppAI first (the login page returns here), then approves or declines. The server re-validates the request: this page
// only relays the authorize URL and opens the URL the server answers with.
const McpAuthorize: React.FC = () => {
  const { theme } = useTheme();
  const [params] = useSearchParams();
  const authorizeUrl = params.get('authorize_url') ?? '';

  const [info, setInfo] = useState<OAuthConsentInfo | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [working, setWorking] = useState(false);

  useEffect(() => {
    if (!authorizeUrl) {
      setError('This page is opened by an AI application. Start the connection from the application.');
      return;
    }
    oauthSvc
      .describe(authorizeUrl)
      .then(setInfo)
      .catch((e: unknown) => setError(e instanceof Error ? e.message : String(e)));
  }, [authorizeUrl]);

  // The server returns the authorize URL (plus a ticket or a deny marker); refuse anything else before leaving the page.
  const leave = useCallback(
    (url: string) => {
      if (!url.startsWith(authorizeUrl)) {
        setError('The server answered with an unexpected address. Nothing was sent to the application.');
        setWorking(false);
        return;
      }
      window.location.assign(url);
    },
    [authorizeUrl]
  );

  const decide = useCallback(
    async (approve: boolean) => {
      setWorking(true);
      setError(null);
      try {
        leave(approve ? await oauthSvc.approve(authorizeUrl) : await oauthSvc.deny(authorizeUrl));
      } catch (e) {
        setError(e instanceof Error ? e.message : String(e));
        setWorking(false);
      }
    },
    [authorizeUrl, leave]
  );

  return (
    <div className={`w-full h-full min-h-screen flex items-center justify-center p-4 ${theme.mainContentSection}`}>
      <div className={`w-full max-w-[480px] flex flex-col gap-3 p-6 border rounded-md ${theme.mainContentSection}`}>
        <div className={`text-lg font-semibold ${theme.title}`}>
          <i className="fa-solid fa-plug mr-2" aria-hidden="true" />
          Connect an AI application
        </div>

        {error && <div className="text-sm text-red-600">{error}</div>}
        {!info && !error && <div className={`text-sm ${theme.label}`}>Checking the request...</div>}

        {info && (
          <>
            <div className={`text-sm ${theme.label}`}>
              <b>{info.ClientName}</b> wants to use AppAI as <b>{info.UserName}</b>.
            </div>
            <ul className={`text-xs list-disc pl-5 flex flex-col gap-1 ${theme.label}`}>
              <li>It can search and call the AppAI APIs that your security groups allow, with your own permissions.</li>
              <li>Everything it does is recorded under your name.</li>
              <li>
                After you decide, your browser returns to <b>{info.RedirectHost}</b>. Continue only if that is the application you started.
              </li>
              <li>You can disconnect it later from your connections list.</li>
            </ul>
            <div className="flex items-center gap-2 pt-2">
              <button
                type="button"
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                disabled={working}
                onClick={() => decide(true)}
              >
                <i className="fa-solid fa-check" aria-hidden="true" /> Allow
              </button>
              <button
                type="button"
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                disabled={working}
                onClick={() => decide(false)}
              >
                <i className="fa-solid fa-xmark" aria-hidden="true" /> Deny
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  );
};

export default McpAuthorize;
