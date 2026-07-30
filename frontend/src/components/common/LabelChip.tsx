import type { Label } from '@/types/team'

export default function LabelChip({ label, onRemove }: { label: Label; onRemove?: () => void }) {
  return (
    <span
      className="label-chip"
      style={{ background: `${label.color}22`, color: label.color, border: `1px solid ${label.color}55` }}
    >
      {label.name}
      {onRemove && (
        <button
          onClick={onRemove}
          aria-label={`Remove ${label.name} label`}
          style={{ background: 'none', border: 'none', color: 'inherit', cursor: 'pointer', marginLeft: 4, padding: 0, fontSize: 11 }}
        >
          ✕
        </button>
      )}
    </span>
  )
}
