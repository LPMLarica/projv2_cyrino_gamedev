# NEXUS — Duelo de Campeões (Unity 6)

Jogo de cartas 3D no estilo Magic: The Gathering com tema de League of Legends.
Você compra cartas do baralho, invoca campeões na mesa e derruba o Nexus inimigo.

## Como rodar (cerca de 5 minutos)

1. **Unity Hub → New project → Universal 3D** (Unity 6, 6000.x). Dê um nome e crie.
2. Feche a cena aberta, se houver alterações. No Explorer/Finder, **copie a pasta `Assets/NexusCards`** deste zip para dentro da pasta `Assets` do projeto novo.
   Volte ao Unity e espere a importação e a compilação terminarem (barra inferior parada, Console sem erros vermelhos).
3. No menu do topo, clique em **NEXUS → 1 - Construir projeto completo**.
   O construtor:
   - baixa as artes oficiais dos campeões (Data Dragon da Riot) para `Resources/ChampionArt`, para o jogo funcionar offline;
   - cria materiais, a malha de cristal, as animações (`.anim`) e os Animator Controllers;
   - cria os prefabs (Carta, Projétil, Nexus, Torre, Espaço de Mesa, Pilha do Baralho, Número Flutuante, AudioManager);
   - monta as cenas **Menu** e **Arena** em `Assets/NexusCards/Generated/Scenes` e as registra no Build Settings.
4. Ao terminar, a cena **Menu** já estará aberta. **Aperte Play.**

> Para gerar um executável: File → Build Profiles → Windows/Mac → Build. As duas cenas já estão na lista.

### Se algo der errado
| Sintoma | Solução |
|---|---|
| Menu **NEXUS** não aparece | Há erro de compilação no Console. Confira se a pasta foi copiada inteira (com `Editor/` e `Scripts/`). |
| Erro sobre `UnityEngine.Input` / Input System | Edit → Project Settings → Player → *Active Input Handling* = **Both**. (O código já suporta os dois, isso é só um fallback.) |
| Cartas sem imagem (degradê colorido) | Sem internet no passo 3. Conecte-se e rode **NEXUS → 2 - Baixar artes dos campeões**. O jogo também tenta baixar sozinho durante a partida. |
| Tudo rosa/magenta | O projeto não é URP nem Built-in padrão. Crie o projeto com o template **Universal 3D** e rode o passo 3 de novo. |
| Quero reconstruir do zero | Apague `Assets/NexusCards/Generated` e rode o passo 3 de novo. |

## Controles (também aparecem no jogo, tecla H)
- **Clique numa carta da mão** e depois **num espaço livre** da sua fileira (ou clique de novo na carta). Teclas **1–8** selecionam e invocam pela posição na mão.
- **Clique num campeão PRONTO** (brilho turquesa) e depois **num alvo destacado**: campeão, Torre ou Nexus.
- **Espaço**: encerrar turno · **Botão direito / Esc**: cancelar · **H**: ajuda · **M**: silenciar · **R**: jogar de novo na tela de resultado.

## Onde está cada coisa (para a avaliação)
| Arquivo | Responsabilidade |
|---|---|
| `Scripts/Core/Match.cs` | Regras: turnos, mana, combate, Torre/Portão, desafios, vitória e derrota |
| `Scripts/Core/ScoreBook.cs` | Pontuação com `HashSet` de IDs de evento: o mesmo evento nunca conta duas vezes |
| `Scripts/Core/Synergy.cs` | Sinergias de região e combos de dupla (a regra adaptada) |
| `Scripts/Core/AIPlanner.cs` | IA do adversário |
| `Scripts/Game/BattleDirector.cs` | Orquestra a partida, instancia prefabs, dispara projéteis, aplica o resultado da colisão |
| `Scripts/Game/PlayerController.cs` | Comandos do jogador (mouse e teclado, Input System novo e antigo) |
| `Scripts/View/Projectile.cs` | Rigidbody com gravidade e `OnCollisionEnter`: acerto causa dano, Portão bloqueia |
| `Scripts/View/CardView.cs` | Carta 3D: queda física na mesa, Animator (ataque/impacto), arte do campeão |
| `Scripts/View/GateController.cs` | Abre o Portão (Animator + desativa o colisor) quando a Torre cai |
| `Scripts/View/StructureView.cs` | Nexus/Torre; desmoronamento com `AddExplosionForce` |
| `Scripts/UI/HUDController.cs`, `ResultPanel.cs`, `MainMenu.cs`, `SceneFlow.cs` | Canvas do menu, HUD, vitória/derrota e troca de cenas |
| `Scripts/Audio/AudioManager.cs` | Música de fundo e efeitos; singleton que não duplica ao reiniciar |
| `Editor/NexusBuilder.cs` | Monta cenas, prefabs, materiais e animações |

## Créditos e aviso legal
Música e efeitos sonoros foram sintetizados para este projeto. Fontes Cinzel e Barlow Semi Condensed (SIL Open Font License).
As artes dos campeões vêm do Data Dragon, o CDN público da Riot Games. NEXUS foi criado sob a política *Legal Jibber Jabber* da Riot Games, usando recursos de propriedade da Riot Games. A Riot Games não endossa nem patrocina este projeto. Uso acadêmico, sem fins comerciais. Não venda nem monetize o jogo.
