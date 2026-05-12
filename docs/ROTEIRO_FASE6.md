# Roteiro de Execução e Revisão — Fase 6 (GenDI)

> Referência: `ROADMAP.md` (Fase 6 — Developer Experience and Ecosystem Expansion)

## 1) Objetivo desta fase

Consolidar o GenDI como solução de DI para uso amplo no ecossistema .NET, elevando qualidade de análise estática, ergonomia de desenvolvimento, integração com plataformas e maturidade de comunidade/documentação.

## 2) Escopo macro (trilhas)

1. Qualidade do source-generator e analyzers
2. Evolução do modelo de registro
3. Suporte de plataformas/frameworks
4. Ergonomia de testes
5. Tooling/IDE
6. Observabilidade
7. Comunidade e ecossistema

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
- Open-generic, factory e module grouping.

### Incremento 6.4 — Plataformas e exemplos

- Minimal API, Worker Service, Blazor WASM e validações AOT mobile.
- Projeto(s) exemplo com cenários reais.

### Incremento 6.5 — Tooling, observabilidade e comunidade

- Templates (VS/Rider/dotnet new)
- Recursos observáveis e exportação de grafo
- Material de comunidade, changelog e localização de docs

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
- [ ] **QG-04** Code-fix provider: migração de constructor injection para property injection.
- [ ] **QG-05** Otimização incremental do source generator.

## 4.2 Modelo de registro

- [ ] **RM-01** `[InjectOptional]`.
- [ ] **RM-02** `[ConditionalInjectable(environmentName)]`.
- [ ] **RM-03** `[DecoratorFor<TService>]`.
- [ ] **RM-04** Open-generic registration (`IRepository<>`).
- [ ] **RM-05** `[InjectableFactory]` em métodos estáticos.
- [ ] **RM-06** `[InjectableModule]` para agrupamento.

## 4.3 Plataforma e framework

- [ ] **PF-01** Minimal API example e documentação.
- [ ] **PF-02** Blazor WASM validation.
- [ ] **PF-03** MAUI/mobile AOT validation.
- [ ] **PF-04** Worker Service example.
- [ ] **PF-05** Exploração suporte F#.

## 4.4 Ergonomia de testes

- [ ] **TE-01** `GenDI.Testing` com `ServiceBuilder`.
- [ ] **TE-02** Integração com helpers de teste de DI abstractions.
- [ ] **TE-03** Exemplo real xUnit usando GenDI.

## 4.5 Tooling e IDE

- [ ] **TL-01** Item-template Visual Studio.
- [ ] **TL-02** Live template Rider.
- [ ] **TL-03** `dotnet new gendi-service`.

## 4.6 Observabilidade

- [ ] **OB-01** `[ObservableService]` com spans OTel.
- [ ] **OB-02** Log de resumo de registros no startup.
- [ ] **OB-03** Exportação de grafo (DOT).

## 4.7 Comunidade e ecossistema

- [ ] **CE-01** Categoria Q&A no Discussions.
- [ ] **CE-02** `CHANGELOG.md` público.
- [ ] **CE-03** Localização de documentação.
- [ ] **CE-04** Repositório sample completo.
- [ ] **CE-05** Expansão da suíte de benchmarks.

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

## 8) Log de revisões deste roteiro

- **v1.0**: criação do roteiro detalhado e início do incremento 6.1 (fundação de analyzers).
