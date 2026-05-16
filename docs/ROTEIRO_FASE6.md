# Roteiro de Execução e Revisão — Fase 6 (GenDI)

> Referência: `ROADMAP.md` (Fase 6 — Developer Experience and Ecosystem Expansion)

## 1) Objetivo desta fase

Consolidar o GenDI como solução de DI para uso amplo no ecossistema .NET, elevando qualidade de análise estática, ergonomia de desenvolvimento, integração com plataformas e maturidade de comunidade/documentação.

## 2) Escopo macro (trilhas)

1. Qualidade do source-generator e analyzers
2. Evolução do modelo de registro
3. Suporte de plataformas/frameworks
4. Ergonomia de testes
5. Estratégias explícitas de registro (Add/TryAdd)
6. Evolução de OptionConfig
7. Tooling/IDE
8. Observabilidade
9. Comunidade e ecossistema

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

### Incremento 6.5 — Estratégias explícitas de registro (Add/TryAdd)

- Registro simples vs múltiplo em `ServiceInjection` e `Injectable`.
- Controle de emissão entre `TryAdd*` e `Add*` conforme configuração do usuário.

### Incremento 6.6 — Evolução de OptionConfig

- Chave opcional para vínculo de configuração (`configurationSection`).
- Restrições de elegibilidade de tipos para options (classes/structs/records concretos, não privados, sem construtor com argumentos).

### Incremento 6.7 — Tooling, observabilidade e comunidade

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
- [x] **RM-10** `OptionConfigAttribute` para mapear tipo concreto em `IOptions<>` com chave/path obrigatório.
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

## 4.5 Estratégias explícitas de registro (Add/TryAdd)

- [ ] **RG-01** Permitir registro simples ou múltiplo no nível de `ServiceInjection` e `Injectable`.
  - Critérios:
    - `ServiceInjection` pode declarar política de registro para contratos anotados.
    - `Injectable` pode declarar política de registro para implementações anotadas.
    - Para interfaces/abstrações da hierarquia sem `[ServiceInjection]`, permitir configuração de estratégia de registro no fluxo inferido.
- [ ] **RG-02** Permitir ao usuário definir estratégia de emissão entre `TryAdd*` e `Add*`.
  - Critérios:
    - Estratégia deve afetar o código gerado de registro para contratos elegíveis.
    - Estratégia deve diferenciar comportamento de registro simples e múltiplo.
    - Cobertura de testes para cenários de sobrescrita e composição de múltiplas implementações.

## 4.6 Evolução de OptionConfig

- [ ] **OP-01** Permitir chave opcional em options para selecionar seção de configuração.
  - Critérios:
    - Quando chave for definida, usar a seção indicada.
    - Quando não definida, usar o nome do tipo de options como seção padrão.
- [ ] **OP-02** Restringir options a tipos elegíveis e construtor compatível.
  - Critérios:
    - Classes concretas (inclui seladas), não privadas.
    - Structs não-ref e não privadas.
    - Records não-ref e não privadas.
    - Construtor sem argumentos ou construtor implícito/padrão.
- [ ] **OP-03** Registrar options com caminho mais performático entre `services.Configure()` e bind equivalente para `IOptions<>`.
  - Critérios:
    - Registro resultante deve disponibilizar `IOptions<TOptions>`.
    - Cobertura de testes para chave explícita, chave padrão por nome de tipo e tipos inválidos.

## 4.7 Tooling e IDE

- [ ] **TL-01** Item-template Visual Studio.
- [ ] **TL-02** Live template Rider.
- [ ] **TL-03** `dotnet new gendi-service`.

## 4.8 Observabilidade

- [ ] **OB-01** `[ObservableService]` com spans OTel.
- [ ] **OB-02** Log de resumo de registros no startup.
- [ ] **OB-03** Exportação de grafo (DOT).

## 4.9 Comunidade e ecossistema

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
- **v1.9**: inclusão das trilhas 4.5 e 4.6 (estratégias de registro Add/TryAdd e evolução de OptionConfig), com renumeração das trilhas subsequentes para 4.7, 4.8 e 4.9.

## 8) Referência detalhada das entregas RM-01..RM-12

- Documentação técnica detalhada: [REGISTRATION_MODEL_RM01_RM12.md](./REGISTRATION_MODEL_RM01_RM12.md) (consolidado RM-01..RM-12)
- Website (documentação pública): `website/docs/advanced/registration-model-rm01-rm12.md`
