import { useEffect, useState } from 'react';
import { integrationService } from '../../webapi/integrationsvc';

/** Server root (scheme and host) for API URLs shown and called from the editors. */
export function useApiServerRoot(): string {
  const [root, setRoot] = useState('');
  useEffect(() => {
    let cancelled = false;
    integrationService.loadApiServerRoot()
      .then((value) => {
        if (!cancelled) setRoot(value);
      })
      .catch(() => {
        if (!cancelled) setRoot('');
      });
    return () => {
      cancelled = true;
    };
  }, []);
  return root;
}
