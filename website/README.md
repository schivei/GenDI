# GenDI Documentation Website

This folder contains the Docusaurus website for GenDI documentation.

## Local development

```bash
cd website
npm ci
npm run start
```

## Production build

```bash
npm run build
npm run serve
```

## Content structure

- `docs/` - detailed product documentation in English
- `src/pages/` - homepage
- `src/css/` - global theme overrides
- `static/img/` - icons and visual assets

The visual theme is intentionally aligned with the `net-mediate` documentation style.

## Documentation parity rule

When Phase 6 behavior/status changes, update website docs in lockstep with:

- `/home/runner/work/GenDI/GenDI/docs/ROTEIRO_FASE6.md` (canonical status matrix)
- `/home/runner/work/GenDI/GenDI/README.md`
