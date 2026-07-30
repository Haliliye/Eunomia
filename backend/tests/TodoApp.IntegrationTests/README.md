# Integration tests

Unlike `TodoApp.UnitTests` (mocked repositories), these tests run against a
**real MongoDB** container via [Testcontainers](https://dotnet.testcontainers.org/).
They exist specifically to catch the class of bug that mocks can't: real
Mongo query-translation behavior (the `ExpressionNotSupportedException` we
hit early on), whether the real text index actually backs keyword search,
and whether `MongoIndexInitializer` actually creates the indexes it claims to.

## Requirements

Docker must be running locally (Docker Desktop on Windows/macOS, or the
Docker daemon on Linux). GitHub Actions' `ubuntu-latest` runner has Docker
preinstalled, so CI needs no extra setup.

## Running

```
cd backend
dotnet test tests/TodoApp.IntegrationTests
```

The first run pulls the `mongo:7` image, which takes a minute; subsequent
runs are fast since the image is cached. One container is shared across all
test classes in the run (`MongoFixture` + `[Collection("Mongo collection")]`)
rather than spinning up a fresh one per test — much faster, and safe since
every test creates its own uniquely-named/ided data.

## What's covered vs. not

Covered: the Mongo-specific repository behaviors most likely to break
silently (`TeamRepository`'s `ElemMatch` query, `UserStoryRepository`'s text
index search + combined filters, `MongoIndexInitializer`'s index creation).

Not covered here: Application-layer command/query handlers (already covered
by mocked unit tests), controllers/HTTP (would need a `WebApplicationFactory`-
based test host — a natural next addition if you want true end-to-end API
tests), and the auth/refresh-token flow's Mongo persistence (`RefreshTokenRepository`,
`UserRepository`, `InvitationRepository` don't have integration tests yet —
same pattern as the ones here, just not written).
