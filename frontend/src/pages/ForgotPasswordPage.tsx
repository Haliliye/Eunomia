import { useState } from 'react'
import { Link } from 'react-router-dom'
import { authApi } from '@/api/auth'

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const [isSubmitting, setSubmitting] = useState(false)
  const [result, setResult] = useState<{ message: string; devResetToken: string | null } | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSubmitting(true)
    try {
      const response = await authApi.forgotPassword(email)
      setResult(response)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div style={{ maxWidth: 360, margin: '80px auto' }}>
      <div style={{ marginBottom: 24, textAlign: 'center' }}>
        <img src="/logo.png" alt="Eunomia" style={{ width: 40, height: 40, borderRadius: 10, display: 'inline-block', marginBottom: 8 }} />
        <h1>Forgot password</h1>
      </div>

      {result ? (
        <div className="card">
          <p>{result.message}</p>
          {/* Only shown when no SMTP server is configured on the backend for
              this deployment — see Smtp:Host in appsettings/.env. When it is
              configured, this is emailed instead and never shown here. */}
          {result.devResetToken && (
            <div className="alert-error" style={{ background: 'var(--color-brand-soft)', color: 'var(--color-brand-ink)', borderColor: 'var(--color-brand)' }}>
              <strong>No SMTP configured:</strong> here's your reset link directly:{' '}
              <Link to={`/reset-password?token=${result.devResetToken}`}>Reset password →</Link>
            </div>
          )}
        </div>
      ) : (
        <form onSubmit={handleSubmit} className="card">
          <div className="field">
            <label>
              Email
              <input className="input" type="email" value={email} onChange={(e) => setEmail(e.target.value)} autoFocus required />
            </label>
          </div>
          <button className="btn btn-primary" type="submit" disabled={isSubmitting} style={{ width: '100%' }}>
            {isSubmitting ? 'Sending…' : 'Send reset link'}
          </button>
        </form>
      )}

      <p style={{ textAlign: 'center', marginTop: 16 }}>
        <Link to="/login">← Back to login</Link>
      </p>
    </div>
  )
}
