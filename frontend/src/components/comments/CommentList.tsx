import { useState } from 'react'
import type { Comment } from '@/types/comment'
import { displayNameOrId } from '@/hooks/useUserNames'
import MarkdownContent from '@/components/common/MarkdownContent'

interface CommentListProps {
  comments: Comment[]
  userNames: Record<string, string>
  currentUserId: string
  onEdit: (comment: Comment, newContent: string) => void
  onDelete: (comment: Comment) => void
}

// Mentions are swapped for a raw <mark> tag BEFORE the content goes through
// markdown parsing — marked passes inline HTML through untouched, and
// MarkdownContent's DOMPurify pass allows <mark> (a standard, harmless tag),
// so both features (mentions + markdown formatting) work in the same comment.
function toMarkdownWithMentions(comment: Comment, userNames: Record<string, string>): string {
  if (comment.mentionedUserIds.length === 0) return comment.content

  const pattern = new RegExp(`(${comment.mentionedUserIds.map((id) => `@${id}`).join('|')})`, 'g')
  return comment.content.replace(pattern, (match) => {
    const mentionedId = comment.mentionedUserIds.find((id) => `@${id}` === match)
    return mentionedId ? `<mark>@${displayNameOrId(userNames, mentionedId)}</mark>` : match
  })
}

export default function CommentList({ comments, userNames, currentUserId, onEdit, onDelete }: CommentListProps) {
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editContent, setEditContent] = useState('')

  if (comments.length === 0) {
    return <p style={{ fontSize: 13 }}>No comments yet.</p>
  }

  const startEdit = (comment: Comment) => {
    setEditingId(comment.id)
    setEditContent(comment.content)
  }

  const saveEdit = (comment: Comment) => {
    if (!editContent.trim()) return
    onEdit(comment, editContent.trim())
    setEditingId(null)
  }

  return (
    <ul className="comment-list">
      {comments.map((comment) => (
        <li className="comment-item" key={comment.id}>
          <div className="comment-meta">
            <span className="comment-author">{displayNameOrId(userNames, comment.authorId)}</span>
            <span className="comment-time">
              {new Date(comment.createdOn).toLocaleString()}
              {comment.editedOn && ' (edited)'}
            </span>
          </div>

          {editingId === comment.id ? (
            <div>
              <textarea
                className="textarea"
                value={editContent}
                onChange={(e) => setEditContent(e.target.value)}
                style={{ minHeight: 48 }}
                autoFocus
              />
              <div style={{ display: 'flex', gap: 6, marginTop: 4 }}>
                <button className="btn btn-sm btn-primary" onClick={() => saveEdit(comment)}>Save</button>
                <button className="btn btn-sm" onClick={() => setEditingId(null)}>Cancel</button>
              </div>
            </div>
          ) : (
            <>
              <div className="comment-content"><MarkdownContent content={toMarkdownWithMentions(comment, userNames)} /></div>
              {comment.authorId === currentUserId && (
                <div style={{ display: 'flex', gap: 10, marginTop: 2 }}>
                  <button className="btn btn-ghost btn-sm" onClick={() => startEdit(comment)}>Edit</button>
                  <button className="btn btn-ghost btn-sm" onClick={() => onDelete(comment)}>Delete</button>
                </div>
              )}
            </>
          )}
        </li>
      ))}
    </ul>
  )
}
