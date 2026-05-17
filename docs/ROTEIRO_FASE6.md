# Roteiro de Execução e Revisão — Fase 6 (GenDI)

> Referência: `ROADMAP.md` (Fase 6 — Developer Experience and Ecosystem Expansion)

## 1) Objetivo desta fase

Consolidar o GenDI como solução de DI para uso amplo no ecossistema .NET, elevando qualidade de análise estática, ergonomia de desenvolvimento, integração com plataformas e maturidade de comunidade/documentação.

## 2) Escopo macro (trilhas)

1. Qualidade do source-generator e analyzers
2. Evolução do modelo de registro
3. Suporte de plataformas/frameworks
4. Ergonomia de testes
5. Explicit registration strategies (Add/TryAdd)
6. OptionConfig evolution
7. Tooling/IDE
8. Observability
9. Community and ecosystem

## 2.1) Official Phase 6 baseline matrix (single source of truth)

| Track | Status | Notes |
|---|---|---|
| 4.1 Source-generator quality | Delivered | Analyzer package, diagnostics, code-fix, and incremental optimization are in place. |
| 4.2 Registration model (RM-01..RM-12) | Delivered | Optional injection, conditional registration, decorators, factory/module support, thread isolation, and cross-assembly discovery are documented and tested. |
| 4.3 Platform/framework support | Delivered | Minimal API, Worker Service, Blazor WASM validation, MAUI manual validation guidance, and F# limitation notes are covered. |
| 4.4 Testing ergonomics | Delivered | `GenDI.Testing` + `ServiceBuilder` and xUnit example suite are present. |
| 4.5 Explicit registration strategies (Add/TryAdd) | Delivered | `RegistrationMultiplicity` + `RegistrationEmissionStrategy` support across attributes is implemented and tested. |
| 4.6 OptionConfig evolution | Delivered | Optional key fallback, eligibility constraints, and `AddOptions<T>().BindConfiguration(section)` fast-path are implemented. |
| 4.7 Tooling/IDE | Pending | VS item template, Rider live template, and `dotnet new` template remain open. |
| 4.8 Observability | Pending | Observable service spans, startup summary log, and graph export remain open. |
| 4.9 Community/ecosystem | Pending | Public changelog, localization, sample repository, and benchmark expansion remain open. |

## 3) Estratégia de entrega incremental

### Incremento 6.1 (este PR) — Fundação de DX com analyzers

- Criar `GenDI.Analyzers` como pacote companion inicial.
- Entregar diagnósticos básicos de uso incorreto:
  - `[Inject]` em propriedade sem `init`.
  - `[Injectable]` aplicado em tipo não concreto (abstrato/interface).
- Cobrir diagnósticos com testes automatizados.
- Integrar projetos na solução e manter padrão de empacotamento.

### Incremento 6.2 — Expansão de diagnósticos + otimizações do generator

- Diagnósticos adicionais de consistência de atributos.
- Refinamento de mensagens/ajuda de IDE.
- Medição de custo incremental do generator e redução de rebuild.

### Incremento 6.3 — Modelo de registro avançado

- `[InjectOptional]`
- `[ConditionalInjectable(environmentName)]`
- `[DecoratorFor<TService>]`
- Factory, module grouping, injeção indireta e políticas avançadas de lifetime.

### Incremento 6.4 — Plataformas e exemplos

- Minimal API, Worker Service, Blazor WASM e validações AOT mobile.
- Projeto(s) exemplo com cenários reais.

### Incremento 6.5 — Explicit registration strategies (Add/TryAdd)

- Single vs multiple registration at the `ServiceInjection` and `Injectable` levels.
- Emission control between `TryAdd*` and `Add*` based on user configuration.

### Incremento 6.6 — OptionConfig evolution

- Optional key for configuration binding (`configurationSection`).
- Type eligibility constraints for options (concrete classes/structs/records, non-private, no constructor with arguments).

### Incremento 6.7 — Tooling, observability, and community

- Templates (VS/Rider/dotnet new)
- Observable resources and graph export
- Community materials, changelog, and docs localization

## 4) Backlog detalhado por trilha (com critérios de aceite)

## 4.1 Qualidade do source-generator

- [x] **QG-01** Criar `GenDI.Analyzers` companion package.
  - Critérios:
    - Projeto packável com saída em `analyzers/dotnet/cs`.
    - Integrado à solução.
- [x] **QG-02** Diagnóstico para `[Inject]` em propriedade sem `init`.
  - Critérios:
    - Emite diagnóstico em IDE/compilação.
    - Não emite quando `get; init;`.
- [x] **QG-03** Diagnóstico para `[Injectable]` em tipo abstrato/interface.
  - Critérios:
    - Emite diagnóstico para classe abstrata.
    - Tratamento preparado para tipo inválido não concreto.
- [x] **QG-04** Code-fix provider: migração de constructor injection para property injection.
- [x] **QG-05** Otimização incremental do source generator.

## 4.2 Modelo de registro

- [x] **RM-01** `[InjectOptional]`.
- [x] **RM-02** `[ConditionalInjectable(environmentName)]`.
- [x] **RM-03** `[DecoratorFor<TService>]`.
- [x] **RM-04** Definir `ServiceLifetime` no `ServiceInjectionAttribute` com fallback:
  `Injectable > ServiceInjection > Transient`.
- [x] **RM-05** Permitir injeção indireta por `[Inject]` sem exigir `[Injectable]` no tipo de implementação.
  - Critérios:
    - Varredura de implementações concretas para o serviço solicitado.
    - Se contrato não tiver `[ServiceInjection]`, tratar o tipo da propriedade como contrato implícito.
    - Proibir open-generic; somente contratos/implementações fechados (closed generic) são elegíveis.
- [x] **RM-06** Permitir override de `ServiceLifetime` no `[Inject]`, refletindo no registro:
  `Inject > Injectable > ServiceInjection > Transient`.
  - Critérios:
    - Registrar somente uma implementação final por resolução.
    - Empate por magnitude de lifetime: `Scoped > Singleton > Transient`.
- [x] **RM-07** Thread isolation no registro por `Injectable`/`ServiceInjection` com os três lifetimes.
- [x] **RM-08** Varredura de dependências entre bibliotecas referenciadas na solução para registro centralizado.
- [x] **RM-09** Suporte a injeção indireta para tipos genéricos fechados quando a implementação concreta for inferível.
- [x] **RM-10** `OptionConfigAttribute` para mapear tipo concreto em `IOptions<>` com chave/path opcional (fallback para nome do tipo).
- [x] **RM-11** `[InjectableFactory]` em métodos estáticos.
- [x] **RM-12** `[InjectableModule]` para agrupamento.

## 4.3 Plataforma e framework

- [x] **PF-01** Minimal API example e documentação.
- [x] **PF-02** Blazor WASM validation.
- [x] **PF-03** MAUI/mobile AOT validation.
- [x] **PF-04** Worker Service example.
- [x] **PF-05** Exploração suporte F#.

## 4.4 Ergonomia de testes

- [x] **TE-01** `GenDI.Testing` com `ServiceBuilder`.
- [x] **TE-02** Integração com helpers de teste de DI abstractions.
- [x] **TE-03** Exemplo real xUnit usando GenDI.

## 4.5 Explicit registration strategies (Add/TryAdd)

- [x] **RG-01** Allow single or multiple registration at the `ServiceInjection` and `Injectable` levels.
  - Criteria:
    - `ServiceInjection` can declare registration policy for annotated contracts.
    - `Injectable` can declare registration policy for annotated implementations.
    - For hierarchy interfaces/abstractions without `[ServiceInjection]`, allow registration strategy configuration in the inferred flow.
- [x] **RG-02** Allow users to define emission strategy between `TryAdd*` and `Add*`.
  - Criteria:
    - Strategy must affect generated registration code for eligible contracts.
    - Strategy must differentiate single and multiple registration behavior.
    - Test coverage for overwrite scenarios and composition of multiple implementations.

## 4.6 OptionConfig evolution

- [x] **OP-01** Allow an optional key in options to select the configuration section.
  - Criteria:
    - When a key is defined, use the specified section.
    - When no key is defined, use the options type name as the default section.
- [x] **OP-02** Restrict options to eligible types and compatible constructors.
  - Criteria:
    - Concrete classes (including sealed), non-private.
    - Non-ref and non-private structs.
    - Non-ref and non-private records.
    - Parameterless constructor or implicit/default constructor.
- [x] **OP-03** Register options using the most performant path between `services.Configure()` and equivalent binding for `IOptions<>`.
  - Criteria:
    - Resulting registration must expose `IOptions<TOptions>`.
    - Test coverage for explicit key, default key by type name, and invalid types.

## 4.7 Tooling and IDE

- [ ] **TL-01** Item-template Visual Studio.
- [ ] **TL-02** Live template Rider.
- [ ] **TL-03** `dotnet new gendi-service`.

## 4.8 Observability

- [ ] **OB-01** `[ObservableService]` with OTel spans.
- [ ] **OB-02** Registration summary log at startup.
- [ ] **OB-03** Graph export (DOT).

## 4.9 Community and ecosystem

- [ ] **CE-01** Q&A category in Discussions.
- [ ] **CE-02** Public `CHANGELOG.md`.
- [ ] **CE-03** Documentation localization.
- [ ] **CE-04** Complete sample repository.
- [ ] **CE-05** Benchmark suite expansion.

## 5) Checklist de execução técnica (por PR)

- [ ] Objetivo e escopo do incremento definidos no PR.
- [ ] Testes novos cobrindo comportamento adicionado.
- [ ] Build/teste local do escopo executados.
- [ ] Sem regressões em funcionalidades existentes.
- [ ] Ajustes em roadmap/documentação quando aplicável.
- [ ] Validação paralela (Code Review + CodeQL) executada.

## 6) Checklist de revisão (quality gate)

- [ ] Compatibilidade com convenções do repositório (`pack.props`, versões, estrutura de testes).
- [ ] Clareza das mensagens de diagnóstico para desenvolvedor.
- [ ] Ausência de breaking changes não planejadas.
- [ ] Cobertura de casos positivos e negativos.
- [ ] Segurança: nenhuma mudança introduz risco relevante.
- [ ] PR legível, incremental e com motivação clara.

## 7) Riscos, dependências e mitigação

- **Risco**: explosão de escopo na Fase 6.
  - **Mitigação**: entregas curtas e revisáveis (6.1, 6.2, ...).
- **Risco**: diagnósticos gerarem ruído excessivo.
  - **Mitigação**: mensagens objetivas, severidade adequada e testes de não-regressão.
- **Risco**: acoplamento alto entre analyzer e generator.
  - **Mitigação**: manter responsabilidades separadas e validar com testes independentes.
- **Dependência**: estabilidade de APIs Roslyn/Microsoft.CodeAnalysis.
  - **Mitigação**: fixar intervalo de versão compatível usado no repositório.
- **Restrição arquitetural**: open-generic é fora de escopo por premissas de NativeAOT.
  - **Mitigação**: validar e bloquear cenários open-generic em roadmap, plano e implementação.

## 8) Log de revisões deste roteiro

- **v1.0**: criação do roteiro detalhado e início do incremento 6.1 (fundação de analyzers).
- **v1.1**: remoção de escopo open-generic e inclusão dos novos itens de plano para fallback de lifetime,
  injeção indireta, thread isolation, varredura de dependências e OptionConfig.
- **v1.2**: implementação do code-fix de migração de constructor injection e otimização incremental no generator.
- **v1.3**: implementação de `[InjectOptional]` e fallback de `ServiceInjectionAttribute.Lifetime`.
- **v1.4**: implementação de `[ConditionalInjectable(environmentName)]` para registro condicional por ambiente.
- **v1.5**: implementação de `[DecoratorFor<TService>]`, injeção indireta por `[Inject]`, override de lifetime no `[Inject]` e thread isolation configurável.
- **v1.6**: implementação de RM-08 até RM-12 (varredura em bibliotecas referenciadas, inferência closed-generic indireta, `OptionConfigAttribute`, `[InjectableFactory]` e `[InjectableModule]`).
- **v1.7**: implementação da trilha 4.3 com exemplos/validações para Minimal API, Worker Service e Blazor WASM, documentação de validação mobile AOT e exploração de suporte F#.
- **v1.8**: implementação da trilha 4.4 com pacote `GenDI.Testing`, integração com helpers de DI abstractions e exemplo real em xUnit.
- **v1.9**: added tracks 4.5 and 4.6 (Add/TryAdd registration strategies and OptionConfig evolution), with subsequent renumbering to 4.7, 4.8, and 4.9.
- **v2.0**: baseline aligned with PR #24, marking 4.6 as delivered and formalizing the single Phase 6 status matrix used by all documentation entry points.

## 8) Referência detalhada das entregas RM-01..RM-12

- Documentação técnica detalhada: [REGISTRATION_MODEL_RM01_RM12.md](./REGISTRATION_MODEL_RM01_RM12.md) (consolidado RM-01..RM-12)
- Website (documentação pública): `website/docs/advanced/registration-model-rm01-rm12.md`
