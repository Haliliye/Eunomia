import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr'

let connection: HubConnection | null = null
let startPromise: Promise<void> | null = null

// One shared connection for the whole app (notifications + board updates
// both ride on it) — see backend/src/TodoApp.Api/Realtime/AppHub.cs.
//
// No accessTokenFactory here — the access token lives in an httpOnly cookie
// now, not somewhere JS can read it. withCredentials makes SignalR's
// negotiate call (and the WebSocket upgrade itself) send that cookie
// automatically, the same way axios does for regular API calls.
export function getRealtimeConnection(): HubConnection {
  if (!connection) {
    connection = new HubConnectionBuilder()
      .withUrl(`${(import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:5001/api').replace(/\/api$/, '')}/hubs/app`, {
        withCredentials: true,
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
