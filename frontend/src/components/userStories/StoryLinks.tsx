import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { userStoriesApi, type ResolvedStoryLink } from '@/api/userStories'
import type { UserStory } from '@/types/userStory'
import { ticketCode } from '@/lib/ticketCode'
import { useToast } from '@/context/ToastContext'

interface StoryLinksProps {
  story: UserStory
  teamName: string
}

const LINK_LABELS: Record<string, string> = {
  Blocks: 'Blocks',
  BlockedBy: 'Blocked by',
  RelatesTo: 'Relates to',
}

// Classic "linked issues" — Blocks/BlockedBy/RelatesTo. Adding a link creates
// the symmetric pair automatically on both stories (see AddStoryLinkCommandHandler).
export default function StoryLinks({ story, teamName }: StoryLinksProps) {
  const { showToast } = useToast()
  const [links, setLinks] = useState<ResolvedStoryLink[]>([])
  const [isLoading, setLoading] = useState(true)
  const [searchQuery, setSearchQuery] = useState('')
  const [searchResults, setSearchResults] = useState<UserStory[]>([])
  const [linkType, setLinkType] = useState<'Blocks' | 'RelatesTo'>('Blocks')

  const load = () => {
    userStoriesApi.getLinks(story.id).then(setLinks).finally(() => setLoading(false))
  }

  useEffect(load, [story.id])

  useEffect(() => {
    if (searchQuery.trim().length < 2) {
      setSearchResults([])
      return
    }
    const timeout = setTimeout(() => {
      userStoriesApi.getByTeam(story.teamId, { keyword: searchQuery }, 1, 8).then((result) => {
        setSearchResults(result.items.filter((s) => s.id !== story.id))
      })
    }, 250)
    return () => clearTimeout(timeout)
  }, [searchQuery, story.teamId, story.id])

  const handleAddLink = async (linkedStoryId: string) => {
    try {
      await userStoriesApi.addLink(story.id, linkedStoryId, linkType)
      setSearchQuery('')
      setSearchResults([])
      load()
    } catch {
      showToast('Could not link that story.', 'error')
    }
  }

  const handleRemoveLink = async (linkedStoryId: string) => {
    try {
      await userStoriesApi.removeLink(story.id, linkedStoryId)
      load()
    } catch {
      showToast('Could not remove that link.', 'error')
    }
  }

  return (
    <div className="card">
      <div className="card-header"><h3>Linked stories</h3></div>

      {isLoading ? (
        <p style={{ fontSize: 13 }}>Loading…</p>
      ) : links.length === 0 ? (
        <p style={{ fontSize: 13 }}>No linked stories yet.</p>
      ) : (
        <ul style={{ listStyle: 'none', margin: 0, padding: 0, marginBottom: 12 }}>
          {links.map((link) => (
            <li key={link.linkedStoryId} style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '6px 0', borderBottom: '1px solid var(--color-border)' }}>
              <span
                className="backlog-status-pill todo"
                style={{ fontSize: 10.5, color: link.linkType === 'Blocks' ? 'var(--color-danger)' : undefined }}
              >
                {LINK_LABELS[link.linkType]}
              </span>
              <Link
                to={`/teams/${link.linkedStoryTeamId}/stories/${link.linkedStoryId}`}
                style={{ flex: 1, textDecoration: link.linkedStoryIsDone ? 'line-through' : undefined }}
              >
                {link.linkedStoryTitle}
              </Link>
              <button className="btn btn-ghost btn-sm" onClick={() => handleRemoveLink(link.linkedStoryId)} aria-label="Remove link">✕</button>
            </li>
          ))}
        </ul>
      )}

      <div style={{ display: 'flex', gap: 8, position: 'relative' }}>
        <select className="pill-select" value={linkType} onChange={(e) => setLinkType(e.target.value as 'Blocks' | 'RelatesTo')}>
          <option value="Blocks">Blocks</option>
          <option value="RelatesTo">Relates to</option>
        </select>
        <input
          className="input"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          placeholder="Search stories to link…"
          style={{ flex: 1 }}
        />
        {searchResults.length > 0 && (
          <ul style={{
            position: 'absolute', top: '100%', left: 0, right: 0, marginTop: 4, zIndex: 10,
            listStyle: 'none', padding: 4, background: 'var(--color-surface)', border: '1px solid var(--color-border)',
            borderRadius: 8, maxHeight: 200, overflowY: 'auto', boxShadow: '0 4px 12px rgba(0,0,0,0.1)',
          }}>
            {searchResults.map((result) => (
              <li key={result.id}>
                <button
                  onClick={() => handleAddLink(result.id)}
                  style={{ display: 'block', width: '100%', textAlign: 'left', background: 'none', border: 'none', padding: '6px 8px', cursor: 'pointer', fontSize: 13 }}
                >
                  <span className="mono" style={{ color: 'var(--color-ink-faint)', marginRight: 6 }}>{ticketCode(teamName, result.id)}</span>
                  {result.title}
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  )
}
