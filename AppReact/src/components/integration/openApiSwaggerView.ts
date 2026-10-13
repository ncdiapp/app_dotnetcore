import { getLoginSessionId } from '../../helper/apiServiceHelper';

/** Open Swagger UI in a new tab. Session stays in the hash so it is not part of the spec URL. */
export function openOpenApiSwaggerView(serverRoot: string, doc: { Id: number; Code?: string; Status?: string }): void {
  const root = (serverRoot || '').replace(/\/$/, '');
  if (!root || !doc.Id) return;
  const spec = doc.Status === 'Published' && doc.Code
    ? `${root}/openapi-doc/${doc.Code}`
    : `${root}/appai/webapi/OpenApiDocument/Spec?id=${doc.Id}`;
  const page = `${root}/appai/openapi-ui?spec=${encodeURIComponent(spec)}`;
  const sid = getLoginSessionId();
  const hash = sid ? `#sid=${encodeURIComponent(sid)}` : '';
  window.open(`${page}${hash}`, '_blank', 'noopener,noreferrer');
}
