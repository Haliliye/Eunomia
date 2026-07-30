// Seeds a rich demo dataset that touches every Phase 1 + Phase 2 feature:
// 7 accounts split into two 4-person teams (one person shared across both),
// 25 user stories, labels, checklists, recurrence, due dates (including
// overdue ones), time tracking, file attachments, RBAC roles, comments with
// mentions, an active sprint per team, a few archived stories, and personal
// tasks (including one converted to a team story) for the shared member —
// so My Tasks, My Work, Calendar, and every tab has something real to show.
//
// Goes through the real API (not direct Mongo writes), same as
// seed-test-data.mjs — see that file's header for why.
//
// Usage:
//   node scripts/seed-phase2-demo.mjs
//   API_BASE_URL=http://localhost:5000/api node scripts/seed-phase2-demo.mjs
//
// Requires Node 18+ (built-in fetch/FormData/Blob) and the backend + MongoDB running.
// Windows/VS https://localhost:5001 self-signed cert:
//   NODE_TLS_REJECT_UNAUTHORIZED=0 node scripts/seed-phase2-demo.mjs

const BASE_URL = process.env.API_BASE_URL || 'http://localhost:5000/api'
const RUN_ID = Date.now()

const STATUSES = ['ToDo', 'Analyze', 'Dev', 'Test', 'Debug', 'Done']
const PATH_TO = {
  ToDo: [],
  Analyze: ['Analyze'],
  Dev: ['Analyze', 'Dev'],
  Test: ['Analyze', 'Dev', 'Test'],
  Debug: ['Analyze', 'Dev', 'Test', 'Debug'],
  Done: ['Analyze', 'Dev', 'Test', 'Done'],
}

// NOTE: the `token` param here is now a raw Cookie header value (e.g.
// "access_token=...; refresh_token=..."), not a bearer JWT — the backend
// sets auth as httpOnly cookies now instead of returning tokens in the
// response body. Kept the param name as `token` at every call site below
// to avoid touching dozens of call sites; only this helper and
// registerAll() (which extracts the cookie) needed to change.
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

// Separate from api() because multipart uploads must NOT have a manually set
// Content-Type — fetch needs to generate the boundary itself.
async function upload(path, { token, fileName, content, mimeType = 'text/plain' } = {}) {
  const formData = new FormData()
  formData.append('file', new Blob([content], { type: mimeType }), fileName)

  const response = await fetch(`${BASE_URL}${path}`, {
    method: 'POST',
    headers: { Cookie: token },
    body: formData,
  })

  if (!response.ok) {
    const text = await response.text().catch(() => '')
    throw new Error(`POST ${path} (upload) -> ${response.status} ${text}`)
  }

  return response.json()
}

async function advanceStatus(token, storyId, targetStatus) {
  for (const step of PATH_TO[targetStatus]) {
    await api(`/userstories/${storyId}/status`, { method: 'PUT', token, body: { status: step } })
  }
}

function daysFromNow(n) {
  const d = new Date()
  d.setDate(d.getDate() + n)
  return d.toISOString()
}

// --- People -----------------------------------------------------------
// Can is deliberately on both teams, so My Work / My Tasks / cross-team
// assignment has something real to combine.
const PEOPLE = [
  { key: 'ayse', displayName: 'Ayşe Kara' },
  { key: 'mert', displayName: 'Mert Doğan' },
  { key: 'zeynep', displayName: 'Zeynep Aksoy' },
  { key: 'can', displayName: 'Can Yıldız' },
  { key: 'selin', displayName: 'Selin Koç' },
  { key: 'burak', displayName: 'Burak Er' },
  { key: 'deniz', displayName: 'Deniz Polat' },
].map((p) => ({
  ...p,
  email: `${p.key}.${RUN_ID}@example.com`,
  password: 'SeedPassword123!',
}))

const byKey = (key) => PEOPLE.find((p) => p.key === key)

// --- Story content, split across the two teams -------------------------
const NOVA_STORIES = [
  { title: 'Fix login redirect loop on expired session', status: 'Done', priority: 'High', points: 3 },
  { title: 'Add dark mode toggle to settings', status: 'Done', priority: 'Medium', points: 2 },
  { title: 'Investigate flaky SignalR reconnect on Safari', status: 'Debug', priority: 'Critical', points: 5, overdue: true },
  { title: 'Add CSV export for backlog', status: 'Test', priority: 'Medium', points: 3 },
  { title: 'Improve error message on invalid invite email', status: 'Dev', priority: 'Low', points: 1 },
  { title: 'Support keyboard navigation on the board', status: 'Analyze', priority: 'Medium', points: 5 },
  { title: 'Add unit tests for sprint completion rollover', status: 'ToDo', priority: 'High', points: 3, dueSoon: true },
  { title: 'Reduce bundle size of vendor chunk', status: 'ToDo', priority: 'Low', points: 2 },
  { title: 'Fix timezone bug in due date display', status: 'ToDo', priority: 'Critical', points: 2, overdue: true },
  { title: 'Weekly dependency audit', status: 'ToDo', priority: 'Low', points: 1, recurrence: 'Weekly' },
  { title: 'Daily standup notes cleanup', status: 'ToDo', priority: 'Low', points: 1, recurrence: 'Daily' },
  { title: 'Add pagination to notifications panel', status: 'ToDo', priority: 'Medium', points: 3, archived: true },
  { title: 'Support drag-and-drop reordering within a column', status: 'ToDo', priority: 'Low', points: 5, archived: true },
]

const ORION_STORIES = [
  { title: 'Add audit log export to CSV', status: 'Done', priority: 'Medium', points: 3 },
  { title: 'Improve mobile layout for the backlog table', status: 'Done', priority: 'Low', points: 2 },
  { title: 'Add "assigned to me" quick filter', status: 'Test', priority: 'Medium', points: 2 },
  { title: 'Fix race condition on concurrent status updates', status: 'Debug', priority: 'Critical', points: 5, overdue: true },
  { title: 'Add email digest for weekly team activity', status: 'Dev', priority: 'Medium', points: 5 },
  { title: 'Support markdown formatting in comments', status: 'Analyze', priority: 'Low', points: 3 },
  { title: 'Add bulk unarchive action', status: 'ToDo', priority: 'Low', points: 1 },
  { title: 'Improve empty states across all tabs', status: 'ToDo', priority: 'Medium', points: 2, dueSoon: true },
  { title: 'Add sprint burndown chart', status: 'ToDo', priority: 'High', points: 8, overdue: true },
  { title: 'Monthly security dependency scan', status: 'ToDo', priority: 'High', points: 2, recurrence: 'Monthly' },
  { title: 'Add two-factor authentication option', status: 'ToDo', priority: 'High', points: 8 },
  { title: 'Fix broken link in password reset email template', status: 'ToDo', priority: 'Low', points: 1, archived: true },
]

const LABELS = [
  { name: 'Bug', color: '#C23B6B' },
  { name: 'Feature', color: '#2B6CB5' },
  { name: 'Tech Debt', color: '#B48A0A' },
]

async function registerAll() {
  for (const person of PEOPLE) {
    const response = await fetch(`${BASE_URL}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: person.email, displayName: person.displayName, password: person.password }),
    })
    if (!response.ok) {
      const text = await response.text().catch(() => '')
      throw new Error(`POST /auth/register -> ${response.status} ${text}`)
    }
    const auth = await response.json()
    person.token = extractCookieHeader(response)
    person.userId = auth.userId
    console.log(`✓ Registered ${person.displayName} <${person.email}>`)
  }
}

async function buildTeam({ name, description, ownerKey, adminKey, memberKeys, stories, labelSeed }) {
  const owner = byKey(ownerKey)
  const admin = byKey(adminKey)
  const members = memberKeys.map(byKey)

  const team = await api('/teams', { method: 'POST', token: owner.token, body: { name, description } })
  console.log(`✓ Created team "${team.name}" (${team.id})`)

  // The admin-to-be must actually join the team before they can be promoted —
  // invite/accept them alongside the plain members, not separately.
  const toInvite = [admin, ...members]
  for (const member of toInvite) {
    await api(`/teams/${team.id}/invitations`, { method: 'POST', token: owner.token, body: { email: member.email } })
    const invitations = await api('/invitations', { token: member.token })
    const invitation = invitations.find((i) => i.teamId === team.id)
    await api(`/invitations/${invitation.id}/accept`, { method: 'PUT', token: member.token })
  }
  console.log(`✓ ${toInvite.map((m) => m.displayName).join(', ')} joined ${team.name}`)

  await api(`/teams/${team.id}/members/${admin.userId}/role`, { method: 'PUT', token: owner.token, body: { role: 'Admin' } })
  console.log(`✓ ${admin.displayName} promoted to Admin on ${team.name}`)

  // Labels (US-125) — owner-only.
  const labels = {}
  for (const label of labelSeed) {
    const created = await api(`/teams/${team.id}/labels`, { method: 'POST', token: owner.token, body: label })
    labels[label.name] = created.id
  }
  console.log(`✓ Created ${labelSeed.length} labels on ${team.name}`)

  // Active sprint.
  const sprint = await api(`/teams/${team.id}/sprints`, {
    method: 'POST',
    token: owner.token,
    body: { name: `${name} — Sprint 1`, startDate: daysFromNow(-3), endDate: daysFromNow(11) },
  })
  await api(`/sprints/${sprint.id}/start`, { method: 'PUT', token: owner.token })
  console.log(`✓ Started "${sprint.name}"`)

  const allMembers = [owner, admin, ...members]
  const createdStories = []

  for (let i = 0; i < stories.length; i++) {
    const spec = stories[i]
    const description = i % 2 === 0 ? `Details for "${spec.title}" — reported during regular usage.` : undefined

    const story = await api('/userstories', { method: 'POST', token: owner.token, body: { teamId: team.id, title: spec.title, description } })

    await advanceStatus(owner.token, story.id, spec.status)
    await api(`/userstories/${story.id}/priority`, { method: 'PUT', token: owner.token, body: { priority: spec.priority } })

    const assignee = allMembers[i % allMembers.length]
    await api(`/userstories/${story.id}/assignee`, { method: 'PUT', token: owner.token, body: { assigneeId: assignee.userId } })

    // Due dates: some overdue (for the overdue-highlight + reminder demo),
    // some due soon (Calendar's "this week" cluster), most spread further out.
    let dueDate = null
    if (spec.overdue) dueDate = daysFromNow(-2)
    else if (spec.dueSoon) dueDate = daysFromNow(1)
    else if (i % 3 === 0) dueDate = daysFromNow(5 + i)

    const current = await api(`/userstories/${story.id}`, { token: owner.token })
    await api(`/userstories/${story.id}`, {
      method: 'PUT',
      token: owner.token,
      body: { title: spec.title, description, dueDate, storyPoints: spec.points, expectedVersion: current.version },
    })

    // Half the stories planned into the sprint, half left in the backlog.
    if (i % 2 === 0) {
      await api(`/userstories/${story.id}/sprint`, { method: 'PUT', token: owner.token, body: { sprintId: sprint.id } })
    }

    // Labels — rotate through whatever this team has.
    const labelNames = Object.keys(labels)
    if (labelNames.length > 0) {
      await api(`/userstories/${story.id}/labels/${labels[labelNames[i % labelNames.length]]}`, { method: 'PUT', token: owner.token })
    }

    // Checklist on every third story.
    if (i % 3 === 0) {
      const items = ['Write tests', 'Update docs', 'Get review']
      for (const [j, text] of items.entries()) {
        const item = await api(`/userstories/${story.id}/checklist-items`, { method: 'POST', token: owner.token, body: { text } })
        if (j === 0) await api(`/userstories/${story.id}/checklist-items/${item.id}/toggle`, { method: 'PUT', token: owner.token })
      }
    }

    // Recurrence.
    if (spec.recurrence) {
      await api(`/userstories/${story.id}/recurrence`, { method: 'PUT', token: owner.token, body: { frequency: spec.recurrence, endDate: null } })
    }

    // Time tracking on about a third of the stories.
    if (i % 3 === 1) {
      await api(`/userstories/${story.id}/estimate`, { method: 'PUT', token: owner.token, body: { hours: spec.points ? spec.points * 2 : 4 } })
      await api(`/userstories/${story.id}/time-logs`, { method: 'POST', token: assignee.token, body: { hours: 1.5, note: 'Initial investigation' } })
      if (spec.status === 'Done') {
        await api(`/userstories/${story.id}/time-logs`, { method: 'POST', token: assignee.token, body: { hours: 2, note: 'Finished implementation' } })
      }
    }

    // A small text attachment on the first two stories of each team.
    if (i < 2) {
      await upload(`/userstories/${story.id}/attachments`, {
        token: owner.token,
        fileName: 'notes.txt',
        content: `Notes for "${spec.title}"\nSeeded ${new Date().toISOString()}\n`,
      })
    }

    // Archive a few, at the very end (after everything else so they still show up in Activity history).
    if (spec.archived) {
      await api(`/userstories/${story.id}/archive`, { method: 'PUT', token: owner.token })
    }

    createdStories.push({ ...story, assigneeId: assignee.userId })
    console.log(`  ✓ [${team.name}] [${i + 1}/${stories.length}] ${spec.title} (${spec.status}, ${spec.priority})`)
  }

  // A couple of comments with mentions.
  for (const [i, story] of createdStories.slice(0, 3).entries()) {
    const author = allMembers[i % allMembers.length]
    const mentioned = allMembers[(i + 1) % allMembers.length]
    await api('/comments', {
      method: 'POST',
      token: author.token,
      body: { userStoryId: story.id, content: `@${mentioned.userId} can you take a look at this one?`, mentionedUserIds: [mentioned.userId] },
    })
  }
  console.log(`✓ Added comments (with mentions) on ${team.name}`)

  return { team, sprint, stories: createdStories }
}

async function seedPersonalTasksForCan(nova) {
  const can = byKey('can')

  await api('/personal-tasks', { method: 'POST', token: can.token, body: { title: 'Renew passport', dueDate: daysFromNow(20) } })
  await api('/personal-tasks', { method: 'POST', token: can.token, body: { title: 'Book dentist appointment', dueDate: daysFromNow(-1) } })
  const toConvert = await api('/personal-tasks', { method: 'POST', token: can.token, body: { title: 'Draft proposal for new onboarding flow', description: 'Sketch out before bringing to the team.' } })
  await api('/personal-tasks', { method: 'POST', token: can.token, body: { title: 'Read up on the new CSV import feature' } })

  // Convert one into a real team story (US-141) — demonstrates the personal -> team hand-off.
  await api(`/personal-tasks/${toConvert.id}/convert`, { method: 'POST', token: can.token, body: { teamId: nova.team.id } })

  console.log('✓ Seeded personal tasks for Can Yıldız (including one converted to a Nova Team story)')
}

async function main() {
  console.log(`Seeding phase-2 demo data against ${BASE_URL} ...\n`)

  await registerAll()

  const nova = await buildTeam({
    name: 'Nova Team',
    description: 'Product squad — seeded Phase 2 demo data.',
    ownerKey: 'ayse',
    adminKey: 'mert',
    memberKeys: ['zeynep', 'can'],
    stories: NOVA_STORIES,
    labelSeed: LABELS,
  })

  const orion = await buildTeam({
    name: 'Orion Team',
    description: 'Platform squad — seeded Phase 2 demo data.',
    ownerKey: 'selin',
    adminKey: 'burak',
    memberKeys: ['deniz', 'can'],
    stories: ORION_STORIES,
    labelSeed: LABELS,
  })

  await seedPersonalTasksForCan(nova)

  console.log(`\nTotal user stories created: ${nova.stories.length + orion.stories.length}`)
  console.log('\nDone. Log in as any of these to explore the seeded data:')
  for (const p of PEOPLE) {
    console.log(`  ${p.displayName.padEnd(16)} ${p.email}   (password: ${p.password})`)
  }
  console.log(`\n  Nova Team:  Ayşe Kara (Owner), Mert Doğan (Admin), Zeynep Aksoy, Can Yıldız`)
  console.log(`  Orion Team: Selin Koç (Owner), Burak Er (Admin), Deniz Polat, Can Yıldız`)
  console.log(`\n  Log in as Can Yıldız (${byKey('can').email}) to see My Work / My Tasks combine`)
  console.log(`  personal tasks with story assignments across BOTH teams.`)
}

main().catch((err) => {
  console.error('\nSeeding failed:', err.message)
  process.exit(1)
})
