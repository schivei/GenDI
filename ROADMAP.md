# GenDI Roadmap

This document outlines the planned phases of development for GenDI.

---

## Phase 1 - Initial Structure and Attributes

**Goal**: Establish the project foundation and implement attribute-based service registration.

- [ ] Create project structure (solution, projects, CI)
- [ ] Implement `InjectableAttribute`
- [ ] Source generator that detects `[Injectable]` classes
- [ ] Generate `AddGenDIServices()` extension method
- [ ] Unit tests for the generator

---

## Phase 2 - Attribute Model and Microsoft DI Integration

**Goal**: Expand attribute-based registration and fully integrate with `Microsoft.Extensions.DependencyInjection`.

- [ ] Implement `ServiceInjectionAttribute`
- [ ] Source generator support for inheritance/interface traversal with `ServiceInjectionAttribute`
- [ ] Registration ordering support (`Group`, `Order`, service name)
- [ ] Support for `Singleton`, `Scoped`, and `Transient` lifetimes
- [ ] Integration tests with a real `IServiceCollection`

---

## Phase 3 - Advanced NativeAOT Support

**Goal**: Ensure full compatibility with NativeAOT publish and IL trimming.

- [ ] Add `ILLink.xml` descriptors to preserve generated types
- [ ] Validate trimming compatibility with `<PublishTrimmed>true</PublishTrimmed>`
- [ ] Validate NativeAOT with `<PublishAot>true</PublishAot>`
- [ ] Document NativeAOT usage in README

---

## Phase 4 - Benchmarks and Optimizations

**Goal**: Measure and improve performance of registration and resolution.

- [ ] Add BenchmarkDotNet project
- [ ] Benchmark startup registration time vs. reflection-based DI
- [ ] Profile and optimize generated code
- [ ] Publish benchmark results in repository

---

## Phase 5 - Official NuGet Publication

**Goal**: Release GenDI publicly on NuGet.org.

- [ ] Set up NuGet package metadata (icon, description, tags, license)
- [ ] Configure GitHub Actions for automated publish on tag
- [ ] Publish pre-release (alpha/beta) for community feedback
- [ ] Address feedback and publish stable `1.0.0` release
- [ ] Announce on GitHub Discussions and social channels
