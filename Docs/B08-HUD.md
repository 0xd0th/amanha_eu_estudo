# B08 · HUD e dinâmica de preenchimento dos status

## Estrutura

```
Assets/_Project/
  Scripts/UI/          AmanhaEuEstudo.UI  (refs: Core, UnityEngine.UI, Unity.TextMeshPro)
  Scripts/Editor/      + B08Menu (agora referencia UI, UnityEngine.UI, Unity.TextMeshPro)
  Art/UI/HUD/Placeholder/   PNGs placeholder + white_pixel.png (gerado pelo menu)
  Data/HudConfig.asset      (gerado pelo menu)
  Prefabs/UI/HUD.prefab, StatBar.prefab   (gerados pelo menu)
  Scenes/B08_HudTestScene.unity           (gerada pelo menu)
```

Dependências: `Core` (sem referências) ← `UI` ← `DebugTools` ← `EditorTools`; `Tests.EditMode` → `Core`, `UI`. O Core não conhece o HUD.

| Tipo | Papel |
|---|---|
| `BarFillAnimation`, `PixelSnap`, `DaysRemaining`, `DebtFill`, `PortraitSelector` | Lógica em C# puro (testada em Edit Mode). |
| `HudConfig` (SO) | Todos os números e sprites: duração, limiar de barra baixa, visual por barra, débito, retrato. |
| `StatBarView` | Ícone + fill (`Image` Filled) + número (TMP). Reutilizável sozinho. |
| `DebtPileView` | Pilha de livros: imagem de trás com alpha `emptyAlpha`, da frente Filled vertical a partir de baixo. |
| `CalendarView` | Número de dias restantes, período e texto de apoio ("Falta 1 dia" / "Faltam N dias"). |
| `PortraitView` | Sprite/tint do retrato via `PortraitSelector`. |
| `HudView` | Orquestra: assina os eventos do `GameManager` no `OnEnable`, desassina no `OnDisable`. |

## Como cada view se conecta

`HudView.OnEnable` pega `GameManager.Instance` (se nulo: `LogWarning` e se desativa), configura as views e lê `State` uma vez, **sem animar**. Depois só reage a eventos (nada lê o `State` no `Update`):

- `OnStatChanged(stat, old, new)` → `GetBar(stat).AnimateTo(new)` e atualiza o retrato.
- `OnDayStarted` / `OnPeriodChanged` → calendário e período.
- `OnStudyDebtChanged(old, new)` (B12) → `DebtPileView.SetDebt(new)` (anima); com old == new é o valor inicial e usa `SetDebtInstant`. O débito vem do Core (`State.studyDebt`), não há mais fonte de débito no HUD.
- `OnGameRestarted` → relê `State` e aplica tudo sem animar. Os `OnStatChanged` com old == new que vêm em seguida não animam nada.

`StatBarView` e `DebtPileView` animam com `Time.unscaledDeltaTime` (funcionam com o jogo pausado). O número da barra segue o valor animado. Abaixo de `lowThreshold` (25) a barra pisca/escurece (cor + ícone + número: nunca só cor).

Nomes das barras: só `DisplayNames.Of(StatType)`. `StatBarView.nameText` (opcional, vazio no HUD) mostra esse nome, para o menu de pausa.

## Pontos de encaixe

- **`StatBar.prefab`**: pode ser usado sozinho (menu de pausa, tela de resultado do minigame). Chame `Configure(HudConfig, StatType, min, max)` e depois `SetInstant(v)` / `AnimateTo(v)`. `OnFillAnimationCompleted` dispara ao fim de cada `AnimateTo` (imediatamente se não havia o que animar).
- **`HudView.GetBar(StatType)`** e **`HudView.DebtPile`** para outros sistemas.
- **`DebtPileView.SetDebt(int)`** (animado) e `SetDebtInstant(int)`: o `HudView` chama esses métodos a partir de `OnStudyDebtChanged` (B12). Outros sistemas não devem chamá-los: o débito real é `State.studyDebt`.
- Eventos críticos abaixo de 25 (diálogo, dificuldade), pausa, resultado do minigame e Burnout: fora do escopo; o `lowThreshold` e `GetBar` são os ganchos.

## Trocar placeholders pela arte definitiva

Só trocar os sprites no `HudConfig` (ícones por barra, estados do retrato) e, se mudar o sprite do calendário/pilha, no prefab `HUD.prefab`. Ajustar `pixelSteps` de cada barra e da pilha ao tamanho final da arte (valores atuais: 48 nas barras, 32 na pilha, **placeholders**, sem cálculo automático). A fonte pixel do jogo entra depois como `TMP_FontAsset` próprio (hoje usa a fonte padrão do TMP).

## Menus (`Tools > Amanhã eu estudo`)

- **Configurar sprites do HUD**: gera `white_pixel.png` (se não existir) e ajusta a importação da pasta Placeholder: Sprite (Single), Point, sem compressão, sem mipmaps.
- **Criar HUD**: checa os TMP Essentials e os 6 sprites antes de criar qualquer coisa (se faltar algo, loga erro e aborta). Cria `HudConfig.asset`, `StatBar.prefab` e `HUD.prefab`. Nunca sobrescreve: se já existir, loga e pula (apague o asset para regenerar).
- **Criar cena de teste do HUD**: `B08_HudTestScene.unity` com GameManager, HUD, `DebugHud` (movido para centro/embaixo). O `HudDebugPanel` (botões de débito) saiu no B12: o débito agora nasce das escolhas. Se a cena já existia, o menu pede confirmação e a recria no mesmo caminho (mantém o GUID).

Canvas: Screen Space - Overlay, Scale With Screen Size 1920x1080, match 0,5, `pixelPerfect` ligado, sem `GraphicRaycaster`, `raycastTarget = false` em tudo: o HUD nunca bloqueia clique.

## Como testar

1. `Tools > Amanhã eu estudo > Criar dados de exemplo` (se ainda não rodou o B02).
2. `Configurar sprites do HUD`, `Criar HUD`, `Criar cena de teste do HUD`.
3. Abrir `B08_HudTestScene` e dar Play. Clicar nas escolhas e ver barras e números animando juntos, o retrato mudando de tint ao baixar a menor barra, o calendário (14 → 13 ...) e o período. Escolhas com débito (ver B12) enchem a pilha. "Reiniciar" volta tudo sem animar.
4. Testes: `Window > General > Test Runner > EditMode > Run All`.

## Decisões pendentes (G03)

- `maxDebt` (14 é placeholder) e `emptyAlpha`.
- Origem do retrato (`LowestStat` é placeholder) e limiares dos estados (50/25/0).
- Ícone de Inteligência: usa `icon_social_pessoas` temporariamente; falta arte definitiva.
- Resolução de referência do Canvas (1920x1080 é placeholder) e `pixelSteps`.
- Cores das barras (placeholder) e limiar/velocidade do alerta de barra baixa.
