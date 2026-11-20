# GDD — NEXUS: Duelo de Campeões

## 1. Visão geral
| | |
|---|---|
| Gênero | Jogo de cartas tático 3D, um jogador contra IA |
| Motor | Unity 6 (URP), C# |
| Referência (projeto-base) | **Magic: The Gathering**: comprar do baralho, pagar custo, invocar criaturas na mesa, combater até zerar a vida do oponente |
| Tema | League of Legends: 24 campeões reais, regiões de Runeterra, Torre, Nexus e Tropas |
| Sessão | 8 a 15 turnos (cerca de 8 a 12 minutos) |

## 2. Loop principal
1. Início do turno: +1 de mana máxima (até 10), mana recarregada, compra 1 carta.
2. Jogar cartas da mão pagando mana: **campeões** caem fisicamente na mesa; **feitiços** (Incendiar, Curar, Exaurir) resolvem na hora.
3. Atacar com campeões prontos. Cada ataque dispara um **projétil físico** (Rigidbody com gravidade). O dano só é aplicado quando o projétil **colide** com o alvo.
4. O atacado revida (dano simultâneo, como no combate do MTG).
5. Encerrar o turno. O inimigo joga, ataca e a Torre dispara.

**Vitória:** Nexus inimigo chega a 0. **Derrota:** seu Nexus chega a 0 (pelos ataques inimigos, pela Torre ou pelo baralho vazio).

## 3. Adaptações em relação ao projeto-base (regras criadas pela equipe)
| Regra | MTG original | NEXUS | Decisão que provoca |
|---|---|---|---|
| **Sinergia regional** | Não existe | Com 2 ou mais campeões da mesma região na mesa, todos ganham bônus: Ionia +1/+1, Demacia +0/+2, Noxus +2 ATQ, Piltover & Zaun +1 ATQ e dano dobrado em estruturas, Freljord +0/+1 e atordoa quem atingem | Vale mais invocar o campeão mais forte ou o que fecha a sinergia? |
| **Combos de dupla** | Não existe | Seis duplas do lore (Xayah+Rakan, Jinx+Vi, Yasuo+Malphite, Garen+Lux, Ashe+Braum, Darius+Draven) dão bônus contínuo e um efeito ao se formarem (dano em área, atordoar todos, curar o Nexus, comprar 2, atacar já) | Segurar a carta até o par chegar ou usá-la agora? Matar primeiro a dupla do inimigo? |
| **Portão físico e Torre** | Ataque direto ao jogador | O Nexus inimigo fica atrás de um Portão com colisor sólido. Projéteis contra o Nexus **ricocheteiam fisicamente**. A Torre atira todo turno no seu campeão mais forte | Atacar a Torre (abre o caminho) ou limpar os campeões inimigos (protege seu Nexus)? |
| **Guardião** | Bloqueio escolhido pelo defensor | Enquanto houver Guardião, só ele pode ser alvo | Ordem dos ataques e uso de Incendiar/Exaurir |
| **Mana automática** | Terrenos | Mana cresce sozinha; o inimigo tem 1 turno de atraso | Curva de mana mais simples, mais tempo para pensar em sinergia |

Trocar só nomes ou aparência não basta. Os três primeiros itens mudam o que o jogador decide a cada turno.

## 4. Situações de desafio (fase única, duas situações distintas)
| | Desafio 1 — Cerco à Torre | Desafio 2 — Nexus Exposto |
|---|---|---|
| Obstáculo | Portão sólido bloqueia ataques ao Nexus | Portão aberto, o Nexus pode ser atacado |
| Ameaça | Torre (10 de vida) atira 1 de dano por turno (2 no Difícil) | Nexus convoca uma **Tropa Guardiã** (1/2) por turno |
| Ações exigidas | Derrubar a Torre, que desmorona com física, e o Portão desce por animação | Eliminar Guardiões antes de atingir o Nexus |
| Gatilho | Início da partida | Torre destruída (evento em C# muda a fase) |

## 5. Pontuação (calculada em C#, `ScoreBook.cs`)
| Evento | Pontos | ID único (não conta duas vezes) |
|---|---|---|
| Abater campeão inimigo | 100 | `kill:<id da carta>` |
| Abater Tropa | 40 | `kill:<id da carta>` |
| Dano em estrutura | 10 por ponto | `hit:<sequência>` |
| Torre destruída | 400 | `turret` |
| Sinergia regional ativada pela 1ª vez | 50 | `region:<região>` |
| Combo formado | 150 | `combo:<combo>:<idA>-<idB>` |
| Vitória | 1000 + 20 por ponto de vida restante | `victory` |

O recorde fica salvo em PlayerPrefs.

## 6. Física e reutilização
- **Mesa** com BoxCollider: as cartas caem com gravidade e não atravessam o tampo.
- **Portão** com BoxCollider e Rigidbody cinemático: obstáculo sólido no caminho do projétil.
- **Projétil** com Rigidbody, gravidade e velocidade balística calculada. `OnCollisionEnter` causa o dano ou registra o bloqueio.
- **Torre/Nexus destruídos**: as peças recebem Rigidbody e `AddExplosionForce`.
- **Prefabs com várias instâncias**: Carta (dezenas, criadas durante a partida), Espaço de Mesa (10 na cena), Projétil, Pilha do Baralho (2), Nexus (2), Número Flutuante, AudioManager (1 por cena).

## 7. Cenas, interface e fluxo
`Menu` (Canvas: Jogar, Como Jogar, Dificuldade, Créditos, Sair) → `Arena` (HUD: pontos, recorde, vida do Nexus dos dois lados, vida da Torre, mana, turno, desafio atual, sinergias ativas, painel de informação, Encerrar Turno) → painel de **Vitória** ou **Derrota** → *Jogar novamente* (recarrega a Arena do zero) ou *Menu*.
Reinício: a cena é recarregada e o `AudioManager` e o carregador de artes são singletons protegidos, então nada é duplicado.

## 8. Animação, áudio e leitura visual
- **Animator/clips**: `Carta_Ataque`, `Carta_AtaqueInimigo`, `Carta_Impacto`, `Carta_Repouso` (controller CartaVisual), `Portao_Abrindo` (controller Portao), `Nexus_Flutuar` (controller Nexus). Ficam em `Generated/Animations`.
- **Áudio**: música de menu e de batalha em loop. Efeitos para comprar, invocar, atacar, acertar, bloqueio no Portão, abertura do Portão, combo, tiro da Torre, morte, vitória e derrota.
- **Leitura**: os aliados usam turquesa e os inimigos usam vermelho. As cartas jogáveis brilham, os alvos válidos ganham anel ou brilho vermelho e o Portão tem energia turquesa. A câmera fixa enquadra mão, mesa, Torre e os dois Nexus.

## 9. Balanceamento
Valores calibrados em 3.000 partidas simuladas da lógica de regras (IA contra IA). Uma IA gulosa no lugar do jogador vence cerca de 67% no Normal. Um jogador passivo perde 100%, então as duas condições de fim podem ser alcançadas. As partidas duram de 8 a 15 turnos.
