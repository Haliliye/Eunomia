import { HubConnectionBuilder, HttpTransportType, LogLevel, type HubConnection } from '@microsoft/signalr'

let connection: HubConnection | null = null
let startPromise: Promise<void> | null = null

// One shared connection for the whole app (notifications + board updates
// both ride on it) — see backend/src/TodoApp.Api/Realtime/AppHub.cs.
//
// No accessTokenFactory here — the access token lives in an httpOnly cookie
// now, not somewhere JS can read it. withCredentials makes SignalR's
// negotiate call (and each subsequent request) send that cookie
// automatically, the same way axios does for regular API calls.
//
// transport: LongPolling only — deliberately excludes WebSockets and even
// Server-Sent Events. This deployment proxies /hubs through Vercel's
// rewrites (see frontend/vercel.json) to keep the auth cookie same-origin,
// and Vercel's rewrite-to-external-URL mechanism only proxies ordinary
// request/response HTTP calls, not a persistent upgraded connection —
// a WebSocket handshake through it comes back as a plain 200, not the 101
// Switching Protocols the client expects, so the upgrade fails outright.
// Long Polling is just repeated ordinary HTTP requests, which the proxy
// handles fine; it's less efficient than a real WebSocket but reliable here.
export function getRealtimeConnection(): HubConnection {
  if (!connection) {
    connection = new HubConnectionBuilder()
      .withUrl(`${(import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:5001/api').replace(/\/api$/, '')}/hubs/app`, {
        withCredentials: true,
        transport: HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()
  }
  return connection
}

// Multiple components may call this on mount — this makes sure we only
// actually start the connection once and everyone awaits the same promise.
export function ensureRealtimeConnectionStarted(): Promise<void> {
  const conn = getRealtimeConnection()
  if (conn.state === 'Connected') return Promise.resolve()
  if (!startPromise) {
    startPromise = conn.start().catch((err) => {
      startPromise = null
      throw err
    })
  }
  return startPromise
}
