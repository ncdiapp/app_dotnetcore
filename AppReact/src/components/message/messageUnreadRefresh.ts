/** Fired after read-state or inbox-changing operations so the header badge can refresh immediately. */
export const APP_AI_REFRESH_UNREAD_MESSAGES = 'appai-refresh-unread-messages';

export function requestUnreadMessageCountRefresh(): void {
  if (typeof window !== 'undefined') {
    window.dispatchEvent(new Event(APP_AI_REFRESH_UNREAD_MESSAGES));
  }
}
