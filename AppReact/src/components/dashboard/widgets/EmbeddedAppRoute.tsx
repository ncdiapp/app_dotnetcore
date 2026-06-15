import React, { useMemo } from 'react';
import { Routes, Route, useLocation } from 'react-router-dom';
import { EMBEDDABLE_ROUTES } from '../../../routes.shared';

type Props = {
  /** A path within this app, e.g. `/MasterDataManagement/%7B...%7D` */
  initialPath: string;
  /** Rendered when no route matches. */
  fallback?: React.ReactNode;
};

/**
 * Render internal app pages as components (NOT iframe).
 * Uses an isolated MemoryRouter so it won't affect the main app router.
 */
export function EmbeddedAppRoute({ initialPath, fallback }: Props) {
  const parentLocation = useLocation();

  const normalizedPath = useMemo(() => {
    const p = (initialPath || '').trim();
    if (!p) return '/';
    return p.startsWith('/') ? p : `/${p}`;
  }, [initialPath]);

  // React Router v6 requires overridden location to begin with the parent route's matched base.
  // We satisfy this by prefixing with the current pathname (parent base), and then matching
  // child routes relative to that base.
  const effectiveLocation = useMemo(() => {
    const base = (parentLocation.pathname || '/').replace(/\/$/, '');
    const suffix = normalizedPath === '/' ? '' : normalizedPath;
    return `${base}${suffix}` || '/';
  }, [parentLocation.pathname, normalizedPath]);

  const notFound =
    fallback ?? (
      <div className="w-full h-full p-4 text-sm text-slate-600">
        Unsupported embedded route: <code className="text-slate-900">{normalizedPath}</code>
      </div>
    );

  return (
    <div className="w-full h-full overflow-hidden">
      {/* IMPORTANT: Do NOT create a nested Router (MemoryRouter) here.
          React Router v6 forbids rendering a Router inside another Router.
          Instead, we render Routes with an overridden location. */}
      <Routes location={effectiveLocation}>
        {EMBEDDABLE_ROUTES.map((r) => (
          <Route
            key={r.path}
            // IMPORTANT: child paths must be relative to the parent base
            path={r.path.replace(/^\//, '')}
            element={r.element}
          />
        ))}

        <Route path="*" element={<>{notFound}</>} />
      </Routes>
    </div>
  );
}

