const base = '/api/v1'
async function req(path, opts = {}) {
  const res = await fetch(base + path, {
    headers: { 'Content-Type': 'application/json' }, credentials: 'same-origin',
    ...opts, body: opts.body ? JSON.stringify(opts.body) : undefined
  })
  const text = await res.text(); const data = text ? JSON.parse(text) : null
  if (!res.ok) throw new Error(data?.error || `Lỗi ${res.status}`)
  return { data, cache: res.headers.get('X-Cache') }
}
export const api = {
  dashboard: () => req('/dashboard'),
  routes: () => req('/routes'),
  saveRoute: (b) => req('/routes', { method: 'POST', body: b }),
  toggleRoute: (id) => req(`/routes/${id}/toggle`, { method: 'POST' }),
  clients: () => req('/clients'),
  createClient: (b) => req('/clients', { method: 'POST', body: b }),
  logs: (take = 100) => req(`/logs?take=${take}`)
}
export const fmtDateTime = (s) => s ? new Date(s).toLocaleString('vi-VN') : '—'
// Gọi thẳng proxy /gw để thử nghiệm
export async function callGw(prefix, path, apiKey) {
  const headers = apiKey ? { 'X-Api-Key': apiKey } : {}
  const t0 = performance.now()
  const res = await fetch(`/gw/${prefix}/${path}`, { headers })
  const body = await res.text()
  return { status: res.status, ms: Math.round(performance.now() - t0), route: res.headers.get('X-Gateway-Route'), body: body.slice(0, 500) }
}
