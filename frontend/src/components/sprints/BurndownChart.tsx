import type { SprintBurndown } from '@/api/sprints'

interface BurndownChartProps {
  burndown: SprintBurndown
}

const WIDTH = 640
const HEIGHT = 220
const PADDING = { top: 16, right: 16, bottom: 28, left: 36 }

// Plain inline SVG — no charting library needed for one ideal line + one
// actual line. Ideal is a straight line from TotalPointsAtStart down to 0
// across the sprint's date range; actual is whatever daily snapshots exist
// so far (see GetSprintBurndownQueryHandler — one snapshot per day, taken
// lazily whenever someone views this).
export default function BurndownChart({ burndown }: BurndownChartProps) {
  const start = new Date(burndown.startDate).getTime()
  const end = new Date(burndown.endDate).getTime()
  const totalDays = Math.max(1, Math.round((end - start) / (1000 * 60 * 60 * 24)))
  const maxPoints = Math.max(burndown.totalPointsAtStart, 1)

  const innerWidth = WIDTH - PADDING.left - PADDING.right
  const innerHeight = HEIGHT - PADDING.top - PADDING.bottom

  const xForDay = (dayOffset: number) => PADDING.left + (dayOffset / totalDays) * innerWidth
  const yForPoints = (points: number) => PADDING.top + innerHeight - (points / maxPoints) * innerHeight

  const idealPath = `M ${xForDay(0)} ${yForPoints(burndown.totalPointsAtStart)} L ${xForDay(totalDays)} ${yForPoints(0)}`

  const actualPoints = burndown.actualSnapshots
    .map((snapshot) => {
      const dayOffset = Math.round((new Date(snapshot.date).getTime() - start) / (1000 * 60 * 60 * 24))
      return { x: xForDay(dayOffset), y: yForPoints(snapshot.remainingPoints), snapshot }
    })
    .sort((a, b) => a.x - b.x)

  const actualPath = actualPoints.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x} ${p.y}`).join(' ')

  return (
    <svg viewBox={`0 0 ${WIDTH} ${HEIGHT}`} style={{ width: '100%', height: 'auto' }} role="img" aria-label="Sprint burndown chart">
      {/* Axes */}
      <line x1={PADDING.left} y1={PADDING.top} x2={PADDING.left} y2={HEIGHT - PADDING.bottom} stroke="var(--color-border-strong)" />
      <line x1={PADDING.left} y1={HEIGHT - PADDING.bottom} x2={WIDTH - PADDING.right} y2={HEIGHT - PADDING.bottom} stroke="var(--color-border-strong)" />

      <text x={4} y={PADDING.top + 4} fontSize="10" fill="var(--color-ink-faint)">{maxPoints}</text>
      <text x={4} y={HEIGHT - PADDING.bottom} fontSize="10" fill="var(--color-ink-faint)">0</text>
      <text x={PADDING.left} y={HEIGHT - 6} fontSize="10" fill="var(--color-ink-faint)">Day 0</text>
      <text x={WIDTH - PADDING.right - 24} y={HEIGHT - 6} fontSize="10" fill="var(--color-ink-faint)">Day {totalDays}</text>

      {/* Ideal line */}
      <path d={idealPath} fill="none" stroke="var(--color-ink-faint)" strokeWidth={1.5} strokeDasharray="4 4" />

      {/* Actual line */}
      {actualPoints.length > 0 && (
        <path d={actualPath} fill="none" stroke="var(--color-brand)" strokeWidth={2} />
      )}
      {actualPoints.map((p) => (
        <circle key={p.snapshot.date} cx={p.x} cy={p.y} r={3} fill="var(--color-brand)">
          <title>{new Date(p.snapshot.date).toLocaleDateString()}: {p.snapshot.remainingPoints} pts remaining</title>
        </circle>
      ))}
    </svg>
  )
}
