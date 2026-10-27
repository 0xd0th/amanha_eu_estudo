# B02 · GameManager, dias e status

## Visão geral

```
Assets/_Project/
  Scripts/Core/        AmanhaEuEstudo.Core        (sem dependência de UGUI/Input System)
  Scripts/DebugTools/  AmanhaEuEstudo.DebugTools  (DebugHud: UI de teste, referencia Core + UGUI + Input System)
  Scripts/Editor/      AmanhaEuEstudo.EditorTools (menus Tools > Amanhã eu estudo)
  Tests/EditMode/      AmanhaEuEstudo.Tests.EditMode
  Data/                GameConfig.asset e Choices/*.asset (gerados pelo menu)
  Scenes/B02_DebugScene.unity
```

| Tipo | Papel |
|---|---|
| `StatType`, `TimeOfDay` | Enums. Nomes das barras: só em `StatType` + `DisplayNames.Of` (renomear = 2 lugares, ambos em `Core`). |
| `GameConfig` (SO) | Valor inicial/mín./máx., total de dias, desgaste diário por barra, catálogo de escolhas. |
| `ChoiceData` (SO) | id, nome, períodos, disponibilidade, deltas, flag "exausto amanhã". |
| `GameState` | Dados puros `[Serializable]` (arrays, sem `Dictionary`): pronto para `JsonUtility`. |
| `GameRules` | Regras em C# puro (testável sem cena). |
| `GameManager` | MonoBehaviour fino: cria `GameRules`, repassa os eventos, expõe a API. |

`GameManager.Instance` é setado no `Awake` (execution order -100) e limpo no `OnDestroy`. Não há `DontDestroyOnLoad` nem estado em campos estáticos além dessa referência.

### API

- `GetAvailableChoices()`: escolhas disponíveis agora (lista vazia se a partida acabou).
- `ApplyChoice(choice, performanceMultiplier = 1f)`: retorna `false` e não muda nada se a partida acabou ou a escolha não está disponível.
- `RestartGame()`.
- `State` (leitura): `day`, `period`, `stats`, `exhaustedToday`, `phase`, `BurnoutStat`.

### Eventos

| Evento | Quando |
|---|---|
| `OnStatChanged(StatType, old, new)` | Só quando o valor (já com clamp) realmente muda. |
| `OnPeriodChanged(TimeOfDay)` | Ao avançar para tarde/noite e, na virada de dia, para manhã. |
| `OnDayEnded(int dia)` | Fim da noite, **antes** do desgaste. |
| `OnDayStarted(int dia)` | Após o desgaste, com o estado do novo dia pronto (não dispara em Burnout nem no dia final). |
| `OnBurnout(StatType)` | No instante em que a barra chega ao mínimo. |
| `OnFinalDayReached` | Fim da noite do último dia (após o desgaste). `State.day` permanece em `TotalDays`. |
| `OnGameRestarted` | Após `RestartGame()`. Só este evento é disparado no reinício; quem escuta deve reler o `State`. |

## Como outros cards se conectam

- **HUD:** escuta `OnStatChanged`, `OnPeriodChanged`, `OnDayStarted`, `OnBurnout`, `OnGameRestarted` e lê `State`. Pode usar `GameManager.Instance` no `OnEnable`/`Start`.
- **Minigames:** a escolha é feita pelo jogador; o minigame roda e depois chama `ApplyChoice(choice, multiplicador)`.
- **Combate:** escuta `OnFinalDayReached`.
- **Save/Load ("Continuar"):** serializar `State`. Ainda não há API para injetar um estado carregado; será necessário adicionar quando o card existir.

## Regras

- Barras sempre entre `MinValue` e `MaxValue` (clamp).
- **Burnout** = qualquer barra chegando a `MinValue` (0), seja por escolha ou pelo desgaste. Os deltas são aplicados na ordem da lista; ao zerar, os restantes são ignorados, o período não avança e nenhuma escolha é aceita depois.
- **Multiplicador de desempenho: só deltas positivos (ganhos) são multiplicados** (arredondado). Deltas negativos (custos) são aplicados integralmente, porque representam o tempo gasto na atividade: ir mal no minigame não pode deixar a escolha de graça. Multiplicador negativo é tratado como 0.
- **Fim da noite:** `OnDayEnded` → desgaste diário (com clamp e checagem de Burnout) → `exhaustedToday = exhaustedTomorrow` → próximo dia (ou `FinalDayReached`). Burnout no desgaste tem prioridade sobre o dia final.
- **Manhã travada** é só um caso de dados: se há uma única escolha disponível na manhã (`Morning` + `RestedOnly`), ela é a única opção. Escolhas `ExhaustedOnly` aparecem apenas quando o jogador acordou exausto. Uma escolha com "Causes Exhaustion Next Day" marca `exhaustedTomorrow`, que só vira `exhaustedToday` na virada. O exausto dura um dia.

## Criar uma nova escolha só com dados

1. `Assets > Create > Amanhã eu estudo > Choice Data`.
2. Preencha id, nome, períodos (`Periods`), disponibilidade e a lista de deltas.
3. Arraste o asset para a lista `Choices` do `GameConfig`.

Nenhum código é necessário.

## Testar pela cena de debug

1. Menu `Tools > Amanhã eu estudo > Criar dados de exemplo` (não sobrescreve nada que já existe; loga o que pulou).
2. Menu `Tools > Amanhã eu estudo > Criar cena de debug`.
3. Abra `Assets/_Project/Scenes/B02_DebugScene.unity` e dê Play.
4. Há um botão por escolha disponível; o texto mostra dia, período, estado de exaustão e as três barras. "Reiniciar" restaura o estado inicial. Burnout mostra mensagem de fim de jogo.

Para exercitar o Exausto: escolha "Virar a noite estudando" ou "Ir para a boate" à noite; na manhã seguinte aparecem "Ir mesmo cansado" e "Faltar e dormir".

Testes automáticos: `Window > General > Test Runner > EditMode > Run All`.

## Decisões pendentes para a equipe (G03)

- **Desgaste diário:** os -5 por barra são **placeholder**. Valores definitivos (podem ser diferentes por barra).
- **Nomes definitivos das barras** (hoje Saúde / Inteligência / Psicológico).
- **Regras completas do Exausto:** duração, penalidades próprias além das escolhas da manhã, quais escolhas da noite o causam.
- **Valores de cada escolha:** todos os deltas dos assets de exemplo são placeholders de teste.
- **Burnout com barra em exatamente 0 vs. abaixo de um limite:** hoje é igual a `MinValue`.
- **Ordem dos deltas dentro de uma escolha** importa se uma escolha tiver custo que zera uma barra e ganho na mesma barra depois (hoje o Burnout vem primeiro).
