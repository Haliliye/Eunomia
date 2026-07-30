import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '@/context/AuthContext'

export default function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setSubmitting] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      await login(email, password)
      navigate('/teams')
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
        <h1>Log in</h1>
      </div>

      <form onSubmit={handleSubmit} className="card">
        <div className="field">
          <label>
            Email
            <input className="input" type="email" value={email} onChange={(e) => setEmail(e.target.value)} autoFocus required />
          </label>
        </div>
        <div className="field">
          <label>
            Password
            <input className="input" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
          </label>
        </div>
        <p style={{ textAlign: 'right', marginTop: -8 }}>
          <Link to="/forgot-password" style={{ fontSize: 12.5 }}>Forgot password?</Link>
        </p>
        {error && <p className="field-error">{error}</p>}
        <button className="btn btn-primary" type="submit" disabled={isSubmitting} style={{ width: '100%' }}>
          {isSubmitting ? 'Logging in…' : 'Log in'}
        </button>
      </form>

      <p style={{ textAlign: 'center', marginTop: 16 }}>
        Don't have an account? <Link to="/register">Create one</Link>
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
