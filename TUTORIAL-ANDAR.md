# Andar com várias salas

Gerador de andar no estilo do Binding of Isaac: uma grade de salas ligadas por portas,
com sala inicial, sala do item e sala do chefe, inimigos que trancam as portas até você
limpar a sala, troca de sala com a câmera deslizando e um minimapa no canto.

## Testar

1. Play. O editor sempre começa pela cena do jogo, `Assets/Scenes/Jogo.unity`.
2. **WASD** anda, **setas** atiram.

Se a cena não tiver ninguém com a tag `Player`, o `Andar` monta o mesmo jogador da cena
top-down (`BootstrapTopDown.CriarJogador`), e também uma HUD com a vida.

As salas são as da pasta `Sala/` (`Sala.Criar`): cada casa do andar vira uma `Sala`
com porta só nos lados que têm vizinha. Salas comuns ganham de 2 a 4 inimigos e a do
chefe ganha o chefe (veja `TUTORIAL-CHEFE.md`). A sala inicial e a do item ficam vazias.
Ao entrar numa sala com inimigo vivo as portas trancam, e abrem quando o último morre.

## Como o andar é sorteado

`GeradorDeAndar` parte do algoritmo do Isaac e escolhe o melhor de vários andares:

1. A sala inicial fica no meio da grade (9 × 8 casas).
2. Cada sala tenta abrir uma vizinha em cada direção, **com porta entre as duas**. A
   vizinha é recusada se a casa já estiver ocupada, se o andar já tiver salas suficientes,
   ou numa moeda de 50%. Casa encostada em mais de uma sala só passa de vez em quando (com
   parede para as outras) e nunca forma um bloco 2 × 2.
3. No meio do crescimento entram uma ou duas **salas-ponte**: numa casa vazia entre duas
   salas que estavam longe pelo caminho, com porta para as duas. Isso fecha um circuito e
   dá para dar a volta em vez de voltar pelas mesmas salas (mais comum do andar 3 em diante).
4. Andar sem o número de salas ou sem becos (salas com uma porta só) para as salas
   especiais é jogado fora. Dos que passam, fica o de melhor **nota**: caminho comprido até
   o chefe (com teto), bastante beco para explorar, formato espalhado, pouco corredor reto
   repetido e um ou outro circuito.
5. O **chefe** vai no beco mais longe da sala inicial. Item, loja, desafio e amaldiçoada
   vão nos outros becos, sempre o mais longe possível das especiais já postas. Beco que
   sobra vira **sala de recompensa**: prêmio garantido ao limpar. A **secreta** vai numa
   casa vazia colada em várias salas (nunca na do chefe).
6. Cada sala comum ganha um **desenho** de pedras e espinhos (`Sala/DisposicoesDaSala.cs`,
   25 desenhos, cada um podendo vir espelhado), tirado de um saco embaralhado: só repete
   depois de usar todos, e nunca igual ao de uma vizinha. Umas poucas salas ficam sem
   obstáculo (nunca duas coladas) e ganham mais enfeites.
7. As salas logo depois da inicial vêm com um inimigo a menos, e as perto do chefe com um a
   mais (`Andar ▸ Dosar Pela Distancia`).

Toda sala nasce ligada por porta à sala que a criou, então sempre existe caminho até o
chefe e até cada sala especial. Número de salas: 8 ou 9 no primeiro andar, cerca de 3 a
mais por andar, até 20.

O gerador não depende da Unity, então dá para testar fora do editor.

### Desenhos de sala

Em `DisposicoesDaSala` um desenho pode ser um quarto da sala (espelhado nos outros três) ou
a sala inteira em 7 linhas de 13 letras (`P` pedra, `E` espinhos, `.` livre). Quando o jogo
abre, cada desenho é conferido: as quatro portas, o centro e todo ladrilho livre precisam se
ligar sem pisar em pedra nem espinho. Desenho que tranca caminho ou deixa um canto fechado
fica de fora, com um aviso no Console.

### Parâmetros do andar

`ParametrosDoAndar.Padrao(numeroDoAndar)` dá os números de cada andar (salas, grade, salas
especiais, circuitos). Para escalar o andar sem mexer no gerador, assine
`Andar.AoPrepararGeracao`, que recebe os parâmetros logo antes de cada sorteio:

```csharp
Andar.Atual.AoPrepararGeracao += p => { p.SalasMinimas += 2; p.SalasMaximas += 2; p.Lacos = 2; };
```

## O que se vê

| | No mundo | No minimapa |
|---|---|---|
| Sala onde você está | — | branca, com moldura dourada piscando |
| Sala visitada | — | cinza |
| Do outro lado da porta de uma visitada | — | cinza escuro |
| Porta entre duas salas | vão na parede | tracinho entre os quadrados (sem tracinho = parede) |
| Sala do item | chão amarelado, batentes dourados na porta | taça dourada |
| Sala do chefe | chão avermelhado, batentes vermelhos na porta | caveira |
| Sala do tesouro (item) | pedestal entre dois candelabros; às vezes dois pedestais: pegou um, o outro some | taça dourada |
| Loja | chão esverdeado, estandartes vermelhos na porta | moeda |
| Sala de desafio | chão alaranjado, troféus vermelhos na porta. Pegar o item fecha as portas e chama ondas de inimigos (2 no andar 1, 3 depois) | troféu |
| Sala amaldiçoada (andar 2+) | chão vinho, espinhos e ídolos na porta. Cada passagem pela porta tira meio coração; dentro tem item ou baú | ídolo |
| Sala secreta | só abre com bomba | baú (depois de entrar) |

O minimapa mostra só o pedaço descoberto do andar, dentro da moldura dourada do Dragon
Regalia. **Segurando Tab** (ou afundando o analógico esquerdo, L3, no controle) o mapa abre grande no meio da tela, com o
nome do andar e a legenda das salas que já apareceram.

## Onde mexer

| Quero mudar | Onde |
|---|---|
| Número do andar, semente, tamanho da grade | Componente `Andar ▸ Geracao` |
| Repetir sempre o mesmo andar | `Andar ▸ Semente` diferente de 0 (o Console mostra a semente de cada Play) |
| Quantos inimigos por sala | `Andar ▸ Inimigos` |
| Cores das salas especiais | `Andar ▸ Cores` |
| Ondas e inimigos da sala de desafio | `Andar ▸ Sala de desafio` |
| Chance de duas opções na sala do tesouro | `Andar ▸ Itens e coletaveis ▸ Chance De Duas Opcoes` |
| Paredes, portas e inimigos em si | Pasta `Sala/` (`Sala`, `Porta`, `FabricaDeInimigos`) |
| Tamanho, cores e tecla do mapa grande | Componente `Minimapa` |
| Desenhos de pedra e espinho das salas | `Sala/DisposicoesDaSala.cs` |
| Salas, circuitos e salas especiais por andar | `ParametrosDoAndar.Padrao` ou `Andar.AoPrepararGeracao` |

## Ganchos para as outras partes

- `Andar.Atual.AoEntrarNaSala` avisa quando o jogador entra numa `Sala`. Trancar e
  destrancar as portas é a própria sala que faz; use `sala.AoLimpar` para soltar prêmio.
- A troca de sala escuta `Porta.AoAtravessar` e põe o jogador em `PontoDeChegada` da
  porta oposta da sala vizinha.
- `Andar.Atual.ProximoAndar()` gera o andar seguinte (para o alçapão depois do chefe).
- `Andar.Atual.Mapa` tem a grade inteira; `SalaAtual` é onde o jogador está.
  `Mapa.TemPorta(sala, lado)` diz se há porta (duas salas coladas podem ter parede entre
  elas), `Mapa.CaminhoAteOChefe` tem as salas do caminho mais curto e cada `SalaDoAndar`
  traz `Distancia`, `Profundidade` (0 no início, 1 no chefe) e `Recompensa`.
- `Mapa.RevelarTudo()` e `Mapa.RevelarEspeciais()` mostram o andar no minimapa (para itens
  tipo mapa e bússola).
- `Andar.Atual.AoPrepararGeracao` muda os parâmetros de cada andar antes do sorteio.
