# GenDI Roadmap

This document outlines the planned phases of development for GenDI.

---

## Phase 1 - Initial Structure and Attributes

**Goal**: Establish the project foundation and implement attribute-based service registration.

- [x] Create project structure (solution, projects, CI)
- [x] Implement `InjectableAttribute`
- [x] Source generator that detects `[Injectable]` classes
- [x] Generate `AddGenDIServices()` extension method
- [x] Unit tests for the generator

---

## Phase 2 - Attribute Model and Microsoft DI Integration

**Goal**: Expand attribute-based registration and fully integrate with `Microsoft.Extensions.DependencyInjection`.

- [x] Implement `ServiceInjectionAttribute`
- [x] Implement `GenDICoverationAttribute` for generated coverage control
- [x] Source generator support for inheritance/interface traversal with `ServiceInjectionAttribute`
- [x] Source generator support for additive `Injectable<TService>` registrations
- [x] Registration ordering support (`Group`, `Order`, service name)
- [x] Support for `Singleton`, `Scoped`, and `Transient` lifetimes
- [x] Integration tests with a real `IServiceCollection`

---

## Phase 3 - Advanced NativeAOT Support

**Goal**: Ensure full compatibility with NativeAOT publish and IL trimming.

- [x] Add `ILLink.xml` descriptors to preserve generated types
- [x] Validate trimming compatibility with `<PublishTrimmed>true</PublishTrimmed>`
- [x] Validate NativeAOT with `<PublishAot>true</PublishAot>`
- [x] Document NativeAOT usage in README

---

## Phase 4 - Benchmarks, Documentation Website, and CI Hardening

**Goal**: Improve developer experience and release readiness while preparing optimization baselines.

- [x] Create Docusaurus website with English-first detailed documentation
- [x] Align website visual theme and layout with the `net-mediate` documentation style
- [x] Add GitHub Pages deployment pipeline for the website
- [x] Add CI/CD and scheduled publish workflows prepared for Sonar/NuGet with bypass (`continue-on-error`)
- [x] Add `versions.props` and `pack.props` package/build metadata following the `net-mediate` pattern
- [x] Add BenchmarkDotNet project
- [x] Benchmark startup registration time vs. reflection-based DI
- [x] Profile and optimize generated code
- [x] Publish benchmark results in repository

---

## Phase 5 - Official NuGet Publication

**Goal**: Release GenDI publicly on NuGet.org.

- [x] Set up NuGet package metadata baseline (versioning/pack props and workflow scaffolding)
- [x] Configure GitHub Actions baseline for package publishing workflows (currently bypassed)
- [ ] Publish pre-release (alpha/beta) for community feedback
- [ ] Address feedback and publish stable `1.0.0` release
- [ ] Announce on GitHub Discussions and social channels
