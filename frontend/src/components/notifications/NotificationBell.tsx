import { useEffect, useState } from 'react'
import { notificationsApi } from '@/api/notifications'
import { invitationsApi } from '@/api/invitations'
import type { Notification } from '@/types/notification'
import { useAuth } from '@/context/AuthContext'
import { useToast } from '@/context/ToastContext'
import { ensureRealtimeConnectionStarted, getRealtimeConnection } from '@/services/realtimeConnection'

const FALLBACK_POLL_MS = 60000

export default function NotificationBell() {
  const { user } = useAuth()
  const { showToast } = useToast()
  const [notifications, setNotifications] = useState<Notification[]>([])
  const [isOpen, setOpen] = useState(false)
  // Invitation ids the user has already responded to in this session — hides
  // Accept/Decline once acted on, even before the notification is refetched.
  const [respondedInvitationIds, setRespondedInvitationIds] = useState<Set<string>>(new Set())

  const load = () => {
    if (!user) return
    notificationsApi.getMine().then(setNotifications)
  }

  useEffect(() => {
    if (!user) return

    load()
    const interval = setInterval(load, FALLBACK_POLL_MS)

    ensureRealtimeConnectionStarted().catch(() => {})

    const connection = getRealtimeConnection()
    const handleNotification = (notification: Notification) => {
      setNotifications((prev) => [notification, ...prev])
    }
    connection.on('notification', handleNotification)

    return () => {
      clearInterval(interval)
      connection.off('notification', handleNotification)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user?.userId])

  const unreadCount = notifications.filter((n) => !n.isRead).length

  const handleMarkRead = async (id: string) => {
    await notificationsApi.markRead(id)
    setNotifications((prev) => prev.map((n) => (n.id === id ? { ...n, isRead: true } : n)))
  }

  const handleMarkAllRead = async () => {
    await notificationsApi.markAllRead()
    setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })))
  }

  const handleAccept = async (notification: Notification) => {
    try {
      await invitationsApi.accept(notification.relatedEntityId)
      setRespondedInvitationIds((prev) => new Set(prev).add(notification.relatedEntityId))
      await handleMarkRead(notification.id)
      showToast('Invitation accepted — you\'re now a member.')
    } catch {
      showToast('Could not accept that invitation — it may no longer be valid.', 'error')
    }
  }

  const handleDecline = async (notification: Notification) => {
    try {
      await invitationsApi.decline(notification.relatedEntityId)
      setRespondedInvitationIds((prev) => new Set(prev).add(notification.relatedEntityId))
      await handleMarkRead(notification.id)
    } catch {
      showToast('Could not decline that invitation — it may no longer be valid.', 'error')
    }
  }

  return (
    <div style={{ position: 'relative' }}>
      <button className="notif-bell" onClick={() => setOpen((prev) => !prev)} aria-label="Notifications">
        🔔
        {unreadCount > 0 && <span className="notif-dot" />}
      </button>
      {isOpen && (
        <div className="notif-panel">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
            <h3 style={{ margin: 0 }}>Notifications</h3>
            <button className="btn btn-ghost btn-sm" onClick={handleMarkAllRead}>Mark all read</button>
          </div>
          {notifications.length === 0 ? (
            <p style={{ fontSize: 13 }}>You're all caught up.</p>
          ) : (
            <ul style={{ listStyle: 'none', margin: 0, padding: 0 }}>
              {notifications.map((n) => {
                const isActionableInvite = n.type === 'TeamInvitation' && !respondedInvitationIds.has(n.relatedEntityId)
                return (
                  <li key={n.id} className={`notif-item ${n.isRead ? '' : 'unread'}`}>
                    <div>{n.message}</div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 2 }}>
                      <span className="notif-time">{new Date(n.createdOn).toLocaleString()}</span>
                      {!n.isRead && !isActionableInvite && (
                        <button className="btn btn-ghost btn-sm" onClick={() => handleMarkRead(n.id)}>Mark read</button>
                      )}
                    </div>
                    {isActionableInvite && (
                      <div style={{ display: 'flex', gap: 6, marginTop: 6 }}>
                        <button className="btn btn-primary btn-sm" onClick={() => handleAccept(n)}>Accept</button>
                        <button className="btn btn-sm" onClick={() => handleDecline(n)}>Decline</button>
                      </div>
                    )}
                  </li>
                )
              })}
            </ul>
          )}
        </div>
      )}
    </div>
  )
}
