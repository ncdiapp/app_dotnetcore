// Lets a page that needs a signed-in user (the AI-application consent page) survive the login screen: the page
// remembers where the user was heading, and the login returns there instead of the default landing page.
const KEY = 'postLoginPath';

// Only these in-app pages may be returned to. The value is a path inside this application, never a full URL.
const ALLOWED_PREFIXES = ['/mcp-authorize'];

export function rememberPostLoginPath(path: string): void {
  try {
    sessionStorage.setItem(KEY, path);
  } catch {
    // storage unavailable: the user simply lands on the default page after login
  }
}

/** The remembered path (and forgets it), or null when there is none or it is not an allowed page. */
export function takePostLoginPath(): string | null {
  try {
    const value = sessionStorage.getItem(KEY);
    sessionStorage.removeItem(KEY);
    if (value && ALLOWED_PREFIXES.some((p) => value === p || value.startsWith(p + '?'))) return value;
  } catch {
    // storage unavailable
  }
  return null;
}
