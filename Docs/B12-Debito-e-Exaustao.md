# B12 · Débito de Estudo e Exaustão (parte 1)

Estende o B02 (GameManager, dias e status) e liga o débito ao HUD do B08. Estados críticos (Gripou/Boiando/Crise) ficam para o B12b.

## O que mudou

```
Scripts/Core/      + StatFactor; GameState, ChoiceData, GameRules, GameManager estendidos
Scripts/UI/        HudView escuta OnStudyDebtChanged (DebtPileView não mudou)
Scripts/Editor/    + StatEntryDrawers (rótulo com o nome da barra), + B12Menu, B02Menu com os valores novos
Scripts/DebugTools DebugHud estendido; HudDebugPanel removido
```

O Core continua sem referências. Nenhum número de balanceamento está no código: tudo vem dos `ChoiceData`.

## Novos campos

### `GameState`
| Campo | Regra |
|---|---|
| `int studyDebt` | Começa em 0 e zera no `Reset`. Mínimo 0, **sem máximo** (o máximo da pilha no HUD é só visual). |

### `ChoiceData`
| Campo | Padrão | Papel |
|---|---|---|
| `int debtDelta` | 0 | Débito somado pela escolha. O débito é consequência de escolhas, não uma escolha. Pode ser negativo (reduz, até 0). |
| `float lowScoreThreshold` | 0 (desligado) | Se > 0 e o multiplicador de desempenho for **menor** que ele, a nota é baixa. `[Min(0)]`. |
| `StatDelta[] lowScoreDeltas` | vazio | Somados aos deltas normais quando a nota é baixa, **sem** multiplicador. |
| `bool lowScoreCausesExhaustion` | false | Nota baixa marca "exausto amanhã". |
| `StatFactor[] gainFactors` | vazio | `{stat, factor}`, aplicado só a deltas **positivos** da barra. Ausente = 1. |
| `bool forceMaxDifficulty` | false | A escolha deve rodar o minigame na dificuldade máxima. |

`OnValidate` avisa (sem corrigir) barra repetida em `deltas`, `lowScoreDeltas` e `gainFactors`, e fator negativo. Na regra, fator repetido se multiplica e fator negativo conta como 0.

O Inspector mostra o nome da barra ("Saúde", "Inteligência"...) como rótulo de cada elemento de `StatDelta[]` e `StatFactor[]` (inclusive o desgaste diário do `GameConfig`).

## Ordem de aplicação em `ApplyChoice(choice, performanceMultiplier = 1f)`

A assinatura não mudou.

1. Deltas normais, na ordem. Ganho: `RoundToInt(base × desempenho × fator)`, **um único arredondamento**. Custo (delta ≤ 0): valor cheio, sem desempenho nem fator.
2. Se `lowScoreThreshold > 0` e `desempenho < lowScoreThreshold`: `lowScoreDeltas`, em valor cheio.
3. `debtDelta` (`studyDebt = max(0, studyDebt + debtDelta)`).
4. Exaustão: `exhaustedTomorrow = true` se `causesExhaustionNextDay` **ou** (nota baixa e `lowScoreCausesExhaustion`).
5. Avança o período (a virada do dia promove `exhaustedTomorrow` a `exhaustedToday`, que dura um dia).

**Burnout** em qualquer barra, nos passos 1 ou 2, interrompe tudo: o que viria depois (deltas restantes, débito, exaustão, avanço) não é aplicado.

## Eventos

Novo: `OnStudyDebtChanged(int oldValue, int newValue)` em `GameRules`, repassado pelo `GameManager`. Durante a partida só dispara quando o valor muda (débito já em 0 que recebe −1 não dispara).

Sequência de início e de reinício (a única mudança em relação ao B02 é a linha 3):

| # | Início (`GameManager.Start`) | Reinício (`RestartGame()`) |
|---|---|---|
| 1 | | `OnGameRestarted` |
| 2 | `OnStatChanged` por barra (old == new) | idem |
| 3 | **`OnStudyDebtChanged(0, 0)`** | idem |
| 4 | `OnDayStarted(1)` | idem |
| 5 | `OnPeriodChanged(Morning)` | idem |

`old == new` significa "valor definido", não uma mudança: o `HudView` usa `SetDebtInstant` nesse caso (sem animar) e `SetDebt` (anima) quando muda de verdade. Ao nascer (`OnEnable`) ele lê `State.studyDebt`. Regra de inscrição do B02 continua valendo (inscrever no `OnEnable`).

## Dificuldade forçada

`GameRules.IsMaxDifficultyForced(choice)` e `GameManager.IsMaxDifficultyForced(choice)` hoje só devolvem `choice.forceMaxDifficulty`. É o ponto de encaixe para os minigames; nenhuma dificuldade de minigame foi implementada.

## Dados

Menu `Tools > Amanhã eu estudo > Atualizar dados (B12)`: altera só os campos abaixo nos assets existentes, loga `[B12] Choice_x.campo: antigo → novo`, salva com `AssetDatabase.SaveAssets` e pula (com aviso) assets que não existem. Rodar de novo não muda nada.

| Escolha | Campos |
|---|---|
| `Choice_skip_and_sleep` | Saúde +15 (GDD); `debtDelta` = 1 |
| `Choice_memes` | `debtDelta` = 1; `lowScoreThreshold` = 0,5; `lowScoreDeltas` = Psicológico +6, Inteligência −6 (**placeholder**); `lowScoreCausesExhaustion` = true |
| `Choice_go_tired` | Inteligência = a de `Choice_go_to_college` (lida do asset); `gainFactors` = Inteligência ×0,5; `forceMaxDifficulty` = true |

O `B02Menu` (`Criar dados de exemplo`) já cria projeto limpo com esses mesmos valores.

## Como testar nas cenas de debug

O `DebugHud` (cenas `B02_DebugScene` e `B08_HudTestScene`) mostra o débito e "Exausto: hoje sim/não · amanhã sim/não". Cada escolha tem dois botões: **nota normal (1.0)** e **nota baixa (0.3)**. Escolhas com `forceMaxDifficulty` mostram `[dificuldade máxima]`.

1. `Atualizar dados (B12)`.
2. Se a `B08_HudTestScene` foi criada antes do B12, rode `Criar cena de teste do HUD` e confirme "Recriar" (a cena é refeita no mesmo caminho, mantendo o GUID).
3. Play. Em uma noite, "Ver memes" com nota baixa: a pilha sobe, aparece "amanhã sim". Na manhã seguinte só há "Ir mesmo cansado" e "Faltar e dormir". "Faltar e dormir" dá +15 de Saúde e +1 de débito.
4. Débito alto → "Reiniciar": a pilha vai a 0 sem animar.

Testes: `Window > General > Test Runner > EditMode > Run All`.

## Pontos de encaixe

- **Combate:** lê `State.studyDebt` (efeito de +10% de vida do Nakara por ponto: **não implementado aqui**, é de outro card) e `State.exhaustedToday` no último dia (já funciona via "Virar a noite").
- **Minigames:** consultam `IsMaxDifficultyForced(choice)`.

## Pendências para os minigames

- A dificuldade precisa aumentar quando o jogador está Exausto, consultando `IsMaxDifficultyForced` e `State.exhaustedToday`.
- Todos os minigames devem entregar o multiplicador como **nota normalizada (base 1.0)**, porque a nota baixa depende disso.
- Hoje os minigames (`Assets/Scripts/Minigame*.cs`) não chamam `ApplyChoice`; a integração é de outro card.

## Fora de escopo, em aberto (B12b)

Estados críticos Gripou/Boiando/Crise (barra abaixo de 25). Proposta, **nada implementado**:

- checagem única na virada do dia, depois do desgaste e do Burnout, valendo para o dia novo;
- o estado fica travado até o fim do dia e é reavaliado na virada seguinte.

O alerta visual abaixo de 25 já existe no HUD (B08).

## Decisões pendentes (G03)

- Limite da nota baixa (0,5) e valores dos deltas de nota baixa (placeholder).
- Outras escolhas que somam débito, e se alguma escolha reduz débito (o código já aceita `debtDelta` negativo).
- Se "Ver memes" também aparece à tarde (o GDD diz "toda noite"; hoje é tarde e noite).
- Se o Exausto aparece no HUD/retrato.
- Débito máximo visual (14, `HudConfig`).
- Valores do desgaste diário (−5, placeholder).
