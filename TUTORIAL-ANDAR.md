# Andar com várias salas

Gerador de andar no estilo do Binding of Isaac: uma grade de salas ligadas por portas,
com sala inicial, sala do item e sala do chefe, inimigos que trancam as portas até você
limpar a sala, troca de sala com a câmera deslizando e um minimapa no canto.

## Testar

1. `Tools ▸ Jogo ▸ Andar ▸ Criar cena de teste do andar` (cria `Assets/Scenes/Andar.unity`).
2. Play. **WASD** anda, **setas** atiram.

A cena já vem com um `Bootstrap` desligado: é a presença dele que impede o Bootstrap de
plataforma de se instalar sozinho e montar a fase lateral por cima do andar.

Se a cena não tiver ninguém com a tag `Player`, o `Andar` monta o mesmo jogador da cena
top-down (`BootstrapTopDown.CriarJogador`), e também uma HUD com a vida.

As salas são as da pasta `Sala/` (`Sala.Criar`): cada casa do andar vira uma `Sala`
com porta só nos lados que têm vizinha. Salas comuns ganham de 2 a 4 inimigos e a do
chefe ganha o chefe (veja `TUTORIAL-CHEFE.md`). A sala inicial e a do item ficam vazias.
Ao entrar numa sala com inimigo vivo as portas trancam, e abrem quando o último morre.

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
| Sala do item | chão amarelado, batentes dourados na porta | ícone dourado |
| Sala do chefe | chão avermelhado, batentes vermelhos na porta | ícone vermelho |

## Onde mexer

| Quero mudar | Onde |
|---|---|
| Número do andar, semente, tamanho da grade | Componente `Andar ▸ Geracao` |
| Repetir sempre o mesmo andar | `Andar ▸ Semente` diferente de 0 (o Console mostra a semente de cada Play) |
| Quantos inimigos por sala | `Andar ▸ Inimigos` |
| Cores das salas especiais | `Andar ▸ Cores` |
| Paredes, portas e inimigos em si | Pasta `Sala/` (`Sala`, `Porta`, `FabricaDeInimigos`) |
| Tamanho e cores do minimapa | Componente `Minimapa` |

## Ganchos para as outras partes

- `Andar.Atual.AoEntrarNaSala` avisa quando o jogador entra numa `Sala`. Trancar e
  destrancar as portas é a própria sala que faz; use `sala.AoLimpar` para soltar prêmio.
- A troca de sala escuta `Porta.AoAtravessar` e põe o jogador em `PontoDeChegada` da
  porta oposta da sala vizinha.
- `Andar.Atual.ProximoAndar()` gera o andar seguinte (para o alçapão depois do chefe).
- `Andar.Atual.Mapa` tem a grade inteira; `SalaAtual` é onde o jogador está.
