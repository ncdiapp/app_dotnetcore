import {
  ensureSessionHeader,
  isAbsoluteHttpUrl,
  joinApiUrl,
  missingPathPlaceholders,
  parseApiConfig,
  queryParamValue,
  serializeApiConfig,
  splitApiUrl,
} from './apiConfigForm';

describe('apiConfigForm', () => {
  const sample = `{
    "BaseUrl": "",
    "Url": "",
    "Headers": { "CurrentUserSessionId": null },
    "QueryParams": { "id": 2 },
    "PathParams": {},
    "PostProcessMethodName": null,
    "ResponseObjectMapToEnvionmentVariable": {},
    "ResponseHeaderNeedToSetCookieNames": [],
    "SpParameters": [{ "Name": "@Id" }]
  }`;

  test('loads a data model config into fields', () => {
    const model = parseApiConfig(sample);
    expect(model.apiUrl).toBe('');
    expect(model.headers[0]).toEqual({ key: 'CurrentUserSessionId', value: '' });
    expect(model.queryParams).toEqual([{ key: 'id', value: '2' }]);
    expect(queryParamValue(model, 'id')).toBe('2');
  });

  test('save writes id as a string, session header as null, and keeps unknown keys', () => {
    const saved = serializeApiConfig(parseApiConfig(sample));
    const obj = saved.object;
    expect(obj.QueryParams).toEqual({ id: '2' });
    expect((obj.Headers as any).CurrentUserSessionId).toBeNull();
    expect(obj.SpParameters).toEqual([{ Name: '@Id' }]);
    expect(obj.ResponseObjectMapToEnvionmentVariable).toEqual({});
    expect(obj.ResponseHeaderNeedToSetCookieNames).toEqual([]);
  });

  test('adds the session header when it is missing', () => {
    const model = parseApiConfig({ BaseUrl: '', Url: '', Headers: {}, QueryParams: {} });
    expect(model.headers[0].key).toBe('CurrentUserSessionId');
    const saved = serializeApiConfig(model);
    expect((saved.object.Headers as any).CurrentUserSessionId).toBeNull();
  });

  test('splits an absolute URL into host and path', () => {
    const split = splitApiUrl('https://shop.example.com/admin/orders/{{PathParams.id}}');
    expect(split.baseUrl).toBe('https://shop.example.com');
    expect(split.url).toBe('admin/orders/{{PathParams.id}}');
    const model = parseApiConfig({});
    model.apiUrl = 'https://shop.example.com/admin/orders/{{PathParams.id}}';
    model.pathParams = [{ key: 'id', value: '1' }];
    const saved = serializeApiConfig(model);
    expect(saved.object.BaseUrl).toBe('https://shop.example.com');
    expect(saved.object.Url).toBe('admin/orders/{{PathParams.id}}');
    expect(joinApiUrl(saved.object.BaseUrl, saved.object.Url)).toBe(model.apiUrl);
    expect(missingPathPlaceholders(model)).toEqual([]);
  });

  test('moves a path prefix from BaseUrl into Url on the next save', () => {
    const model = parseApiConfig({ BaseUrl: 'https://host/api', Url: 'orders' });
    expect(model.apiUrl).toBe('https://host/api/orders');
    const saved = serializeApiConfig(model);
    expect(saved.object.BaseUrl).toBe('https://host');
    expect(saved.object.Url).toBe('api/orders');
  });

  test('keeps a non-http URL entirely in Url', () => {
    const saved = serializeApiConfig({ ...parseApiConfig({}), apiUrl: '{{Env.ApiRoot}}/orders' });
    expect(saved.object.BaseUrl).toBe('');
    expect(saved.object.Url).toBe('{{Env.ApiRoot}}/orders');
    expect(isAbsoluteHttpUrl('{{Env.ApiRoot}}/orders')).toBe(false);
    expect(isAbsoluteHttpUrl('https://shop.example.com/a')).toBe(true);
  });

  test('merges a query string from the URL into Query Params', () => {
    const model = parseApiConfig({ QueryParams: { id: '1' } });
    model.apiUrl = 'https://shop.example.com/orders?status=open';
    const saved = serializeApiConfig(model);
    expect(saved.object.Url).toBe('orders');
    expect(saved.object.QueryParams).toEqual({ id: '1', status: 'open' });
  });

  test('ensureSessionHeader does not replace an existing value', () => {
    const cfg = ensureSessionHeader({ Headers: { CurrentUserSessionId: 'abc' }, SpName: 'dbo.Get' });
    expect((cfg.Headers as any).CurrentUserSessionId).toBe('abc');
    expect(cfg.SpName).toBe('dbo.Get');
    const added = ensureSessionHeader({ SpName: 'dbo.Get' });
    expect((added.Headers as any).CurrentUserSessionId).toBeNull();
  });
});
