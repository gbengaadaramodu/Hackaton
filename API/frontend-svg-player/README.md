# Svg-2d player patch for NewFrontend

The session workspace (API folder) blocks direct writes outside itself, so copy
these three files over the matching paths in NewFrontend:

- `src/api/simulations.js` -> `src/api/simulations.js`
- `src/components/sim/SvgScenePlayer.jsx` -> `src/components/sim/SvgScenePlayer.jsx`
- `src/pages/sessions/SessionWorkspace.jsx` -> `src/pages/sessions/SessionWorkspace.jsx` (full wired copy)

What the wired copy does:

- Imports `getCatalog` and `SvgScenePlayer`.
- Derives `modelKey` from `session.modelKey` or `result.model.modelKey`.
- Finds the catalog simulation and uses its `parameters` (name, label, min,
  max, step, unit) for sliders. Falls back to 0-100 when no definition exists.
- Renders `<SvgScenePlayer modelKey={modelKey} state={session.state} />` above
  the controls, so the scene always matches applied (server) state.

Player behavior:

- POST `/api/v1/simulations/render` with `{ modelKey, state }`.
- Shows server SVG, Play/Pause scrub over `result.trajectory`, Refresh, and
  error text. Respects `prefers-reduced-motion` (no autoplay, Play disabled).

Run:

- Backend from `C:\OtherProject\Hackaton\API`: `dotnet run --project src/SimuLearn.Api` listens on `http://localhost:5109` per `Properties/launchSettings.json`. Set `VITE_API_URL=http://localhost:5109` for the frontend (default fallback is `http://localhost:5196`).
- Frontend from `C:\OtherProject\Hackaton\NewFrontend`: `npm run dev` on port 3001, then open `http://localhost:3001`, log in, launch a ramp, circuit, or independence session, move a slider, press Apply, and confirm the scene refreshes.

Verification status: contract verified statically (endpoint, camelCase shape,
trajectory fields, no script injection). Live curl/browser check not run in this
session because shell execution is unavailable.
