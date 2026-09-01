# Plano de Ação — Template Metroidvania 2D

Base: [relatorio-pesquisa-template-metroidvania.md](./relatorio-pesquisa-template-metroidvania.md)
Princípio geral: **1 fase = 1 sistema pequeno e coeso. Sem avançar de fase sem revisão sua.** Isso não é burocracia — é a mitigação direta ao risco de "loop de geração carregada" levantado na pesquisa (§5 do relatório).

## Visão (atualizada — 2026-08-28)

Este projeto **não é só o template**: é o jogo metroidvania real (MVP), construído *ao mesmo tempo* que o template nasce dele.

- **Entregável primário**: um MVP jogável do metroidvania — escopo pequeno, mas fim-a-fim (anda, pula, combate básico, 1 habilidade de gate, 2-3 salas conectadas, 1 save).
- **Entregável secundário, extraído do primeiro**: `Assets/NakeDev.Template2D` continua desenhado para sair como `.unitypackage` e virar o próximo template — mas ele só é "promovido" a template depois de provar que funciona dentro de um jogo real, não antes. Código genérico (Core, Player, SceneManagement) fica desacoplado de conteúdo específico do MVP (sprites da "Girl Plataform", salas específicas, balanceamento); conteúdo específico do jogo fica em `Assets/Game`, claramente separado do template.
- Cada fase abaixo ainda vale como unidade de trabalho, mas agora é dupla: "isso resolve um passo do MVP" **e** "isso fica genérico o bastante pra ir pro template". Quando as duas coisas conflitarem, o MVP jogável vence — o template se ajusta depois, não o contrário.

Convenção de cada fase abaixo:
- **Objetivo** — o que existe ao final que não existia antes.
- **Reaproveita de** — o que vem pronto (ou quase) dos seus repositórios `Template-CoreSystem`/`Template-FirstPersonController`.
- **Criado do zero** — o que é novo, específico de 2D/metroidvania.
- **Critério de pronto** — como saber que a fase terminou (verificável, não "parece bom").
- **Guardrail de IA** — o limite de escopo que evita geração excessiva nessa fase específica.

---

## Fase 0 — Fundação do projeto ✅ Concluída

**Objetivo**: esqueleto de pastas e convenções documentadas, antes de qualquer gameplay. **Sem asmdef, sem package UPM/DLL** — tudo como scripts soltos em `Assets`, pensado para exportar depois como `.unitypackage` e importar em qualquer projeto novo com duplo-clique.

**Reaproveita de**: estrutura de namespace (`NakeDev.*`), convenção de `InspectorLineAttribute`, referência a "Regras" nos comentários do Core.

**Criado do zero**:
- Estrutura de pastas por feature: `Assets/NakeDev.Template2D/Scripts/Core`, `Player` — **feito**. `Interaction`, `Inventory`, `Combat`, `Abilities`, `SceneManagement`, `SaveSystem`, `UI` — **ainda não criadas** (chegam junto com as fases que as usam). `Editor` — **feito** (drawers dos atributos, em `Scripts/Editor/Inspector`).
- Reorganização de pastas (2026-09-01): `Assets/_Project` foi desmembrado em `Assets/NakeDev.Template2D` (código genérico exportável) e `Assets/Game` (conteúdo específico deste MVP: Art, Data tunado, Prefabs, Scenes, Audio, VFX, Animation, Localization). Ferramentas pessoais que não fazem parte deste template (ex: Hierarchify) foram para `Assets/NakeDev.Tools`. `Assets/ThirdParty` guarda só o que é de terceiros de verdade (TextMesh Pro).
- `CONVENTIONS.md` na raiz do template — **descartado por decisão sua (2026-08-28)**. Os comentários no código continuam citando "Regra 3", "Regra 5", "Regra 6", "Regra 9" — essas regras seguem valendo e sendo a referência real, só não vão virar um arquivo formal à parte. Se algum dia doer não ter esse documento centralizado, é só reabrir esta fase.
- `GameStateSO` portado do Core — **feito** ([GameStateSO.cs](../Assets/NakeDev.Template2D/Scripts/Core/GameStateSO.cs)), com `GameStateManager.asset` configurado na cena.
- Namespace do projeto migrado de `PlanA` para `NakeDev` em todos os scripts e assets serializados — **feito**.

**Critério de pronto**: projeto compila ✅, `GameStateSO` funcional numa cena de teste ✅, pasta `NakeDev.Template2D` exportável via `Assets → Export Package` sem erros de dependência faltando — não testado ainda.

**Guardrail de IA**: gerar só esqueleto (pastas, 1 SO). Nenhuma lógica de gameplay nesta fase. Nenhum asmdef.

---

## Fase 1 — Player Controller 2D + Animação crossfade-only ✅ Concluída

**Objetivo**: personagem 2D anda, pula, cai, com animação 100% via crossfade (sem transições no Animator Controller).

**Reaproveita de**: `AnimatorBrain.cs` (praticamente sem alteração — já é crossfade puro e agnóstico de gênero de jogo), padrão de `InputReader` + interface de input (`IMovementInput`), estados via enum simples.

**Decisão de input (confirmada)**: New Input System (`com.unity.inputsystem`) nativo, via `.inputactions` gerado. **Sem InputManager global/singleton.** `InputReader` como componente por entidade, consumido só via interface (`IMovementInput`).

**Criado do zero** — todos entregues:
- [`PlayerLocomotion2D.cs`](../Assets/NakeDev.Template2D/Scripts/Player/Locomotion/PlayerLocomotion2D.cs): `Rigidbody2D` + raycast/overlap-based ground e wall check, com coyote time e jump buffering funcionais.
- [`PlayerAnimationController.cs`](../Assets/NakeDev.Template2D/Scripts/Player/Animation/PlayerAnimationController.cs): dispara `AnimatorBrain.PlayAnimation(hash, crossfadeTime)` a partir de Idle/Run/Jump/Fall, hashes cacheados.
- [`PlayerFacing2D.cs`](../Assets/NakeDev.Template2D/Scripts/Player/Animation/PlayerFacing2D.cs): flip de direção do sprite.
- [`LocomotionConfigSO.cs`](../Assets/NakeDev.Template2D/Scripts/Player/Locomotion/LocomotionConfigSO.cs): tuning centralizado (movimento, pulo, ground/wall check).
- Estado do player via enum simples — HFSM não introduzida (correto, YAGNI ainda vale).

**Bônus já adiantado desta fase para a Fase 5** (double jump, wall slide, wall jump já implementados dentro de `PlayerLocomotion2D`/`LocomotionConfigSO`, seguindo a Regra 9 revisada — ver Fase 5).

**Decisão revisada (Regra 5 — 2026-08-28)**: com o combate se aproximando (Fase 4) e o Animator Controller crescendo (16+ estados soltos hoje, mais vindo com Combat), a regra "crossfade-only" deixa de significar "tudo solto numa lista plana" e passa a significar **"nenhuma seta/transição automática configurada no Animator Controller — a mudança de estado é sempre disparada por código via `AnimatorBrain.PlayAnimation`"**. Duas ferramentas passam a ser permitidas por não violarem esse princípio:
- **Sub-State Machines**: puramente organizacionais (agrupam estados visualmente dentro do Animator Controller). Não adicionam transição nenhuma — o código continua chamando `CrossFade` pelo nome curto do estado, que funciona independente de aninhamento contanto que o nome seja único no controller inteiro. Uso: agrupar por categoria — `Locomotion` (Move + Jump/Fall/Landing), `Abilities` (double jump, wall slide, wall jump, dash — mesmo agrupamento que `AbilityFlagsSO` vai gatear na Fase 5), `Dodge` (slide, crouch), e futuramente `Combat`.
- **Blend Tree**: permitido **só onde o movimento é um espectro contínuo controlado por 1 parâmetro**, não para ações discretas. Único caso hoje: locomoção no chão (`PlayerIdle → PlayerWalk → PlayerRun`) via um float `Speed`, usando `AnimatorBrain.SetFloat` (já existe no wrapper, sem uso até agora). Crossfada pra dentro do blend tree 1x (ao tocar o chão) e depois só atualiza o float por frame — isso não é uma transição configurada, continua sendo controle 100% via código. Pulo/queda/aterrissagem continuam soltos (não entram no blend tree) porque são disparados por evento, não por um valor contínuo. Ataques, dash, wall jump, slide — mesma lógica: ficam soltos, nunca em blend tree.

**Critério de pronto**: ✅ cena `[DEV] Mechanics.unity` com sprite anda/pula/cai suavemente, troca de animação sem seta de transição no Animator Controller (`PlayerAnimator.controller` com states soltos + crossfade por código). Reorganização em Sub-State Machines + Blend Tree de locomoção fica registrada aqui como próximo passo desta fase, ainda não aplicada.

**Guardrail de IA**: gerar controller + animação nessa fase, nada de combate/dano/gate ainda mesmo que "pareça fácil de encaixar".

---

## Fase 2 — Câmera por sala (Cinemachine 2D, estilo Hollow Knight) ⏳ Não iniciada

**Objetivo**: câmera com follow contínuo e suavizado (não câmera fixa por tela, tipo Mega Man), confinada à geometria real de cada sala, com blend suave na transição — comportamento de referência: **Hollow Knight**.

**Reaproveita de**: nada diretamente (Core é 1ª pessoa) — só o princípio de "trigger ativa comportamento".

**Criado do zero**:
- Virtual Camera por sala (Cinemachine) usando `CinemachineFramingTransposer` com **dead zone** e **look-ahead** leve na direção horizontal do movimento.
- `Confiner2D` referenciando um `PolygonCollider2D` desenhado na **forma real da sala**.
- `RoomCameraTrigger`: Collider2D que sobe prioridade da vcam da sala ao entrar; invalidar cache de bounding do `Confiner2D` ao trocar de sala.
- **Fora de escopo desta fase (e do template)**: screen shake, camera punch/kick em impacto.

**Critério de pronto**: 2 salas de teste com geometria irregular lado a lado, câmera segue suavizada, respeita o polígono de cada sala, troca de sala com blend suave.

**Guardrail de IA**: só câmera. Resistir a implementar scene loading aditivo aqui (Fase 3) ou shake/feedback de impacto.

**Pré-requisito ainda não feito**: pacote `com.unity.cinemachine` não está no `Packages/manifest.json` — instalar como primeiro passo desta fase.

---

## Fase 3 — Scene management aditivo (salas) ⏳ Não iniciada

**Objetivo**: mundo dividido em cena persistente + cenas de sala carregadas/descarregadas aditivamente.

**Reaproveita de**: `GameStateSO` (para bloquear input durante carregamento) — já disponível e pronto pra ser consumido aqui.

**Criado do zero**:
- `RoomLoader`: carrega/descarrega cena aditiva ao cruzar um `RoomTransitionTrigger`, reposiciona player no ponto de entrada correspondente.
- Cena persistente com player, câmeras (raiz), managers (GameState, Inventory, Abilities).

**Critério de pronto**: transitar entre 2+ salas descarrega a anterior (validar via Hierarchy/Profiler), player aparece no ponto de entrada correto.

**Guardrail de IA**: não misturar lógica de save aqui ainda — só carregar/descarregar. Persistência é Fase 6.

**Nota de alinhamento (MVP)**: as cenas hoje existentes (`Splashscreen.unity`, `[DEV] Mechanics.unity`, `SampleScene.unity`) são de bootstrap/prototipagem, não salas do jogo. Esta fase é onde a estrutura real de salas do MVP nasce — decidir aqui também quantas salas o MVP vai ter (sugestão: 2-3, o mínimo pra provar 1 gate de habilidade).

---

## Fase 4 — Combate e dano ⏳ Não iniciada

**Objetivo**: player e inimigos podem causar/receber dano de forma desacoplada.

**Reaproveita de**: `AnimatorBrain` para animação de hit/attack via crossfade (já disponível).

**Criado do zero**:
- `IDamageable` (contrato), `HealthComponent` (estado + eventos `OnDamaged`/`OnDeath`), `DamageDealer` (componente simples).
- Hook de animação: `HealthComponent.OnDamaged` dispara crossfade de hit via `AnimatorBrain` (sem "juice" — sem hit-stop, sem screen shake).

**Critério de pronto**: player e 1 inimigo de teste trocam dano, ambos usando `IDamageable`/`HealthComponent`, morte dispara evento consumível por outros sistemas.

**Guardrail de IA**: gerar só o contrato + componente + 1 exemplo de uso. Sem sistema de armas/combo ainda.

**Nota**: os sprites de ataque/dodge/hit já importados em `Assets/Game/Art/Sprites/Girl Plataform` (aerial dash, atk, hurt, dodge atk...) dão material de sobra pra essa fase — não precisa gerar arte nova, só ligar o que já existe ao `AnimatorBrain`.

---

## Fase 5 — Ability-gating + Inventário 🔶 Em andamento

**Objetivo**: progressão via habilidades que desbloqueiam áreas (o "gate" clássico de metroidvania).

**Decisão revisada (Regra 9 — CONVENTIONS.md)**: as habilidades de movimento (double jump, wall slide, wall jump, dash) **não** viram componentes `*Ability` separados. Toda a lógica de movimento fica dentro de `PlayerLocomotion2D`, e todo o tuning num único `LocomotionConfigSO`.

**Progresso real**:
- ✅ Double jump — implementado (`MaxExtraJumps`/`ExtraJumpForce` em `LocomotionConfigSO`).
- ✅ Wall slide — implementado (`WallCheckDistance`/`WallSlideSpeed`).
- ✅ Wall jump — implementado (`WallJumpForceX/Y`, `WallJumpControlLockTime`, recarrega o double jump ao sair da parede).
- ❌ Dash — ainda não implementado (sem campos de dash em `LocomotionConfigSO` nem lógica em `PlayerLocomotion2D`).
- ❌ `AbilityFlagsSO` — ainda não criado. Hoje as habilidades acima estão **sempre ligadas**; ainda não há gate consultável (`if (AbilityFlags.DoubleJump)` etc.).
- ❌ `AbilityGateSO`/obstáculo de mundo — ainda não criado.

**Criado do zero (restante)**:
- `AbilityFlagsSO`: guarda flags de habilidade (`DoubleJump`, `WallClimb`, `Dash`...). `PlayerLocomotion2D` passa a **consultar** a flag antes de consumir a habilidade.
- `AbilityGateSO`: obstáculo no mundo consulta a flag e libera passagem.
- Dash entra como mais um bloco de lógica/campos dentro de `PlayerLocomotion2D`/`LocomotionConfigSO`.

**Referência de mercado analisada**: asset `MetroidvaniaController`, estudado e depois removido do projeto (não está mais em `Assets/`) — só a lista de habilidades e parâmetros de tuning foram aproveitados como ponto de partida, nenhum código copiado.

**Critério de pronto**: obstáculo de teste bloqueia o player até uma flag de debug ser ativada; ativar a flag libera a passagem sem reiniciar a cena. Desligar uma flag de habilidade remove o comportamento correspondente sem quebrar o resto da locomoção.

**Guardrail de IA**: gerar 1 peça por vez — sugestão de ordem: (1) `AbilityFlagsSO` + consulta de flag nas habilidades já existentes, (2) `AbilityGateSO` + 1 obstáculo de teste, (3) dash como habilidade nova já nascendo atrás de flag. Sem UI de "habilidades desbloqueadas" ainda (Fase 7).

---

## Fase 6 — Save / Checkpoint ⏳ Não iniciada

**Objetivo**: progresso persiste entre sessões, por sala.

**Reaproveita de**: `GameStateSO` (estado `Loading` durante save/load).

**Criado do zero**:
- Interface `ISaveable` (componentes declaram o que persistir).
- `SaveSystem` central: serializa flags de habilidade, itens do inventário, salas visitadas, checkpoints ativados. Regra: inimigos comuns respawnam ao recarregar sala; chefes/eventos únicos não.

**Critério de pronto**: salvar, fechar o jogo (Play Mode stop + restart), carregar — habilidades e itens coletados persistem.

**Guardrail de IA**: escopo mínimo de save (flags, inventário, sala atual). Não generalizar para "sistema de save genérico para qualquer dado" (YAGNI).

---

## Fase 7 — UI/HUD base ⏳ Não iniciada

**Objetivo**: HUD mínimo funcional (vida, ícone de interação, feedback de item coletado).

**Criado do zero**: barra/contador de vida ligado a `HealthComponent.OnDamaged`, prompt de interação, toast simples de "item coletado" ligado a eventos de inventário.

**Critério de pronto**: HUD reage a dano, coleta de item e interação sem polling (tudo via evento).

**Guardrail de IA**: UI mínima e funcional, sem animação de UI "bonita".

---

## Fase 8 — Editor tooling & QoL ⏳ Não iniciada

**Objetivo**: portar as ferramentas de produtividade solo do Core.

**Reaproveita de**: `InspectorLineAttribute` + `InspectorLineDrawer`, padrão `Reset()` de auto-configuração.

**Criado do zero**: nada estrutural — só adaptar os auto-configs existentes para os componentes 2D criados nas fases anteriores.

**Critério de pronto**: arrastar `PlayerLocomotion2D`/equivalente num GameObject novo já configura collider/layer certos sem passo manual.

**Guardrail de IA**: só tooling de editor, zero gameplay novo.

---

## Fase 9 — Validação de expansão (checkpoint de arquitetura) ⏳ Não iniciada

**Objetivo**: confirmar que o template realmente "abre espaço" em vez de limitar, antes de declarar v1.0 pronta — e, com a visão atualizada, confirmar que ele sobreviveu a ser extraído de um jogo real.

**Como validar** (sem escrever jogo completo, só protótipos de estresse):
1. Adicionar 1 inimigo com IA simples usando `HealthComponent`/`IDamageable` sem modificar Combat core → confirma desacoplamento.
2. Adicionar 1 habilidade nova e 1 gate novo sem tocar em `AbilityFlagsSO`/`AbilityGateSO` existentes → confirma extensibilidade.
3. Adicionar 1 sala nova com sua própria vcam/confiner sem tocar em `RoomLoader` → confirma que scene management escala por conteúdo, não por código.
4. Revisão de tamanho de arquivo: nenhum script deveria passar de ~150–200 linhas nesse ponto; `PlayerControls.cs` (475 linhas) é gerado pelo Input System e fica de fora dessa contagem — os demais devem ser revisados.
5. **Novo**: exportar `Assets/NakeDev.Template2D` como `.unitypackage` e importar num projeto Unity vazio → confirma que o template realmente separa do conteúdo específico do MVP (sprites, salas, balanceamento do jogo).

**Critério de pronto**: os 5 testes acima passam sem editar código dos sistemas core.

---

## Regra de governança de IA para todas as fases

1. Cada fase é uma conversa/tarefa própria — não pedir "faça as fases 1 a 4 de uma vez".
2. Ao final de cada fase, revisão sua antes de eu prosseguir (ler o diff, entender o raciocínio — não só rodar e ver se compila).
3. Se uma fase gerar mais do que ~3-4 arquivos novos ou qualquer arquivo grande (200+ linhas), paramos e dividimos antes de continuar.
4. Preferir portar/adaptar código dos seus repositórios existentes a gerar do zero, sempre que o padrão já existir lá.
5. Nenhum sistema de "juice" (screen shake, hit-stop, squash&stretch automático, partículas de feedback) entra no template — isso é decisão de jogo específico, adicionada organicamente depois, fora deste plano.
6. Sem asmdef, sem package UPM, sem DLL. O template é uma pasta de scripts em `Assets`, exportada como `.unitypackage` e importada em cada projeto novo.
7. **Novo**: ao escrever qualquer sistema, perguntar "isso é regra do gênero (metroidvania) ou regra deste jogo específico?". Regra de gênero → vai pro template (`NakeDev.Template2D/Scripts/Core`, `Player`, etc., sem depender de conteúdo específico). Regra do jogo específico (nome de habilidade, número de salas, balanceamento) → fica isolado em dados/config, nunca hardcoded no sistema genérico.

---

## Status resumido (2026-08-28)

| Fase | Status |
|---|---|
| 0 — Fundação | ✅ Concluída (`CONVENTIONS.md` descartado por decisão; pastas de features futuras chegam junto com cada fase) |
| 1 — Player Controller + Animação | ✅ Concluída — reorganização em Sub-State Machines + Blend Tree (Regra 5 revisada) ainda não aplicada |
| 2 — Câmera por sala | ⏳ Não iniciada (falta instalar Cinemachine) |
| 3 — Scene management | ⏳ Não iniciada |
| 4 — Combate e dano | ⏳ Não iniciada (arte já disponível) |
| 5 — Ability-gating + Inventário | 🔶 Em andamento — double/wall jump/slide prontos; faltam flags, gate e dash |
| 6 — Save/Checkpoint | ⏳ Não iniciada |
| 7 — UI/HUD | ⏳ Não iniciada |
| 8 — Editor tooling | ⏳ Não iniciada |
| 9 — Validação de expansão | ⏳ Não iniciada |
