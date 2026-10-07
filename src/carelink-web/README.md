# CareLink web client

React and TypeScript follow-up worklist for the CareLink assessment.

## Commands

```powershell
npm install --legacy-peer-deps
npm run dev
npm run lint
npm test
npm run build
```

The development server runs at `http://localhost:5173` and proxies `/api`
requests to the ASP.NET Core API at `http://localhost:5214`.

The UI includes facility/status filtering, sorting, pagination, manager and
clinic demonstration access profiles, responsive layouts, accessible labels,
and explicit loading, empty, validation, unauthorised, forbidden and general
error states.

The shared clinical theme is defined with semantic CSS tokens in `src/App.css`.
Inter weights 400, 500 and 600 are bundled as local WOFF2 assets through
`@fontsource/inter`; the application does not depend on an external font service.
