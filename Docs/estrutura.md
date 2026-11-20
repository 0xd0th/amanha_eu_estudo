# estrutura do jogo

os arquivos próprios do jogo ficam em `Assets/_game`. use nomes em minúsculas e snake_case para pastas e assets, sem espaços ou acentos. objetos e elementos da hierarquia usam PascalCase. scripts e identificadores c# seguem as convenções abaixo. mantenha os nomes especiais exigidos pelo unity e os arquivos de pacotes externos.

## importação da arte existente

- `font` → `art/fonts`
- `hud` → `art/ui/hud`
- `menus` → `art/ui/menus`
- `personagens` → `art/characters`
- `screens/abertura` → `art/screens/opening`
- `screens/casa` → `art/screens/home`
- `screens/facul` → `art/screens/college`
- `screens/academia` → `art/screens/gym`

preserve as subpastas dos assets e normalize os nomes na importação. mova assets já importados pela janela project do unity para preservar as referências. não importe os arquivos de ícones de pasta do macos.

## cenas planejadas

- `00_opening.unity`: splash e menu principal
- `01_intro.unity`: introdução
- `02_home.unity`: casa
- `03_college.unity`: faculdade
- `04_gym.unity`: academia

as cenas acima são planejadas; ainda não foram criadas. a sequência narrativa depende do gdd e do storyboard.

## cenas existentes

todas ficam em `Assets/_game/scenes`:

- `00_opening`: splash, vídeo de introdução e menu principal
- `choice_prototype`: escolhas pelo telefone
- `MinigameAula`, `MinigameProcrastinacao`, `MinigameAcademia`, `MinigameQuiz`: minigames
- `Chefe`, `jogatina`: boss e fase de turnos
- `B02_DebugScene`, `B08_HudTestScene`: cenas de teste do núcleo de regras e do hud (geradas pelos menus de editor)

alguns nomes de cena ainda não seguem snake_case porque são carregados por nome em cenas e scripts (por exemplo `PauseManager.VoltarAoMenuPrincipal`). renomeie somente depois de conferir esses usos.

## responsabilidades

pastas de código em `Assets/_game/scripts`:

- `core`: regras, estado do jogo, dias e status (`AmanhaEuEstudo.Core`, sem dependência de ui)
- `hud`: barras, calendário, retrato e pilha de débito (`AmanhaEuEstudo.UI`)
- `debug_tools`: hud de depuração (`AmanhaEuEstudo.DebugTools`)
- `editor_tools`: menus `Tools > Amanhã eu estudo` e drawers do inspector (`AmanhaEuEstudo.EditorTools`)
- `ui`: abertura, menu, pausa, configurações e transições
- `choices`: escolhas pelo telefone
- `minigames`: minigames e seus componentes
- `boss`: boss, turnos e gerenciador de tempo
- `dialogue`, `gameplay`: reservadas, ainda vazias
- `editor`: ferramentas de editor dos menus e das escolhas pelo telefone

outras pastas de `Assets/_game`:

- `tests/edit_mode`: testes do núcleo de regras e do hud
- `prefabs`: elementos reutilizáveis (`ui`, `minigames`)
- `data/game_rules`: `GameConfig`, `HudConfig` e `choices/` (escolhas com `ChoiceData`)
- `data/choices`: situações das escolhas pelo telefone (`ChoiceDefinition`)
- `animations`: animações de interface e personagens
- `audio`: músicas e efeitos
- `art/minigames`, `art/screens/boss`: arte dos minigames e do boss

regra importante: uma pasta com `.asmdef` agrupa todos os scripts dentro dela num assembly próprio. coloque um `.asmdef` somente em pasta dedicada, nunca em pasta que misture scripts de outras áreas.

## caminhos fixos no código

os menus de editor gravam assets por caminho e nome. se mover ou renomear algo, atualize as constantes:

- `editor_tools/B02Menu.cs`: dados, `choices`, cena `B02_DebugScene`
- `editor_tools/B08Menu.cs`: `art/ui/hud/placeholder`, `HudConfig`, `GameConfig`, `prefabs/ui` e cena `B08_HudTestScene`
- `editor_tools/B12Menu.cs`: `data/game_rules/choices`
- `editor/ChoiceSceneBuilder.cs`: cena `choice_prototype`, `prefabs/ui/choice_card` e `data/choices`

## convenções de nomes

- pastas e assets (imagens, fontes, áudio, cenas e prefabs): `snake_case`, como `splash_screen.png` e `00_opening.unity`.
- objetos da cena e elementos da interface na hierarquia: `PascalCase`, como `OpeningController`, `SplashPanel`, `MainMenuPanel` e `NewGameButton`. o nome do objeto é independente do nome do arquivo de asset.
- scripts e classes c#: `PascalCase`, como `OpeningController.cs` e `OpeningController`. mantenha o nome do arquivo igual ao da classe principal, especialmente para componentes do unity.
- métodos e propriedades c#: `PascalCase`, como `StartGame()` e `SplashDuration`.
- campos privados, variáveis locais e parâmetros: `camelCase`, como `splashDuration`.
- exemplos de scripts planejados: `OpeningController.cs`, `MainMenuController.cs`, `SettingsPanel.cs`, `ScreenTransition.cs` e `IntroController.cs`.

as pastas dos scripts continuam em minúsculas, por exemplo: `Assets/_game/scripts/ui/OpeningController.cs`.

os arquivos do template original foram preservados. os assets visuais de `organizar` foram distribuídos em `Assets/_game/art`, com nomes em minúsculas e snake_case. o registro dessa importação está em `Docs/organizacao_assets.json`.

o guia das escolhas pelo telefone está em `Docs/choices_readme.md`. a documentação das tarefas B02, B08 e B12 está na mesma pasta.

## hierarquia planejada da abertura

```text
00_opening.unity
├── MainCamera
├── EventSystem
├── OpeningController
└── Canvas
    ├── SplashPanel
    │   └── SplashImage
    ├── MainMenuPanel
    │   ├── Background
    │   ├── TitleText
    │   └── Buttons
    │       ├── NewGameButton
    │       └── SettingsButton
    └── SettingsPanel
        ├── TitleText
        └── BackButton
```

o arquivo da cena continua como `00_opening.unity`. o objeto `OpeningController` recebe o script `OpeningController.cs`. um arquivo de prefab pode se chamar `new_game_button.prefab`, enquanto seu objeto na hierarquia se chama `NewGameButton`.
