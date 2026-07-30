// Deterministic color per user id (like Jira's avatar colors) — same person
// always gets the same color across the app, without a backend field for it.
const PALETTE = ['#0B6E63', '#B3261E', '#B4530A', '#3B5BDB', '#7048A8', '#2F6F4E', '#A6215E']

export function avatarColor(userId: string): string {
  let hash = 0
  for (let i = 0; i < userId.length; i++) {
    hash = (hash * 31 + userId.charCodeAt(i)) | 0
  }
  return PALETTE[Math.abs(hash) % PALETTE.length]
}
