import { useEffect, useState } from 'react'
import { commentsApi } from '@/api/comments'
import type { Comment } from '@/types/comment'
import type { TeamMember } from '@/types/team'
import CommentList from './CommentList'
import CommentForm from './CommentForm'
import { useAuth } from '@/context/AuthContext'
import { useToast } from '@/context/ToastContext'
import { useConfirm } from '@/context/ConfirmContext'

interface CommentSectionProps {
  userStoryId: string
  members: TeamMember[]
  userNames: Record<string, string>
}

export default function CommentSection({ userStoryId, members, userNames }: CommentSectionProps) {
  const { user } = useAuth()
  const { showToast } = useToast()
  const confirm = useConfirm()
  const [comments, setComments] = useState<Comment[]>([])
  const [isLoading, setLoading] = useState(true)

  useEffect(() => {
    commentsApi.getByUserStory(userStoryId)
      .then(setComments)
      .finally(() => setLoading(false))
  }, [userStoryId])

  const handleSubmit = async (content: string, mentionedUserIds: string[]) => {
    const comment = await commentsApi.add(userStoryId, content, mentionedUserIds)
    setComments((prev) => [...prev, comment])
  }

  const handleEdit = async (comment: Comment, newContent: string) => {
    try {
      const updated = await commentsApi.update(comment.id, newContent, comment.mentionedUserIds)
      setComments((prev) => prev.map((c) => (c.id === comment.id ? updated : c)))
    } catch {
      showToast('Could not save that edit.', 'error')
    }
  }

  const handleDelete = async (comment: Comment) => {
    const confirmed = await confirm({ title: 'Delete this comment?', confirmLabel: 'Delete', danger: true })
    if (!confirmed) return

    try {
      await commentsApi.delete(comment.id)
      setComments((prev) => prev.filter((c) => c.id !== comment.id))
    } catch {
      showToast('Could not delete that comment.', 'error')
    }
  }

  if (!user) return null

  return (
    <div>
      <h3>Comments</h3>
      {isLoading ? (
        <p>Loading…</p>
      ) : (
        <CommentList
          comments={comments}
          userNames={userNames}
          currentUserId={user.userId}
          onEdit={handleEdit}
          onDelete={handleDelete}
        />
      )}
      <CommentForm members={members} userNames={userNames} onSubmit={handleSubmit} />
    </div>
  )
}
