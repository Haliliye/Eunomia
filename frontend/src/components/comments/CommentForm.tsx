import { useRef, useState } from 'react'
import type { TeamMember } from '@/types/team'
import { displayNameOrId } from '@/hooks/useUserNames'

interface CommentFormProps {
  members: TeamMember[]
  userNames: Record<string, string>
  onSubmit: (content: string, mentionedUserIds: string[]) => void
}

// Finds an unfinished "@word" right before the cursor, if any — used both
// to decide whether to show the autocomplete dropdown and to know exactly
// which slice of text to replace once a member is picked.
function findMentionQuery(text: string, cursor: number): { start: number; query: string } | null {
  const upToCursor = text.slice(0, cursor)
  const match = upToCursor.match(/@(\w*)$/)
  if (!match) return null
  return { start: cursor - match[0].length, query: match[1] }
}

export default function CommentForm({ members, userNames, onSubmit }: CommentFormProps) {
  const [content, setContent] = useState('')
  const [mentionedUserIds, setMentionedUserIds] = useState<string[]>([])
  const [mentionQuery, setMentionQuery] = useState<{ start: number; query: string } | null>(null)
  const textareaRef = useRef<HTMLTextAreaElement>(null)

  const matchingMembers = mentionQuery
    ? members.filter((m) => displayNameOrId(userNames, m.userId).toLowerCase().includes(mentionQuery.query.toLowerCase()))
    : []

  const handleChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    const value = e.target.value
    setContent(value)
    setMentionQuery(findMentionQuery(value, e.target.selectionStart))
  }

  const insertMention = (userId: string) => {
    if (!mentionQuery) return
    const before = content.slice(0, mentionQuery.start)
    const after = content.slice(mentionQuery.start + 1 + mentionQuery.query.length)
    const next = `${before}@${userId} ${after}`
    setContent(next)
    setMentionedUserIds((prev) => (prev.includes(userId) ? prev : [...prev, userId]))
    setMentionQuery(null)
    // Put the cursor right after the inserted mention, not at the end of
    // the whole comment — matters once there's text after it too.
    requestAnimationFrame(() => {
      const pos = before.length + userId.length + 2
      textareaRef.current?.focus()
      textareaRef.current?.setSelectionRange(pos, pos)
    })
  }

  const handleMentionClick = (userId: string) => {
    setContent((prev) => `${prev}${prev.endsWith(' ') || prev === '' ? '' : ' '}@${userId} `)
    setMentionedUserIds((prev) => (prev.includes(userId) ? prev : [...prev, userId]))
  }

  const handleSubmit = () => {
    if (!content.trim()) return
    onSubmit(content.trim(), mentionedUserIds)
    setContent('')
    setMentionedUserIds([])
    setMentionQuery(null)
  }

  return (
    <div style={{ position: 'relative' }}>
      <textarea
        ref={textareaRef}
        className="textarea"
        value={content}
        onChange={handleChange}
        onKeyDown={(e) => {
          if (e.key === 'Escape') setMentionQuery(null)
        }}
        placeholder="Add a comment… (type @ to mention someone, Markdown supported)"
        style={{ minHeight: 56 }}
      />

      {mentionQuery && matchingMembers.length > 0 && (
        <div
          className="notif-panel"
          style={{ top: 'auto', right: 'auto', left: 0, width: 220, padding: 4 }}
        >
          {matchingMembers.map((m) => (
            <button
              key={m.userId}
              className="btn btn-ghost btn-sm"
              style={{ display: 'block', width: '100%', textAlign: 'left' }}
              onClick={() => insertMention(m.userId)}
            >
              @{displayNameOrId(userNames, m.userId)}
            </button>
          ))}
        </div>
      )}

      {members.length > 0 && (
        <div className="mention-chip-row">
          {members.map((m) => (
            <button key={m.userId} className="mention-chip" onClick={() => handleMentionClick(m.userId)}>
              @{displayNameOrId(userNames, m.userId)}
            </button>
          ))}
        </div>
      )}
      <button className="btn btn-sm btn-primary" onClick={handleSubmit}>Post comment</button>
    </div>
  )
}
