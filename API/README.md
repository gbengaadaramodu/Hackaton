# SimuLearn API

The C# backend for the Generative Interactive Educational Simulators pilot described in
[`Generative_Interactive_Educational_Simulators_BRD_SRS.md`](../Generative_Interactive_Educational_Simulators_BRD_SRS.md).

It is a modular monolith on **ASP.NET Core 10** that owns the parts the brief treats as
non-negotiable:

- **Deterministic physics** lives in a small, allowlisted set of simulation models. A language model
  can choose *which* model and *which* starting values to use, but it never computes a result and
  never emits code, HTML or URLs.
- **Server-authoritative sessions** with optimistic concurrency, snapshots and learning events.
- **Grounded tutoring**: every number in a tutor reply is checked against the current calculated
  state (or comes from the deterministic explanation), and AI output is always labelled.
- **Curriculum workflow**: reviewer-gated publication, class rosters, assignments, assessments and
  teacher reports.
- **Safety basics**: JWT auth with role policies, per-user rate limits, idempotent POSTs,
  request validation, RFC 7807 errors, correlation ids, and audit records for privileged actions.

## Solution layout

| Project | Responsibility |
|---|---|
| `src/SimuLearn.Domain` | Entities, simulation models, scoring and scene validation. No infrastructure references. |
| `src/SimuLearn.Application` | Use-case services, DTO contracts, abstractions (AI, persistence, events). |
| `src/SimuLearn.Infrastructure` | EF Core context, migrations, seeding, JWT/PBKDF2, OpenAI-compatible chat client. |
| `src/SimuLearn.Api` | Minimal-API endpoints, auth policies, middleware, rate limiting, idempotency, health. |
| `tests/SimuLearn.Tests` | xUnit v3 unit tests (physics, scoring, scene rules) and API integration tests. |

The dependency direction is `Api → Infrastructure → Application → Domain`, so the physics and
curriculum rules stay testable without a database or an HTTP host.

## Quick start

Requirements: **.NET 10 SDK** (the projects target `net10.0`).

```bash
cd API
dotnet build
dotnet run --project src/SimuLearn.Api
```

The development profile listens on <http://localhost:5109>:

- `GET /` – service banner
- `GET /api/v1/health` – health report, including database connectivity
- `/scalar/v1` – interactive API reference (Development only), with a bearer-token field so you can
  call protected routes after `POST /api/v1/auth/login`
- `/openapi/v1.json` – OpenAPI 3.1 document; the `bearer` scheme is attached only to routes that
  require authentication

Migrations run at startup and demo data is seeded on the first run. The SQLite file is created at
`src/SimuLearn.Api/simulearn.db` (relative `Data Source` values are resolved against the content
root, so the database does not depend on the working directory).

### Demo accounts

All demo users share the password from `Seed:Password` (`SimuLearn2026!` in
`appsettings.Development.json`):

| Email | Role |
|---|---|
| `student@simulearn.demo` / `student2@simulearn.demo` | Student |
| `teacher@simulearn.demo` | Teacher (owns the seeded class and lessons) |
| `reviewer@simulearn.demo` | Content reviewer |
| `admin@simulearn.demo` | Administrator |

Seeded content: Physics subject with two objectives (`PHY-MECH-INCLINE`,
`PHY-ELEC-OHMS-LAW`), two published lessons, two published assessments, one class (`PHY202`) with
two students, and one assignment.

```bash
TOKEN=$(curl -s -X POST http://localhost:5109/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"student@simulearn.demo","password":"SimuLearn2026!"}' | jq -r .accessToken)

curl -s http://localhost:5109/api/v1/catalog -H "Authorization: Bearer $TOKEN" | jq
```

## The learning loop

1. `POST /api/v1/simulation-requests` with a natural-language prompt (or `POST /api/v1/sessions`
   with a `modelKey`, `lessonId` or `assignmentId`).
2. The request interpreter picks an allowlisted model and parameters, either through the configured
   chat model or the deterministic keyword interpreter when no provider is configured.
3. The scene validator re-checks the proposal: unknown model keys, unknown parameters, out-of-range
   values, duplicate controls and unexpected JSON members are rejected. The server — not the
   provider — decides labels, ranges and controls.
4. `POST /api/v1/sessions` starts a session and stores the initial state plus a snapshot.
5. `POST /api/v1/sessions/{id}/commands` applies validated parameter changes with an optional
   `expectedVersion` for optimistic concurrency; the response carries the recalculated
   observations and a deterministic explanation of the physics.
6. `POST /api/v1/sessions/{id}/messages` asks the grounded tutor; `POST .../comparisons` compares a
   candidate state with the session's starting state.
7. `POST /api/v1/sessions/{id}/status` completes or abandons the session; teachers read progress
   through `/api/v1/reports/...`.

## Endpoints

All routes are under `/api/v1` and require a bearer token unless noted.

| Area | Routes |
|---|---|
| Auth | `POST /auth/register` (anonymous), `POST /auth/login` (anonymous), `GET /auth/me` |
| Catalog | `GET /catalog`, `GET /simulations/templates`, `GET /simulations/templates/{modelKey}`, `POST /simulations/preview` |
| Requests | `POST /simulation-requests`, `GET /simulation-requests`, `GET /simulation-requests/{id}` |
| Sessions | `POST /sessions`, `GET /sessions`, `GET /sessions/{id}`, `POST /sessions/{id}/commands`, `POST /sessions/{id}/messages`, `GET /sessions/{id}/messages`, `POST /sessions/{id}/comparisons`, `POST /sessions/{id}/reset`, `POST /sessions/{id}/status` |
| Lessons | `POST /lessons`, `GET /lessons`, `GET /lessons/{id}`, `PUT /lessons/{id}`, `POST /lessons/{id}/submit`, `POST /lessons/{id}/review`, `POST /lessons/{id}/publish`, `POST /lessons/{id}/duplicate`, `GET /lessons/{id}/assessments` |
| Classes | `POST /classes`, `POST /classes/join`, `GET /classes`, `GET /classes/{id}`, `GET /classes/{id}/members`, `POST /assignments`, `GET /assignments` |
| Assessments | `GET /assessments`, `GET /assessments/{id}`, `POST /assessments/{id}/attempts`, `GET /assessments/attempts/mine` |
| Reports | `GET /reports/classes/{id}`, `GET /reports/students/{id}` |
| Admin | `GET /admin/users`, `PUT /admin/users/{id}/roles`, `POST /admin/users/{id}/status`, `GET /admin/content-reports`, `PUT /admin/content-reports/{id}`, `GET /admin/audit`, `POST /content-reports` |
| Health | `GET /health` (anonymous) |

### Idempotency

`POST /sessions`, `POST /sessions/{id}/commands` and `POST /sessions/{id}/messages` accept an
`Idempotency-Key` header. A successful response is stored for 24 hours and replayed for an identical
retry; reusing the same key with a different body returns `409 idempotency_key_reuse`.

### Errors

Failures return `application/problem+json` with a stable `code` (`validation_failed`,
`version_conflict`, `session_closed`, `rate_limited`, `invalid_request`, …), a `traceId` and, for
validation failures, an `errors` map keyed by parameter or field name.

## Configuration

Configuration is standard ASP.NET Core (`appsettings*.json`, environment variables, user secrets).

| Key | Default | Notes |
|---|---|---|
| `Database:Provider` | `sqlite` | `sqlite` or `postgres` (`postgresql` is accepted) |
| `Database:Migrate` | `true` | Apply pending migrations at startup |
| `ConnectionStrings:Default` | `Data Source=simulearn.db` | Relative SQLite paths are resolved against the content root |
| `Jwt:Issuer` / `Jwt:Audience` | `simulearn` / `simulearn-client` | Token issuer and audience |
| `Jwt:SigningKey` | *(empty)* | HMAC key; **required** outside local development |
| `Jwt:AccessTokenMinutes` | `120` | Access token lifetime |
| `Ai:Enabled` | `true` | Master switch for generative calls |
| `Ai:BaseUrl` / `Ai:ApiKey` / `Ai:Model` | *(empty)* | Any OpenAI-compatible chat completion endpoint |
| `Ai:ProviderName` | `openai-compatible` | Recorded on AI-labelled content |
| `Ai:TimeoutSeconds` | `30` | Per-call timeout |
| `Seed:Enabled` | `false` | Seed demo curriculum, lessons, assessments and accounts |
| `Seed:Password` | *(empty)* | Shared demo password, minimum 10 characters |
| `Cors:AllowedOrigins` | `http://localhost:5173`, `http://localhost:3000` | Browser origins allowed to call the API |

Set secrets with environment variables or `dotnet user-secrets`; never commit `Jwt:SigningKey` or
`Ai:ApiKey`. Without AI configuration the platform is fully functional: interpretation falls back to
keyword matching and tutoring to deterministic, grounded explanations, and responses report
`usedFallback: true`.

### PostgreSQL

```bash
export Database__Provider=postgres
export ConnectionStrings__Default="Host=localhost;Database=simulearn;Username=…;Password=…"
dotnet ef database update -p src/SimuLearn.Infrastructure -s src/SimuLearn.Api
```

SQLite stores `DateTimeOffset` values as UTC ticks (see `SimuLearnDbContext`), which keeps
timestamp ordering and comparisons translatable on both providers.

## Simulation models

Two models ship with the pilot, both in `src/SimuLearn.Domain/Simulations/Models`:

| Model key | Relationship | Parameters |
|---|---|---|
| `inclined-plane-v1` | `a = g(sin θ − μk cos θ)` | mass, angle, friction coefficient, ramp length |
| `simple-dc-circuit-v1` | `I = V / R`, `P = V·I` | source voltage, resistance |

To add one: derive from `SimulationModelBase`, declare parameters, keywords and concept notes,
implement `Compute` (observations, trajectory, summary, grounding values) and `ExplainChange`, then
register it in `DependencyInjection.AddInfrastructure`. Until it is registered, the model key is
rejected by the registry and the scene validator.

## Roles

| Capability | Student | Teacher | Reviewer | Admin |
|---|---|---|---|---|
| Explore simulations, ask the tutor, take assessments | ✓ | ✓ | ✓ | ✓ |
| Create/edit lessons, publish approved drafts | | ✓ | | ✓ |
| Review lessons, handle content reports | | | ✓ | ✓ |
| Class rosters, assignments, class reports | | ✓ | | ✓ |
| Users, roles, audit log | | | | ✓ |

A decision record is stored per review (`ContentReview`) and publication copies the draft
configuration into an immutable published version.

## Tests

```bash
cd API
dotnet test --project tests/SimuLearn.Tests
```

The suite covers:

- **Physics and validation** – Ohm's law, power, the inclined-plane equation, the at-rest friction
  case, range/unknown-parameter rejection, defaulting and clamping, registry allowlisting.
- **Assessment scoring** – single choice, exact-match multi-select, numeric tolerance, missing
  answers, pass threshold.
- **Scene validation** – canonicalisation by the server, unknown model keys, out-of-range initial
  state, unexpected JSON members, duplicate controls.
- **Integration** – the API is hosted with `WebApplicationFactory` against a throwaway SQLite
  database: authentication and role policies, the prompt-to-session loop, version conflicts,
  idempotent retries, malformed bodies, the lesson review workflow, grounded tutoring, assessment
  attempts, class reports and audit access.

## Operations notes

- Each request carries `X-Correlation-Id` (accepted from the client when well-formed); it appears in
  logs, error payloads and audit entries.
- Rate limits: 20 requests/minute on auth endpoints, 30 requests/minute on tutor calls, partitioned
  by user id (or client address when anonymous).
- Health: `/api/v1/health` reports overall status plus the database check.
- Logs never include passwords, tokens or full prompts at Information level.
- Learning events (`LearningEvents`) and security audit entries (`AuditEvents`) are written in the
  same transaction as the operation that produced them.

## Known gaps and next steps

- **Frontend**: the browser renderer for `svg-2d` scenes is not part of this repository yet
  (see the `Frontend` folder for work in progress).
- **Auth hardening**: only short-lived access tokens exist; refresh tokens, revocation and password
  reset are not implemented. `Jwt:SigningKey` must be supplied by the deployment environment.
- **Content authoring UI**: lessons and assessments are created through the API; assessment authoring
  endpoints are limited to seeded/attached content.
- **Retention**: session snapshots and conversations have no automatic retention job yet
  (NFR-007 requires one before production).
- **Multi-tenancy**: institution ids are stored and filtered on owner identity, but no
  cross-institution isolation tests exist beyond role checks.
- **Postgres**: the provider is wired and migrations are provider-agnostic, but CI only exercises
  SQLite.
