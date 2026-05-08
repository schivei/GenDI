# 🤝 Contributing

GenDI is open source and community-driven. Every improvement — no matter how small — makes
the project stronger and keeps the .NET ecosystem moving forward.

## 🌟 Why contribute?

- 🎯 Shape the future of attribute-first DI in .NET
- 🚀 Get early access to features and influence the roadmap
- 📈 Build a public track record in a growing open-source project
- 💙 Help .NET developers escape constructor boilerplate every day

## 🛠️ What you can contribute

You don't need to write a source generator to make a difference. Here are great entry points:

| Area | Examples |
|---|---|
| 🐛 Bug reports | Unexpected generator output, edge-case attributes |
| 📝 Documentation | Typos, clearer examples, translations |
| ✅ Tests | New edge cases, integration scenarios |
| 🚀 Features | Items on the [Roadmap](./roadmap) |
| 🌍 Translations | Portuguese, Spanish, German docs |
| 💡 Ideas | Open a Discussion with your proposal |

## 🔧 Development baseline

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

- 🏷️ attribute-first model
- ⚡ generated registration/activation path
- 🚀 NativeAOT-aware behavior

## 📋 Pull request guidance

- 📝 Describe behavior changes clearly.
- 📚 Include updated documentation for public-facing changes.
- ✅ Keep tests aligned with new behavior.

---

## ❤️ Sponsor GenDI

GenDI is free, open-source, and maintained in personal time. If your team or product benefits
from the project — whether that's cleaner code, faster startups, or saved engineering hours —
please consider sponsoring development.

Sponsorship directly funds:

- 🔧 Continued maintenance and bug fixes
- 🚀 New features from the roadmap
- 📚 Documentation improvements and community support
- 📦 Longer-term NuGet package hosting and tooling

**[💖 Sponsor on GitHub](https://github.com/sponsors/schivei)**

Even a one-time contribution goes a long way. For companies shipping software built with GenDI,
a recurring sponsorship ensures the project stays healthy and keeps getting better.

---

_Thank you to everyone who uses, shares, or contributes to GenDI._ 🙏
