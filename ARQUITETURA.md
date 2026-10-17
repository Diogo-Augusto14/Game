# Arquitetura do jogo

Como as peças do jogo novo se encaixam. A ideia e as etapas estão no `PLANO.md`; o `MANUAL.md` continua
sendo o caderno de estudo das peças da Unity.

## Dar Play

A cena do jogo é `Assets/Cenas/Jogo.unity`. O Play do editor sempre começa por ela, qualquer que seja a
cena aberta (menu **Jogo ▸ Play sempre pela cena do jogo**, marcado = ligado).

A cena é feita de objetos de verdade, editáveis no Inspector: o **Jogador** (um prefab, em
`Assets/Prefabs/Jogador.prefab`), a **Main Camera**, a **Luz global**, o **Andar** (que cava a caverna
ao dar Play e espalha os Bruxos, um prefab) e o **Boneco de treino** (prefab), no acampamento do andar 1. Também tem a **Interface** (o HUD e os menus). O menu
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
| `G` | clique do analógico esquerdo | O tiro especial da arma na mão (com recarga) |
| `Tab` | `Select` (Back) | O mapa do andar inteiro (o jogo para); de novo, `Esc` ou `B` fecha |

## Pastas

| Pasta | O que tem |
|---|---|
| `Assets/Scripts/Jogador` | Controles, movimento e esquiva, animação, rastro da esquiva, cursor de mira, morte, os heróis e a habilidade deles |
| `Assets/Scripts/Armas` | Os dados de uma arma (asset), a arma com a munição dela, quem atira, o tiro em voo, a arma na mão e no chão, o baú, a caixa de munição e a munição na tela |
| `Assets/Scripts/Combate` | Vida e dano (a mesma peça pra todo mundo), o piscar branco e o impacto (hitstop e números de dano) |
| `Assets/Scripts/Inimigos` | O inimigo que anda e atira, a animação e a morte dele, os jeitos extras (sumir, escudo, dividir, invocar), os chefes e o boneco de treino |
| `Assets/Scripts/Itens` | Moedas e chaves (a bolsa), os coletáveis, a explosão, os itens passivos e ativos, as sinergias, o pedestal (também da loja e do altar) e o orbe guardião |
| `Assets/Scripts/Cenario` | O que enche a caverna: loja, altar de sangue, emboscada e desafio, área escura, serra no trilho, tronco rolante, espinhos, mesas e barris que quebram, baratas e velas |
| `Assets/Scripts/Progresso` | Heróis liberados, estatísticas, conquistas, bestiário, a tela de progresso e a partida salva |
| `Assets/Scripts/Andar` | A caverna: cavar, construir as paredes, espalhar os inimigos, a sala secreta, a saída, a troca de andar e o caminho (o tema de cada mundo) |
| `Assets/Scripts/Mundo` | A câmera |
| `Assets/Scripts/Interface` | O HUD, o minimapa, o menu inicial, a pausa, os controles e o fim da partida |
| `Assets/Scripts/Nucleo` | Peças pequenas usadas por todo lado (cortar folha de animação, efeito que toca uma vez) |
| `Assets/Shaders` | `Silhueta`: pinta o desenho de uma cor só (o branco do golpe) |
| `Assets/Editor` | Montar a cena e o Play pela cena do jogo. Em `Testes/`, os testes automáticos (Window ▸ General ▸ Test Runner, aba EditMode): toda folha de animação dos prefabs e dos heróis dá pelo menos um quadro, nenhuma imagem passa do tamanho máximo da importação (a Unity encolheria e o corte sairia errado), nenhum desenho atravessa a emenda entre dois quadros e, parado ou andando, nenhum bicho pula de lado (é o que acontece quando o quadro do corte é menor que o desenhado) |
| `Assets/Prefabs` | Jogador, os 31 inimigos, os 7 chefes e o Boneco de treino |
| `Assets/Dados/Resources/ArmasDosHerois` | A arma do começo de cada herói (o arco do Arqueiro, a espada do Cavaleiro...) |
| `Assets/Dados/Armas` | Os assets das armas: as do personagem e do Bruxo (`ArcoDoArqueiro`, `MagiaDoBruxo`) as achadas (`Varinha`, `Tomo`, `BestaDeRepeticao`, `Cajado`, `Machado`) e as dos inimigos (`FlechaDoEsqueleto`, `AnelDoNecromante`, `EspiralDoOlho`, `GotasDaGosma`) |
| `Assets/Arte/Armas` | Os desenhos das armas achadas, dos tiros (do jogador e dos inimigos) e da bolsa de munição (feitos por `Ferramentas/Armas/desenhar.py`) |
| `Assets/Arte/Gerada` | Imagens feitas pelo montador: a mira do cursor, a sombra, a bala inimiga e o boneco de treino |
| `Assets/Arte/OldPrison` | A arte da caverna, do pacote Old Prison: chão, paredes, abismo, sangue e enfeites (32 × 32 por ladrilho) |
| `Assets/Arte/Resources` | A arte dos outros pacotes (provisória) |
| `Ferramentas/OldPrison` | `importar.py`: tira do pacote Old Prison as folhas e as regras do Tiled que a caverna usa |
| `Ferramentas/Areas` | `importar.py`: tira das demos Grass Land e Ancient Ruins a arte das áreas seguras (`Resources/Areas`: grama, piso, árvores, arbustos, capim, pedras, ruínas, a fonte, o altar, o cálice, o mercador e a criatura da sorte) e do Village (interiors) a mobília (`Resources/Decoracao/Vila`: armários, estantes, prateleiras, barris, caixotes, mesas, tapetes, castiçais, armaduras, quadros, potes que quebram, o baú) |
| `Ferramentas/Temas` | `importar.py`: tira dos pacotes Old Prison, Crypt e The Depths of the Mountain a arte da Cripta e das Profundezas (`DadosDosTemas.cs`) e a decoração (`Resources/Decoracao`: as peças do Old Prison em `Prisao/`, caixões, estátuas, cristais, tochas, velas, a chama mágica, a porta grande de madeira, o lançador de fogo) |
| `Ferramentas/Personagens` | `importar.py`: recorta dos pacotes as folhas dos inimigos e do Rei Esqueleto (em volta do boneco, 32 pixels por unidade) e grava `quadros.json`. `desarmar_herois.py`: tira a arma do desenho dos heróis (parado, andando e morte, em `SemArma/`): no jogo a arma aparece só na mão |
| `Pacotes/` | Os pacotes comprados, descompactados (fora do Git deste repositório público, ver `CLAUDE.md`) |

## As peças do jogador

Tudo no objeto **Jogador**, cada peça com um trabalho só:

| Script | O que faz |
|---|---|
| `ControlesDoJogador` | Lê teclado, mouse e controle pelo Input System. O resto do jogo pergunta pra ele (`Movimento`, `Mira`, `Atirando`, `ConsumirEsquiva`) e nunca lê tecla direto |
| `MovimentoDoJogador` | Anda com aceleração e freio; a esquiva é uma arrancada curta na direção do andar, sem tomar dano no começo (`Invulneravel`), com recarga curta |
| `AnimacaoDoJogador` | Parado, andando, atirando e rolando na esquiva. O arco puxa e solta a cada tiro, parado ou andando (andando, o corpo dá um pulinho de 1 pixel a cada passo, porque o desenho do pacote não tem "andar atirando"). O corpo sempre olha pro lado da mira |
| `RastroDaEsquiva` | As cópias azuladas que ficam pra trás na esquiva |
| `ArmaDoJogador` | As duas armas: começa só com a do personagem (o arco, infinito). Troca entre elas, atira pra onde mira no ritmo da arma e gasta a munição; sem munição, só o clique. Pegar outra arma ocupa a mão vazia ou troca a da mão (o arco também). Não atira no meio da esquiva. O `G` solta o tiro especial da arma (ver **O tiro especial**) |
| `ArmaNaMao` | Desenha a arma da mão, girando pra mira, com o coice do tiro. Vale pra todas, a do herói também: desde a 1.18.0 o boneco do herói não tem a arma desenhada (as folhas `SemArma/`), e o corpo não toca mais a animação de ataque |
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

## O andar: salas que fecham

Cada andar é um punhado de salas ligadas por corredores curtos, como no Enter the Gungeon (desde a
1.12.0; antes era uma caverna grande e aberta, e ficava chato caçar os últimos inimigos). Entrou numa
sala de luta, as grades fecham; só abrem com todos mortos, e cai um prêmio. Limpou a sala do fim, o
portal abre (as salas de luta do lado ficam opcionais, pelos prêmios; limpar o andar inteiro ainda vale
a cura da Carne Assada). Tudo fica no objeto
**Andar** da cena (`GeradorDoAndar`), com os números no Inspector.

| Script | O que faz |
|---|---|
| `PlantaDeSalas` | Monta a planta: a sala do começo no centro, um caminho de salas de luta até a do fim (maior), salas do lado (mais lutas e as especiais: loja, tesouro, altar, desafio) e às vezes um atalho entre duas lutas vizinhas. As salas ficam numa grade (19 × 18 de um meio ao outro); cada uma é um retângulo com cantos cortados, nichos e, nas grandes, pilares. Corredores de 3, e a ponta encostada em cada sala de luta é a porta dela. Às vezes, uma **sala secreta** pendurada numa sala qualquer (menos a do começo): o corredor até ela fica de fora do chão (a `passagemSecreta`): é desenhado como parede, mas é uma parede de ilusão |
| `SalaDeLuta` | Uma sala de luta: os inimigos dormem em grupos (um bicho principal por grupo, às vezes outro no meio) até o jogador entrar (ver pela porta não acorda; levar tiro sim). Entrou, fecha as portas e acorda todo mundo; às vezes vem mais uma onda saindo do chão (a sala do fim sempre tem uma, e duas do 4º andar de salas em diante). Limpou: abre e solta o **prêmio da sala** (`PremioDaSala`, desde a 1.21.0), que já vem sorteado ao montar o andar e aparece boiando em cima das portas, do lado de fora, e no minimapa, como no Hades: nenhum (uma parte das salas), moedas, coração, chave, cristal, item ou planta; a do fim sempre dá (mais) e abre o portal (`PremiosDasSalas`). Baú, desde a 1.20.1, é um só por andar e só metade das vezes (`GeradorDoAndar.EspalharArmas`, `chanceDeBau`): às vezes é o maldito, o amaldiçoado ou um mímico; arma só sai de baú (nada de arma largada no chão) |
| `PortaDaSala` | A porta da sala de luta: o portão de caveira do Old Prison nas quatro paredes (um arco de pedra da largura do corredor, sempre ali; as duas folhas fecham e abrem pra dentro da sala). Em cima e embaixo, de frente (embaixo, todo dentro do corredor, e a câmera vê a caveira, o lado de fora); nos lados, deitado, como as portas do Isaac. Quem está atrás do arco vê através dele. No Porão, o portão recolorido de marrom (`Temas/Porao/PortaoDeCaveira`, feito pelo `Ferramentas/Temas/recolorir_porao.py`). Fechada, é parede (camada `Wall`): segura gente e tiro |
| `Caverna` | As ferramentas da planta: ajeita pras paredes do Old Prison e escolhe os buracos e as poças (o cavar com "andarilhos", da caverna antiga, ficou aí sem uso) |
| `Pedreiro` | Transforma a planta em mundo com a arte do Old Prison, em Tilemaps: abismo, chão, sangue, enfeites e paredes, mais os colisores das paredes (camada `Wall`) e dos buracos (camada `Buraco`), só onde encostam no chão. A passagem secreta é desenhada como parede, mas os colisores são feitos com ela aberta; o `Remendar` redesenha aquele pedaço quando ela se revela |
| `ParedeSecreta` | A parede de ilusão da sala secreta: desenhada igual às outras (só não tem tocha nem móvel na frente), mas sem colisor (o `Pedreiro` faz os colisores com a passagem aberta): quem anda contra ela entra, e o tiro atravessa. Entrou, ela se revela (poeira, o som do segredo) e o `Pedreiro.Remendar` redesenha aquele pedaço como corredor. A sala tem um item de graça, um baú, moedas, uma chave e a mobília do tesouro. Só nos mundos com parede (nas Profundezas as salas flutuam) |
| `Automapa` | As regras de encaixe do Tiled ("automapping"): pega os ladrilhos escolhidos pelos cantos e põe as faces de tijolo, os encontros e as variações, igual o Tiled faz |
| `DadosDoOldPrison` | As tabelas de cantos e as regras do pacote, já convertidas (gerado pela ferramenta; não editar à mão) |
| `GeradorDoAndar` | Monta o andar, põe os inimigos em cada sala de luta, conta quantos faltam, abre a saída quando a sala do fim fica limpa (ou o último morre) e troca de andar. Na tela: o nome do andar, o contador e, quando faltam 2 salas ou menos (fora de luta), uma seta na beirada apontando pra mais perto |
| `Saida` | O vórtice: abre na sala do fim quando ela fica limpa (no chefe, onde ele morreu) e, pisado, leva pro próximo andar. Na área segura pode levar pra um tema (a escolha do caminho): fica da cor dele, com o nome embaixo (em cima, ele caía debaixo do texto da HUD) |
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

O primeiro andar tem 4 salas de luta (3 no caminho, contando a do fim, e 1 do lado) com 4 a 6 inimigos
cada (a do fim, 2 a mais), mais as especiais sorteadas (loja 65%, tesouro 50% e sempre no andar 1, altar 25%, desafio 25%, fonte 30%, cela 22%, ferreiro 25%, apostas 20%). O tesouro (desde a 1.21.0) mostra dois itens: pegou um, o outro some (`Pedestal.Escolha`).
Uma sala a mais no caminho a cada 3 andares de salas, uma do lado a cada 4, e meio inimigo a mais por
sala a cada andar. Depois do último andar, a partida acaba em vitória e recomeça.

### A arte do Old Prison e as regras do Tiled

O pacote Old Prison vem com arquivos do **Tiled Map Editor**: as folhas de ladrilhos, a tabela de
**cantos** de cada folha (qual ladrilho vai em cada combinação dos 4 cantos: dentro ou fora da parede)
e as **regras de encaixe** (automapping), que põem embaixo de cada beirada a face de tijolo de 2 de
altura e sorteiam variações (tijolo rachado, musgo). O jogo faz o mesmo na hora, a cada andar:

1. a `PlantaDeSalas` monta e a `Caverna` **ajeita** a planta: nada de parede com menos de 3 de altura (o topo mais a face
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
  (uGUI), com a arte do Dragon Regalia (faixas, molduras, botões de losango, ponteiro, setas) na
  versão **sombria** (`InterfaceSombria`, desde a 1.19.0: a mesma arte recolorida pelo
  `Ferramentas/Interface/sombria.py` em ferro velho, pedra escura, pano cor de sangue e brasa; a
  colorida do pacote fica de reserva em `InterfaceDragao`), as
  fontes Jersey 15 e Jacquard 12, os desenhos das teclas e dos botões do controle nas dicas (trocam
  sozinhos entre teclado e controle, Xbox ou PlayStation), a transição escura entre telas e a música.

| Tela | O que tem |
|---|---|
| `TelaDeInicio` | Título na faixa cor de sangue, cinzas e brasas subindo no fundo, o herói (retrato animado, descrição, números; as setas aparecem quando tiver mais de um), Jogar, Continuar (com a partida salva, e do lado da tecla qual é: "Andar 4 com o Soldado"), Configurações e Sair do jogo, os controles com o desenho das teclas, os créditos e a versão. Só na primeira vez: "tentar de novo" vai direto pro jogo |
| `TelaDeOpcoes` | Volume da música e dos efeitos (barras), tela cheia, resolução e tremor da tela, tudo salvo (`Opcoes`) |
| `TelaDePausa` | `Esc`, `P` ou `Start`: o andar, as armas, Continuar, Salvar e sair, Reiniciar partida, Configurações e Sair do jogo. O que abandona a partida (Salvar e sair, Reiniciar, Sair do jogo) pede dois apertos ("Aperte de novo pra...") e não tem atalho: até a 1.22.3, R reiniciava e Q ia pro menu na hora, e no jogo eles são o item e a troca de arma |
| `TelaDeFimDeJogo` | Morreu (clarão vermelho) ou venceu (dourado): câmera lenta, o retrato do herói caindo ou parado, o resumo (herói, andar, tempo, inimigos e chefes derrotados, armas) e Tentar de novo, Menu principal, Sair do jogo |
| `TelaDeNovidades` | Na primeira vez que abre uma versão nova, as novidades dela (as notas do lançador) |

| Peça | O que faz |
|---|---|
| `TelaSimples`, `MenuDeBotoes` | Montar canvas, textos, painéis e a coluna de botões animados (teclado, controle e mouse) |
| `TransicaoDeTela` | Escurece, troca (recarregar a cena, voltar ao menu) e clareia |
| `ArteDaInterface`, `FonteDoJogo`, `IconeDeTecla`, `DicaDupla` | A arte e as fontes lidas de `Resources`, e as dicas com o desenho das teclas |
| `Controle` | O controle nos menus (qual foi mexido por último, Xbox ou PlayStation) |
| `Sons`, `Musica` | Os sons dos menus e a música: menu, uma por caverna, chefe, chefe final, vitória e fim de jogo. `M` liga e desliga a música, `N` os efeitos |
| `Idioma`, `Traducoes` | O idioma (1.19.0): portugues ou ingles, nas Configuracoes. O jogo e escrito em portugues e traduz na hora de mostrar (`Idioma.T`, e `Idioma.F` pros modelos com lacunas, "Andar {0} de {1}"); as pecas que desenham texto (`TelaSimples`, `MenuDeBotoes`, `Desenho.Texto`, `TextoFlutuante`, os avisos) ja traduzem sozinhas. O dicionario sai do `Ferramentas/Idioma/ingles.py` pelo `gerar.py` (que confere se cada chave existe no jogo). Texto novo: ponha o par la e rode o gerar |
| `DicasDaPrimeiraVez` | As dicas da primeira partida: cada uma aparece uma vez na vida (PlayerPrefs), em fila, embaixo da tela |
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

Os do mundo 2 e 3 continuam aparecendo no 4. (O contorno claro em volta dos inimigos saiu na 1.16.0.)

| Script | O que faz |
|---|---|
| `InimigoAtirador` | O jeito de todos: anda até o jogador (ou recua, se ele chega perto demais), prepara o ataque e ataca. O ataque é a arma (o padrão dela), a investida ou os dois (corre e atira no fim). Ainda: `jeito` (reto, rodeando ou aos trancos), `voa` (passa por cima dos buracos), investidas seguidas e fúria com pouca vida |
| `DanoAoEncostar` | Machuca e empurra quem é do outro lado e encosta |
| `TiroAoMorrer` | Ao morrer, solta um disparo da arma dele (as gotas da Gosma) |
| `SomeEAparece` | De vez em quando desbota e aparece em outro lugar perto do jogador (Bruxo, Fogo-Fátuo) |
| `EscudoFrontal` | Segura o golpe que vem de frente (abre quando ele ataca); é um `IBloqueioDeDano` da `Vida` |
| `DivideAoMorrer` | Ao morrer vira pedaços menores (a Bolha vira duas Bolinhas) |
| `Invocador` | Levanta ajudantes do chão (o Necromante, até 2 esqueletos vivos); eles saem crescendo (`SaindoDoChao`) |
| `AnimacaoDoInimigo` | Parado, andando, atacando e morrendo, olhando pro jogador. O quadro do disparo chega bem quando o ataque sai |
| `Vida` | Lado `Inimigos`; a vida muda de um pra outro, e a `armadura` segura uma parte do dano |
| `PiscarAoTomarDano` | Fica branco a cada golpe |
| `MorteDoInimigo` | Para, deixa de bater nas coisas, toca a morte e some numa nuvem de poeira |

O **clima** (`ClimaDoAndar`, 1.19.0): som de fundo em loop por mundo e eventos longe (gota, corrente,
rangido, gemido, vento, pedrinhas; `Resources/Ambiente`, feitos pelo `Ferramentas/Audio/ambiente.py`),
poeira e névoa embaixo do véu do escuro (aparecem só na luz) e a vinheta nas bordas. As tochas
(`LuzTremula`) engasgam e balançam a luz.

O **campeão-chefe** (`Campeao.SortearChefe`): às vezes na sala do fim (andar 2+), dois poderes, 6× a
vida e a barra de vida no alto (`Campeao.ChefeAtivo`, desenhada pela HUD).

Os **campeões** (`Campeao`, desde a 1.19.0): ao montar as salas, cada inimigo tem uma chance
(`Partida.ChanceDeCampeao`: 5% no andar 1, sobe 1% por andar até 16%, o dobro no modo maldito; nas
ondas, metade) de virar campeão: 20% maior, 2,5× a vida, uma aura que pulsa e uma luz da cor do poder,
e o nome em cima quando acorda. Os poderes: **furioso** (`InimigoAtirador.RitmoExtra` e `MaisTiros`),
**flamejante** (deixa `ChamaNoChao`, que fere só o jogador), **blindado** (`Vida.Escudo`: um escudo de
2,2 s a cada 6,5 s), **veloz** (`VelocidadeExtra`) e **estourado** (anel de 12 tiros ao morrer). Chefe
não vira. Morto, solta 3 moedas, às vezes chave ou coração, e um cristal (`Campeao.Premiar`).

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

O **Boneco de treino** (`Assets/Prefabs/BonecoDeTreino.prefab`), no acampamento do andar 1, serve
pra testar as armas: a vida dele é imortal, ele pisca e balança na estaca pro lado do golpe
(`BonecoDeTreino`), e nunca sai do lugar. É um objeto da cena; o `GeradorDoAndar` liga ele só no andar 1
(até a 1.22.3 ele ficava no mesmo ponto em todo andar, inclusive no meio da arena do chefe, segurando os
tiros).

## Chefes

Cada chefe tem um **andar só dele**: um salão (a `Arena`) sem caverna e sem inimigos comuns, com quatro
pilares pra se esconder dos tiros. O jogador entra por baixo, o chefe espera no meio de cima. Depois
de uma apresentação de 2 segundos (aí o nome e a barra de vida aparecem no alto da tela), ele alterna os
ataques sem repetir o mesmo duas vezes seguidas, andando um pouco entre eles. Com metade da vida entra
em **fúria**: fica avermelhado, mais rápido, e ganha um ataque a mais. Morto, o portal abre e cai um
baú no meio do salão.

A partida tem **12 andares em 4 mundos**: duas cavernas e o chefe, três vezes, e no fim duas cavernas e
o Olho do Abismo ou o Rei Goblin. O chefe de cada mundo é sorteado entre dois (`chefe` e `ouEntao` na lista **Andares**);
o nome do andar dele vem do próprio chefe (`lugar`).

| Mundo | Chefes | Ataques |
|---|---|---|
| 1 | **Minotauro** (320) ou **Lobisomem Alfa** (300) | Minotauro: investidas, pisão, cortes. Lobisomem: botes seguidos, garras em dois leques, uivo que chama cães; na fúria, botes com garras |
| 2 | **Senhor da Guerra** (380) ou **Demônio do Martelo** (360) | Senhor da Guerra: machadada (onda de choque), machados bumerangue (vão e voltam), investida, grito que chama orcs. Demônio: pula em você e cai soltando um anel, espiral, cuspe de fogo (marcas no chão que explodem) |
| 3 | **Rei Necromante** (400) ou **Golem de Brasa** (480) | Rei: some e reaparece longe com um anel, ossos que saem das marcas em volta de você, muralha de tiros, levanta esqueletos |
| final | **Olho do Abismo** (650) ou **Rei Goblin** (640) | Olho: não anda. Anel, rajada, espiral, chama olhos; com 60% da vida: dois anéis, leques e o raio; com 25%: tiros em cruz girando o tempo todo. Rei Goblin (o Goblin Brutamonte das Profundezas no dobro do tamanho, 1.18.0): martelada de cristal (dois anéis), salto esmagador (cai soltando pedras), cristais que saem do chão em volta de você, varrida em leque; na fúria, chama goblins assassinos, três investidas e chuva de cristais; no fim, espiral |

Os chefes vêm do tema do mundo (ver **O caminho**, embaixo): escolhendo a Cripta como mundo 2, o chefe
dela (Rei Necromante, Golem ou Rei Esqueleto) aparece no andar 6.

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

## Itens, moedas e o recheio da caverna

Do jogo antigo, adaptado pra caverna (`Assets/Scripts/Itens` e `Assets/Scripts/Cenario`):

- **Bolsa**: moedas (compram na loja) e chaves (abrem o baú trancado). Caem dos inimigos (`Coletavel`).
  As bombas saíram na 1.18.0: no lugar delas, o tiro especial de cada arma (**G**), que também é o
  único que quebra as pedras rachadas.
- **Itens**: os 27 passivos do jogo antigo (`CatalogoDeItens`) mudam os números do jogador
  (`EstatisticasDoJogador`: dano, ritmo, tiros a mais, atravessar, perseguir, fênix, escudo, orbes...);
  13 **sinergias** (dois itens juntos dão um bônus). Os 6 **ativos** ficam no **R** e recarregam
  derrotando inimigos. Saem em pedestais: baú trancado e amaldiçoado, desafio, altar de sangue (paga com
  vida), loja (paga com moedas), tesouro e o prêmio do chefe.
- **Recheio** (`RecheioDaCaverna`): as salas especiais ganham o que são (loja, altar, desafio, tesouro
  com dois itens pra escolher um). Nas salas de luta saem pedra rachada com
  prêmio e área escura, e as armadilhas: serra no trilho, tronco rolante e espinhos (ferem o jogador e
  os inimigos). O tronco fica parado encostado numa parede e, de tempos em tempos, atravessa a sala até a
  parede da frente (o caminho dele vai de parede a parede, nunca passa por porta nem buraco). Parado, segura
  gente e tiro como uma parede; rolando, machuca no comprimento todo (três círculos ao longo dele). Mesas (viram de lado no primeiro tiro), barris, caixotes, baratas e velas. Nada nos
  corredores nem na frente das portas; o que ocupa lugar sai do mapa de caminhos. (A emboscada saiu: a
  própria sala de luta já é uma.)
- **Eventos** (1.19.0): a **cela** (`TipoDeSala.Cela`, 22% dos andares, ícone de chave no mapa) com o
  `Prisioneiro`: com uma chave o portão da cela sobe, ele dá um cristal e abre uma `Loja` 40% mais barata
  ali mesmo. O **baú maldito** (`TipoDeBau.Maldito`, 20% dos andares do 2 em diante, vermelho e
  tremendo): abre de graça, dá o melhor de três itens e uma arma rara ou melhor, e solta uma emboscada de
  duas ondas em volta (`Emboscada.Soltar`, sem prêmio no fim).
- **Mundos com arte própria** (1.14.0, `EstiloDeLadrilhos`): Porão (o Old Prison em marrom), Catacumbas
  (o Old Prison), **Cripta** (pacote Crypt: paredes e chão do Crypt, sem buracos, caixões, estátuas,
  cruzes, estandartes e o lançador de fogo na parede) e **Profundezas** (pacote The Depths of the
  Mountain: sem paredes, as salas flutuam sobre o vazio, com estátuas douradas, cristais, ouro, potes que
  quebram e rochas saindo do vazio). Tochas acesas nas paredes de todos os mundos com parede; as portas
  das salas são o portão de caveira do Old Prison, nas quatro paredes (desde a 1.18.0; antes, a porta
  grande de madeira do Crypt); os espinhos são os do Crypt.
- **Iluminação** (1.16.0, `Iluminacao`, `Escuridao`, `FonteDeLuz`): o andar é escuro. Não usa as luzes 2D
  do URP (no build elas não escureciam nada): a `Escuridao` é um véu preto por cima do mundo, uma textura
  pequena (4 pontos por célula, filtrada) refeita a cada quadro, com a luz de fundo bem fraca num tom de
  cada mundo (um pouco mais clara no salão do chefe) e buracos claros em volta de cada `FonteDeLuz`: o
  herói (fraca), tochas (nas paredes de todos os mundos com parede, e no salão do chefe), velas,
  candelabros, cristais, a chama mágica, o portal, a estátua de fogo e os itens no pedestal. A cor das
  luzes tinge um pouco o véu (o fogo deixa a volta quente). Tiros, moedas, efeitos, números de dano,
  preços da loja, armas no chão e as marcas dos chefes ficam na camada de desenho "Brilho", que é
  desenhada por cima do véu. O fundo da câmera escurece junto.
- **Área segura** (1.17.0, `AreaSegura`): depois de cada chefe (menos o último), antes do próximo mundo, um
  lugar aberto e claro, sem inimigos, cercado de árvores: a Clareira (Grass Land) depois do mundo 1 e as
  Ruínas Antigas (Ancient Ruins) depois dos outros (ao entardecer depois do mundo 3). Tem o mercador
  (`Mercador`, acena quando o jogador chega) com a barraca e três coisas à venda (`Loja` sem o balcão),
  a fonte que cura tudo uma vez (`FonteDaCura`), o cálice dos espíritos (`CaliceDosEspiritos`: coração e
  moedas, uma vez), a criatura da sorte (`CriaturaDaSorte`: passeia, um carinho solta moedas e ela foge),
  um baú de graça (o do Village) e potes que quebram. O portal: na clareira, num círculo de pedras; nas
  ruínas, o altar acende quando o jogador chega perto e abre o portal em cima dele (`AltarDoPortal`).
  Não conta como andar; o "Continuar" salvo ali volta pra ela (a escolha do caminho ainda não foi feita).
- **O caminho** (1.18.0, `GeradorDoAndar.Caminho`): cada mundo tem um tema (Porão, Catacumbas, Cripta,
  Profundezas), com o desenho, a luz, a música, a decoração, os inimigos e o chefe dele. Na Clareira
  (depois do primeiro chefe) são **dois portais**, cada um com o nome e o que tem lá: o escolhido vira o
  mundo 2 e o outro, o mundo 3; as Profundezas são sempre o fim. Os inimigos e o chefe saem da parte da
  lista **Inimigos** e **Andares** do tema (`AndarNoTema`: na Cripta como mundo 2, o andar 4 sorteia como
  o andar 7); o tamanho das salas e a quantidade de inimigos seguem o andar de verdade. O salvamento guarda
  a ordem.
- **Minimapa** (1.18.0, `Minimapa`): no canto de cima à direita, um pedaço do mapa em volta do jogador;
  `Tab` (ou o Select do controle) abre o mapa inteiro, com o jogo parado. Só aparecem as salas em que
  entrou e, apagadas, as vizinhas, com os corredores; as de luta com inimigos ficam vermelhas, as especiais
  têm ícone (loja, tesouro, altar, desafio, a do fim e a secreta, depois de aberta), e o portal aberto e o
  jogador piscam. Na arena do chefe e na área segura não tem mapa.
- **Salas especiais mobiliadas** (1.17.0, `Decoracao.Mobiliar`, pacote Village): a loja da caverna tem o
  mercador atrás de uma mesa, armários de poções, prateleiras na parede, expositor, barris, caixotes e
  sacos; o tesouro, armaduras, cabides de armas, baús e escudos; o altar, estantes de livros,
  escrivaninha, globo, quadros e troféus; o desafio, armas e armaduras. Tapete no meio e candelabros
  acesos. Os potes do Village quebram com os cacos da cor deles (`Quebravel.Pote`), também nos
  depósitos do Old Prison; teias nos cantos de cima das salas.
- **Decoração das salas** (1.16.0, `Decoracao`): cada sala ganha cenas montadas nos cantos, com as peças
  dos pacotes. No Old Prison (Porão e Catacumbas): depósito (tonéis, barris e caixotes que quebram, sacos,
  baldes), tortura (dama de ferro, guilhotina, tronco, correntes, bolas de espinhos), cela (esqueleto
  acorrentado, gaiola), mesa posta (cadeiras, caneca, vela acesa) e ossário (esqueleto, ossos, velas ou a
  chama mágica azul); na parede de cima, entre as tochas, estandartes, retratos, correntes e esqueletos
  acorrentados; gaiolas penduradas, correntes caindo do teto, candelabros; e miudezas pelo chão (ossos,
  pedras, papel, correntes), mais perto das paredes. A Cripta ganhou bancos, livros, montes de
  velas, ossos e correntes; as Profundezas, a espada fincada e pedras.
  Desde a 1.17.1 cada peça só vai onde o desenho inteiro cabe (`Decoracao.Cabe`): não encosta no desenho de
  outra (nem nas tochas, estandartes e teias da parede), o pé fica em chão livre, nada passa da parede do
  lado nem da de baixo e o que é alto só cobre uma célula da face da parede de cima (tonéis, armários e a
  dama de ferro vão pras paredes do lado). Encostada na parede do lado, a peça larga é empurrada pra
  dentro. Também fica longe das armadilhas, do caminho do tronco e da serra, das velas e do que já estava
  no chão (baús, armas, pedras, mesas). A beirada da parede do lado conta 0,45 pra dentro do chão (o pior
  ladrilho, nas quinas dos degraus), sem folga; nas Profundezas o chão acaba no meio da célula da beirada.
  Na frente do lançador de fogo fica livre o leque inteiro do fogo, até o alcance. A loja desce o meio
  quando a mesa do mercador não cabe no alto da sala (`Decoracao.MeioDaLoja`), e nada encosta nos
  pedestais.
- **Inimigos dos pacotes**: Catacumbas: esqueleto, escudeiro, mago e assassino (Old Prison). Cripta:
  esqueletos, aranhas e minhocões que somem na terra (Crypt). Profundezas: goblins, goblin assassino,
  pote mímico e os demônios (Depths). O **Rei Esqueleto** (Depths) é sorteado como chefe da Cripta.
- **Temas dos mundos** (antes da 1.14.0): Porão (marrom), Catacumbas (o azul do Old Prison), Cripta (verde) e Abismo
  (vermelho), com o chão e as paredes recoloridos (`Resources/Temas`) e a música de cada um.
- **Arcos especiais** (nos baús): explosivo, de gelo (deixa lento), venenoso e ricochete (quica nas
  paredes) — o `efeito` da arma (`EfeitoDoTiro`); a `CondicaoDoInimigo` cuida do gelo e do veneno.

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
| `Conquistas` | 17 conquistas; ganhar mostra o `AvisoDeConquista`, que entra pela direita embaixo do minimapa (no canto de cima ficam o andar e os inimigos da HUD) |
| `Desbloqueios`, `PlantaNoChao` | O que vai sendo liberado (1.21.0, como no Dead Cells e no Isaac): 7 armas e 6 itens começam trancados e só saem depois de achar a **planta** deles (3 por mundo; caem do chefe do mundo, às vezes de um campeão, e podem ser prêmio de sala; a primeira da vida sai na sala do fim do andar 1), e 12 itens são liberados por **conquistas** (`PorConquista`). `Melhorias.Liberada` (armas) e `CatalogoDeItens.Sortear` (itens) perguntam. No desafio do dia tudo vale. A aba **Desbloqueios** da `TelaDeProgresso` mostra tudo |
| `Acampamento`, `Fogueira`, `Falante`, `Falas`, `Conversa` | O **acampamento** (1.21.0): a sala do começo do andar 1, maior (15 x 11), com a fogueira, a estátua dos cristais e três pessoas pra conversar (Bento, o mercador; Mestre Ferraz, o ferreiro, que ali não forja; o Apostador, sem apostas). Cada conversa comenta a última partida (`Falas.GuardarFim`, chamado pelo `Registro` ao morrer ou zerar) e conta o próximo pedaço da história, um por partida, liberado pela profundidade alcançada, plantas e vitórias. Os textos ficam todos no `Falas.cs` |
| `Bestiario` | Nome e "como luta" de cada inimigo e chefe |
| `TelaDeProgresso` | O botão **Progresso** do menu: abas de estatísticas, conquistas e bestiário |
| `Salvamento` | A partida salva: só no começo de cada andar e de cada área segura (herói, andar, vida, armas, bolsa, itens, números, o caminho e o modo). **Continuar** no menu volta pro começo do andar salvo, com o que tinha ao chegar nele (ou pra área segura, se salvou nela), como no Gungeon; o **Salvar e sair** da pausa só sai. Até a 1.22.3 ele gravava o meio do andar, e sair e voltar montava o andar de novo com prêmios novos (itens, moedas e cristais sem fim). Morrer, vencer ou jogar de novo apaga |
| `Melhorias` | Os **cristais** e as melhorias compradas com eles (desde a 1.19.0): vida, desconto na loja, chaveiro, arsenal (libera as armas com `DadosDaArma.arsenal`) e pólvora (recarga do especial). Comprar é na `EstatuaDosCristais` (começo do andar 1 e áreas seguras), pela `TelaDaEstatua` |
| `Partida` | O **modo** da partida: normal, **maldito** (liberado ao zerar: inimigos 25% mais rápidos, 25% mais inimigos e uma onda a mais, campeões em dobro; mais sorte, raras mais cedo e cristais em dobro) ou **desafio do dia** (`Semear` põe o `Random` na semente da data UTC e do andar antes de montar cada andar; sem melhorias; recorde do dia por pontos). Escolhido na `TelaDosModos` (botão **Modos de jogo** do menu) |

Os cristais caem dos campeões (1), dos chefes (2 + andar/3), raramente de qualquer inimigo (1,2%), do
prisioneiro solto e ao zerar (10); tudo em dobro no maldito. Pegar já guarda (`Melhorias.Ganhar`): morrer
não perde.

## Criar uma arma

**Create ▸ Jogo ▸ Arma** numa pasta do projeto (o costume é `Assets/Dados/Armas`), preencher no
Inspector (desenho do tiro, ritmo, velocidade, alcance, dano, quantos tiros por disparo, abertura,
dispersão, tremor, som) e arrastar o asset na `ArmaDoJogador`. Dá pra mexer nos números com o jogo
rodando.

A **raridade** (`raridade`, desde a 1.18.0): comum, incomum, rara, épica ou lendária. Muda a cor do nome
no HUD, o brilho da arma no chão (as comuns não brilham) e a chance no baú: o peso de cada uma é o
`pesoNoBau` vezes um fator do andar (`Raridades.Fator`), então as comuns somem aos poucos e as raras
aparecem mais fundo. As armas dos heróis são comuns.

| Raridade | Armas |
|---|---|
| Comum | Varinha de Faíscas, Machado de Arremesso (e as dos heróis) |
| Incomum | Tomo das Brasas, Besta de Repetição |
| Rara | Cajado do Trovão, Arco de Gelo, Arco Venenoso |
| Épica | Arco Explosivo, Arco Ricochete |
| Lendária | Tomo Proibido (espiral de brasas roxas que perseguem), Besta Real (3 virotes que se dividem em 3 ao acertar), Lâmina Real, Cetro do Abismo |

As armas da 1.19.0 e os efeitos novos do `DadosDaArma`:

| Arma | Raridade | Arsenal | O que tem |
|---|---|---|---|
| Lâmina Curta | Incomum | - | Corpo a corpo (`golpeNaMao`: balança na mão), `cortaTiros`: o corte apaga os tiros do outro lado que encosta |
| Bumerangue | Incomum | - | `volta`: freia e volta pra mão; atravessa |
| Cajado da Tempestade | Épica | 1 | `encadeia` 3: ao acertar, um `Raio` pula pros inimigos perto (até 4,5 unidades), 70% do dano e caindo 15% por salto |
| Lâmina Real | Lendária | 2 | Corta tiros e `devolveTiros`: o tiro muda de lado e bate 4× |
| Bumerangue de Prata | Rara | 2 | Dois bumerangues |
| Cetro do Abismo | Lendária | 3 | Orbes que `persegue`m e `encadeia` 4 |

O Cajado do Trovão também `encadeia` (2). `divideAoAcertar` (a Besta Real) solta o `Fragmento` da arma
em leque. Com `arsenal` > 0 a arma só sai (baú, chão, fonte) depois desse nível do Arsenal; no desafio
do dia, todas saem. Os desenhos novos saem do `Ferramentas/Armas/armas_novas.py`.

A **Fonte dos Desejos** (`FonteDosDesejos`): joga nela a arma da mão (`ArmaDoJogador.Largar`) e ela
devolve outra, de uma raridade sorteada a partir da jogada: 45% uma acima, 35% a mesma (outra arma),
20% uma abaixo. Uma vez só. Fica na área segura e, às vezes (30%), numa sala só dela no andar
(`TipoDeSala.Fonte`, com ícone no minimapa).

O **ferreiro** (`Ferreiro`, 1.20.0): forja a arma da mão pagando moedas (`ArmaDoJogador.Forjar`). A
forjada é uma cópia (`DadosDaArma.Forjada(nivel)`, guardada na original) com `forja` 1 a 3
(`ForjaMaxima`), +30% de dano por nível e uma raridade acima por nível; `Origem` devolve o asset de
verdade (o baú e a fonte comparam por ele) e `NomeNaTela` mostra "Nome +2". O salvamento guarda a forja
de cada mão. Preço: 15/25/35/50/70 pela raridade + 10 por forja, com o desconto da loja. Fica na área
segura e, às vezes (25%), numa sala só dele (`TipoDeSala.Ferreiro`).

O **apostador** (`Apostador`, `TelaDasApostas`, 1.20.0, `TipoDeSala.Apostas`, 20% dos andares): aposta de
5, 10 ou 20 moedas, 2d6 contra 2d6; ganhou, volta em dobro; empate, volta igual. 5 apostas por mesa.

O **baú mímico** (`Bau.TalvezMimico`, `MimicoDoBau`, 1.20.0): os baús espalhados pela caverna (e o da
pedra rachada), do andar 2 em diante, têm 8% a 20% de chance de ser um mímico. Parado, só treme de vez em
quando; ao abrir, vira um inimigo feito no código (`Vida`, `DanoAoEncostar`, `MorteDoInimigo`) que pula
mordendo e cospe um leque de moedas, e morto solta uma arma incomum ou melhor. A arte (o baú da prisão
com dentes, olhos e língua), a bigorna, a mesa e os dados saem do `Ferramentas/Cenario/desenhos.py`.

Os itens das armas novas (1.20.0): **Lâmina Larga** (`CorteVezes`: o corte das armas com `cortaTiros`
fica maior e mais forte), **Corda Encantada** (`VoltasExtras`: o `Projetil` do bumerangue, ao chegar na
mão, sai de novo pra mira) e **Bobina de Cobre** (`AlcanceDoRaioVezes`, `SaltosExtras`, `ChanceDeRaio`:
`Raio.Encadear` recebe o alcance), com as sinergias Tempestade Viva, Retorno Certeiro, Dança das Lâminas
(`LaminasDevolvem`), Lâmina Giratória (`BumerangueCorta`) e Bumerangue Elétrico.

O **tiro especial** (botão direito ou `G`, LT no controle; desde a 1.18.0): toda arma tem um, feito dela mesma na hora
(`DadosDaArma.Especial`, uma cópia com o padrão mudado), sem gastar munição e com recarga
(`recargaDoEspecial`, guardada por arma na `ArmaCarregada`: trocar de arma não zera). O tipo é o
`especial` do asset:

| Tipo | O que sai | Armas |
|---|---|---|
| Leque | Um leque largo dos tiros da arma, 50% mais forte | Os arcos, a besta |
| Anel | 14 tiros em volta, 50% mais fortes | Varinha, tomo, cetro do Padre |
| Carregado | Um tiro grande que atravessa, 6× o dano e empurra muito | Lança, cajado do trovão |
| Giro | Dois anéis de 8 cortes que atravessam, 2× o dano | As espadas, o sabre |
| Explosivo | Um tiro maior que explode onde bate, 3× o dano | Os machados, o cajado de fogo |

Só o dano do especial quebra a pedra rachada (`Dano.especial`). O Barril de Pólvora deixa ele 50% mais
forte e com 30% menos recarga; o ativo Pavio Eterno deixa ele pronto na hora. No HUD, em cima da
moldura da arma: o nome e a barra da recarga.

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
