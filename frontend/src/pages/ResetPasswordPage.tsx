import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { authApi } from '@/api/auth'

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''
  const navigate = useNavigate()
  const [newPassword, setNewPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setSubmitting] = useState(false)
  const [isDone, setDone] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      await authApi.resetPassword(token, newPassword)
      setDone(true)
    } catch (err) {
      setError(extractErrorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div style={{ maxWidth: 360, margin: '80px auto' }}>
      <div style={{ marginBottom: 24, textAlign: 'center' }}>
        <img src="/logo.png" alt="Eunomia" style={{ width: 40, height: 40, borderRadius: 10, display: 'inline-block', marginBottom: 8 }} />
        <h1>Reset password</h1>
      </div>

      {!token ? (
        <div className="alert-error" role="alert">This link is missing its reset token — request a new one.</div>
      ) : isDone ? (
        <div className="card">
          <p>Your password has been changed.</p>
          <button className="btn btn-primary" onClick={() => navigate('/login')} style={{ width: '100%' }}>Go to login</button>
        </div>
      ) : (
        <form onSubmit={handleSubmit} className="card">
          <div className="field">
            <label>
              New password
              <input className="input" type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} minLength={8} autoFocus required />
              <span style={{ fontSize: 11.5, color: 'var(--color-ink-faint)', display: 'block', marginTop: 4 }}>
                At least 8 characters, with an uppercase letter, a number, and a symbol.
              </span>
            </label>
          </div>
          {error && <p className="field-error" role="alert">{error}</p>}
          <button className="btn btn-primary" type="submit" disabled={isSubmitting} style={{ width: '100%' }}>
            {isSubmitting ? 'Saving…' : 'Set new password'}
          </button>
        </form>
      )}

      <p style={{ textAlign: 'center', marginTop: 16 }}>
        <Link to="/login">← Back to login</Link>
      </p>
    </div>
  )
}

function extractErrorMessage(err: unknown): string {
  if (typeof err === 'object' && err !== null && 'response' in err) {
    const response = (err as { response?: { data?: { error?: string } } }).response
    if (response?.data?.error) return response.data.error
  }
  return 'Something went wrong. Please try again.'
}
