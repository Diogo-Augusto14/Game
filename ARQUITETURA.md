# Arquitetura do jogo

Como as peças do jogo novo se encaixam. A ideia e as etapas estão no `PLANO.md`; o `MANUAL.md` continua
sendo o caderno de estudo das peças da Unity.

## Dar Play

A cena do jogo é `Assets/Cenas/Jogo.unity`. O Play do editor sempre começa por ela, qualquer que seja a
cena aberta (menu **Jogo ▸ Play sempre pela cena do jogo**, marcado = ligado).

A cena é feita de objetos de verdade, editáveis no Inspector: o **Jogador** (um prefab, em
`Assets/Prefabs/Jogador.prefab`), a **Main Camera**, a **Luz global**, o **Andar** (que cava a caverna
ao dar Play e espalha os Bruxos, um prefab) e o **Boneco de treino** (prefab), na clareira do começo. Também tem a **Interface** (o HUD e os menus). O menu
**Jogo ▸ Montar a cena do zero** (`Assets/Editor/MontarJogo.cs`) refaz tudo como no começo: o que foi
mudado à mão na cena e no prefab se perde.

| Teclado e mouse | Controle | Ação |
|---|---|---|
| `W A S D` ou setas | analógico esquerdo ou cruz | Andar |
| Mouse | analógico direito (solto: mira pra onde anda) | Mirar |
| Botão esquerdo (segurar) | `RT` ou `RB` | Atirar |
| `Espaço`, `Shift` ou botão direito | `LT`, `LB` ou `A` | Esquiva |
| `Esc` ou `P` | `Start` | Pausar |
| `M` / `N` | `LB` / `RB` (nos menus) | Música / efeitos liga e desliga |
| `Q` ou a roda do mouse | `Y` | Trocar de arma |
| `E` | `B` | Pegar arma, abrir baú |

## Pastas

| Pasta | O que tem |
|---|---|
| `Assets/Scripts/Jogador` | Controles, movimento e esquiva, animação, rastro da esquiva, cursor de mira, morte, os heróis e a habilidade deles |
| `Assets/Scripts/Armas` | Os dados de uma arma (asset), a arma com a munição dela, quem atira, o tiro em voo, a arma na mão e no chão, o baú, a caixa de munição e a munição na tela |
| `Assets/Scripts/Combate` | Vida e dano (a mesma peça pra todo mundo), o piscar branco e o impacto (hitstop e números de dano) |
| `Assets/Scripts/Inimigos` | O inimigo que anda e atira, a animação e a morte dele, os jeitos extras (sumir, escudo, dividir, invocar), o contorno claro, os chefes e o boneco de treino |
| `Assets/Scripts/Progresso` | Heróis liberados, estatísticas, conquistas, bestiário, a tela de progresso e a partida salva |
| `Assets/Scripts/Andar` | A caverna: cavar, construir as paredes, espalhar os inimigos, a saída e a troca de andar |
| `Assets/Scripts/Mundo` | A câmera |
| `Assets/Scripts/Interface` | O HUD, o menu inicial, a pausa, os controles e o fim da partida |
| `Assets/Scripts/Nucleo` | Peças pequenas usadas por todo lado (cortar folha de animação, efeito que toca uma vez) |
| `Assets/Shaders` | `Silhueta`: pinta o desenho de uma cor só (o branco do golpe) |
| `Assets/Editor` | Montar a cena e o Play pela cena do jogo |
| `Assets/Prefabs` | Jogador, os 31 inimigos, os 7 chefes e o Boneco de treino |
| `Assets/Dados/Resources/ArmasDosHerois` | A arma do começo de cada herói (o arco do Arqueiro, a espada do Cavaleiro...) |
| `Assets/Dados/Armas` | Os assets das armas: as do personagem e do Bruxo (`ArcoDoArqueiro`, `MagiaDoBruxo`) as achadas (`Varinha`, `Tomo`, `BestaDeRepeticao`, `Cajado`, `Machado`) e as dos inimigos (`FlechaDoEsqueleto`, `AnelDoNecromante`, `EspiralDoOlho`, `GotasDaGosma`) |
| `Assets/Arte/Armas` | Os desenhos das armas achadas, dos tiros (do jogador e dos inimigos) e da bolsa de munição (feitos por `Ferramentas/Armas/desenhar.py`) |
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
| `ArmaDoJogador` | As duas armas: começa só com a do personagem (o arco, infinito). Troca entre elas, atira pra onde mira no ritmo da arma e gasta a munição; sem munição, só o clique. Pegar outra arma ocupa a mão vazia ou troca a da mão (o arco também). Não atira no meio da esquiva |
| `ArmaNaMao` | Desenha a arma achada na mão, girando pra mira, com o coice do tiro (com o arco não aparece nada: ele já está no desenho do Arqueiro) |
| `InteracaoDoJogador` | Acha a coisa usável mais perto (arma no chão, baú) e usa no `E` (a dica em cima dela é da interface) |
| `CursorDaMira` | Troca o cursor por uma mira enquanto o mouse mira; com o controle, o cursor some |
| `Vida` | 6 de vida; depois de um golpe, 1 segundo sem tomar outro. A esquiva também protege (o `MovimentoDoJogador` é um `IInvulneravel`) |
| `PiscarAoTomarDano` | Fica branco no golpe e pisca durante o segundo sem dano |
| `MorteDoJogador` | Vida acabou: desliga os controles, o corpo cai e abre a `TelaDeFimDeJogo` (câmera lenta, escurece, resumo) |
| `HabilidadeDoHeroi` | A habilidade do herói (F ou o X do controle) e a recarga dela; posta pelo `Herois.Aplicar` |

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
| `Arena` | A planta do andar do chefe: um salão oval com quatro pilares, no mesmo formato da caverna (o `Pedreiro` constrói igual) |
| `MapaDeCaminhos` | O caminho de qualquer ponto da caverna até o jogador, contornando paredes, buracos e baús (os inimigos usam quando não dá pra ir reto) |

Cada célula da caverna tem 1 unidade e a célula (x, y) fica no ponto (x, y) do mundo; o jogador começa
no (0, 0). A lista **Andares** do objeto Andar diz a ordem da partida; hoje são 6:

| Andar | O que é |
|---|---|
| 1 e 2 | Cavernas |
| 3 | **Covil do Minotauro** (o chefe) |
| 4 e 5 | Cavernas |
| 6 | **Coração da Prisão** (o Golem, o chefe final) |

A primeira caverna tem umas 1500 células de chão (umas 7 telas cheias de chão) e 24 inimigos; cada
caverna seguinte tem 400 células e 6 inimigos a mais. Depois do último andar, a partida acaba em
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

## A interface

Duas partes:

- **O HUD** (objeto **Interface** da cena, `TelaDoJogo` + `HudDoJogo`): vida em corações, andar e
  inimigos, a arma na moldura com a munição, a barra do chefe, o nome do andar e a dica do que
  dá pra usar. Desenhado no OnGUI em pixel art de tamanho inteiro (`Desenho`). Some com qualquer menu
  aberto (o OnGUI desenha por cima dos menus).
- **Os menus, copiados do jogo antigo** (`Assets/Scripts/Interface`), montados por código em canvas
  (uGUI), com a arte do Dragon Regalia (faixas, molduras, botões de losango, ponteiro, setas), as
  fontes Jersey 15 e Jacquard 12, os desenhos das teclas e dos botões do controle nas dicas (trocam
  sozinhos entre teclado e controle, Xbox ou PlayStation), a transição escura entre telas e a música.

| Tela | O que tem |
|---|---|
| `TelaDeInicio` | Título com a faixa rosa, o herói (retrato animado, descrição, números; as setas aparecem quando tiver mais de um), Jogar, Configurações e Sair do jogo, os controles com o desenho das teclas, os créditos e a versão. Só na primeira vez: "tentar de novo" vai direto pro jogo |
| `TelaDeOpcoes` | Volume da música e dos efeitos (barras), tela cheia, resolução e tremor da tela, tudo salvo (`Opcoes`) |
| `TelaDePausa` | `Esc`, `P` ou `Start`: o andar, as armas, Continuar, Reiniciar partida, Configurações, Menu principal e Sair do jogo |
| `TelaDeFimDeJogo` | Morreu (clarão vermelho) ou venceu (dourado): câmera lenta, o retrato do herói caindo ou parado, o resumo (herói, andar, tempo, inimigos e chefes derrotados, armas) e Tentar de novo, Menu principal, Sair do jogo |
| `TelaDeNovidades` | Na primeira vez que abre uma versão nova, as novidades dela (as notas do lançador) |

| Peça | O que faz |
|---|---|
| `TelaSimples`, `MenuDeBotoes` | Montar canvas, textos, painéis e a coluna de botões animados (teclado, controle e mouse) |
| `TransicaoDeTela` | Escurece, troca (recarregar a cena, voltar ao menu) e clareia |
| `ArteDaInterface`, `FonteDoJogo`, `IconeDeTecla`, `DicaDupla` | A arte e as fontes lidas de `Resources`, e as dicas com o desenho das teclas |
| `Controle` | O controle nos menus (qual foi mexido por último, Xbox ou PlayStation) |
| `Sons`, `Musica` | Os sons dos menus e a música: menu, uma por caverna, chefe, chefe final, vitória e fim de jogo. `M` liga e desliga a música, `N` os efeitos |
| `ResumoDaPartida`, `VersaoDoJogo`, `Herois` | Os números da partida, a versão instalada e os heróis da escolha (por enquanto só o Arqueiro) |

O volume dos efeitos é o do `AudioListener` (vale pra todos os sons do jogo); a música fica de fora e
tem o volume dela. Do jogo antigo **ficaram de fora** o "Continuar" (partida salva), a tela de
"Progresso" (conquistas e bestiário) e os "números de dano": dependem de sistemas que o jogo novo ainda
não tem.

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

São 31, quase todos vindos do jogo antigo (a arte é a do Tiny RPG), separados por mundo: cada um diz na
lista **Inimigos** do objeto Andar o primeiro e o último andar em que aparece e o **peso** do sorteio.

| Mundo | Inimigos |
|---|---|
| 1 (andares 1 e 2) | Esqueleto (investida), Gosma (estoura em gotas), Bruxo (some e reaparece, leque de 3), Esqueleto Arqueiro, Geleia (pula e espirra gotas), Morceguinho (voa aos trancos), Orc (machadadas), Bolha (divide em duas), Cão Infernal (bote) |
| 2 (4 e 5) | Morcego (rodeia e mergulha), Lobisomem (rodeia, duas disparadas), Orc e Esqueleto Blindados (armadura: metade do dano), Necromante (anéis, levanta esqueletos), Cavaleiro da Lança (investida longa), Urso (não é empurrado, anel de pedras), Orc Montado (galope duplo) |
| 3 (7 e 8) | Demônio (corte à frente), Demônia (rodeia, leque), Demônia da Foice (anel de cortes), Fogo-Fátuo (voa, some, chamas em cruz), Monstro de Sangue (anel de sangue), Demônio do Tridente (arremesso), Demônio Arqueiro (flechas de fogo), Olho (espiral), Demônio das Lâminas (3 arrancadas), Orc de Elite (fúria), Esqueleto do Espadão (onda de corte), Cavaleiro do Escudo (bloqueia de frente), Cavaleiro Canhão (parado, rajada) |

Os do mundo 2 e 3 continuam aparecendo no 4. Todo inimigo ganha um **contorno claro** de 1 pixel
(`ContornoClaro`), pra não sumir no chão escuro.

| Script | O que faz |
|---|---|
| `InimigoAtirador` | O jeito de todos: anda até o jogador (ou recua, se ele chega perto demais), prepara o ataque e ataca. O ataque é a arma (o padrão dela), a investida ou os dois (corre e atira no fim). Ainda: `jeito` (reto, rodeando ou aos trancos), `voa` (passa por cima dos buracos), investidas seguidas e fúria com pouca vida |
| `DanoAoEncostar` | Machuca e empurra quem é do outro lado e encosta |
| `TiroAoMorrer` | Ao morrer, solta um disparo da arma dele (as gotas da Gosma) |
| `SomeEAparece` | De vez em quando desbota e aparece em outro lugar perto do jogador (Bruxo, Fogo-Fátuo) |
| `EscudoFrontal` | Segura o golpe que vem de frente (abre quando ele ataca); é um `IBloqueioDeDano` da `Vida` |
| `DivideAoMorrer` | Ao morrer vira pedaços menores (a Bolha vira duas Bolinhas) |
| `Invocador` | Levanta ajudantes do chão (o Necromante, até 2 esqueletos vivos); eles saem crescendo (`SaindoDoChao`) |
| `ContornoClaro` | O contorno claro (quatro máscaras deslocadas 1 pixel recortam um retângulo claro atrás do desenho) |
| `AnimacaoDoInimigo` | Parado, andando, atacando e morrendo, olhando pro jogador. O quadro do disparo chega bem quando o ataque sai |
| `Vida` | Lado `Inimigos`; a vida muda de um pra outro, e a `armadura` segura uma parte do dano |
| `PiscarAoTomarDano` | Fica branco a cada golpe |
| `MorteDoInimigo` | Para, deixa de bater nas coisas, toca a morte e some numa nuvem de poeira |

Quem nasce no meio do andar (bolinhas, esqueletos levantados) entra na conta pelo
`GeradorDoAndar.Registrar`. O golpe tem peso (`Impacto`): o número do dano pula do inimigo
(`TextoFlutuante`, desliga nas configurações) e o jogo congela uns centésimos nos golpes fortes, nas
mortes e quando o jogador apanha.

### Padrões de bala

O padrão é da **arma** (o asset), então qualquer inimigo (ou o jogador) pode usar qualquer um:

- **leque**: `tirosPorDisparo` tiros separados por `abertura` graus;
- **anel**: com `anel` ligado, os tiros saem em volta toda;
- **rajada**: `rajada` disparos seguidos, um a cada `intervaloDaRajada`, cada um girado `giroNaRajada`
  graus (um anel em rajada girando vira **espiral**);
- **no voo**: `aceleracao` (o tiro freia ou acelera), `curva` (vai virando) e `giroDoDesenho` (o desenho
  roda, como o machado).

A `Rajada` (`Assets/Scripts/Armas/Rajada.cs`) solta os disparos no tempo certo; o inimigo fica parado
enquanto ela sai.

O corpo é um Rigidbody2D Dynamic, como o do jogador: inimigo não atravessa o jogador nem outro inimigo.

Todo inimigo começa **dormindo**: parado até ver o jogador (a 10 de distância, sem parede no meio) ou levar
um tiro. Acordado, não esquece mais e só atira com o caminho livre. Com o caminho livre, vai reto na
direção do jogador; com parede, buraco ou baú no meio, segue o `MapaDeCaminhos`.

O `MapaDeCaminhos` é um mapa de distâncias: partindo da célula do jogador, cada célula de chão recebe
quantos passos está dele (uma busca que espalha como água, até 40 passos). Pra chegar no jogador, o
inimigo vai pra vizinha com menos passos; na diagonal, só se as duas vizinhas retas forem chão (senão
raspa na quina). O mapa só é refeito quando o jogador muda de célula.

O **Boneco de treino** (`Assets/Prefabs/BonecoDeTreino.prefab`), na clareira onde o jogador começa, serve
pra testar as armas: a vida dele é imortal, ele pisca e balança na estaca pro lado do golpe
(`BonecoDeTreino`), e nunca sai do lugar.

## Chefes

Cada chefe tem um **andar só dele**: um salão (a `Arena`) sem caverna e sem inimigos comuns, com quatro
pilares pra se esconder dos tiros. O jogador entra por baixo, o chefe espera no meio de cima. Depois
de uma apresentação de 2 segundos (aí o nome e a barra de vida aparecem no alto da tela), ele alterna os
ataques sem repetir o mesmo duas vezes seguidas, andando um pouco entre eles. Com metade da vida entra
em **fúria**: fica avermelhado, mais rápido, e ganha um ataque a mais. Morto, o portal abre e cai um
baú no meio do salão.

A partida tem **12 andares em 4 mundos**: duas cavernas e o chefe, três vezes, e no fim duas cavernas e
o Olho do Abismo. O chefe de cada mundo é sorteado entre dois (`chefe` e `ouEntao` na lista **Andares**);
o nome do andar dele vem do próprio chefe (`lugar`).

| Mundo | Chefes | Ataques |
|---|---|---|
| 1 | **Minotauro** (320) ou **Lobisomem Alfa** (300) | Minotauro: investidas, pisão, cortes. Lobisomem: botes seguidos, garras em dois leques, uivo que chama cães; na fúria, botes com garras |
| 2 | **Senhor da Guerra** (380) ou **Demônio do Martelo** (360) | Senhor da Guerra: machadada (onda de choque), machados bumerangue (vão e voltam), investida, grito que chama orcs. Demônio: pula em você e cai soltando um anel, espiral, cuspe de fogo (marcas no chão que explodem) |
| 3 | **Rei Necromante** (400) ou **Golem de Brasa** (480) | Rei: some e reaparece longe com um anel, ossos que saem das marcas em volta de você, muralha de tiros, levanta esqueletos |
| final | **Olho do Abismo** (650) | Não anda. Anel, rajada, espiral, chama olhos; com 60% da vida: dois anéis, leques e o raio; com 25%: tiros em cruz girando o tempo todo |

Na **virada de fase** (a fúria, `ViradaDeFase`), igual pra todos: ruge, a tela treme, o jogo congela um
instante, os tiros inimigos no ar somem e uma frase aparece em cima dele.

| Script | O que faz |
|---|---|
| `Chefe` | A luta: apresentação, escolha dos ataques, investidas, rajadas e fúria (a barra de vida é da interface). Cada ataque (`AtaqueDoChefe`) diz a arma (o padrão), quantas investidas, o preparo, qual animação toca e se é só da fúria; e ainda se as investidas são pulos (`salto`), se some antes (`sumir`), quem levanta do chão (`invocar`) e quantas marcas no chão atiram (`marcas`). A `armaDoFim` atira sozinha com pouca vida |
| `IAnimavel` | O que a `AnimacaoDoInimigo` precisa saber (pra onde olha, se anda, quando ataca): serve pro inimigo comum e pro chefe |
| `AnimacaoDoInimigo` | Ganhou `outrosAtaques`: mais folhas de ataque, cada uma com o quadro do golpe (o chefe escolhe qual toca) |

Os chefes são os desenhos do Minotauro e do Golem no dobro do tamanho, pesados (o jogador não empurra)
e sem empurrão dos golpes. Encostar neles machuca (`DanoAoEncostar`).

Pra criar outro chefe: duplicar o prefab de um, trocar as folhas e a lista de ataques (cada um com uma
arma de padrão), e pôr o prefab num andar da lista **Andares**.

## Criar um inimigo que atira

Duplicar o prefab de um inimigo parecido, trocar as folhas na `AnimacaoDoInimigo` (e o **quadro do
disparo**), mexer nos números do `InimigoAtirador` e da `Vida`, e criar uma arma nova pra ele
(**Create ▸ Jogo ▸ Arma**) com o desenho da bala e o padrão. Pra um que bate de perto: sem arma, com a
**investida** ligada e um `DanoAoEncostar`. Pra ele aparecer, pôr o prefab na lista **Inimigos** do
objeto Andar, com o primeiro andar e o peso.

## Tamanho das coisas

Todo desenho usa **20 pixels por unidade**: o boneco, que tem uns 20 pixels de altura, fica com 1
unidade, e a flecha, o chão e o resto ficam na mesma proporção. Desenho novo segue a mesma regra.

Quem está mais embaixo na tela é desenhado na frente (o `Renderer2D` ordena pelo eixo Y), então o
jogador passa na frente ou atrás do boneco e dos inimigos conforme a altura.

## Heróis

Nove, os do Tiny RPG (vieram do jogo antigo), na lista de `Jogador/Herois.cs`: vida, velocidade, as
folhas, a arma do começo (em `Dados/Resources/ArmasDosHerois`, infinita e trocável como qualquer arma) e a
habilidade (F ou o X do controle; a barra fica embaixo dos corações).

| Herói | Vida | Arma | Habilidade | Libera |
|---|---|---|---|---|
| Arqueiro | 3 | Arco (flechas rápidas) | Chuva de flechas (3 anéis) | livre |
| Soldado | 4 | Arco do Soldado (mais forte) | Rajada de flechas (4 leques) | vencer o chefe do mundo 1 |
| Lanceiro | 4 | Lança (atravessa) | Investida a cavalo | chefe do mundo 2 |
| Cavaleiro | 5 | Espada (onda de corte curta) | Escudo (3 s sem dano, desmancha tiros) | chefe do mundo 3 |
| Mago | 2,5 | Cajado de Fogo | Meteoro (cai onde mira) | zerar o jogo |
| Espadachim | 3 | Sabre (cortes rápidos) | Redemoinho | zerar com o Cavaleiro |
| Machadeiro | 4 | Machado (o golpe mais forte) | Fúria (dano em dobro por 6 s) | zerar com o Arqueiro |
| Padre | 3 | Cetro (estrelas) | Reza (cura um coração) | zerar com o Mago |
| Templário | 6 | Espada Sagrada | Anel sagrado (dois anéis de cortes) | zerar 3 vezes |

`Herois.Aplicar` põe o herói no jogador (no começo do andar 1 e ao trocar no menu). As armas de espada
e lança **atravessam** (`atravessa`: acertam cada inimigo uma vez e seguem). Com a arma de um herói na
mão, o corpo faz a animação de ataque dele a cada tiro (o quadro do disparo é do herói).

## Progresso e partida salva

Tudo no PlayerPrefs (`Assets/Scripts/Progresso`), como no jogo antigo:

| Script | O que faz |
|---|---|
| `Progresso` | Mundos vencidos, vitórias e com quem zerou: o que libera os heróis (e avisa quando libera) |
| `Registro` | Estatísticas de todas as partidas e o bestiário (visto, derrotados, quem mais te matou) |
| `Conquistas` | 17 conquistas; ganhar mostra o `AvisoDeConquista` no canto |
| `Bestiario` | Nome e "como luta" de cada inimigo e chefe |
| `TelaDeProgresso` | O botão **Progresso** do menu: abas de estatísticas, conquistas e bestiário |
| `Salvamento` | A partida salva: no começo de cada andar e no **Salvar e sair** da pausa (herói, andar, vida, armas, números). **Continuar** no menu volta pro começo do andar salvo. Morrer, vencer ou jogar de novo apaga |

## Criar uma arma

**Create ▸ Jogo ▸ Arma** numa pasta do projeto (o costume é `Assets/Dados/Armas`), preencher no
Inspector (desenho do tiro, ritmo, velocidade, alcance, dano, quantos tiros por disparo, abertura,
dispersão, tremor, som) e arrastar o asset na `ArmaDoJogador`. Dá pra mexer nos números com o jogo
rodando.

Pra ela aparecer na caverna: dar um **desenho na mão** (sem ele a arma não cai em baú nem no chão), a
**munição** (`semPente` e o total em `municaoMaxima`, ou `infinita`; o pente com recarga ainda funciona, mas nenhuma arma usa) e o **peso no baú** (quanto maior, mais
sai; 0 = nunca), e pôr o asset na lista **Armas** do objeto Andar.

## Armas, baús e munição

O jogador carrega **duas armas** e começa só com a do personagem (o arco do Arqueiro, infinito). Pegar
uma arma ocupa a mão vazia; com as duas cheias, troca a que está na mão, **o arco também** (senão ele
fica inútil no fim do jogo): a velha cai no chão com a munição que tinha (o arco largado aparece com
o desenho dele, o `desenhoNoChao`).

A munição é **um número só por arma**: quantos tiros ela ainda tem, sem pente nem recarga. Acabou, só
o clique; a bolsa de munição enche metade. Os números ficam no asset (`DadosDaArma`); o que sobra de
cada arma fica na `ArmaCarregada`, que vai junto quando a arma cai no chão.

| Script | O que faz |
|---|---|
| `ArmaCarregada` | Uma arma de verdade: os dados (o asset) mais a munição dela |
| `Interativo` | O que o jogador usa chegando perto e apertando `E` (a base da arma no chão e do baú) |
| `ArmaNoChao` | Arma caída, flutuando; `E` pega (e larga a que estava na mão no lugar) |
| `Bau` | Fechado na caverna; `E` abre e solta uma arma sorteada pelo peso, diferente das que o jogador tem, e às vezes uma bolsa de munição. Conta como parede: segura tiro, e os inimigos contornam |
| `CaixaDeMunicao` | Encostou, enche metade da munição da arma achada (se ela não estiver cheia) |

No objeto Andar: **3 baús** e **1 arma no chão** por andar, longe do começo e longe uns dos outros (o baú do
lado do começo existe, `bauNoComeco`, mas fica desligado: o jogo começa só com o arco). Cada inimigo morto tem **12%** de chance de largar uma
bolsa de munição.

As armas achadas são de fantasia (não precisam existir, precisam encaixar no mundo):

| Arma | Como atira |
|---|---|
| **Varinha de Faíscas** | Tiro a tiro, faíscas rápidas; 120 tiros |
| **Tomo das Brasas** | 6 brasas abertas que vão freando (de perto); 40 disparos |
| **Besta de Repetição** | Segura o gatilho: virotes sem parar; 240 virotes |
| **Cajado do Trovão** | Um raio lento de sair, forte e de longe; 30 raios |
| **Machado de Arremesso** | Um machado girando por vez, pesado e com empurrão; 40 machados |
 Os desenhos saem do `Ferramentas/Armas/desenhar.py`
(pixel por pixel, em letras, com o contorno feito sozinho).
