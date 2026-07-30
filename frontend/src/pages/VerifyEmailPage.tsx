import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { authApi } from '@/api/auth'

export default function VerifyEmailPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''
  const [status, setStatus] = useState<'checking' | 'success' | 'error'>('checking')

  useEffect(() => {
    if (!token) { setStatus('error'); return }
    authApi.verifyEmail(token).then(() => setStatus('success')).catch(() => setStatus('error'))
  }, [token])

  return (
    <div style={{ maxWidth: 360, margin: '80px auto', textAlign: 'center' }}>
      <img src="/logo.png" alt="Eunomia" style={{ width: 40, height: 40, borderRadius: 10, marginBottom: 16 }} />
      <div className="card">
        {status === 'checking' && <p>Verifying…</p>}
        {status === 'success' && <p>Your email is verified. You're all set — if you're logged in on this browser and still see a reminder banner, refresh the page.</p>}
        {status === 'error' && <p>This verification link is invalid or has expired. Request a new one from Settings.</p>}
      </div>
      <p style={{ marginTop: 16 }}><Link to="/teams">Go to My Teams</Link></p>
    </div>
  )
}
