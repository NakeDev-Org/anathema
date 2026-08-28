# Plano de Ação — Localization automatizada (Unity Localization + Crowdin)

Base: integração feita em 2026-08-28 usando `com.unity.localization` 1.5.12 já presente no `Packages/manifest.json`, e um projeto Crowdin já existente (sem arquivos enviados ainda).
Princípio geral: **o Unity nunca fala com o Crowdin diretamente.** Eles se conectam através de 1 arquivo texto neutro (CSV multilíngue) versionado no Git. Cada lado só sabe ler/escrever esse arquivo — isso é o que torna o pipeline simples de debugar e barato de automatizar sendo 1 dev só.

---

## 1. Visão geral do pipeline

```
┌─────────────────┐   Export CSV    ┌──────────────────────┐   push CSV    ┌───────────┐
│ Unity Editor     │ ───────────────▶ │ Assets/Localization/  │ ──────────────▶ │  GitHub   │
│ String Tables    │                 │ Tables/*.csv          │               │  (main)   │
│ (fonte da verdade│ ◀─────────────── │ (versionado no Git)   │ ◀────────────── │           │
│  em runtime)     │   Import CSV     └──────────────────────┘   PR automático └─────┬─────┘
└─────────────────┘                                                                  │
                                                                                       │ Action dispara
                                                                        ┌──────────────▼──────────────┐
                                                                        │  GitHub Actions              │
                                                                        │  crowdin-upload.yml (push)   │
                                                                        │  crowdin-download.yml (cron) │
                                                                        └──────────────┬──────────────┘
                                                                                       │ upload sources /
                                                                                       │ download translations
                                                                        ┌──────────────▼──────────────┐
                                                                        │        Crowdin project       │
                                                                        │  tradutores trabalham aqui   │
                                                                        └───────────────────────────────┘
```

**Duas pontas manuais, de propósito** (não dá pra eliminar sem rodar Unity em modo batch na CI — ver Fase 5):
1. Depois de escrever/editar texto no Unity → 1 clique em **Export** antes de commitar.
2. Depois que o PR de tradução do Crowdin for mergeado → 1 clique em **Import** no Unity.

Tudo entre esses dois cliques (subir pro Crowdin, notificar tradutor, baixar tradução, abrir PR) é automático.

---

## 2. Estado atual (2026-08-28) ✅

| Peça | Onde | Status |
|---|---|---|
| Package `com.unity.localization` | `Packages/manifest.json` | ✅ instalado (1.5.12) |
| Locale de origem (pt) | [Assets/Localization/Locales/Locale-pt-BR.asset](../Assets/Localization/Locales/Locale-pt-BR.asset) | ✅ criado |
| Locale alvo (en) | [Assets/Localization/Locales/Locale-en.asset](../Assets/Localization/Locales/Locale-en.asset) | ✅ criado |
| String Table Collection | [Assets/Localization/Tables/UI.asset](../Assets/Localization/Tables/UI.asset) (+ `UI Shared Data`, `UI_en`, `UI_pt`) | ✅ criada, com 1 entrada de exemplo (`UI_SAMPLE_HELLO`) |
| CSV multilíngue (ponte com Crowdin) | [Assets/Localization/Tables/UI.csv](../Assets/Localization/Tables/UI.csv) | ✅ exportado e testado (round-trip export→import validado) |
| Ferramenta de sync (Editor) | [Assets/_Project/Editor/Localization/LocalizationCsvSync.cs](../Assets/_Project/Editor/Localization/LocalizationCsvSync.cs) | ✅ menus `NakeDev/Localization/Export...` e `Import...` |
| Config do Crowdin CLI/Action | [crowdin.yml](../crowdin.yml) (raiz do repo) | ✅ criado |
| Automação de upload | [.github/workflows/crowdin-upload.yml](../.github/workflows/crowdin-upload.yml) | ✅ criado — dispara em push na `main` tocando `*.csv` |
| Automação de download + PR | [.github/workflows/crowdin-download.yml](../.github/workflows/crowdin-download.yml) | ✅ criado — cron diário (12:00 UTC) |
| Secrets no GitHub (`CROWDIN_PROJECT_ID`, `CROWDIN_PERSONAL_TOKEN`) | GitHub → Settings → Secrets | ❌ **pendente — só você pode fazer isso** |
| Commit/push dos arquivos acima | Git | ❌ **pendente — nada disso foi commitado ainda** |

**Gotcha já identificado (documentando para não te surpreender depois)**: o asset do locale de origem se chama `Locale-pt-BR.asset` no disco, mas o `LocaleIdentifier.Code` real dele é `pt` (não `pt-BR`) — porque `Locale.CreateLocale(SystemLanguage.Portuguese)` sempre gera o código genérico `pt`, independente do nome do arquivo. Isso é só um detalhe cosmético (nome de arquivo ≠ código do locale), mas se você um dia quiser `pt-BR` de verdade (com plural rules e formatação específica do Brasil, diferente de Portugal), veja a Fase 3 abaixo — vale trocar antes de o projeto crescer, porque o `LocaleIdentifier.Code` vira parte da chave usada em toda parte (nome de coluna no CSV, nome de arquivo `UI_pt.asset`, etc.) e trocar depois é um find-and-replace chato.

---

## 3. Anatomia do CSV — por que cada coluna existe

Arquivo real gerado ([UI.csv](../Assets/Localization/Tables/UI.csv)):

```csv
Key,Id,Shared Comments,English(en),English(en) Comments,Portuguese(pt),Portuguese(pt) Comments
"UI_SAMPLE_HELLO",2467172352,"","Hello, world!","","Ola, mundo!",""
```

| Coluna | Vem de | Papel no Crowdin (`scheme` em [crowdin.yml](../crowdin.yml)) |
|---|---|---|
| `Key` | Nome da entrada na `SharedTableData` | `identifier` — como o Crowdin identifica a string entre uploads |
| `Id` | Hash numérico interno da Unity | ignorada (`,,`) — Crowdin nunca precisa disso |
| `Shared Comments` | Comentário opcional por chave (contexto p/ todos os idiomas) | `context` — aparece pro tradutor como nota |
| `English(en)` | Tabela `UI_en.asset` | `en` — coluna de **destino**: Crowdin escreve a tradução aqui de volta no mesmo arquivo |
| `English(en) Comments` | Comentário específico da entrada em inglês | ignorada |
| `Portuguese(pt)` | Tabela `UI_pt.asset` | `source_phrase` — o texto que os tradutores veem como original |
| `Portuguese(pt) Comments` | Comentário específico da entrada em português | ignorada |

O `scheme` inteiro fica: `"identifier,,context,en,,source_phrase,"` — 7 posições, uma por coluna, na mesma ordem em que a Unity exporta.

**Por que multilíngue num arquivo só, e não 1 CSV por idioma?** Porque é exatamente o formato que a extensão CSV nativa da Unity Localization já produz (`Csv.Export`/`Csv.ImportInto` em `UnityEditor.Localization.Plugins.CSV`) — zero conversão de formato no meio do caminho. Cada nova String Table Collection vira 1 CSV novo, cada um com sua própria seção em `crowdin.yml` se o scheme mudar (ver Fase 2).

---

## 4. Workflow do dia a dia (com 1 dev só)

1. Você adiciona/edita texto direto na String Table Collection pelo editor da Unity (`Window → Asset Management → Localization Tables`), escrevendo o texto em português (coluna fonte).
2. Antes de commitar: menu **NakeDev → Localization → Export All Collections to CSV**.
3. `git add`, commit, `git push` na `main` (ou PR normal, se você preferir revisar antes de mergear).
4. **[automático]** `crowdin-upload.yml` detecta que um `.csv` mudou, sobe as strings novas pro Crowdin.
5. Você (ou um tradutor externo, se algum dia contratar um) traduz dentro do Crowdin.
6. **[automático]** todo dia às 12:00 UTC, `crowdin-download.yml` baixa o que já foi traduzido e abre um PR (`l10n/crowdin-translations`) com o CSV atualizado.
7. Você revisa e mergeia o PR.
8. Você puxa a `main`, abre o Unity, roda **NakeDev → Localization → Import All Collections from CSV**.
9. Os `StringTable` assets (`UI_en.asset`, `UI_pt.asset`, etc.) ficam com a tradução nova, prontos pra usar em runtime.

Passos 4 e 6 não exigem nada de você. Passos 2 e 8 são os únicos cliques manuais — ver Fase 5 para eliminar até esses.

---

## 5. Setup pendente (só você pode fazer)

1. **Segredos no GitHub** (repo `NakeDev-Org/projeto2D` → Settings → Secrets and variables → Actions):
   - `CROWDIN_PROJECT_ID` — no Crowdin, em Project Settings → API → "Project ID".
   - `CROWDIN_PERSONAL_TOKEN` — em Account Settings → API → "New Token" (escopo mínimo: `Project`, com acesso ao projeto certo).
2. **Confirmar idiomas no projeto Crowdin**: o projeto já existe mas está vazio — confirme que o idioma de origem lá está como Português (Brasil) e que Inglês está na lista de idiomas alvo, senão o upload do CSV vai falhar silenciosamente por mismatch de idioma.
3. **Commitar os arquivos já criados**: `Assets/Localization/`, `Assets/_Project/Editor/Localization/`, `crowdin.yml`, `.github/workflows/crowdin-*.yml` estão todos como untracked no Git ainda — nada foi commitado, de propósito, pra você revisar antes.
4. Depois do primeiro push com os secrets configurados, rode manualmente o workflow **Crowdin - Upload sources** uma vez (aba Actions → "Run workflow") pra validar a conexão antes de esperar o cron.

---

## 6. Roadmap de expansão

### Fase 1 — Bootstrap ✅ Concluída
Locales, 1 String Table Collection, CSV export/import, workflows de upload/download. (Tudo listado na seção 2.)

### Fase 2 — Mais Collections (organização por domínio) ⏳
**Objetivo**: sair de 1 collection `UI` genérica para collections separadas por domínio, do jeito que times de localização de verdade organizam — evita 1 arquivo CSV gigante e misturado.

Sugestão de collections:
- `UI` — botões, menus, HUD (o que já existe).
- `Dialogue` — falas de NPC/cutscene (metroidvania costuma ter bastante disso).
- `Items` — nomes/descrições de itens do inventário.
- `SystemMessages` — mensagens de erro, save/load, confirmações.

**Critério de pronto**: cada collection nova gera seu próprio `.csv` em `Assets/Localization/Tables/`, e o `crowdin.yml` já cobre todos automaticamente (o glob `*.csv` já existente cobre isso sem editar o `crowdin.yml` — só confirmar que o `scheme` continua válido pra cada uma, já que todas nascem com a mesma estrutura de colunas via `LocalizationCsvSync`).

**Guardrail**: não criar collection nova até ter conteúdo real pra ela — sem esqueleto vazio "pra usar depois".

### Fase 3 — pt-BR de verdade (plural rules regionais) ⏳
**Objetivo**: trocar o locale genérico `pt` pelo `pt-BR` de verdade (`Locale.CreateLocale(new LocaleIdentifier("pt-BR"))`), se algum dia o jogo precisar de plural rules ou formatação de número/data específica do Brasil (diferente de Portugal).

**Critério de pronto**: `LocaleIdentifier.Code` = `pt-BR` em todo lugar (nome de coluna no CSV vira `Portuguese (Brazil)(pt-BR)`, nome de asset `UI_pt-BR.asset`), `scheme` do `crowdin.yml` atualizado de `source_phrase` na coluna `pt` pra coluna `pt-BR`.

**Guardrail**: só fazer essa migração numa sessão dedicada — é um rename que toca todo asset de tabela existente, não misturar com adição de conteúdo novo na mesma sessão.

### Fase 4 — Mais idiomas alvo ⏳
**Objetivo**: adicionar idiomas além do inglês (ex.: espanhol) quando o MVP justificar.

**Passo a passo**: (1) criar o `Locale` novo na Unity (`LocalizationCsvSync` não precisa mudar — `Csv.Export` já inclui qualquer locale que a collection tiver), (2) adicionar o idioma como alvo no projeto Crowdin, (3) adicionar a letra do idioma no `scheme` do `crowdin.yml` na posição da nova coluna (ex.: `es`).

**Guardrail**: 1 idioma por vez — validar round-trip completo (export → upload → tradução manual de teste → download → import) antes de adicionar o próximo.

### Fase 5 — Eliminar os 2 cliques manuais (CI headless) ⏳
**Objetivo**: rodar o Unity em modo batch (`-batchmode -executeMethod`) dentro do próprio GitHub Actions, chamando `LocalizationCsvSync.ExportAll`/`ImportAll` direto na pipeline — aí nem o clique de Export nem o de Import ficam por sua conta.

**Por que não fizemos isso agora**: exige um runner com Unity instalado (self-hosted, ou a imagem Docker do [game-ci/unity-builder](https://game.ci/)) e um **arquivo de licença Unity ativado na CI** (Personal license tem processo próprio de ativação headless) — é infraestrutura a mais que vale a pena só quando o atrito dos 2 cliques manuais realmente incomodar no dia a dia.

**Critério de pronto**: push de um CSV editado direto no Crowdin (sem passar pela Unity) resulta em PR que, ao ser mergeado, já teria as `StringTable` re-importadas automaticamente por um job de CI — sem abrir o Editor.

**Guardrail**: não migrar pra isso "porque dá pra fazer" — só quando o volume de idiomas/collections tornar os cliques manuais um gargalo real (Regra geral do projeto: YAGNI).

### Fase 6 — QA de tradução ⏳
**Objetivo**: pegar strings esquecidas (chave existe em pt mas não em en) antes que virem bug em produção.

**Criado do zero**: um `EditorWindow` ou item de menu simples que varre todas as String Table Collections e lista entradas com valor vazio em qualquer locale que não seja a fonte — roda como checagem manual antes de cada release, não precisa ser CI ainda.

**Guardrail**: relatório, não bloqueio automático de build — decisão de travar CI nisso fica pra quando (e se) o projeto tiver mais de 1 pessoa mexendo em texto.

---

## 7. Troubleshooting comum

| Sintoma | Causa provável | Solução |
|---|---|---|
| Action falha com "Project not found" | `CROWDIN_PROJECT_ID` errado ou secret não configurado | Conferir em Crowdin → Project Settings → API |
| Action falha com "Unauthorized" | `CROWDIN_PERSONAL_TOKEN` expirado/sem escopo | Gerar token novo com escopo de Project |
| CSV sobe pro Crowdin mas coluna `en` não aparece pra traduzir | Idioma inglês não está na lista de idiomas alvo do projeto Crowdin | Adicionar o idioma no Crowdin antes do próximo upload |
| Import na Unity não muda nada | Esqueceu de fazer `git pull` depois do PR do Crowdin mergeado, ou o CSV não foi salvo pelo Action | Conferir se `UI.csv` no disco tem o texto traduzido antes de rodar o Import |
| `Csv.ImportInto` lança erro de coluna | Alguém editou o cabeçalho do CSV manualmente (renomeou coluna) | Nunca editar o cabeçalho à mão — ele é gerado pela Unity, mudanças de locale devem vir de mudar a Collection na Unity e reexportar |
