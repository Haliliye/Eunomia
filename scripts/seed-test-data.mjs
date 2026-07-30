// Seeds a realistic test dataset: 5 team members + a team + 30 user stories
// spread across statuses/priorities/assignees/sprints, plus a few comments.
//
// Goes through the real API (not direct Mongo writes) so every rule the app
// actually enforces — password hashing, invitation accept/decline, the
// status workflow's transition graph, optimistic concurrency — is respected
// exactly like a real user would trigger it. Slower than a raw DB insert,
// but the data it produces is guaranteed to be something the app could have
// produced on its own.
//
// Usage:
//   node scripts/seed-test-data.mjs
//   API_BASE_URL=http://localhost:5000/api node scripts/seed-test-data.mjs
//
// Requires Node 18+ (built-in fetch) and the backend + MongoDB running.
// If you're hitting the Windows/VS https://localhost:5001 setup (self-signed
// dev cert), Node's fetch will reject it — either run against the Docker
// Compose http://localhost:5000 endpoint instead, or run with:
//   NODE_TLS_REJECT_UNAUTHORIZED=0 node scripts/seed-test-data.mjs

const BASE_URL = process.env.API_BASE_URL || 'http://localhost:5000/api'
const RUN_ID = Date.now() // keeps emails unique across repeated runs

const MEMBERS = [
  { displayName: 'Ada Lovelace', role: 'owner' },
  { displayName: 'Grace Hopper', role: 'member' },
  { displayName: 'Alan Turing', role: 'member' },
  { displayName: 'Katherine Johnson', role: 'member' },
  { displayName: 'Margaret Hamilton', role: 'member' },
].map((m) => ({
  ...m,
  email: `${m.displayName.toLowerCase().replace(/\s+/g, '.')}.${RUN_ID}@example.com`,
  password: 'SeedPassword123!',
}))

const STATUSES = ['ToDo', 'Analyze', 'Dev', 'Test', 'Debug', 'Done']
const PRIORITIES = ['Critical', 'High', 'Medium', 'Low']

// Mirrors the backend's real transition graph (UserStory.ChangeStatus) —
// changeStatus rejects illegal jumps, so reaching e.g. "Test" from a fresh
// ToDo story means walking ToDo -> Analyze -> Dev -> Test one hop at a time.
const PATH_TO = {
  ToDo: [],
  Analyze: ['Analyze'],
  Dev: ['Analyze', 'Dev'],
  Test: ['Analyze', 'Dev', 'Test'],
  Debug: ['Analyze', 'Dev', 'Test', 'Debug'],
  Done: ['Analyze', 'Dev', 'Test', 'Done'],
}

const STORY_TITLES = [
  'Fix login redirect loop on expired session',
  'Add dark mode toggle to settings',
  'Refactor dashboard query for large teams',
  'Investigate flaky SignalR reconnect on Safari',
  'Add CSV export for backlog',
  'Improve error message on invalid invite email',
  'Support keyboard navigation on the board',
  'Add unit tests for sprint completion rollover',
  'Reduce bundle size of vendor chunk',
  'Fix timezone bug in due date display',
  'Add pagination to notifications panel',
  'Support drag-and-drop reordering within a column',
  'Add audit log export to CSV',
  'Improve mobile layout for the backlog table',
  'Add "assigned to me" quick filter',
  'Fix race condition on concurrent status updates',
  'Add email digest for weekly team activity',
  'Support markdown formatting in comments',
  'Add bulk unarchive action',
  'Improve empty states across all tabs',
  'Add sprint burndown chart',
  'Fix N+1 query in dashboard aggregation',
  'Add two-factor authentication option',
  'Support custom team avatars',
  'Add "duplicate story" action',
  'Fix broken link in password reset email template',
  'Add rate-limit warning banner',
  'Support filtering by multiple assignees at once',
  'Add webhook support for story status changes',
  'Improve loading skeleton on slow connections',
]

// NOTE: the `token` param here is now a raw Cookie header value (e.g.
// "access_token=...; refresh_token=..."), not a bearer JWT — the backend
// sets auth as httpOnly cookies now instead of returning tokens in the
// response body. Kept the param name as `token` at every call site below
// to avoid touching dozens of call sites; only this helper and the
// registration step (which extracts the cookie) needed to change.
async function api(path, { method = 'GET', token, body } = {}) {
  const response = await fetch(`${BASE_URL}${path}`, {
    method,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Cookie: token } : {}),
    },
    body: body ? JSON.stringify(body) : undefined,
  })

  if (!response.ok) {
    const text = await response.text().catch(() => '')
    throw new Error(`${method} ${path} -> ${response.status} ${text}`)
  }

  const contentType = response.headers.get('content-type') ?? ''
  return contentType.includes('application/json') ? response.json() : null
}

// Extracts just the "name=value" part of each Set-Cookie header (dropping
// HttpOnly/SameSite/Path/etc. attributes, which are only meaningful to a
// real browser) and joins them into one Cookie header value.
function extractCookieHeader(response) {
  const setCookies = response.headers.getSetCookie?.() ?? []
  return setCookies.map((c) => c.split(';')[0]).join('; ')
}

async function advanceStatus(token, storyId, targetStatus) {
  for (const step of PATH_TO[targetStatus]) {
    await api(`/userstories/${storyId}/status`, { method: 'PUT', token, body: { status: step } })
  }
}

function pick(arr, i) {
  return arr[i % arr.length]
}

async function main() {
  console.log(`Seeding against ${BASE_URL} ...`)

  // 1. Register 5 accounts.
  for (const member of MEMBERS) {
    const response = await fetch(`${BASE_URL}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: member.email, displayName: member.displayName, password: member.password }),
    })
    if (!response.ok) {
      const text = await response.text().catch(() => '')
      throw new Error(`POST /auth/register -> ${response.status} ${text}`)
    }
    const auth = await response.json()
    member.token = extractCookieHeader(response)
    member.userId = auth.userId
    console.log(`✓ Registered ${member.displayName} <${member.email}>`)
  }

  const owner = MEMBERS[0]
  const others = MEMBERS.slice(1)

  // 2. Owner creates the team.
  const team = await api('/teams', {
    method: 'POST',
    token: owner.token,
    body: { name: 'Rocket Team', description: 'Seeded test data — 5 members, 30 stories.' },
  })
  console.log(`✓ Created team "${team.name}" (${team.id})`)

  // 3. Owner invites the other 4; each of them logs in (already has a token
  // from registration) and accepts their own invitation.
  for (const member of others) {
    await api(`/teams/${team.id}/invitations`, { method: 'POST', token: owner.token, body: { email: member.email } })

    const invitations = await api('/invitations', { token: member.token })
    const invitation = invitations.find((i) => i.teamId === team.id)
    await api(`/invitations/${invitation.id}/accept`, { method: 'PUT', token: member.token })
    console.log(`✓ ${member.displayName} accepted the invite`)
  }

  // 4. One sprint, started, so the Sprints tab and sprint filter have something to show.
  const sprintStart = new Date()
  const sprintEnd = new Date(sprintStart)
  sprintEnd.setDate(sprintEnd.getDate() + 14)
  const sprint = await api(`/teams/${team.id}/sprints`, {
    method: 'POST',
    token: owner.token,
    body: { name: 'Sprint 1', startDate: sprintStart.toISOString(), endDate: sprintEnd.toISOString() },
  })
  await api(`/sprints/${sprint.id}/start`, { method: 'PUT', token: owner.token })
  console.log(`✓ Created and started "${sprint.name}"`)

  // 5. 30 stories, spread across status/priority/assignee/points/sprint.
  const createdStories = []
  for (let i = 0; i < STORY_TITLES.length; i++) {
    const title = STORY_TITLES[i]
    const description = i % 3 === 0 ? `Details for "${title}" — reported during regular usage.` : undefined

    const story = await api('/userstories', {
      method: 'POST',
      token: owner.token,
      body: { teamId: team.id, title, description },
    })

    const status = pick(STATUSES, i)
    const priority = pick(PRIORITIES, i + 1)
    const assignee = i % 6 === 5 ? null : pick(MEMBERS, i).userId // ~1 in 6 stays unassigned
    const storyPoints = i % 4 === 0 ? null : [1, 2, 3, 5, 8][i % 5]
    const inSprint = i % 2 === 0 // half the stories planned into the sprint, half left in backlog

    await advanceStatus(owner.token, story.id, status)
    await api(`/userstories/${story.id}/priority`, { method: 'PUT', token: owner.token, body: { priority } })
    if (assignee) {
      await api(`/userstories/${story.id}/assignee`, { method: 'PUT', token: owner.token, body: { assigneeId: assignee } })
    }
    if (storyPoints) {
      const current = await api(`/userstories/${story.id}`, { token: owner.token })
      await api(`/userstories/${story.id}`, {
        method: 'PUT',
        token: owner.token,
        body: { title, description, dueDate: null, storyPoints, expectedVersion: current.version },
      })
    }
    if (inSprint) {
      await api(`/userstories/${story.id}/sprint`, { method: 'PUT', token: owner.token, body: { sprintId: sprint.id } })
    }

    createdStories.push(story)
    console.log(`✓ [${i + 1}/${STORY_TITLES.length}] ${title} (${status}, ${priority})`)
  }

  // 6. A handful of comments (with a mention) so Collaboration has something to show too.
  const commentTargets = createdStories.slice(0, 4)
  for (const [i, story] of commentTargets.entries()) {
    const author = pick(MEMBERS, i)
    const mentioned = pick(others, i)
    await api('/comments', {
      method: 'POST',
      token: author.token,
      body: {
        userStoryId: story.id,
        content: `@${mentioned.userId} can you take a look at this one?`,
        mentionedUserIds: [mentioned.userId],
      },
    })
  }
  console.log(`✓ Added comments (with mentions) on ${commentTargets.length} stories`)

  console.log('\nDone. Log in as any of these to explore the seeded team:')
  for (const m of MEMBERS) {
    console.log(`  ${m.displayName.padEnd(20)} ${m.email}   (password: ${m.password})`)
  }
  console.log(`\nTeam: "${team.name}" — open it from "Your Teams" in the sidebar after logging in.`)
}

main().catch((err) => {
  console.error('\nSeeding failed:', err.message)
  process.exit(1)
})
