interface SkeletonProps {
  className?: string
  style?: React.CSSProperties
}

export function Skeleton({ className = '', style }: SkeletonProps) {
  return <div className={`skeleton ${className}`} style={style} aria-hidden="true" />
}

export function SkeletonTeamGrid() {
  return (
    <div className="team-grid" role="status" aria-label="Loading teams">
      {Array.from({ length: 6 }).map((_, i) => (
        <Skeleton key={i} className="skeleton-tile" />
      ))}
    </div>
  )
}

export function SkeletonTable({ rows = 5 }: { rows?: number }) {
  return (
    <div className="backlog-list" role="status" aria-label="Loading stories">
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} style={{ padding: '7px 12px', borderBottom: '1px solid var(--color-border)' }}>
          <Skeleton style={{ height: 24 }} />
        </div>
      ))}
    </div>
  )
}

export function SkeletonBoard() {
  return (
    <div className="board" role="status" aria-label="Loading board">
      {Array.from({ length: 3 }).map((_, col) => (
        <div className="board-column" key={col}>
          <Skeleton className="skeleton-title" style={{ width: '60%' }} />
          {Array.from({ length: 3 }).map((_, row) => (
            <Skeleton key={row} style={{ height: 64, marginBottom: 8 }} />
          ))}
        </div>
      ))}
    </div>
  )
}
