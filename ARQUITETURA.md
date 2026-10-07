# Arquitetura do jogo

Como as peças do jogo novo se encaixam. A ideia e as etapas estão no `PLANO.md`; o `MANUAL.md` continua
sendo o caderno de estudo das peças da Unity.

## Dar Play

A cena do jogo é `Assets/Cenas/Jogo.unity`. O Play do editor sempre começa por ela, qualquer que seja a
cena aberta (menu **Jogo ▸ Play sempre pela cena do jogo**, marcado = ligado).

A cena é feita de objetos de verdade, editáveis no Inspector: o **Jogador** (um prefab, em
`Assets/Prefabs/Jogador.prefab`), a **Main Camera**, a **Luz global** e o **Chão infinito**. O menu
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
| `Assets/Scripts/Jogador` | Controles, movimento e esquiva, animação, rastro da esquiva, cursor de mira |
| `Assets/Scripts/Armas` | Os dados de uma arma (asset), quem atira e o tiro em voo |
| `Assets/Scripts/Mundo` | Câmera e chão infinito |
| `Assets/Scripts/Nucleo` | Peças pequenas usadas por todo lado (cortar folha de animação) |
| `Assets/Editor` | Montar a cena e o Play pela cena do jogo |
| `Assets/Dados/Armas` | Os assets das armas (`ArcoDoArqueiro.asset`) |
| `Assets/Arte/Gerada` | Imagens feitas pelo montador: a mira do cursor e a sombra |
| `Assets/Arte/Resources` | A arte dos pacotes (provisória) |

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

Fora do jogador: `Projetil` (o tiro em voo: vai até o alcance e some, ou some ao bater em algo sólido),
`CameraDoJogo` (segue o jogador e olha um pouco pra frente, na direção da mira; `Tremer` dá o tremor) e
`ChaoInfinito` (um ladrilho repetido que sempre cobre a tela).

## Tamanho das coisas

Todo desenho usa **20 pixels por unidade**: o boneco, que tem uns 20 pixels de altura, fica com 1
unidade, e a flecha, o chão e o resto ficam na mesma proporção. Desenho novo segue a mesma regra.

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
