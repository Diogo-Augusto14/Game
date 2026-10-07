# Arquitetura do jogo

Como as peças do jogo novo se encaixam. A ideia e as etapas estão no `PLANO.md`; o `MANUAL.md` continua
sendo o caderno de estudo das peças da Unity.

## Dar Play

A cena do jogo é `Assets/Cenas/Jogo.unity`. O Play do editor sempre começa por ela, qualquer que seja a
cena aberta (menu **Jogo ▸ Play sempre pela cena do jogo**, marcado = ligado).

A cena é feita de objetos de verdade, editáveis no Inspector: o **Jogador** (um prefab, em
`Assets/Prefabs/Jogador.prefab`), a **Main Camera**, a **Luz global**, o **Andar** (que cava a caverna
ao dar Play e espalha os Bruxos, um prefab) e o **Boneco de treino** (prefab), na clareira do começo. O menu
**Jogo ▸ Montar a cena do zero** (`Assets/Editor/MontarJogo.cs`) refaz tudo como no começo: o que foi
mudado à mão na cena e no prefab se perde.

| Teclado e mouse | Controle | Ação |
|---|---|---|
| `W A S D` ou setas | analógico esquerdo ou cruz | Andar |
| Mouse | analógico direito (solto: mira pra onde anda) | Mirar |
| Botão esquerdo (segurar) | `RT` ou `RB` | Atirar |
| `Espaço`, `Shift` ou botão direito | `LT`, `LB` ou `A` | Esquiva |

## Pastas

| Pasta | O que tem |
|---|---|
| `Assets/Scripts/Jogador` | Controles, movimento e esquiva, animação, rastro da esquiva, cursor de mira, morte |
| `Assets/Scripts/Armas` | Os dados de uma arma (asset), quem atira e o tiro em voo |
| `Assets/Scripts/Combate` | Vida e dano (a mesma peça pra todo mundo) e o piscar branco |
| `Assets/Scripts/Inimigos` | O inimigo que anda e atira, a animação e a morte dele, o boneco de treino |
| `Assets/Scripts/Andar` | A caverna: cavar, construir as paredes, espalhar os inimigos, a saída e a troca de andar |
| `Assets/Scripts/Mundo` | A câmera |
| `Assets/Scripts/Nucleo` | Peças pequenas usadas por todo lado (cortar folha de animação, efeito que toca uma vez) |
| `Assets/Shaders` | `Silhueta`: pinta o desenho de uma cor só (o branco do golpe) |
| `Assets/Editor` | Montar a cena e o Play pela cena do jogo |
| `Assets/Prefabs` | Jogador, Bruxo e Boneco de treino |
| `Assets/Dados/Armas` | Os assets das armas (`ArcoDoArqueiro.asset`, `MagiaDoBruxo.asset`) |
| `Assets/Arte/Gerada` | Imagens feitas pelo montador: a mira do cursor, a sombra, a bala inimiga e o boneco de treino |
| `Assets/Arte/OldPrison` | A arte da caverna, do pacote Old Prison: chão, paredes, abismo, sangue e enfeites (32 × 32 por ladrilho) |
| `Assets/Arte/Resources` | A arte dos outros pacotes (provisória) |
| `Ferramentas/OldPrison` | `importar.py`: tira do pacote Old Prison as folhas e as regras do Tiled que a caverna usa |

## As peças do jogador

Tudo no objeto **Jogador**, cada peça com um trabalho só:

| Script | O que faz |
|---|---|
| `ControlesDoJogador` | Lê teclado, mouse e controle pelo Input System. O resto do jogo pergunta pra ele (`Movimento`, `Mira`, `Atirando`, `ConsumirEsquiva`) e nunca lê tecla direto |
| `MovimentoDoJogador` | Anda com aceleração e freio; a esquiva é uma arrancada curta na direção do andar, sem tomar dano no começo (`Invulneravel`), com recarga curta |
| `AnimacaoDoJogador` | Parado, andando, atirando e rolando na esquiva. O arco puxa e solta a cada tiro, parado ou andando (andando, o corpo dá um pulinho de 1 pixel a cada passo, porque o desenho do pacote não tem "andar atirando"). O corpo sempre olha pro lado da mira |
| `RastroDaEsquiva` | As cópias azuladas que ficam pra trás na esquiva |
| `ArmaDoJogador` | Atira com a arma (`DadosDaArma`) pra onde mira, no ritmo dela; não atira no meio da esquiva |
| `CursorDaMira` | Troca o cursor por uma mira enquanto o mouse mira; com o controle, o cursor some |
| `Vida` | 6 de vida; depois de um golpe, 1 segundo sem tomar outro. A esquiva também protege (o `MovimentoDoJogador` é um `IInvulneravel`) |
| `PiscarAoTomarDano` | Fica branco no golpe e pisca durante o segundo sem dano |
| `MorteDoJogador` | Vida acabou: desliga os controles, o corpo cai, câmera lenta, a tela escurece e a partida recomeça |

Fora do jogador: `Projetil` (o tiro em voo: vai até o alcance e some, ou some ao bater em algo sólido;
parede ele procura olhando o caminho da frente a cada passo, então não atravessa parede nenhuma) e
`CameraDoJogo` (segue o jogador e olha um pouco pra frente, na direção da mira; `Tremer` dá o tremor,
`Pular` leva a câmera direto pro jogador quando ele troca de andar).

## O andar: uma caverna gigante

Cada andar é uma caverna grande e aberta, sem salas nem portas, como no Nuclear Throne. Tudo fica no
objeto **Andar** da cena (`GeradorDoAndar`), com os números no Inspector.

| Script | O que faz |
|---|---|
| `Caverna` | Cava a planta: "andarilhos" saem do centro, andam, viram, se dividem e às vezes abrem uma galeria larga, até a caverna ter o tamanho pedido. Sai tudo ligado. O começo é sempre uma clareira. Depois ajeita a planta pras paredes do Old Prison e escolhe os buracos e as poças |
| `Pedreiro` | Transforma a planta em mundo com a arte do Old Prison, em Tilemaps: abismo, chão, sangue, enfeites e paredes, mais os colisores das paredes (camada `Wall`) e dos buracos (camada `Buraco`), só onde encostam no chão |
| `Automapa` | As regras de encaixe do Tiled ("automapping"): pega os ladrilhos escolhidos pelos cantos e põe as faces de tijolo, os encontros e as variações, igual o Tiled faz |
| `DadosDoOldPrison` | As tabelas de cantos e as regras do pacote, já convertidas (gerado pela ferramenta; não editar à mão) |
| `GeradorDoAndar` | Monta o andar, espalha os inimigos em grupos longe do começo, conta quantos faltam, abre a saída quando o último morre e troca de andar. Na tela: o nome do andar, o contador e, quando sobram 3 ou menos, uma seta na beirada apontando pro mais perto |
| `Saida` | O vórtice: abre onde morreu o último inimigo e, pisado, leva pro próximo andar |

Cada célula da caverna tem 1 unidade e a célula (x, y) fica no ponto (x, y) do mundo; o jogador começa
no (0, 0). O primeiro andar tem umas 1500 células de chão (umas 7 telas cheias de chão) e 24 inimigos; cada andar
seguinte tem 500 células e 8 inimigos a mais. São 3 andares: depois do último, a partida acaba em
vitória e recomeça.

### A arte do Old Prison e as regras do Tiled

O pacote Old Prison vem com arquivos do **Tiled Map Editor**: as folhas de ladrilhos, a tabela de
**cantos** de cada folha (qual ladrilho vai em cada combinação dos 4 cantos: dentro ou fora da parede)
e as **regras de encaixe** (automapping), que põem embaixo de cada beirada a face de tijolo de 2 de
altura e sorteiam variações (tijolo rachado, musgo). O jogo faz o mesmo na hora, a cada andar:

1. a `Caverna` cava e **ajeita** a planta: nada de parede com menos de 3 de altura (o topo mais a face
   de 2 não cabem) nem de encontro só pela diagonal (o pacote não tem desenho pra ele);
2. o `Pedreiro` escolhe cada ladrilho pelos cantos: o **topo das paredes** são as células de parede com
   mais 2 de parede embaixo, porque essas 2 de baixo ficam pra face de tijolo; o **chão** é tudo que não
   é buraco; as **poças** são manchas no chão;
3. o `Automapa` aplica as regras do pacote (as mesmas do Tiled; conferi que, aplicadas no mapa de
   exemplo do pacote, saem iguais ao que o artista fez).

Cada ladrilho tem 1 unidade (32 pixels do pacote) e fica **entre** as células: os cantos dele são os
centros de 4 células. Os **buracos** são retângulos de abismo nas partes abertas, longe do começo, com
chão em volta: seguram quem anda, mas o tiro passa por cima (e o inimigo enxerga por cima). As
**poças de sangue** e os ossos, pedrinhas e papéis no chão são só enfeite.

As paredes ficam atrás de quem anda: quem encosta na parede de cima fica na frente dos tijolos.

Pra trazer o pacote de novo (outra versão, outra cor), rodar `Ferramentas/OldPrison/importar.py` com a
pasta do pacote descompactado: ele refaz as folhas em `Assets/Arte/OldPrison` e o `DadosDoOldPrison.cs`.

## Vida e dano

Uma peça só, a `Vida`, serve pro jogador, pros inimigos e pro boneco. Cada uma diz de que **lado** está
(`Jogador` ou `Inimigos`), quanta vida tem, se é **imortal** (o boneco), quanto tempo fica **sem tomar
dano** depois de um golpe e quanto o **empurrão** mexe nela, mais os sons e o tremor da câmera.

O tiro (`Projetil`) entrega um `Dano` (quanto tira, pra onde empurra, quem mandou) pra `Vida` de quem
ele tocou. Se não pegou, o tiro **atravessa** em vez de sumir:

- quem é do **mesmo lado** de quem atirou (bala de inimigo passa pelos outros inimigos e pelo boneco);
- quem está **protegido**: na esquiva, no tempinho depois de um golpe, ou já morto.

O dano e o empurrão saem da arma (`dano` e `empurrao` no asset). O empurrão só soma na velocidade do
corpo: quem anda (jogador ou inimigo) freia de volta sozinho, e por isso é só um "empurrãozinho".

Quem quer reagir escuta a `Vida`: `AoTomarDano` (piscar, balançar o boneco) e `AoMorrer` (a morte do
jogador, a do inimigo, a animação de morte).

## Inimigos

O **Bruxo** (`Assets/Prefabs/Bruxo.prefab`) é o primeiro: anda até o jogador e, a uns 8 de distância,
para, prepara o tiro (a animação do ataque é o aviso) e solta uma bala lenta na direção dele.

| Script | O que faz |
|---|---|
| `InimigoAtirador` | Anda até o jogador (para a 4 de distância), prepara meio segundo e atira com a arma dele (`MagiaDoBruxo.asset`), a cada uns 2 segundos |
| `AnimacaoDoInimigo` | Parado, andando, atacando e morrendo, olhando pro jogador. O quadro do disparo chega bem quando a bala sai |
| `Vida` | 15 de vida (5 flechas), lado `Inimigos` |
| `PiscarAoTomarDano` | Fica branco a cada flechada |
| `MorteDoInimigo` | Para, deixa de bater nas coisas, toca a morte e some numa nuvem de poeira |

O corpo é um Rigidbody2D Dynamic, como o do jogador: inimigo não atravessa o jogador nem outro inimigo.

O Bruxo começa **dormindo**: parado até ver o jogador (a 10 de distância, sem parede no meio) ou levar
um tiro. Acordado, não esquece mais; vai reto na direção do jogador (ainda não sabe contornar parede) e
só atira com o caminho livre.

O **Boneco de treino** (`Assets/Prefabs/BonecoDeTreino.prefab`), na clareira onde o jogador começa, serve
pra testar as armas: a vida dele é imortal, ele pisca e balança na estaca pro lado do golpe
(`BonecoDeTreino`), e nunca sai do lugar.

## Criar um inimigo que atira

Duplicar o prefab do Bruxo, trocar as folhas na `AnimacaoDoInimigo` (e o **quadro do disparo**), mexer
nos números do `InimigoAtirador` e da `Vida`, e criar uma arma nova pra ele (**Create ▸ Jogo ▸ Arma**)
com o desenho da bala. Pra ele aparecer, pôr o prefab na lista **Inimigos** do objeto Andar (os inimigos
de cada grupo são sorteados dessa lista).

## Tamanho das coisas

Todo desenho usa **20 pixels por unidade**: o boneco, que tem uns 20 pixels de altura, fica com 1
unidade, e a flecha, o chão e o resto ficam na mesma proporção. Desenho novo segue a mesma regra.

Quem está mais embaixo na tela é desenhado na frente (o `Renderer2D` ordena pelo eixo Y), então o
jogador passa na frente ou atrás do boneco e dos inimigos conforme a altura.

## Trocar o boneco

Na `AnimacaoDoJogador` do Jogador: arrastar as folhas novas (`parado`, `andando`, `ataque`), acertar o
**tamanho do quadro** (os do pacote são 100 × 100) e o **quadro do disparo** (o quadro do ataque em que o
tiro sai). Cada folha é uma tira com os quadros lado a lado, todos do mesmo tamanho, com o boneco no meio
(`Nucleo/FolhaDeSprites.cs` corta na hora; não precisa fatiar no Sprite Editor).

## Criar uma arma

**Create ▸ Jogo ▸ Arma** numa pasta do projeto (o costume é `Assets/Dados/Armas`), preencher no
Inspector (desenho do tiro, ritmo, velocidade, alcance, dano, quantos tiros por disparo, abertura,
dispersão, tremor, som) e arrastar o asset na `ArmaDoJogador`. Dá pra mexer nos números com o jogo
rodando.
