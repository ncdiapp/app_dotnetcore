import React from 'react';
import { buildRoutePathFromParamObj, buildParamObjFromRouteCodeAndLink, getReactPathForRouteCode } from '../../../helper/navigationHelper';
import { EmbeddedAppRoute } from './EmbeddedAppRoute';

type Props = {
  item: any;
  // Use loose types here because theme keys are strongly typed in the app.
  theme: any;
  t: any;
  onInternalShortcut: (stateName: string, menuLinkStr: string, menuName?: string) => void;
  onSearchShortcut: (searchId: string, isSavedSearchParameter?: boolean, displayTitle?: string) => void;
};

function isExternalUrl(url: string): boolean {
  return /^https?:\/\//i.test(url);
}

function isSameOriginAbsoluteUrl(url: string): boolean {
  try {
    const u = new URL(url);
    return u.origin === window.location.origin;
  } catch {
    return false;
  }
}

function looksLikeInternalPath(value: string): boolean {
  const v = value.trim();
  // Treat only explicit paths as internal. Avoid misclassifying IDs like "8600".
  return (
    v.startsWith('/') ||
    v.startsWith('./') ||
    v.startsWith('../') ||
    v.startsWith('#') ||
    v.startsWith('?')
  );
}

function toInternalPath(urlOrPath: string): string {
  // - If it's relative: return as-is (ensure leading slash)
  // - If it's same-origin absolute: strip origin, keep pathname+search+hash
  try {
    const u = new URL(urlOrPath, window.location.origin);
    if (u.origin === window.location.origin) {
      const path = `${u.pathname}${u.search}${u.hash}`;
      return path.startsWith('/') ? path : `/${path}`;
    }
  } catch {
    // ignore
  }
  return urlOrPath.startsWith('/') ? urlOrPath : `/${urlOrPath}`;
}

function normalizeLinkType(linkType: any): 'SystemPage' | 'WebPopup' | 'Unknown' {
  // API sometimes returns 0/1 or "0"/"1" or string enums
  if (linkType === 0 || linkType === '0' || String(linkType).toLowerCase() === 'systempage') return 'SystemPage';
  if (linkType === 1 || linkType === '1' || String(linkType).toLowerCase() === 'webpopup') return 'WebPopup';
  return 'Unknown';
}

function buildRoutePath(routeCode: string, paramObj?: any): string {
  const reactPath = getReactPathForRouteCode(routeCode);
  const route = reactPath.startsWith('/') ? reactPath : `/${reactPath}`;
  return buildRoutePathFromParamObj(route, paramObj);
}

export function DashboardWidgetRenderer({ item, theme, t, onInternalShortcut, onSearchShortcut }: Props) {
  const widgetType = item?.WidgetItemType;
  const displayName = item?.DisplayTitle || item?.DisplayName || 'Widget';

  switch (widgetType) {
    case 1: {
      // ExternalPage: iframe ONLY for real external URLs.
      // If the payload is relative or same-origin, render as embedded route (no iframe).
      const url = item?.ExternalUrl;
      if (typeof url === 'string' && url.trim()) {
        const trimmed = url.trim();
        if (isSameOriginAbsoluteUrl(trimmed) || looksLikeInternalPath(trimmed)) {
          return <EmbeddedAppRoute initialPath={toInternalPath(trimmed)} />;
        }
        // If it's not a URL and not a path, it might actually be a PARAM for an internal route.
        // In that case, try to use InternalPageRoute or DomElementTag as the route code.
        const maybeRouteCode = (item?.InternalPageRoute || item?.DomElementTag || '').toString().trim();
        if (maybeRouteCode) {
          const paramObj = buildParamObjFromRouteCodeAndLink(maybeRouteCode, trimmed);
          const routePath = buildRoutePath(maybeRouteCode, paramObj);
          return <EmbeddedAppRoute initialPath={routePath} />;
        }
        // Non-http(s) and not a path: treat as invalid url, do not embed
        if (!isExternalUrl(trimmed)) {
          return (
            <div className={`w-full h-full p-4 ${theme.mainContentSection}`}>
              <div className={`text-sm font-semibold mb-2 ${t('text_title')}`}>{displayName}</div>
              <div className={`text-xs ${t('text_default')}`}>
                ExternalPage Widget: invalid URL <code>{trimmed}</code>
              </div>
            </div>
          );
        }
      }
      return (
        <div className="w-full h-full flex flex-col">
          {displayName && (
            <div className={`px-4 py-2 border-b ${t('border_default')} ${theme.mainHeader}`}>
              <h3 className={`text-sm font-semibold ${t('text_title')}`}>{displayName}</h3>
            </div>
          )}
          <div className="flex-auto overflow-hidden">
            <iframe
              src={url}
              className="w-full h-full border-0"
              title={displayName}
              sandbox="allow-same-origin allow-scripts allow-forms allow-popups"
            />
          </div>
        </div>
      );
    }

    case 2: {
      // InternalPage — render component directly (NO iframe)
      // Prefer LinkToListMenu when present
      if (item?.LinkToListMenu) {
        const linkMenu = item.LinkToListMenu;
        const linkTypeNorm = normalizeLinkType(linkMenu.LinkType);
        const routeCode = (linkMenu.RouteCode || '').toString().trim();
        const linkParamRaw = (linkMenu.Link || '').toString().trim();

        const looksLikeParamOnly =
          !!linkParamRaw &&
          !looksLikeInternalPath(linkParamRaw) &&
          !isExternalUrl(linkParamRaw) &&
          !isSameOriginAbsoluteUrl(linkParamRaw);

        // If we have a RouteCode and the "Link" doesn't look like a URL/path, treat it as a param.
        // (Your dashboard widgets often pass "8600" / "8595" which are params for the component.)
        if (routeCode && looksLikeParamOnly) {
          const paramObj = buildParamObjFromRouteCodeAndLink(routeCode, linkParamRaw);
          const routePath = buildRoutePath(routeCode, paramObj);
          return <EmbeddedAppRoute initialPath={routePath} />;
        }

        if (linkTypeNorm === 'SystemPage') {
          const paramObj = buildParamObjFromRouteCodeAndLink(routeCode, linkParamRaw);
          const routePath = buildRoutePath(routeCode, paramObj);
          return <EmbeddedAppRoute initialPath={routePath} />;
        }

        if (linkTypeNorm === 'WebPopup') {
          // WebPopup: iframe ONLY for real external URLs.
          // If it's relative/same-origin, render embedded route to avoid loading another SPA instance.
          const url = linkMenu.Link;
          if (typeof url === 'string' && url.trim()) {
            const trimmed = url.trim();
            if (isSameOriginAbsoluteUrl(trimmed) || looksLikeInternalPath(trimmed)) {
              return <EmbeddedAppRoute initialPath={toInternalPath(trimmed)} />;
            }
            // Non-http(s) and not a path: treat as invalid url, do not embed
            if (!isExternalUrl(trimmed)) {
              return (
                <div className={`w-full h-full p-4 ${theme.mainContentSection}`}>
                  <div className={`text-sm font-semibold mb-2 ${t('text_title')}`}>{linkMenu.Name || displayName}</div>
                  <div className={`text-xs ${t('text_default')}`}>
                    WebPopup Widget: invalid URL <code>{trimmed}</code>
                  </div>
                </div>
              );
            }
          }
          return (
            <div className="w-full h-full flex flex-col">
              {linkMenu.Name && (
                <div className={`px-4 py-2 border-b ${t('border_default')} ${theme.mainHeader}`}>
                  <label className={`text-sm font-semibold ${t('text_title')}`}>{linkMenu.Name}</label>
                </div>
              )}
              <div className="flex-auto overflow-hidden">
                <iframe src={url} className="w-full h-full border-0" title={linkMenu.Name || displayName} />
              </div>
            </div>
          );
        }
      }

      // Some payloads put the target in DomElementTag (e.g. MasterDataManagement)
      if (item?.DomElementTag) {
        const params =
          item.InternalPageParams != null && Object.keys(item.InternalPageParams).length > 0
            ? item.InternalPageParams
            : item.ParameterKeyValue != null && String(item.ParameterKeyValue).trim() !== ''
              ? buildParamObjFromRouteCodeAndLink(String(item.DomElementTag), String(item.ParameterKeyValue))
              : undefined;
        const routePath = buildRoutePath(String(item.DomElementTag), params ?? {});
        return <EmbeddedAppRoute initialPath={routePath} />;
      }

      // Fallback: try InternalPageRoute
      const internalRoute = item?.InternalPageRoute;
      if (internalRoute && typeof internalRoute === 'string' && !isExternalUrl(internalRoute)) {
        const params =
          item.InternalPageParams != null && Object.keys(item.InternalPageParams).length > 0
            ? item.InternalPageParams
            : item.ParameterKeyValue != null && String(item.ParameterKeyValue).trim() !== ''
              ? buildParamObjFromRouteCodeAndLink(internalRoute, String(item.ParameterKeyValue))
              : {};
        const routePath = buildRoutePath(internalRoute, params);
        return <EmbeddedAppRoute initialPath={routePath} />;
      }

      return (
        <div className={`w-full h-full p-4 ${theme.mainContentSection}`}>
          <div className={`text-sm font-semibold ${t('text_title')}`}>{displayName}</div>
          <div className={`text-xs mt-2 ${t('text_default')}`}>Internal page widget: unsupported payload</div>
        </div>
      );
    }

    case 3:
    case 4: {
      // Directive widgets — render component directly
      const directiveName: string = (item?.DomElementTag || item?.DirectiveName || '').toString();
      const lower = directiveName.toLowerCase();

      // Search (AppSearch)
      if (lower === 'appsearch' || lower === 'search' || lower === 'masterdatamanagement') {
        const searchId = item?.TargetId || item?.ParameterKeyValue || item?.SearchId;
        const isSavedSearch = item?.IsSavedSearchParameter === true || item?.IsSavedSearchParameter === 'true';

        if (searchId) {
          const routePath = buildRoutePath('MasterDataManagement', { searchId, isSavedSearch });
          return <EmbeddedAppRoute initialPath={routePath} />;
        }
        return (
          <div className={`w-full h-full p-4 ${theme.mainContentSection}`}>
            <div className={`text-sm font-semibold mb-2 ${t('text_title')}`}>{displayName}</div>
            <div className={`text-xs ${t('text_default')}`}>Search Widget: No SearchId specified</div>
          </div>
        );
      }

      // MyApplications
      if (lower === 'myapplications' || lower === 'application') {
        const routePath = buildRoutePath('my-applications');
        return <EmbeddedAppRoute initialPath={routePath} />;
      }

      // Unknown directive
      return (
        <div className={`w-full h-full p-4 ${theme.mainContentSection}`}>
          <div className={`text-sm font-semibold mb-2 ${t('text_title')}`}>{displayName}</div>
          <div className={`text-xs ${t('text_default')}`}>Directive: {directiveName || 'Unknown'}</div>
        </div>
      );
    }

    case 5: {
      // ExternalShortcut
      return (
        <div className={`w-full h-full p-4 flex flex-col items-center justify-center ${theme.mainContentSection}`}>
          <a
            href={item?.ExternalUrl}
            target="_blank"
            rel="noopener noreferrer"
            className={`${theme.button_default} px-4 py-2 rounded-lg hover:opacity-80 transition-opacity flex items-center gap-2`}
          >
            <i className="fa-solid fa-external-link" aria-hidden="true"></i>
            <span>{displayName}</span>
          </a>
        </div>
      );
    }

    case 6: {
      // InternalShortcut (keep as navigation; not iframe)
      if (item?.LinkToListMenu) {
        const linkMenu = item.LinkToListMenu;
        const linkType = linkMenu.LinkType;

        if (linkType === 0 || linkType === 'SystemPage') {
          const linkParam = linkMenu.Link || '';
          return (
            <div
              className={`w-full h-full p-4 flex flex-col items-center justify-center ${theme.mainContentSection}`}
              style={{ cursor: 'pointer', position: 'relative' }}
              onClick={() => onInternalShortcut(linkMenu.RouteCode || '', linkParam, linkMenu.Name || displayName)}
            >
              <div
                style={{
                  width: '100px',
                  height: '82px',
                  textAlign: 'center',
                  position: 'absolute',
                  top: 0,
                  bottom: 0,
                  left: 0,
                  right: 0,
                  margin: 'auto',
                }}
              >
                {linkMenu.ImageUrl && (
                  <img
                    className="DesktopShortcutItemImage"
                    style={{ height: '48px', maxWidth: '100px', maxHeight: '80px' }}
                    src={linkMenu.ImageUrl}
                    alt={linkMenu.Name}
                  />
                )}
                <div className="DesktopShortcutItemLabel">{linkMenu.Name || displayName}</div>
              </div>
            </div>
          );
        }

        if (linkType === 1 || linkType === 'WebPopup') {
          const linkUrl = linkMenu.Link || '';
          const routeCode = (linkMenu.RouteCode || '').toString().trim();
          
          // If RouteCode exists, treat as internal route and use internal navigation
          // Otherwise, check if Link is an external URL
          if (routeCode) {
            // Internal route - use internal navigation (open app tab)
            const linkParam = linkUrl || '';
            return (
              <div
                className={`w-full h-full p-4 flex flex-col items-center justify-center ${theme.mainContentSection}`}
                style={{ cursor: 'pointer', position: 'relative' }}
                onClick={() => onInternalShortcut(routeCode, linkParam, linkMenu.Name || displayName)}
              >
                <div
                  style={{
                    width: '100px',
                    height: '82px',
                    textAlign: 'center',
                    position: 'absolute',
                    top: 0,
                    bottom: 0,
                    left: 0,
                    right: 0,
                    margin: 'auto',
                  }}
                >
                  {linkMenu.ImageUrl && (
                    <img
                      className="DesktopShortcutItemImage"
                      style={{ height: '48px', maxWidth: '100px', maxHeight: '80px' }}
                      src={linkMenu.ImageUrl}
                      alt={linkMenu.Name}
                    />
                  )}
                  <div className="DesktopShortcutItemLabel">{linkMenu.Name || displayName}</div>
                </div>
              </div>
            );
          }
          
          // Only open in new window if it's an external URL (http/https)
          // If Link is not an external URL and no RouteCode, treat as internal route
          if (!isExternalUrl(linkUrl)) {
            // Not an external URL - try to use as internal route if possible
            const fallbackRouteCode = linkMenu.RouteCode || item?.InternalPageRoute || item?.DomElementTag || '';
            if (fallbackRouteCode) {
              return (
                <div
                  className={`w-full h-full p-4 flex flex-col items-center justify-center ${theme.mainContentSection}`}
                  style={{ cursor: 'pointer', position: 'relative' }}
                  onClick={() => onInternalShortcut(fallbackRouteCode.toString(), linkUrl, linkMenu.Name || displayName)}
                >
                  <div
                    style={{
                      width: '100px',
                      height: '82px',
                      textAlign: 'center',
                      position: 'absolute',
                      top: 0,
                      bottom: 0,
                      left: 0,
                      right: 0,
                      margin: 'auto',
                    }}
                  >
                    {linkMenu.ImageUrl && (
                      <img
                        className="DesktopShortcutItemImage"
                        style={{ height: '48px', maxWidth: '100px', maxHeight: '80px' }}
                        src={linkMenu.ImageUrl}
                        alt={linkMenu.Name}
                      />
                    )}
                    <div className="DesktopShortcutItemLabel">{linkMenu.Name || displayName}</div>
                  </div>
                </div>
              );
            }
          }
          
          // External URL - open in new window
          return (
            <a
              target="_blank"
              href={linkUrl}
              className={`w-full h-full p-4 flex flex-col items-center justify-center ${theme.mainContentSection}`}
              style={{ cursor: 'pointer', position: 'relative', textDecoration: 'none' }}
              rel="noreferrer"
            >
              <div
                style={{
                  width: '100px',
                  height: '82px',
                  textAlign: 'center',
                  position: 'absolute',
                  top: 0,
                  bottom: 0,
                  left: 0,
                  right: 0,
                  margin: 'auto',
                }}
              >
                {linkMenu.ImageUrl && (
                  <img
                    className="DesktopShortcutItemImage"
                    style={{ height: '48px', maxWidth: '100px', maxHeight: '80px' }}
                    src={linkMenu.ImageUrl}
                    alt={linkMenu.Name}
                  />
                )}
                <div className="DesktopShortcutItemLabel">{linkMenu.Name || displayName}</div>
              </div>
            </a>
          );
        }
      }

      // Fallback
      return (
        <div className={`w-full h-full p-4 flex flex-col items-center justify-center ${theme.mainContentSection}`}>
          <button
            onClick={() => onInternalShortcut(item?.InternalStateName, item?.InternalLinkStr, item?.DisplayName)}
            className={`${theme.button_default} px-4 py-2 rounded-lg hover:opacity-80 transition-opacity flex items-center gap-2`}
          >
            <i className="fa-solid fa-link" aria-hidden="true"></i>
            <span>{displayName}</span>
          </button>
        </div>
      );
    }

    case 7: {
      // Search Widget (navigation)
      return (
        <div className={`w-full h-full p-4 flex flex-col items-center justify-center ${theme.mainContentSection}`}>
          <button
            onClick={() => onSearchShortcut(item?.SearchId, item?.IsSavedSearchParameter, item?.DisplayName)}
            className={`${theme.button_default} px-4 py-2 rounded-lg hover:opacity-80 transition-opacity flex items-center gap-2`}
          >
            <i className="fa-solid fa-search" aria-hidden="true"></i>
            <span>{displayName}</span>
          </button>
        </div>
      );
    }

    default: {
      return (
        <div className={`w-full h-full p-4 ${theme.mainContentSection}`}>
          <div className={`text-sm font-semibold ${t('text_title')}`}>Unknown Widget Type</div>
          <div className={`text-xs mt-2 ${t('text_default')}`}>Widget Type: {String(widgetType)}</div>
          <div className={`text-xs mt-1 ${t('text_default')}`}>Properties: {JSON.stringify(Object.keys(item || {}))}</div>
        </div>
      );
    }
  }
}

