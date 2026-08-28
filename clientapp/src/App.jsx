import React, { useEffect, useState } from 'react'
import { Routes, Route, NavLink, Outlet } from 'react-router-dom'
import { api, fmtDateTime, callGw } from './api'

function Badge({ text, css }) { return <span className={`badge ${css || 'secondary'}`}>{text}</span> }
function Flash({ msg }) { return msg ? <div className={`flash ${msg.ok ? 'ok' : 'err'}`}>{msg.text}</div> : null }
function Modal({ title, onClose, wide, children }) {
  return (
    <div className="modal-bg" onClick={onClose}>
      <div className="modal" style={wide ? { maxWidth: 640 } : undefined} onClick={e => e.stopPropagation()}>
        <div className="row" style={{ marginBottom: 12 }}><h2 style={{ flex: 1, margin: 0 }}>{title}</h2>
          <button className="btn gray sm" style={{ flex: 'none' }} onClick={onClose}>Đóng</button></div>{children}
      </div>
    </div>
  )
}
function Field({ label, children }) { return <div style={{ flex: 1 }}><label>{label}</label>{children}</div> }
function statusCss(c) { return c >= 500 ? 'danger' : c >= 400 ? 'warning' : c >= 200 && c < 300 ? 'success' : 'secondary' }

function Layout() {
  return (
    <>
      <nav className="nav"><span className="brand">🚪 MiniGate</span>
        <NavLink to="/" end>Tổng quan</NavLink><NavLink to="/routes">Tuyến</NavLink>
        <NavLink to="/clients">Client</NavLink><NavLink to="/logs">Nhật ký</NavLink><NavLink to="/playground">Thử nghiệm</NavLink></nav>
      <div className="wrap"><Outlet /></div>
    </>
  )
}

function Dashboard() {
  const [d, setD] = useState(null); const [cache, setCache] = useState('')
  useEffect(() => { api.dashboard().then(r => { setD(r.data); setCache(r.cache) }) }, [])
  if (!d) return <p className="muted">Đang tải…</p>
  const maxR = Math.max(1, ...d.topRoutes.map(x => x.count))
  return (
    <>
      <h1>Tổng quan Gateway {cache && <span className="pill">cache: {cache}</span>}</h1>
      <div className="grid kpis" style={{ marginBottom: 18 }}>
        <div className="kpi"><div className="v">{d.routes} <span style={{ fontSize: 14 }} className="muted">({d.activeRoutes} bật)</span></div><div className="l">Tuyến</div></div>
        <div className="kpi"><div className="v">{d.clients}</div><div className="l">Client</div></div>
        <div className="kpi"><div className="v">{d.requests24h}</div><div className="l">Request 24h</div></div>
        <div className="kpi"><div className="v">{d.avgLatency}ms</div><div className="l">Độ trễ TB</div></div>
      </div>
      <div className="grid" style={{ gridTemplateColumns: '1fr 1fr' }}>
        <div className="card"><h2>Top tuyến (24h)</h2><div className="funnel">{d.topRoutes.map((x, i) => (<div className="bar" key={i}><div className="lbl">{x.route}</div><div className="track"><div className="fill" style={{ width: `${(x.count / maxR) * 100}%` }} /></div><div className="n">{x.count}</div></div>))}</div></div>
        <div className="card"><h2>Theo mã HTTP</h2><table><tbody>{d.byStatus.map((x, i) => <tr key={i}><td><Badge text={x.code} css={statusCss(x.code)} /></td><td className="right">{x.count}</td></tr>)}</tbody></table></div>
      </div>
    </>
  )
}

function RoutesPage() {
  const [rows, setRows] = useState([]); const [edit, setEdit] = useState(null); const [msg, setMsg] = useState(null)
  const load = () => api.routes().then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const toggle = async (id) => { try { await api.toggleRoute(id); load() } catch (e) { setMsg({ ok: false, text: e.message }) } }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 1 }}>Tuyến (route → upstream)</h1><button className="btn sm" style={{ flex: 'none' }} onClick={() => setEdit({ id: 0, name: '', prefix: '', upstreamBaseUrl: '', requireAuth: false, timeoutSeconds: 30 })}>+ Thêm tuyến</button></div>
      <Flash msg={msg} />
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Prefix</th><th>Tên</th><th>Upstream</th><th>Auth</th><th>Trạng thái</th><th></th></tr></thead>
          <tbody>{rows.map(r => (<tr key={r.id}><td><span className="pill">/gw/{r.prefix}</span></td><td>{r.name}</td><td className="muted" style={{ fontSize: 12 }}>{r.upstreamBaseUrl}</td>
            <td>{r.requireAuth ? <Badge text="Cần key" css="warning" /> : <span className="muted">Mở</span>}</td>
            <td><Badge text={r.isActive ? 'Bật' : 'Tắt'} css={r.isActive ? 'success' : 'dark'} /></td>
            <td className="right"><button className="btn ghost sm" style={{ flex: 'none' }} onClick={() => setEdit(r)}>Sửa</button> <button className="btn gray sm" style={{ flex: 'none' }} onClick={() => toggle(r.id)}>{r.isActive ? 'Tắt' : 'Bật'}</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={6} className="muted" style={{ padding: 20 }}>Chưa có tuyến.</td></tr>}</tbody></table>
      </div>
      {edit && <RouteForm r={edit} onClose={() => setEdit(null)} onSaved={() => { setEdit(null); load() }} />}
    </>
  )
}

function RouteForm({ r, onClose, onSaved }) {
  const [f, setF] = useState({ ...r }); const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => { try { if (!f.prefix || !f.upstreamBaseUrl) { setErr('Cần prefix + upstream'); return } await api.saveRoute(f); onSaved() } catch (e) { setErr(e.message) } }
  return (
    <Modal title={f.id ? 'Sửa tuyến' : 'Thêm tuyến'} onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Prefix (sau /gw/)"><input value={f.prefix} onChange={e => up('prefix', e.target.value)} /></Field>
        <Field label="Tên"><input value={f.name} onChange={e => up('name', e.target.value)} /></Field></div>
      <Field label="Upstream base URL"><input value={f.upstreamBaseUrl} onChange={e => up('upstreamBaseUrl', e.target.value)} placeholder="https://minipim.onrender.com" /></Field>
      <div className="row"><Field label="Timeout (giây)"><input type="number" value={f.timeoutSeconds} onChange={e => up('timeoutSeconds', Number(e.target.value))} /></Field></div>
      <label style={{ display: 'flex', gap: 6, alignItems: 'center', marginTop: 8 }}><input type="checkbox" style={{ width: 'auto' }} checked={f.requireAuth} onChange={e => up('requireAuth', e.target.checked)} /> Yêu cầu X-Api-Key</label>
      <div style={{ marginTop: 16 }}><button className="btn" onClick={save}>Lưu tuyến</button></div>
    </Modal>
  )
}

function Clients() {
  const [rows, setRows] = useState([]); const [name, setName] = useState(''); const [rate, setRate] = useState(60); const [msg, setMsg] = useState(null)
  const load = () => api.clients().then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const add = async () => { try { if (!name) return; await api.createClient({ name, rateLimitPerMin: Number(rate) }); setName(''); load() } catch (e) { setMsg({ ok: false, text: e.message }) } }
  return (
    <>
      <h1>API Client</h1><Flash msg={msg} />
      <div className="card"><div className="row">
        <Field label="Tên client"><input value={name} onChange={e => setName(e.target.value)} /></Field>
        <Field label="Rate limit (req/phút)"><input type="number" value={rate} onChange={e => setRate(e.target.value)} /></Field>
        <div style={{ flex: 'none', alignSelf: 'flex-end' }}><button className="btn" onClick={add}>+ Cấp API key</button></div></div></div>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Tên</th><th>API Key</th><th className="right">Rate limit</th><th>Trạng thái</th></tr></thead>
          <tbody>{rows.map(c => (<tr key={c.id}><td>{c.name}</td><td style={{ fontFamily: 'monospace', fontSize: 12 }}>{c.apiKey}</td><td className="right">{c.rateLimitPerMin}/phút</td>
            <td><Badge text={c.isActive ? 'Bật' : 'Tắt'} css={c.isActive ? 'success' : 'dark'} /></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={4} className="muted" style={{ padding: 20 }}>Chưa có client.</td></tr>}</tbody></table>
      </div>
    </>
  )
}

function Logs() {
  const [rows, setRows] = useState([])
  useEffect(() => { api.logs(200).then(r => setRows(r.data)) }, [])
  return (
    <>
      <h1>Nhật ký request</h1>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Thời gian</th><th>Tuyến</th><th>Client</th><th>Method</th><th>Path</th><th className="right">HTTP</th><th className="right">Độ trễ</th></tr></thead>
          <tbody>{rows.map(l => (<tr key={l.id}><td className="muted" style={{ fontSize: 12 }}>{fmtDateTime(l.at)}</td><td>{l.routeName}</td><td>{l.clientName}</td><td>{l.method}</td>
            <td className="muted" style={{ fontSize: 12 }}>{l.path}</td><td className="right"><Badge text={l.statusCode} css={statusCss(l.statusCode)} /></td><td className="right">{l.latencyMs}ms</td></tr>))}
            {rows.length === 0 && <tr><td colSpan={7} className="muted" style={{ padding: 20 }}>Chưa có request.</td></tr>}</tbody></table>
      </div>
    </>
  )
}

function Playground() {
  const [routes, setRoutes] = useState([]); const [f, setF] = useState({ prefix: '', path: '', apiKey: '' }); const [res, setRes] = useState(null); const [err, setErr] = useState(null)
  useEffect(() => { api.routes().then(r => { setRoutes(r.data); if (r.data[0]) setF(s => ({ ...s, prefix: r.data[0].prefix })) }) }, [])
  const call = async () => { try { setErr(null); const r = await callGw(f.prefix, f.path, f.apiKey); setRes(r) } catch (e) { setErr(e.message) } }
  return (
    <>
      <h1>Thử nghiệm gọi qua /gw</h1>
      <div className="card">
        <div className="row"><Field label="Tuyến"><select value={f.prefix} onChange={e => setF({ ...f, prefix: e.target.value })}>{routes.map(r => <option key={r.id} value={r.prefix}>{r.prefix} → {r.upstreamBaseUrl}</option>)}</select></Field>
          <Field label="Đường dẫn con"><input value={f.path} onChange={e => setF({ ...f, path: e.target.value })} placeholder="healthz" /></Field>
          <Field label="X-Api-Key (nếu cần)"><input value={f.apiKey} onChange={e => setF({ ...f, apiKey: e.target.value })} /></Field></div>
        <div style={{ marginTop: 12 }}><button className="btn" onClick={call}>GET /gw/{f.prefix}/{f.path}</button></div>
      </div>
      {err && <Flash msg={{ ok: false, text: err }} />}
      {res && (
        <div className="card">
          <div className="row" style={{ marginBottom: 8 }}><Badge text={`HTTP ${res.status}`} css={statusCss(res.status)} /><span className="pill" style={{ flex: 'none' }}>{res.ms}ms</span>{res.route && <span className="pill" style={{ flex: 'none' }}>route: {res.route}</span>}</div>
          <pre style={{ background: '#f8fafc', padding: 12, borderRadius: 8, overflow: 'auto', fontSize: 12, maxHeight: 300 }}>{res.body}</pre>
        </div>
      )}
    </>
  )
}

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<Dashboard />} />
        <Route path="routes" element={<RoutesPage />} />
        <Route path="clients" element={<Clients />} />
        <Route path="logs" element={<Logs />} />
        <Route path="playground" element={<Playground />} />
      </Route>
    </Routes>
  )
}
