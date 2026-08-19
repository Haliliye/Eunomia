import { useEffect, useState } from 'react'
import { accountApi } from '@/api/auth'
import type { NotificationPreferences } from '@/types/notificationPreferences'
import { useToast } from '@/context/ToastContext'
import { Skeleton } from '@/components/common/Skeleton'
import JiraIntegrationCard from '@/components/settings/JiraIntegrationCard'
import AzureDevOpsIntegrationCard from '@/components/settings/AzureDevOpsIntegrationCard'
import GitHubIntegrationCard from '@/components/settings/GitHubIntegrationCard'

// Excludes reminderLeadTimeHours (a number, handled by its own input below,
// not a checkbox) — narrowing this here is what makes prefs[opt.key] resolve
// to plain boolean instead of boolean | number for the checkbox's `checked`.
type BooleanPreferenceKey = Exclude<keyof NotificationPreferences, 'reminderLeadTimeHours'>

const OPTIONS: { key: BooleanPreferenceKey; label: string; description: string }[] = [
  { key: 'notifyOnAssignment', label: 'Assigned to a story', description: 'When someone assigns a user story to you.' },
  { key: 'notifyOnMention', label: 'Mentioned in a comment', description: 'When someone @-mentions you in a comment.' },
  { key: 'notifyOnInvitation', label: 'Team invitations', description: "New invitations, and when someone accepts yours. Turning this off doesn't hide pending invitations from the Teams page — it only silences the bell." },
  { key: 'notifyOnDueSoon', label: 'Due date coming up', description: "Reminds you before a story assigned to you is due — see the lead time below." },
]

export default function SettingsPage() {
  const { showToast } = useToast()
  const [prefs, setPrefs] = useState<NotificationPreferences | null>(null)
  const [isLoading, setLoading] = useState(true)
  const [isSaving, setSaving] = useState(false)

  useEffect(() => {
    accountApi.getNotificationPreferences().then(setPrefs).finally(() => setLoading(false))
  }, [])

  const handleToggle = (key: BooleanPreferenceKey) => {
    if (!prefs) return
    setPrefs({ ...prefs, [key]: !prefs[key] })
  }

  const handleSave = async () => {
    if (!prefs) return
    setSaving(true)
    try {
      await accountApi.updateNotificationPreferences(prefs)
      showToast('Notification preferences saved.')
    } catch {
      showToast('Could not save your preferences.', 'error')
    } finally {
      setSaving(false)
    }
  }

  return (
    <section>
      <div className="page-header">
        <div>
          <span className="page-header-eyebrow">Account</span>
          <h1>Settings</h1>
        </div>
      </div>

      <div className="card">
        <div className="card-header"><h3>Notifications</h3></div>
        <p style={{ marginBottom: 12 }}>Choose what you want to be notified about.</p>

        {isLoading || !prefs ? (
          <Skeleton style={{ height: 120 }} />
        ) : (
          <>
            {OPTIONS.map((opt) => (
              <label key={opt.key} style={{ display: 'flex', gap: 10, alignItems: 'flex-start', padding: '10px 0', borderBottom: '1px solid var(--color-border)' }}>
                <input type="checkbox" checked={prefs[opt.key]} onChange={() => handleToggle(opt.key)} style={{ marginTop: 3 }} />
                <span>
                  <div style={{ fontWeight: 500 }}>{opt.label}</div>
                  <div style={{ fontSize: 12.5, color: 'var(--color-ink-muted)' }}>{opt.description}</div>
                </span>
              </label>
            ))}

            <div className="field" style={{ marginTop: 12, opacity: prefs.notifyOnDueSoon ? 1 : 0.5 }}>
              <label htmlFor="reminder-lead-time">Remind me this many hours before the due date</label>
              <input
                id="reminder-lead-time"
                className="input"
                type="number"
                min={1}
                max={168}
                value={prefs.reminderLeadTimeHours}
                disabled={!prefs.notifyOnDueSoon}
                onChange={(e) => setPrefs({ ...prefs, reminderLeadTimeHours: Math.max(1, Number(e.target.value) || 1) })}
                style={{ maxWidth: 120 }}
              />
            </div>

            <button className="btn btn-primary" style={{ marginTop: 16 }} onClick={handleSave} disabled={isSaving}>
              {isSaving ? 'Saving…' : 'Save changes'}
            </button>
          </>
        )}
      </div>

      <JiraIntegrationCard />
      <AzureDevOpsIntegrationCard />
      <GitHubIntegrationCard />
    </section>
  )
}
