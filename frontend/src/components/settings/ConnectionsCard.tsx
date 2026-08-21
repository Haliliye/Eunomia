import JiraIntegrationCard from './JiraIntegrationCard'
import AzureDevOpsIntegrationCard from './AzureDevOpsIntegrationCard'
import GitHubIntegrationCard from './GitHubIntegrationCard'
import GitLabIntegrationCard from './GitLabIntegrationCard'

/**
 * Replaces the four separate full-width integration cards with one
 * "Connections" section — each provider is now just a row (connect/
 * disconnect status), not its own card. Importing a project/repo lives
 * exclusively in the "Import a team" flow on the Teams page now, not here —
 * this section is purely about whether an account is linked.
 */
export default function ConnectionsCard() {
  return (
    <div className="card" style={{ marginTop: 20 }}>
      <div className="card-header"><h3>Connections</h3></div>
      <p style={{ marginBottom: 12, fontSize: 12.5, color: 'var(--color-ink-muted)' }}>
        Link an account here, then use "Import a team" on the Teams page to bring in its projects or repos.
      </p>
      <div style={{ display: 'flex', flexDirection: 'column' }}>
        <JiraIntegrationCard />
        <AzureDevOpsIntegrationCard />
        <GitHubIntegrationCard />
        <GitLabIntegrationCard />
      </div>
    </div>
  )
}
