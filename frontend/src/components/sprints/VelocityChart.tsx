import type { VelocityPoint } from '@/api/sprints'

interface VelocityChartProps {
  points: VelocityPoint[]
}

const WIDTH = 640
const HEIGHT = 220
const PADDING = { top: 16, right: 16, bottom: 40, left: 36 }

// Bar chart of completed points per finished sprint — planned (light) vs.
// completed (solid) side by side, so a trend of "committing more than we
// finish" is visible at a glance. Plain inline SVG, same approach as
// BurndownChart — no charting library needed for this.
export default function VelocityChart({ points }: VelocityChartProps) {
  const maxValue = Math.max(1, ...points.map((p) => Math.max(p.plannedPoints ?? 0, p.completedPoints)))
  const innerWidth = WIDTH - PADDING.left - PADDING.right
  const innerHeight = HEIGHT - PADDING.top - PADDING.bottom
  const groupWidth = innerWidth / points.length
  const barWidth = Math.min(28, groupWidth / 3)

  const yFor = (value: number) => PADDING.top + innerHeight - (value / maxValue) * innerHeight
  const average = points.reduce((sum, p) => sum + p.completedPoints, 0) / points.length

  return (
    <svg viewBox={`0 0 ${WIDTH} ${HEIGHT}`} style={{ width: '100%', height: 'auto' }} role="img" aria-label="Team velocity chart">
      <line x1={PADDING.left} y1={PADDING.top} x2={PADDING.left} y2={HEIGHT - PADDING.bottom} stroke="var(--color-border-strong)" />
      <line x1={PADDING.left} y1={HEIGHT - PADDING.bottom} x2={WIDTH - PADDING.right} y2={HEIGHT - PADDING.bottom} stroke="var(--color-border-strong)" />

      <text x={4} y={PADDING.top + 4} fontSize="10" fill="var(--color-ink-faint)">{maxValue}</text>
      <text x={4} y={HEIGHT - PADDING.bottom} fontSize="10" fill="var(--color-ink-faint)">0</text>

      {/* Average line */}
      <line
        x1={PADDING.left} x2={WIDTH - PADDING.right} y1={yFor(average)} y2={yFor(average)}
        stroke="var(--color-brand)" strokeDasharray="4 4" strokeWidth={1}
      />
      <text x={WIDTH - PADDING.right - 70} y={yFor(average) - 4} fontSize="10" fill="var(--color-brand)">avg {average.toFixed(1)}</text>

      {points.map((point, i) => {
        const groupX = PADDING.left + i * groupWidth + groupWidth / 2
        return (
          <g key={point.sprintId}>
            {point.plannedPoints !== undefined && (
              <rect
                x={groupX - barWidth} y={yFor(point.plannedPoints)}
                width={barWidth} height={HEIGHT - PADDING.bottom - yFor(point.plannedPoints)}
                fill="var(--color-border-strong)"
              >
                <title>{point.sprintName}: {point.plannedPoints} pts planned</title>
              </rect>
            )}
            <rect
              x={groupX} y={yFor(point.completedPoints)}
              width={barWidth} height={HEIGHT - PADDING.bottom - yFor(point.completedPoints)}
              fill="var(--color-brand)"
            >
              <title>{point.sprintName}: {point.completedPoints} pts completed</title>
            </rect>
            <text x={groupX} y={HEIGHT - PADDING.bottom + 14} fontSize="10" fill="var(--color-ink-faint)" textAnchor="middle">
              {point.sprintName.length > 10 ? `${point.sprintName.slice(0, 9)}…` : point.sprintName}
            </text>
          </g>
        )
      })}
    </svg>
  )
}
