import { useRef, useState } from 'react'
import type { Attachment } from '@/types/userStory'
import { userStoriesApi } from '@/api/userStories'
import { useToast } from '@/context/ToastContext'
import { displayNameOrId } from '@/hooks/useUserNames'

interface AttachmentsProps {
  userStoryId: string
  attachments: Attachment[]
  userNames: Record<string, string>
  onChange: () => void
}

const MAX_SIZE_BYTES = 10 * 1024 * 1024

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function isPreviewable(contentType: string): boolean {
  return contentType.startsWith('image/') || contentType === 'application/pdf'
}

// US-134/135/136: upload, preview/download, and remove attachments.
export default function Attachments({ userStoryId, attachments, userNames, onChange }: AttachmentsProps) {
  const { showToast } = useToast()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [isUploading, setUploading] = useState(false)

  const handleFileSelected = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    e.target.value = '' // lets the same file be re-selected later if needed
    if (!file) return

    // US-134 AC: a clear validation error before even attempting the upload.
    if (file.size > MAX_SIZE_BYTES) {
      showToast('That file is over the 10 MB limit.', 'error')
      return
    }

    setUploading(true)
    try {
      await userStoriesApi.uploadAttachment(userStoryId, file)
      onChange()
      showToast(`"${file.name}" attached.`)
    } catch {
      showToast("That file type isn't supported, or the upload failed.", 'error')
    } finally {
      setUploading(false)
    }
  }

  const handleOpen = async (attachment: Attachment) => {
    try {
      const blobUrl = await userStoriesApi.downloadAttachment(userStoryId, attachment.id)
      if (isPreviewable(attachment.contentType)) {
        window.open(blobUrl, '_blank')
      } else {
        const link = document.createElement('a')
        link.href = blobUrl
        link.download = attachment.fileName
        link.click()
      }
      // Not revoking the blob URL immediately — the opened tab/download still
      // needs it. The browser cleans these up when the page/tab is closed.
    } catch {
      showToast('Could not open that attachment.', 'error')
    }
  }

  const handleRemove = async (attachment: Attachment) => {
    const confirmed = window.confirm(`Remove "${attachment.fileName}"?`)
    if (!confirmed) return

    try {
      await userStoriesApi.removeAttachment(userStoryId, attachment.id)
      onChange()
      showToast('Attachment removed.')
    } catch {
      showToast('Could not remove that attachment.', 'error')
    }
  }

  return (
    <div>
      <div className="card-header">
        <h3>Attachments</h3>
      </div>

      {attachments.length === 0 ? (
        <p style={{ fontSize: 13 }}>No files attached yet.</p>
      ) : (
        <ul style={{ listStyle: 'none', margin: 0, padding: 0 }}>
          {attachments.map((attachment) => (
            <li key={attachment.id} style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '8px 0', borderBottom: '1px solid var(--color-border)' }}>
              <span aria-hidden="true">{isPreviewable(attachment.contentType) ? '🖼️' : '📄'}</span>
              <button
                onClick={() => handleOpen(attachment)}
                style={{ background: 'none', border: 'none', color: 'var(--color-brand)', cursor: 'pointer', padding: 0, textAlign: 'left', flex: 1 }}
                title={isPreviewable(attachment.contentType) ? 'Preview' : 'Download'}
              >
                {attachment.fileName}
              </button>
              <span className="mono" style={{ fontSize: 11.5, color: 'var(--color-ink-faint)' }}>
                {formatSize(attachment.sizeBytes)} · {displayNameOrId(userNames, attachment.uploadedByUserId)}
              </span>
              <button className="btn btn-ghost btn-sm" onClick={() => handleRemove(attachment)} aria-label="Remove attachment">✕</button>
            </li>
          ))}
        </ul>
      )}

      <input ref={fileInputRef} type="file" onChange={handleFileSelected} style={{ display: 'none' }} />
      <button className="btn btn-sm" style={{ marginTop: 12 }} onClick={() => fileInputRef.current?.click()} disabled={isUploading}>
        {isUploading ? 'Uploading…' : '+ Attach a file'}
      </button>
      <span style={{ fontSize: 11.5, color: 'var(--color-ink-faint)', marginLeft: 8 }}>Up to 10 MB.</span>
    </div>
  )
}
