# Contributing

Contributions are welcome.

## Development baseline

1. Build and test locally:

```bash
dotnet build GenDI.slnx
dotnet test GenDI.slnx
```

2. If website files changed, validate docs build:

```bash
cd website
npm ci
npm run build
```

3. Keep changes scoped and consistent with repository architecture:

- attribute-first model
- generated registration/activation path
- NativeAOT-aware behavior

## Pull request guidance

- Describe behavior changes clearly.
- Include updated documentation for public-facing changes.
- Keep tests aligned with new behavior.
