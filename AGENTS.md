# CLAUDE.md

This file is the primary source of project-specific instructions for AI agents working on this codebase.
It should be kept up-to-date as architectural and tooling decisions are made.

> **Self-updating rule**: When a TBD item is decided during a session, propose updating the relevant
> section of this file before considering the task complete.

---

## Project Overview

Shoplists is a web application for grocery shopping. The MVP is deliberately simple: users own lists
of items that can be ticked on/off. More advanced features (sharing, purchase tracking, price analysis,
suggestions) will follow once the basics are solid and the app gets real usage.

See `README.md` for feature ideas.

---

## Tech Stack

### Frontend
- **Framework**: Nuxt 4 (Vue 3, TypeScript)
- **Rendering**: Hybrid — SSR on initial load, hydrating to SPA
- **BFF**: Implemented in Nuxt (Nitro server routes). The browser authenticates via a secure httpOnly
  cookie. The BFF extracts the access token from the encrypted cookie and proxies API calls to the
  backend with the token attached. Token and session management are the BFF's responsibility.
- **Language**: TypeScript with strict type safety as a quality goal
- **Node**: Latest LTS (currently v24)
- **Package manager**: npm
- **CSS**: Native CSS (with nesting). **No Tailwind — this is non-negotiable.**
- **UI library**: PrimeVue v4 (Aura preset, styled mode with design tokens / CSS variables)
- **API client generation**: TBD (possibly Nuxt Open Fetch module; decide based on current Nuxt best practices)
- **Testing**: Use Playwright via MCP tooling during implementation to functionally and visually verify
  changes. No Playwright test code in the repo — testing is done interactively by the agent.

### Backend
- **Runtime**: .NET 10
- **API style**: Minimal APIs
- **API documentation**: OpenAPI (UI TBD, likely Scalar)
- **ORM**: Entity Framework Core
- **Database**: PostgreSQL
- **Architecture**: TBD (clean architecture or vertical slices — to be decided in a dedicated session)
- **Mediator**: TBD (library choice pending due to MediatR license changes)
- **Error handling**: TBD (likely a result/error response pattern, not exceptions)
- **Testing**: Unit and integration tests, strict coverage expected. Framework TBD (likely TUnit).
  Exact setup to be decided in a dedicated session.
- **Solution structure**: TBD — exact projects, layering, and library choices to be defined later.
  The `backend/` folder will contain a `.slnx` solution file and the Aspire AppHost at minimum.

### Infrastructure & Local Development
- **Orchestration**: Aspire (AppHost) manages the full local stack — backend services, frontend
  dev server, PostgreSQL, and any other dependencies. 
- **To run locally**: Run `aspire run` from the repo root. It spins up everything.
  Aspire assigns ports dynamically — use the Aspire MCP to discover resource URLs.
- **Authentication**: OpenID Connect to an external provider (Pocket ID or Entra External ID).
  Auth is NOT a concern of this application — we plug in via standard OIDC on both frontend and backend.
- **CI/CD**: GitHub Actions (TBD). PR validations will enforce build, test, and lint checks.
  CD will build container images and push to a registry. Deployment is out of scope for now.

### MCP Tooling

The following MCP servers are configured in `.mcp.json` and available to agents:

| Server | Purpose | When to use |
|---|---|---|
| **nuxt** | Nuxt documentation, modules, deploy providers, changelog | Nuxt 4 API questions, module discovery, deployment config |
| **mslearn** | Microsoft Learn docs search, fetch, and code samples | .NET, EF Core, Aspire, and any Microsoft technology |
| **aspire** | Inspect running Aspire resources, logs, and traces | Local dev diagnostics — check resource status, discover dynamically assigned URLs, view logs, inspect traces |
| **playwright** | Full browser automation (navigate, click, fill, screenshot, snapshot) | UI verification during and after implementation — no Playwright test code in repo |
| **primevue** | PrimeVue v4 component docs, props, events, slots, theming, accessibility | PrimeVue component APIs, design tokens, passthrough, theming, usage examples |
| **context7** | Up-to-date documentation and code examples for any library | General-purpose doc lookup for libraries not covered by dedicated MCP servers |

**Tool selection guidance for documentation lookups:**
- Nuxt 4 / Vue / Nitro → **nuxt** MCP (primary), **context7** (supplementary)
- .NET / EF Core / Aspire / Minimal APIs → **mslearn** MCP (primary), **context7** (supplementary)
- PrimeVue v4 (components, theming, design tokens) → **primevue** MCP (primary), **context7** (supplementary)
- Other libraries (test frameworks, mediator libs, etc.) → **context7**
- Always prefer a dedicated MCP tool over web search when one exists for the technology.

---

## Project Structure

```
repo root/
  backend/
    Shoplists.slnx          -> .NET solution root
    Shoplists.AppHost/       -> Aspire AppHost (orchestrates entire stack)
    ...                      -> Future API/domain projects
  frontend/
    ...                      -> Nuxt application
```

---

## Development Practices

### Documentation-First for Bleeding-Edge Technologies

This project uses recent/bleeding-edge versions of many technologies (.NET 10, Nuxt 4, Aspire 13.2,
TUnit, etc.) that are likely underrepresented or inaccurately represented in training data.

**Hard rule**: Before implementing anything involving the technologies listed below, look up current
documentation using the appropriate MCP tool (see MCP Tooling section above). Do not rely on training
data for API shapes, configuration patterns, or library usage. Plan the approach using verified
documentation before writing code.

Technologies requiring doc verification:
- Aspire (all versions — not ".NET Aspire", just "Aspire" since the polyglot rebrand) → **mslearn**
- TUnit (or whichever test framework is chosen) → **context7**
- Nuxt 4 → **nuxt** MCP
- PrimeVue v4 (design tokens, component APIs) → **primevue** MCP
- Any mediator library chosen → **context7**
- EF Core with .NET 10 (verify new APIs/patterns) → **mslearn**
- Any other library added that is < 1 year old or in preview → **context7**

Always prefer a dedicated MCP tool over web search. See the MCP Tooling section for tool selection.

### Code Quality & Validation

Before considering any task complete:
1. **Build** — all projects must compile without errors or warnings
2. **Tests** — all existing tests must pass
3. **Lint** — all configured linters must pass
4. **Format** — all configured formatters must pass (CSharpier for C#, others TBD)
5. **Playwright verification** — for UI changes, use the Playwright MCP to visually and functionally
   verify the result

Code style is enforced by tooling (`.editorconfig`, CSharpier, ESLint/Prettier for frontend — exact
config TBD). Follow whatever these tools dictate. Do not override or bypass them.

### API Contract Sync

OpenAPI specs generated by the backend are the source of truth for API contracts. The frontend API
client should be generated from these specs so types stay in sync. Exact tooling TBD.

### UI Design

- **Mobile-first**: Design and implement for mobile viewports first.
- **Desktop enhancement**: Gracefully enhance for larger screens — do not just stretch the mobile layout.
- Both mobile and desktop are primary use cases.

### Agent Conduct

- Challenge ideas and suggestions when better approaches exist. The goal is good software engineering,
  not blind execution of requests.
- When making architectural or tooling decisions, propose updating this file.
- Prefer small, incremental changes. Avoid large rewrites unless explicitly discussed and approved.
- When uncertain about requirements or approach, ask — do not assume.

---

## TBD Decisions Tracker

The following decisions are explicitly deferred. Each should be resolved in a dedicated session and
this file updated accordingly.

| Decision | Notes | Status |
|---|---|---|
| Backend architecture pattern | Clean architecture vs vertical slices | Not started |
| Backend solution structure | Project layout, layering | Not started |
| Mediator library | MediatR alternatives due to license changes | Not started |
| Result/error pattern library | For application layer error handling | Not started |
| Test framework & setup | Likely TUnit; integration test infrastructure | Not started |
| UI library | PrimeVue v4 with Aura preset, styled mode | **Decided** |
| CSS approach details | Native CSS with nesting for now | Not started |
| API client generation | Nuxt Open Fetch or alternative | Not started |
| API documentation UI | Likely Scalar | Not started |
| Code style configuration | Frontend: @nuxt/eslint + @antfu/eslint-config (ESLint Stylistic, no Prettier). Backend: CSharpier (TBD). .editorconfig in place. | **Decided (frontend)** |
| CI/CD pipeline | GitHub Actions configuration | Not started |
