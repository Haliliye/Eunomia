// Derives a stable, Jira/Linear-style short code for a story from the team
// name and the story's own id — no backend change needed since it's fully
// deterministic from data we already have.
export function ticketCode(teamName: string, storyId: string): string {
  const initials = teamName
    .trim()
    .split(/\s+/)
    .map((w) => w[0])
    .join('')
    .slice(0, 3)
    .toUpperCase() || 'GEN'

  const hash = storyId.replace(/-/g, '').slice(0, 6).toUpperCase()
  return `${initials}-${hash}`
}
