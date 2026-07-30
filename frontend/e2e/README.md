# E2E tests (Playwright)

These test real user flows against a running app + backend + MongoDB — not
mocked. Each test registers its own throwaway account (see `helpers.ts`), so
tests don't collide with each other or with real data across runs.

## Setup (one-time)

```
cd frontend
npm install
npx playwright install chromium
```

## Running

You need **both** the backend and frontend actually running first (these
tests don't start them for you):

```
# terminal 1
cd backend && dotnet run --project src/TodoApp.Api

# terminal 2
cd frontend && npm run dev

# terminal 3
cd frontend && npm run test:e2e
```

Or with the interactive UI (easier for debugging): `npm run test:e2e:ui`.

## What's covered

- `auth.spec.ts` — register → land on My Teams → log out → log back in;
  and a wrong-password attempt correctly shows an error instead of proceeding.
- `team-and-story.spec.ts` — create a team → create a story → move it through
  the workflow via the Backlog's status dropdown → confirm it shows up in the
  matching Board column.

## Not covered yet

- Actual drag-and-drop on the Board (simulating a real `@dnd-kit` drag in
  Playwright is finicky — the status-change assertion above exercises the
  same backend behavior via the dropdown instead).
- Collaboration features (comments, mentions, invitations) and SignalR-driven
  live updates across two simultaneous browser contexts.
- Any of this running in CI yet — `.github/workflows/ci.yml` only builds the
  frontend; wiring up a backend + MongoDB service container plus these tests
  in CI is a natural next step.
