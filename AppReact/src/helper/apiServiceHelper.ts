import { store, RootState } from '../redux/store';

export const getHeaders = () => {
        const userContext = (store.getState() as RootState).userSession.userContext;

    const headers = new Headers();
    headers.append('Content-Type', 'application/json');

    if (userContext?.SessionId) {
        headers.append('CurrentUserSessionId', userContext.SessionId);
    }

    return headers;
}

/** Login session used when an API config CurrentUserSessionId is null or blank. */
export function getLoginSessionId(): string {
    const fromStore = (store.getState() as RootState).userSession.userContext?.SessionId;
    if (fromStore) return String(fromStore);
    try {
        return localStorage.getItem('sessionId') || '';
    } catch {
        return '';
    }
}

/**
 * Headers for an API test call.
 * A null or blank CurrentUserSessionId in config keeps the current login session.
 * A non-blank config value is sent instead.
 */
export function buildApiTestHeaders(configHeaders?: Record<string, unknown> | null): Headers {
    const headers = new Headers(getHeaders());
    let configSession = '';
    let hasSessionKey = false;
    Object.entries(configHeaders ?? {}).forEach(([key, value]) => {
        if (key.toLowerCase() === 'currentusersessionid') {
            hasSessionKey = true;
            configSession = value == null ? '' : String(value).trim();
            return;
        }
        if (value == null || String(value).trim() === '') return;
        headers.set(key, String(value));
    });
    const sessionId = !hasSessionKey || configSession === '' ? getLoginSessionId() : configSession;
    if (sessionId) {
        headers.set('CurrentUserSessionId', sessionId);
        document.cookie = `CurrentUserSessionId=${sessionId}; path=/`;
    }
    return headers;
}


export const getHeadersWithAuth = (authValue?: string) => {
    if (authValue) {
        return {
            'Authorization': authValue,
        };
    }
}