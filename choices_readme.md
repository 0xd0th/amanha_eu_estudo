# escolhas pelo telefone

Abra `Assets/_game/scenes/choice_prototype.unity` e use Play. O exemplo contém quatro opções, uma bloqueada. Role a lista para ver todas. Ao escolher, os botões são bloqueados e aparece a confirmação.

## composição com outras cenas

A cena não contém câmera nem altera a abertura. Abra com **Open Scene Additive** para compor no editor. Em execução, inclua a cena no Scene List do Build Profile e carregue com `SceneManager.LoadSceneAsync("choice_prototype", LoadSceneMode.Additive)`. Carregue somente uma instância. O fundo semitransparente bloqueia cliques na interface atrás do telefone, mas não pausa a simulação nem comandos de gameplay; a cena hospedeira deve suspender esses comandos enquanto a escolha estiver aberta.

`ChoiceSceneInput` usa o EventSystem existente ou cria um temporário se a cena estiver sozinha. O Canvas usa ordem 100; ajuste se a interface hospedeira já utilizar ordens maiores.

## criar situações

No Project, use **Create > Game > Choices > Choice**. Preencha contexto, pergunta e opções. IDs precisam ser únicos dentro da situação. Os atributos usam IDs livres, permitindo saúde, inteligência, social ou atributos futuros sem alterar a interface.

No `ChoiceSystem`, configure `Initial Choice` para testar. Desmarque `Show On Start` quando a cena hospedeira for abrir a escolha através de `ChoiceController.Show(definition)`. `Close()` esconde a interface, e `Show()` permite reutilizá-la.

Cada impacto contém ID, nome, sprite e valor interno. A interface mostra somente o ícone e nome do atributo: sem sinal, valor, seta ou cor de ganho/perda. Impactos zero são omitidos e atributos repetidos aparecem uma única vez. Os quadrados neutros são placeholders de ícones.

`Available` e `Unavailable Reason` configuram bloqueios. `CanChoose` permite uma condição dinâmica adicional; ela é reavaliada antes de aceitar o clique. Para atualizar visualmente uma condição que mudou, apresente novamente a situação com `Show()`.

## conectar as consequências

Assine `ChoiceController.Selected` para receber a situação e a opção, incluindo seus impactos. A cena hospedeira aplica os efeitos, salva o progresso e abre a próxima situação. O componente não pressupõe um sistema de status existente. O evento `On Selected` do Inspector também entrega o ID da opção para conexões simples. Evite aplicar os mesmos efeitos pelos dois eventos e remova assinaturas quando o consumidor for desativado.

A seleção é travada antes de emitir os eventos, prevenindo efeitos duplicados por cliques repetidos. `Show()` inicia uma nova rodada de escolha.

## substituir os mockups

- Na cena, substitua as imagens `HandPlaceholder` e `PhoneFramePlaceholder`, ajustando seus RectTransforms. Mantenha a tela e seus controles alinhados ao telefone.
- Edite `Assets/_game/prefabs/ui/choice_card.prefab` para trocar o fundo dos cartões, fontes e composição.
- Defina os sprites dos atributos nos impactos do asset `exam_invitation.asset` ou de suas próprias situações.
- Preserve as referências dos componentes `PhoneChoiceView` e `ChoiceCardView` ao editar a hierarquia.

Os cartões são instanciados em Play e usam uma área rolável. O exemplo não modifica os status reais do jogador; demonstra apresentação, disponibilidade e seleção.

## validação

Compilação e testes em Play Mode realizados numa cópia temporária com Unity 6000.6.0f1: quatro cartões, opção bloqueada, seleção única, bloqueio após seleção, ausência dos valores de impacto nos textos, reabertura e fechamento. Também passou a convivência com um EventSystem em outra cena: apenas o sistema hospedeiro permaneceu ativo. A inspeção visual na janela do editor não foi possível nesta sessão.
