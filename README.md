# Arcanis Duels - Projeto de Redes

É um jogo de turnos que utiliza cartas aleatóriamente dadas aos jogadores para eles fazerem a melhor sequência para atacarem um ao outro até ficarem sem vida.
Eles têm apenas atributos de vida, ataque e velocidade e cada personagem tem esses valores diferentes.
O jogo foi baseado principalmente em Fairy Tail Dungeons.

---

## Como Jogar

Abre a build e é deparado com a tela inicial de login, que depois o leva para a cena principal com as opções:
    - "Create Lobby" - Muda de cena, gera um código e esse jogador será o host.
    - "Find Match" - Abre um painel, aceita um código, muda de cena e usar esse código para conectar.
    - "Select Character" - Muda de cena para escolher um personagem.
    - "Select Deck" - Muda de cena para montar um deck.
    - "Quit" - Saí de jogo.

Todas as cenas têm um botão para retornar ao menu principal.
O jogo apenas permite criar ou entrar em partida se tiver selecionado um personagem e montado um deck de 20 cartas.

---

## Descrição Técnica

### O que foi implementado

A mecânica principal do jogo é enviar as cartas da mão para o servidor fazer os calculos, recebendo as sequências e vendo os donos das cartas, o servidor faz os cálculos pela ordem de velocidades ou se existirem cartas com prioridade, a sequência passa a ter prioridade.

O sistema de rede começou com base no sistema utilizado em aulas de rede, alterando para tratar um dos jogadores, o que gera o código da sessão, como Host. Uso o sistema de Relay do Unity para criar ligações P2P.

A conexão da ligação é feita com dados armazenados em "SelectionData.cs", que é uma classe que guarda durante o jogo o nome do personagem escolhido e o deck para inicializar no início do jogo, se o jogador pretende ser Host ou não com um booleano, caso sim, terá de clicar em "Create Lobby", e um código em string, que o cliente deverá clicar em "Find Match" para surgir uma tela que pede o código a enviar.

Isto é enviado para uma cena onde faz a conexão inteira, até enviar ambos os jogadores para uma nova cena, onde spawnam os seus personagens(sendo o Host sempre à esquerda) e o servidor distribuí a mão para cada jogador, surgindo na parte inferior central da tela.

Foi implementado um sistema de login básico, mas inseguro(Não tem nenhum tipo de encriptação para quando a passe é guardada no computador do jogador).

### Técnicas utilizadas

- **Unity Netcode for GameObjects** — sistema de networking principal
- **Relay (Unity Services)** — para ligações P2P sem servidor dedicado
- **NetworkVariable** — sincronização de estado entre clientes
- **ClientRpc / ServerRpc** — comunicação entre servidor e clientes
- **SceneManagement via NetworkSceneManager** — carregamento de cenas sincronizado
- **Login (Unity Authentication)** — para realizar sign in ou login ao abrir o jogo. (Implementado, mas não usado, era para um sistema de matchmaking)

---

## Mensagens de Rede

| Mensagem | Tipo | Direção | Descrição |
|---|---|---|---|
| `DisposeCardsClientRpc` | ClientRpc | Servidor → Clientes | Envia as mãos de cartas para cada jogador |
| `ReceiveSequencesServerRpc` | ServerRpc | Cliente → Servidor | Envia a sequência de cartas selecionada |
| `InitVisualsClientRpc` | ClientRpc | Servidor → Clientes | Inicializa a UI com referências aos personagens |
| `InitSelectorsClientRpc` | ClientRpc | Servidor → Clientes | Inicializa os seletores de cartas |
| `CallDeathAndEndGameClientRpc` | ClientRpc | Servidor → Clientes | Notifica fim de jogo e mostra resultado |
| `_actualTurn` | NetworkVariable | Servidor → Clientes | Número do turno atual |
| Qualquer Stats Do Personagem | NetworkVariable | Servidor → Clientes | Valores de atribuidos aos personagens que são alterados ao longo do jogo. |

---

## Análise de Largura de Banda

_Estimativa do custo de rede por turno(Expectativa calculada com ajuda de AI):_

| Mensagem | Tamanho estimado | Frequência |
|---|---|---|
| `DisposeCardsClientRpc` (2 mãos × 3 cartas) | ~100 bytes | 1× por turno |
| `ReceiveSequencesServerRpc` | ~50 bytes | 1× por jogador por turno |
| `NetworkVariable` updates (vida, turno) | ~8 bytes cada | Quando mudam |

_Gráficos presentes no UnityCloud, usando o Relay_
![alt text](Assets/Images/image.png)

Infelizmente temos poucos dados ainda para tirar conclusões, porém no dia 6 de Junho, onde finalmente o jogo passava de um turno, os valores chegaram a 1.2 MiB, sendo cerca de 3 turnos para a sessão acabar. O valor é relativamente baixo se levarmos em conta que o valor máximo para usar os serviços online do Unity por mês é de 50 GB/mês.

---

## Diagrama de Arquitectura de Redes

```
┌─────────────────────────────────────────────────────┐
│                   Unity Relay Server                │
│              (Unity Gaming Services)                │
└───────────────────────────┬─────────────────────────┘
                            │
                ┌───────────┴───────────┐
                │                       │
        ┌───────▼────────┐     ┌────────▼───────┐
        │   Host/Server  │     │    Client      │
        │  (Player 1)    │◄───►│  (Player 2)    │
        │                │     │                │
        │ BattleManager  │     │ BattleManager  │
        │ CharacterStats │     │ CharacterStats │
        │ Visualizer     │     │ Visualizer     │
        │ CardSelector   │     │ CardSelector   │
        └────────────────┘     └────────────────┘
```

Client-Server com Host (um dos jogadores é servidor e cliente simultaneamente). A comunicação é feita via Unity Relay para evitar a necessidade de IP público.

---

## Diagrama de Protocolo

```
Host/Server                                    Client
     │                                            │
     │── Conecta ambos à cena de batalha ───────► │
     │                                            │
     │── SpawnWithOwnership() ──────────────────► │
     │                                            │
     │── InitSelectorsClientRpc ────────────────► │
     │── InitVisualsClientRpc ──────────────────► │
     │── DisposeCardsClientRpc (hand1 + hand2) ─► │
     │                                            │
     │         [Jogadores selecionam cartas]      │
     │                                            │
     │◄── ReceiveSequencesServerRpc (Player 1) ── │
     │◄── ReceiveSequencesServerRpc (Player 2) ── │
     │                                            │
     │   [Servidor processa turno]                │
     │   [NetworkVariable _health atualizado]     │
     │                                            │
     │── DisposeCardsClientRpc (novas mãos) ────► │
     │                                            │
     │   [Repete até um ficar sem vida]           │
     │                                            │
     │── CallDeathAndEndGameClientRpc ──────────► │
     │                                            │
```

---

## Bibliografia

- [Unity Netcode — RPCs](https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/rpc/)
- [Unity Authentication Services](https://docs.unity.com/en-us/authentication)
- [Tutorial das Bases de um Jogo Multiplayer em Unity](https://www.youtube.com/watch?v=3yuBOB3VrCk)
- [Aulas de Redes do Professor Diogo Andrade](https://www.youtube.com/playlist?list=PLheBz0T_uVP2xi8RJiPhm31mPHXWgztqZ)
- [Repositório do Projeto de Redes de Júlia Costa](https://github.com/Juhhxx/ProjectoRedes)
- Uso do DeepSeekAI e ClaudAI para correção de bugs, opiniões sobre eficiência e ideias alternativas face aos bloqueios de progresso.
- Arte criada por mim e também retirada do Craftpix.
