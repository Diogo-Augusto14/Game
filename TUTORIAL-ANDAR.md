# Andar com várias salas

Gerador de andar no estilo do Binding of Isaac: uma grade de salas ligadas por portas,
com sala inicial, sala do item e sala do chefe, troca de sala com a câmera deslizando e
um minimapa no canto.

## Testar

1. `Tools ▸ Jogo ▸ Andar ▸ Criar cena de teste do andar` (cria `Assets/Scenes/Andar.unity`).
2. Play. Ande com **WASD** ou as setas.

A cena já vem com um `Bootstrap` desligado: é a presença dele que impede o Bootstrap de
plataforma de se instalar sozinho e montar a fase lateral por cima do andar.

Se a cena não tiver ninguém com a tag `Player`, o `Andar` cria um **jogador de teste**
(um quadradinho sem gravidade). Quando o movimento top-down de verdade existir, basta ele
estar na cena com a tag `Player` que o andar usa ele.

## Como o andar é sorteado

`GeradorDeAndar` segue o algoritmo do Isaac:

1. A sala inicial fica no meio da grade (9 × 8 casas).
2. Cada sala tenta abrir uma vizinha em cada direção. A vizinha é recusada se a casa já
   estiver ocupada, se o andar já tiver salas suficientes, se ela ficaria colada em mais
   de uma sala, ou numa moeda de 50%.
3. Se não deu o número de salas, ou faltam becos (salas com uma porta só), tenta de novo.
4. O **chefe** vai no beco mais longe da sala inicial; o **item**, em outro beco sorteado.

Número de salas: 8 ou 9 no primeiro andar, cerca de 3 a mais por andar, até 20.

O gerador não depende da Unity, então dá para testar fora do editor.

## O que se vê

| | No mundo | No minimapa |
|---|---|---|
| Sala onde você está | — | branca |
| Sala visitada | — | cinza |
| Vizinha de uma visitada | — | cinza escuro |
| Sala do item | chão amarelado, porta dourada | ícone dourado |
| Sala do chefe | chão avermelhado, porta vermelha | ícone vermelho |

## Onde mexer

| Quero mudar | Onde |
|---|---|
| Número do andar, semente, tamanho da grade | Componente `Andar ▸ Geracao` |
| Repetir sempre o mesmo andar | `Andar ▸ Semente` diferente de 0 (o Console mostra a semente de cada Play) |
| Tamanho da sala, largura da porta | `Andar ▸ Tamanho da sala` (o padrão 13 × 7 é o do Isaac) |
| Cores do chão, paredes e portas | `Andar ▸ Cores` |
| Tamanho e cores do minimapa | Componente `Minimapa` |

## Ganchos para as outras partes

- `Andar.Atual.AoEntrarNaSala` avisa quando o jogador entra numa sala. A sala com inimigos
  se pendura aqui: chama `sala.Trancar(true)` ao entrar e `Trancar(false)` quando o último
  inimigo morrer. Porta trancada vira parede de verdade.
- `Andar.Atual.ProximoAndar()` gera o andar seguinte (para o alçapão depois do chefe).
- `Andar.Atual.Mapa` tem a grade inteira; `SalaAtual` é onde o jogador está.
