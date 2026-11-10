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

## responsabilidades

- `scripts/core`: fluxo geral
- `scripts/ui`: abertura, menu, configurações e transições
- `scripts/dialogue`: diálogos e introdução
- `scripts/gameplay`: sistemas de jogo
- `scripts/minigames`: minigames
- `prefabs`: elementos reutilizáveis
- `data`: dados de autoria, não arquivos de progresso do jogador
- `animations`: animações de interface e personagens
- `audio`: músicas e efeitos

## convenções de nomes

- pastas e assets (imagens, fontes, áudio, cenas e prefabs): `snake_case`, como `splash_screen.png` e `00_opening.unity`.
- objetos da cena e elementos da interface na hierarquia: `PascalCase`, como `OpeningController`, `SplashPanel`, `MainMenuPanel` e `NewGameButton`. o nome do objeto é independente do nome do arquivo de asset.
- scripts e classes c#: `PascalCase`, como `OpeningController.cs` e `OpeningController`. mantenha o nome do arquivo igual ao da classe principal, especialmente para componentes do unity.
- métodos e propriedades c#: `PascalCase`, como `StartGame()` e `SplashDuration`.
- campos privados, variáveis locais e parâmetros: `camelCase`, como `splashDuration`.
- exemplos de scripts planejados: `OpeningController.cs`, `MainMenuController.cs`, `SettingsPanel.cs`, `ScreenTransition.cs` e `IntroController.cs`.

as pastas dos scripts continuam em minúsculas, por exemplo: `Assets/_game/scripts/ui/OpeningController.cs`.

os arquivos do template original foram preservados. os assets visuais de `organizar` foram distribuídos em `Assets/_game/art`, com nomes em minúsculas e snake_case.

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
