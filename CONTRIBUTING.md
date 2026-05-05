# Contributing to GenDI

Thank you for your interest in contributing to GenDI!

---

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Opening Issues](#opening-issues)
- [Submitting Pull Requests](#submitting-pull-requests)
- [Commit Standards](#commit-standards)
- [Review Process](#review-process)

---

## Code of Conduct

Please read and follow our [Code of Conduct](CODE_OF_CONDUCT.md).

---

## Getting Started

1. **Fork** the repository on GitHub.
2. **Clone** your fork locally:
   ```bash
   git clone https://github.com/<your-username>/GenDI.git
   cd GenDI
   ```
3. **Create a branch** for your work:
   ```bash
   git checkout -b feat/my-feature
   ```
4. Make your changes and ensure all existing tests pass.
5. Push your branch and open a pull request.

---

## Opening Issues

- Use the [issue templates](.github/ISSUE_TEMPLATE/) for bugs or feature requests.
- Search existing issues before opening a new one.

---

## Submitting Pull Requests

- Target the `main` branch unless otherwise specified.
- Each PR should address a **single** concern or feature.
- Reference related issues using `Closes #<number>` or `Fixes #<number>`.
- Ensure CI checks pass before requesting a review.
- Keep your branch up-to-date before submitting:
  ```bash
  git fetch origin
  git rebase origin/main
  ```

---

## Commit Standards

GenDI follows [Conventional Commits](https://www.conventionalcommits.org/).

### Format

```
<type>(<optional scope>): <description>
```

### Types

| Type       | Description                                            |
|------------|--------------------------------------------------------|
| `feat`     | A new feature                                          |
| `fix`      | A bug fix                                              |
| `docs`     | Documentation-only changes                            |
| `refactor` | Code change with no feature or fix                     |
| `test`     | Adding or updating tests                               |
| `chore`    | Build process, dependencies, or tooling                |
| `perf`     | Performance improvement                                |
| `ci`       | CI configuration changes                               |

### Examples

```
feat(generator): add InjectableAttribute source generator
fix(di): resolve scoped lifetime registration issue
docs: update README with usage examples
```

---

## Review Process

1. A maintainer will review your PR within **5 business days**.
2. Address any requested changes and update your branch.
3. Once approved, a maintainer merges using **squash-and-merge**.
4. Delete your feature branch after merging.

Thank you for contributing to GenDI!
