# CLAUDE.md

## Project
AI Landing Page Generator. The user describes their startup or product in
a text area. The app sends that description to the Claude API and renders
a complete, single-page marketing landing page generated from the response.

## Stack
- Next.js (App Router, TypeScript, src/ directory)
- Tailwind CSS for styling
- Claude API (model: claude-sonnet-4-6) called from a Next.js API route
  (never from the client — the API key must stay server-side)

## How the app works
1. `/` renders a form: a textarea for the startup description and a
   "Generate" button.
2. On submit, the client calls `POST /api/generate`.
3. The API route sends the description to the Claude API, asking it to
   return structured JSON describing landing page sections (hero headline,
   subheadline, 3 feature blocks, call-to-action text).
4. The client renders that JSON as a styled landing page preview on the
   same page, below the form.
5. Show a loading state while waiting on the API call, and a clear error
   state if the call fails.

## Design direction
Use the frontend-design skill for the generated landing page's visual
design. Each generated landing page should feel distinct and intentional
for the described startup — not a generic templated layout. Avoid the
default AI-generated look (cream background + serif + terracotta, or
black + neon accent, or broadsheet newspaper style) unless the startup
description specifically calls for one of those.

## Conventions
- TypeScript everywhere, strict mode on
- Keep components in `src/components/`
- Keep the API route in `src/app/api/generate/route.ts`
- Read the Anthropic API key from `process.env.ANTHROPIC_API_KEY`,
  never hardcode it
- Keep the form and the generated output on the same page (no routing
  between them) for a fast, single-screen demo experience

## Out of scope for this version
- No user accounts or saved history
- No deployment config beyond what's needed to run locally