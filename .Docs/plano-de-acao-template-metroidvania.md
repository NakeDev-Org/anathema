# Plano de Ação — Template Metroidvania 2D

Base: [relatorio-pesquisa-template-metroidvania.md](./relatorio-pesquisa-template-metroidvania.md)
Princípio geral: **1 fase = 1 sistema pequeno e coeso. Sem avançar de fase sem revisão sua.** Isso não é burocracia — é a mitigação direta ao risco de "loop de geração carregada" levantado na pesquisa (§5 do relatório).

Convenção de cada fase abaixo:
- **Objetivo** — o que existe ao final que não existia antes.
- **Reaproveita de** — o que vem pronto (ou quase) dos seus repositórios `Template-CoreSystem`/`Template-FirstPersonController`.
- **Criado do zero** — o que é novo, específico de 2D/metroidvania.
- **Critério de pronto** — como saber que a fase terminou (verificável, não "parece bom").
- **Guardrail de IA** — o limite de escopo que evita geração excessiva nessa fase específica.

---

## Fase 0 — Fundação do projeto

**Objetivo**: esqueleto de pastas e convenções documentadas, antes de qualquer gameplay. **Sem asmdef, sem package UPM/DLL** — tudo como scripts soltos em `Assets`, pensado para exportar depois como `.unitypackage` e importar em qualquer projeto novo com duplo-clique.

**Reaproveita de**: estrutura de namespace `nakatimat.*`, convenção de `InspectorLineAttribute`, referência a "Regras" nos comentários do Core.

**Criado do zero**:
- Estrutura de pastas por feature (não por tipo): `Assets/_Project/Core`, `Player`, `Interaction`, `Inventory`, `Combat`, `Abilities`, `SceneManagement`, `SaveSystem`, `UI`, `Editor`. A separação continua existindo pela organização de pastas/namespace — só não é imposta por compilação separada.
- `CONVENTIONS.md` na raiz do template: formaliza as "Regras" que hoje só existem como comentário solto (naming, SRP por componente, quando usar SO vs interface, quando usar evento estático vs Event Channel SO).
- `GameStateSO` portado do Core (praticamente 1:1, trocando estados específicos de FPS por `Playing/Paused/Menu/Cutscene`).

**Critério de pronto**: projeto compila, `CONVENTIONS.md` revisado por você, `GameStateSO` funcional numa cena de teste trocando estado via script de debug, pasta `_Project` exportável via `Assets → Export Package` sem erros de dependência faltando.

**Guardrail de IA**: gerar só esqueleto (pastas, 1 SO). Nenhuma lógica de gameplay nesta fase. Nenhum asmdef.

---

## Fase 1 — Player Controller 2D + Animação crossfade-only

**Objetivo**: personagem 2D anda, pula, cai, com animação 100% via crossfade (sem transições no Animator Controller).

**Reaproveita de**: `AnimatorBrain.cs` (praticamente sem alteração — já é crossfade puro e agnóstico de gênero de jogo), padrão de `InputReader` + interface de input (`IInteractionInput` → generalizar/estender para `IMovementInput` se necessário), `PlayerManager` como "Brain" orquestrador.

**Decisão de input (confirmada)**: New Input System (`com.unity.inputsystem`) nativo, via `.inputactions` gerado — mesmo padrão dos repos atuais. **Sem InputManager global/singleton.** `InputReader` continua como componente por entidade (fica no GameObject do player), consumido pelos outros sistemas só via interface (`IInteractionInput`/equivalente 2D), nunca por referência direta à classe concreta — consistente com o `GameStateSO`, que já evita singleton pesado por princípio.

**Criado do zero**:
- `PlayerLocomotion2D`: `Rigidbody2D`/`CharacterController2D` (raycast-based, seguindo referência Tarodev), com coyote time e jump buffering (funcionais, não estéticos — ver relatório §2).
- `PlayerAnimationController`: dispara `AnimatorBrain.PlayAnimation(hash, crossfadeTime)` a partir dos estados de movimento (Idle/Run/Jump/Fall), com hashes cacheados como `PlayerLocomotionAnimation` já faz.
- Estado do player via enum simples (`PlayerState.Locomotion/Jump/Fall/...`) — **não** introduzir HFSM ainda (YAGNI; só migrar quando o switch ficar difícil de ler, conforme relatório §2).

**Critério de pronto**: cena de teste com 1 sprite, anda/pula/cai suavemente, troca de animação sem nenhuma seta de transição configurada no Animator Controller (só states soltos + crossfade por código).

**Guardrail de IA**: gerar controller + animação nessa fase, nada de combate/dano/gate ainda mesmo que "pareça fácil de encaixar".

---

## Fase 2 — Câmera por sala (Cinemachine 2D, estilo Hollow Knight)

**Objetivo**: câmera com follow contínuo e suavizado (não câmera fixa por tela, tipo Mega Man), confinada à geometria real de cada sala, com blend suave na transição — comportamento de referência: **Hollow Knight**.

**Reaproveita de**: nada diretamente (Core é 1ª pessoa) — só o princípio de "trigger ativa comportamento" já visto em `InteractionScanner`.

**Criado do zero**:
- Virtual Camera por sala (Cinemachine) usando `CinemachineFramingTransposer` com **dead zone** (ignora pequenos movimentos verticais — pulinho, queda curta) e **look-ahead** leve na direção horizontal do movimento.
- `Confiner2D` referenciando um `PolygonCollider2D` desenhado na **forma real da sala** (não uma caixa genérica) — a câmera respeita paredes irregulares e tetos baixos, igual ao jogo de referência.
- `RoomCameraTrigger`: Collider2D que sobe prioridade da vcam da sala ao entrar; Cinemachine faz o blend sozinho (sem corte brusco). Precisa invalidar o cache de bounding do `Confiner2D` ao trocar de sala (bug documentado da Unity).
- **Fora de escopo desta fase (e do template)**: screen shake, camera punch/kick em impacto — isso é "juice" de jogo específico, decidido fora do núcleo (Hollow Knight tem, mas fica de fora daqui por decisão sua).

**Critério de pronto**: 2 salas de teste com geometria irregular lado a lado, câmera segue suavizada (sem grudar 1:1 no player), respeita o polígono de cada sala sem mostrar área fora dela, troca de sala com blend suave ao atravessar a divisa.

**Guardrail de IA**: só câmera. Resistir à tentação de já implementar scene loading aditivo aqui (isso é Fase 3) ou qualquer shake/feedback de impacto.

---

## Fase 3 — Scene management aditivo (salas)

**Objetivo**: mundo dividido em cena persistente + cenas de sala carregadas/descarregadas aditivamente.

**Reaproveita de**: `GameStateSO` (para bloquear input durante carregamento).

**Criado do zero**:
- `RoomLoader`: carrega/descarrega cena aditiva ao cruzar um `RoomTransitionTrigger`, reposiciona player no ponto de entrada correspondente.
- Cena persistente com player, câmeras (raiz), managers (GameState, Inventory, Abilities).

**Critério de pronto**: transitar entre 2+ salas descarrega a anterior (validar via Hierarchy/Profiler que a cena antiga saiu de memória), player aparece no ponto de entrada correto.

**Guardrail de IA**: não misturar lógica de save aqui ainda — só carregar/descarregar. Persistência de estado é Fase 6.

---

## Fase 4 — Combate e dano

**Objetivo**: player e inimigos podem causar/receber dano de forma desacoplada.

**Reaproveita de**: padrão Strategy do `InteractionActionSO` (mesma filosofia aplicada a "ações de dano"), `AnimatorBrain` para animação de hit/attack via crossfade.

**Criado do zero**:
- `IDamageable` (contrato), `HealthComponent` (estado + eventos `OnDamaged`/`OnDeath`), `DamageDealer` (componente simples que localiza `IDamageable` e chama `TakeDamage`).
- Hook de animação: `HealthComponent.OnDamaged` dispara crossfade de hit via `AnimatorBrain` (sem qualquer sistema de "juice" — sem hit-stop, sem screen shake; isso fica para fase de polish do jogo real, fora do template).

**Critério de pronto**: player e 1 inimigo de teste trocam dano, ambos usando o mesmo `IDamageable`/`HealthComponent`, morte dispara evento consumível por outros sistemas.

**Guardrail de IA**: gerar só o contrato + componente + 1 exemplo de uso. Não gerar sistema de armas/combo ainda — isso é conteúdo de jogo, não do template.

---

## Fase 5 — Ability-gating + Inventário

**Objetivo**: progressão via habilidades que desbloqueiam áreas (o "gate" clássico de metroidvania).

**Decisão revisada (Regra 9 — CONVENTIONS.md)**: diferente do desenho original desta fase, as habilidades de movimento (double jump, wall slide, wall jump, dash) **não** viram componentes `*Ability` separados. Toda a lógica de movimento fica dentro de `PlayerLocomotion2D`, e todo o tuning num único `LocomotionConfigSO` — mais fácil de controlar num workflow solo do que caçar valores espalhados em vários assets/componentes. Double jump já foi consolidado assim (ver `Assets/_Project/Player/PlayerLocomotion2D.cs` e `LocomotionConfigSO.cs`).

**Reaproveita de**: `InventoryManager`/`InventoryEvents` quase 1:1 (já é "enxuto" e sem UI acoplada). Padrão Strategy do `InteractionActionSO` reaproveitado para `AbilityGateSO`.

**Criado do zero**:
- `AbilityFlagsSO`: mesmo espírito do `GameStateSO`, mas guardando flags de habilidade (`DoubleJump`, `WallClimb`, `Dash`...). `PlayerLocomotion2D` só **consulta** a flag antes de consumir a habilidade (ex.: não pular duplo se `AbilityFlags.DoubleJump` estiver desligada) — a lógica de movimento em si não se move daqui.
- `AbilityGateSO` (extends o mesmo padrão de `InteractionActionSO`/nova base de "obstáculo"): obstáculo no mundo consulta a flag e libera passagem.
- Wall slide + wall jump + dash entram como mais lógica/campos dentro de `PlayerLocomotion2D`/`LocomotionConfigSO`, um de cada vez (não tudo numa sessão só).

**Referência de mercado analisada**: o asset `MetroidvaniaController` (importado em `Assets/MetroidvaniaController` para estudo) tem exatamente essas 4 habilidades implementadas, mas como referência de **ideia**, não de código — a implementação dele é uma God Class (`CharacterController2D.cs`) misturando movimento+dash+wall-slide+wall-jump+dano+morte+reload de cena, com campos públicos, Input Manager legado (`Input.GetAxisRaw`/`GetKeyDown`), pulo via `AddForce` inconsistente com velocidade direta, animação via `Animator.SetBool` (contradiz a Regra 5) e screen shake na câmera (contradiz a Regra 6). Nenhum desse código deve ser copiado — só a lista de habilidades e os parâmetros de tuning (força de dash, altura de wall-jump) valem como ponto de partida a recalibrar.

**Critério de pronto**: obstáculo de teste bloqueia o player até uma flag de debug ser ativada; ativar a flag libera a passagem sem reiniciar a cena. Desligar uma flag de habilidade remove o comportamento correspondente em `PlayerLocomotion2D` sem quebrar o resto da locomoção.

**Guardrail de IA**: gerar 1 habilidade de movimento por vez dentro de `PlayerLocomotion2D` (ex.: só wall slide+wall jump nesta sessão, dash em outra) — mesmo sendo um arquivo só, o princípio de "um sistema por vez, revisão antes de avançar" (Regra 8) continua valendo por incremento de lógica, não por arquivo novo. Sem UI de "habilidades desbloqueadas" ainda (isso é Fase 7, UI).

---

## Fase 6 — Save / Checkpoint

**Objetivo**: progresso persiste entre sessões, por sala.

**Reaproveita de**: `GameStateSO` (estado `Loading` durante save/load), estrutura de eventos já estabelecida.

**Criado do zero**:
- Interface `ISaveable` (componentes declaram o que persistir).
- `SaveSystem` central: serializa flags de habilidade, itens do inventário, salas visitadas, checkpoints ativados. Regra: inimigos comuns respawnam ao recarregar sala; chefes/eventos únicos não.

**Critério de pronto**: salvar, fechar o jogo (Play Mode stop + restart), carregar — habilidades e itens coletados persistem.

**Guardrail de IA**: escopo mínimo de save (o que já existe: flags, inventário, sala atual). Não generalizar para "sistema de save genérico para qualquer dado" — resolve o problema de hoje, não hipóteses futuras (YAGNI).

---

## Fase 7 — UI/HUD base

**Objetivo**: HUD mínimo funcional (vida, ícone de interação, feedback de item coletado).

**Reaproveita de**: padrão de `IconInteraction` (ícone de interação por tipo de dispositivo) adaptado para 2D/UI Toolkit ou uGUI, conforme preferência.

**Criado do zero**: barra/contador de vida ligado a `HealthComponent.OnDamaged`, prompt de interação, toast simples de "item coletado" ligado a `InventoryEvents`.

**Critério de pronto**: HUD reage a dano, coleta de item e interação sem polling (tudo via evento).

**Guardrail de IA**: UI mínima e funcional, sem animação de UI "bonita" (isso é polish de jogo específico, não do template).

---

## Fase 8 — Editor tooling & QoL

**Objetivo**: portar as ferramentas de produtividade solo do Core.

**Reaproveita de**: `InspectorLineAttribute` + `InspectorLineDrawer`, padrão `Reset()` de auto-configuração (aplicar em componentes 2D: auto-adicionar `Rigidbody2D`/`Collider2D` corretos, auto-setar layers).

**Criado do zero**: nada estrutural — só adaptar os auto-configs existentes para os novos componentes 2D criados nas fases anteriores.

**Critério de pronto**: arrastar `PlayerLocomotion2D`/`InteractableObject2D` num GameObject novo já configura collider/layer certos sem passo manual.

**Guardrail de IA**: só tooling de editor, zero gameplay novo.

---

## Fase 9 — Validação de expansão (checkpoint de arquitetura)

**Objetivo**: confirmar que o template realmente "abre espaço" em vez de limitar, antes de declarar v1.0 pronta.

**Como validar** (sem escrever jogo completo, só protótipos de estresse):
1. Adicionar 1 inimigo com IA simples usando `HealthComponent`/`IDamageable` sem modificar Combat core → confirma desacoplamento.
2. Adicionar 1 habilidade nova (ex.: dash) e 1 gate novo sem tocar em `AbilityFlagsSO`/`AbilityGateSO` existentes → confirma extensibilidade Strategy.
3. Adicionar 1 sala nova com sua própria vcam/confiner sem tocar em `RoomLoader` → confirma que scene management escala por conteúdo, não por código.
4. Revisão de tamanho de arquivo: nenhum script deveria passar de ~150–200 linhas nesse ponto; se passou, é sinal de dividir responsabilidade (SRP).

**Critério de pronto**: os 4 testes acima passam sem editar código dos sistemas core — só adicionando assets/componentes novos.

---

## Regra de governança de IA para todas as fases

1. Cada fase é uma conversa/tarefa própria — não pedir "faça as fases 1 a 4 de uma vez".
2. Ao final de cada fase, revisão sua antes de eu prosseguir (ler o diff, entender o raciocínio — não só rodar e ver se compila).
3. Se uma fase gerar mais do que ~3-4 arquivos novos ou qualquer arquivo grande (200+ linhas), paramos e dividimos antes de continuar.
4. Preferir portar/adaptar código dos seus repositórios existentes a gerar do zero, sempre que o padrão já existir lá.
5. Nenhum sistema de "juice" (screen shake, hit-stop, squash&stretch automático, partículas de feedback) entra no template — isso é decisão de jogo específico, adicionada organicamente depois, fora deste plano.
6. Sem asmdef, sem package UPM, sem DLL. O template é uma pasta de scripts em `Assets`, exportada como `.unitypackage` e importada em cada projeto novo — decisão explícita porque só você usa o template.
