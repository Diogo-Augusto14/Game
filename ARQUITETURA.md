# Arquitetura do jogo

Como as peças se encaixam, o que cada script faz e onde mexer quando quiser ajustar algo.
O `MANUAL.md` continua sendo o caderno de estudo das peças da Unity; este arquivo é sobre
o **código deste jogo**.

---

## 0. Só dar Play

O jogo é um roguelike visto de cima, no estilo do Isaac. A cena do jogo é
`Assets/Scenes/Jogo.unity`: uma câmera, a luz 2D e um objeto com o componente `Andar`, que
monta tudo por código (andar, salas, jogador, menu, HUD, música).

A câmera (`Andar.EnquadrarCamera`) cabe a largura de uma sala e refaz o zoom sempre que a
janela muda de tamanho. Cada casa tem uma cortina preta (`Sala.Coberta`) que só sai da sala onde
o jogador está: em tela mais quadrada que 16:9 as salas vizinhas (inclusive a secreta) não
aparecem; os buracos do chão são da própria sala e continuam aparecendo.

O Play do editor **sempre começa por essa cena**, mesmo com outra aberta
(`Editor/CenaDoAndar.cs`, menu `Tools ▸ Jogo ▸ Play sempre pela cena do jogo`, que dá pra
desmarcar). Na primeira vez, `Tools ▸ Jogo ▸ Preparar projeto` cria as camadas e tags que
faltarem (também roda sozinho ao dar Play se faltar alguma).

`Assets/Scenes/TopDown.unity` é a sala de treino (`BootstrapTopDown`): uma sala só, com
bonecos de treino, fora do build.

O antigo jogo de plataforma (boneco com espada, Bootstrap, SampleScene e a arte dele) foi
removido do projeto.

| Tecla | Ação |
|---|---|
| `W A S D` | Andar em 8 direções |
| `Setas` | Atirar (só as 4 retas; vale a última seta apertada) |
| `E` | Bomba |
| `Shift` (controle: `RB` ou `RT`) | Dash: arrancada curta, sem tomar dano durante ela, com recarga |
| `Esc` | Pausa |
| `M` / `N` | Liga/desliga música / efeitos |
| `I` (Select no controle) | Mostra o efeito dos itens pegos, um por um (ou passe o mouse no item) |

| Script | O que faz |
|---|---|
| `TopDown/MovimentoTopDown.cs` | Andar com aceleração/freio, gravidade zero, recebe empurrão do `Vida`; o dash (velocidade, duração, recarga e a proteção que faz dele esquiva) |
| `TopDown/RastroDoDash.cs` | O que se vê no dash: poeira do Tiny Swords, rastro azulado do herói e o brilho quando a recarga acaba |
| `TopDown/GolpeDeEspada.cs` | Heróis de espada/machado: brilho de lâmina a cada golpe |
| `TopDown/AnimacaoDoTiro.cs` | Tiro animado: a onda de corte tremula e se abre ao sair |
| `TopDown/AtiradorTopDown.cs` | Dano, alcance, cadência, velocidade do tiro; o desenho do tiro vem do herói |
| `TopDown/Lagrima.cs` | O projétil: voa até o alcance, bate em `IDanificavel` ou parede e estoura |
| `TopDown/Herois.cs` | Os heróis jogáveis e os números de cada um. Cavaleiro, templário, espadachim e machadeiro (`OndaDeCorte`) disparam ondas de corte que atravessam inimigos e golpeiam em combo (ataque 1, 2 e 3 da folha) |
| `TopDown/Progresso.cs` | O que fica salvo entre partidas (PlayerPrefs): vitórias e heróis liberados. Só o Arqueiro Azul começa livre; os outros saem vencendo chefes e zerando (condições em `Herois.cs`). `Tools ▸ Jogo ▸ Apagar progresso salvo` bloqueia tudo de novo |
| `TopDown/BootstrapTopDown.cs` | `CriarJogador` (usado pelo `Andar`) e a sala de treino |
| `combate/Vida.cs` | Vida, dano, invencibilidade e empurrão de tudo que apanha |
| `Jogador/Entrada.cs` | Teclado (WASD e setas separados) |
| `UI/Hud.cs` | Corações (numa área de altura fixa: vida a mais encolhe os corações em vez de empurrar o resto) e a lista de controles |
| `Itens/HudDoInventario.cs` | Moedas, chaves e bombas em coluna embaixo da vida, e o aviso do item pego |
| `Itens/PainelDeItens.cs` | Itens pegos na lateral direita; mouse ou `I`/Select mostra o nome e o efeito (`ItemPassivo.Descricao`) |

---

## 7. Sala estilo Isaac (visão de cima)

Pasta `Assets/Scripts/Sala/`. Nada aqui usa gravidade.

**Testar:** `Tools ▸ Jogo ▸ Sala ▸ Criar cena de teste` e Play. WASD anda, setas atiram.
Saindo por uma porta aberta, a demo troca por uma sala nova com um inimigo a mais.

| Arquivo | Assunto |
|---|---|
| `Sala.cs` | Chão, 4 paredes, portas; fecha ao entrar, abre quando morre o último inimigo. Na sala pronta do Old Prison um véu (`VeuDoChao`, `ContrasteTiradoDoPiso`) puxa o piso pra cor média do tema e baixa o contraste das manchas de terra; ossos de enfeite ficam apagados e o esqueleto inteiro mais ainda, pra não parecer inimigo |
| `Porta.cs` | Aberta / fechada / não existe (vira parede); avisa `AoAtravessar`. Abrir e fechar são animados (o portão sobe e desce quadro a quadro, com tremor e um pulinho no fim); fechando bloqueia na hora, abrindo só deixa passar quando o portão chegou em cima (`Passavel`). `AbrirNaHora` é a montagem sem animação. Na sala do Old Prison as portas dos lados ganham um pouco de luz no vão, um fio claro em cada batente e um brilho fraco no chão (`MontarLuzDoVao`), senão a saída sumia na parede |
| `InimigoDeSala.cs` | Base: dormir, acordar, dano por encostar, empurrão na direção do golpe |
| `ContornoClaro.cs` | Borda clara de 1 pixel em volta de todo inimigo (`FabricaDeInimigos` põe), pra bicho escuro não sumir no chão. Sem shader: 4 `SpriteMask` com o quadro do desenho, deslocadas 1 pixel, recortam um retângulo claro logo atrás dele. Some na morte e enquanto o barril está disfarçado (`InimigoDeSala.Disfarcado`) |
| `InimigoPerseguidor.cs` | Vai atrás do jogador em zigue-zague |
| `InimigoAtirador.cs` | Mantém distância, telegrafa (incha) e atira |
| `TiroDaSala.cs` | Projétil dos dois lados (inimigo acerta só o jogador e vice-versa) |
| `InimigoDemonio.cs` | Demônio (arte importada): persegue, ergue a espada e corta à frente |
| `InimigoDeSangue.cs` | Monstro de Sangue (arte importada): lento, espirra um anel de gotas |
| `InimigoComArte.cs` | Base dos inimigos do Tiny Swords: animação, caveira ao morrer, manter distância |
| `InimigoDeGolpe.cs` | Corpo a corpo com arte: goblin da tocha (golpe pro lado, pra cima ou pra baixo) e esqueleto da espada |
| `InimigoEsqueletoFoice.cs` | Esqueleto da foice: lento; perto, gira a foice duas vezes em volta de si |
| `InimigoVampiro.cs` | Vampiro: se encolhe na capa e dá um bote; se acerta, suga vida e se cura |
| `InimigoGoblinDinamite.cs` | Goblin da dinamite: fica longe e joga dinamite (`DinamiteLancada.cs`) onde o jogador está; um círculo vermelho avisa onde cai |
| `InimigoBarril.cs` | Barril de TNT: parece um barril parado; o goblin sai, corre até o jogador, acende o pavio e explode. Morto a tiro, explode na hora |
| `InimigoArqueiro.cs` | Arqueiro sombrio: mantém distância, puxa o arco e solta uma flecha reta |
| `FabricaDeInimigos.cs` | Receita de cada inimigo, montada por código |
| `JogadorDeTeste.cs` | Boneco **provisório**; só nasce se a cena não tiver objeto com tag `Player` |
| `DemoDaSala.cs` | Cena de teste: uma sala, e troca por outra ao sair pela porta |

Uso por código (é o que o gerador de andar deve chamar):

```csharp
Sala sala = Sala.Criar("Sala 3", centro, new[] { LadoDaPorta.Cima, LadoDaPorta.Esquerda });
sala.CriarInimigo(TipoDeInimigo.Atirador, new Vector2(3f, 1f));   // posição relativa ao centro
sala.PortaEm(LadoDaPorta.Cima).AoAtravessar.AddListener(porta => /* ir pra sala de cima */);
sala.AoLimpar.AddListener(() => /* soltar prêmio */);
```

A sala não troca o jogador de sala: ela só avisa. `Porta.PontoDeChegada` é onde pôr o
jogador quando ele chega por aquela porta.

---

### Tema de cada mundo

`Andar/TemaDoAndar.cs` dá a cara de cada mundo: nome (aparece no aviso "Mundo 2 - Fase 1: Catacumbas"
e na pausa), chão, parede, tom, quantos ossos/runas/candelabros enfeitam as salas e a lista
de inimigos comuns com peso. O `Andar` escolhe o tema antes de montar as salas e
`ArteGerada.Chao`/`Tijolo` desenham o chão e a parede dele.

| Mundo | Tema | Inimigos comuns |
|---|---|---|
| 1 | Porão | goblins da tocha, barril, gosmas, bolhas, morcegos, cão infernal, orc, bruxo, minotauro, monstro de sangue |
| 2 | Catacumbas | saltador, orcs, goblin da dinamite, arqueiro sombrio, lobisomem, esqueletos, cavaleiro do escudo, sentinela |
| 3 | Cripta | esqueletos (foice, espadão, arqueiro...), vampiro, necromante, fogo-fátuo, cavaleiros, sentinela, urso |
| 4 (último) | Abismo | demônios e demônias, monstro de sangue, cão infernal, bolha, fogo-fátuo, orc elite, minotauro, vampiro |

Chefes, salas especiais e desbloqueios não mudam com o tema. A sala de desafio usa a
lista do tema nas ondas.

**Salas grandes** (`GeradorDeAndar.Juntar`): depois de montar o andar, casas comuns vizinhas e já
ligadas por porta viram uma sala só, como no Isaac: corredor (2x1), sala alta (1x2), 2x2 e em L
(`FormaDaSala`, `ParametrosDoAndar.ChanceDeSalaGrande`). Cada casa continua sendo uma `Sala` no
mundo, com as portas dela pro lado de fora; entre as casas não tem parede nem porta
(`Juncoes`), e pedaços recortados da própria imagem da sala cobrem as paredes que sumiram
(`Sala.Costurar`). As casas fecham, acordam e abrem juntas (`Sala.Agrupar`). A câmera acompanha o
jogador dentro da sala grande sem sair dela (`Andar.AlvoDaCamera`), e cada casa tem uma cortina
preta (`Sala.Coberta`) que esconde tudo o que não é a sala atual.

Cada sala comum tem uma **espécie dominante** e, quase sempre, uma ou duas de apoio
(`Andar.MontarBando`): a dominante leva de 55% a 80% do orçamento da sala (`inimigosPorSala`, mais
os extras da fase), as de apoio o resto, e o número de cada uma sai do custo do bicho
(`CustoNoBando`): bicho fraco vem em mais, pesado em menos. Barril, goblin da dinamite, necromante e
sentinela têm teto por sala (`MaximoNaSala`). Cada um nasce num ponto seu, longe das portas e dos
outros, com um pouco mais ou menos de velocidade.

Movimento: quem persegue contorna pedra, bloco e buraco por um mapa de distâncias nos ladrilhos da
sala (`Sala.ProximoPasso`, via `InimigoDeSala.PeloCaminho`); quem atira (bruxo, arqueiros, goblin da
dinamite...) passeia pela sala entre um tiro e outro (`InimigoDeSala.Passear`) e só recua quando o
jogador chega perto demais.

---

### Mundos, fases e dificuldade

O jogo tem 4 mundos com 3 fases cada (`Andar.quantidadeDeMundos` e `Andar.fasesPorMundo`).
Cada mundo usa um tema da tabela acima: Mundo 1 Porão, 2 Catacumbas, 3 Cripta e 4 Abismo.
`Andar.NumeroDoAndar` conta todas as fases (1 a 12); `Andar.Mundo`, `Andar.Fase` e
`Andar.NomeDaFase` ("Mundo 2 - Fase 3") saem dele. Toda fase termina num chefe e no
alçapão; a última fase do último mundo tem o Olho do Abismo e termina a partida.

| Mundo | Chefes das fases 1 e 2 (a 2 nunca repete a 1) | Chefe da fase 3 (o mais forte) |
|---|---|---|
| 1 Porão | Golem de Magma ou Minotauro Furioso | Demônio do Martelo |
| 2 Catacumbas | Golem de Magma ou Demônio do Martelo | Minotauro Furioso |
| 3 Cripta | Demônio do Martelo ou Minotauro Furioso | Rei Necromante |
| 4 Abismo | Minotauro Furioso ou Rei Necromante | Olho do Abismo (final) |

`Andar/DificuldadeDaFase.cs` guarda todos os números que sobem aos poucos, a cada fase:

- inimigos: vida +8% e velocidade +2,5% por fase; +1 inimigo por sala a cada mundo e mais
  um na fase 3 (máximo +3); na fase 1 de cada mundo só aparece o começo da lista do tema,
  e a variedade abre até a lista inteira na fase 3; do mundo 2 em diante, 15% de chance de
  um inimigo do mundo anterior;
- inimigos campeões (`Sala/Campeao.cs`): nenhum na primeira fase, depois +3% por fase até
  30%. Vermelho tem o dobro da vida, amarelo é bem mais rápido, roxo (mundo 3+) as duas
  coisas; sempre solta prêmio;
- comportamentos novos: sentinela atira em 8 direções na fase 3 de cada mundo e do mundo 2
  em diante; o monstro de sangue solta mais gotas do mundo 2 em diante;
- chefes: vida +6% por fase, e o da fase 3 mais 15%; o Olho do Abismo ganha no máximo 50%;
- dano no jogador (`Vida.MultiplicadorDeDanoRecebido`): normal nos mundos 1 e 2, 1,5x no
  mundo 3, coração inteiro (2x) no mundo 4. Vale pra tiro, encostada e espinho;
- armadilhas e salas: menos salas vazias (-2% por fase), espinhos da fase 2 em diante,
  sala de desafio com mais inimigos por onda e uma onda a mais no mundo 4;
- recompensas: drop de inimigo +1% e prêmio de sala +2% por fase; o chefe deixa moedas
  (uma por mundo, o dobro no chefe da fase 3);
- tamanho do mapa: o gerador recebe `TamanhoDoMapa` (sobe a cada duas fases, de 1 a 6).

Desbloqueios: Soldado, Arqueiro e Cavaleiro saem ao vencer o chefe da fase 3 dos mundos
1, 2 e 3 (`Herois.Heroi.LiberaNoMundo`, `Progresso.VenceuMundo`). Os que dependem de zerar
continuam iguais.

## 8. Arte importada (pacotes)

Imagens de pacote ficam em `Assets/Arte/Resources/` e são lidas por
`Arte/ArteImportada.cs` (sem fatiar no Sprite Editor: os recortes estão no código). O
import de cada PNG já vem no `.meta`: Sprite, filtro Point, sem compressão, sem mipmap.

| Pasta | Pacote | Onde aparece |
|---|---|---|
| `Personagens/*` (20 bichos) e `Personagens/Projeteis` | Tiny RPG Character Asset Pack 02 (versão com sombra quando existe) | Todos os inimigos antigos (perseguidor = cão infernal, atirador = bruxo, investidor = minotauro, saltador = gosma, sentinela = cavaleiro do canhão, divisor = bolha), os chefes (golem, demônio do martelo, monstro-olho), o vendedor da loja e as variações `Morcego`, `CavaleiroLanca`, `CavaleiroEscudo`, `DemonioTridente`, `DemonioLaminas`, `DemonioArqueiro`, `Demonia`, `FogoFatuo`, `DemoniaFoice`; do Pack 01 v2.0: `Orc`, `OrcBlindado`, `OrcElite`, `OrcMontado`, `EsqueletoGuerreiro`, `EsqueletoBlindado`, `EsqueletoEspadao`, `EsqueletoArqueiro`, `Geleia`, `Morceguinho`, `Lobisomem`, `Urso` e `Necromante`. Animados por `Animacao/AnimacaoDePersonagem.cs`, que toca o ataque sozinho no telégrafo |
| `Personagens/Herois/*` e os tiros `FlechaDoSoldado`, `FlechaDoArqueiro`, `Dardo`, `BolaDeFogo`, `Cristais` | Tiny RPG Character Asset Pack 01 v2.0 | Os 9 heróis jogáveis (soldado, cavaleiro, templário, lanceiro, espadachim, machadeiro, arqueiro, mago, padre). `TopDown/Herois.cs` guarda vida, velocidade e tiro de cada um; o menu (`UI/TelaDeInicio.cs`) escolhe com esquerda/direita; o padre cura sozinho (`TopDown/CuraDoHeroi.cs`) |
| `InterfacePixel` | Pixel UI pack 3 | Corações da vida, barras (vermelha/amarela) do chefe, placa de preço da loja |
| `InterfaceDragao` | Tiny RPG Dragon Regalia GUI (CC0) | Molduras douradas do menu/pausa/fim de jogo, faixas de título e do aviso de andar, botões dos menus, ícones redondos de configurações e de sair, setas e ponteiro, moldura da barra do chefe, cursor do mouse (`ArteDaInterface.cs`) |
| `Fontes` | Jersey 15 e Jacquard 12 (Google Fonts, licença OFL: `LICENCA-*.txt` do lado) | Jersey 15 em todo texto do jogo (números bem legíveis; o arquivo tem o unitsPerEm diminuído pra ficar do tamanho da fonte antiga); Jacquard 12 (gótica pixelada) nos títulos grandes: nome do jogo, Pausado, Você morreu, Configurações (`UI/FonteDoJogo.cs`) |
| `Teclas` | Controllers and Keyboard (Vryell) | Desenho das teclas nas dicas de controle do menu, da pausa, do fim de jogo e da HUD (`TelaSimples.LinhaDeTeclas`, `IconeDeTecla`) |
| `Sons` (os `.wav`) | Universal UI Soundpack (Nathan Gibson, CC BY 4.0: crédito no menu) | Sons de menu: navegar, confirmar, abrir/fechar pausa, herói bloqueado, herói liberado, aviso de fase |
| `Sons` (os `.ogg`) e `Musica` | Feitos pro jogo; vozes e monstros do Freedoom (BSD); músicas gravadas no soundfont MuseScore General (MIT) | Os efeitos de jogo e as 10 músicas (seção 10). Créditos e licenças em `Assets/StreamingAssets/CREDITOS-AUDIO.txt`, que vai junto no build |
| `Masmorra/Esqueleto`, `EsqueletoFoice`, `Vampiro` | Enemy Animations Set (tiras de 32×32) | Inimigos `Esqueleto`, `EsqueletoFoice` e `Vampiro`, com a própria animação de morte |
| `Masmorra/Tocha`, `Candelabro`, `Objetos` | 2D Dungeon Asset Pack v5.2 e 2D Pixel Dungeon Asset Pack v2.0 | Tochas acesas na parede de cima de toda sala, candelabro num canto, caveira e ossos entre os enfeites de chão |
| `Masmorra/Chao`, `Parede`, `Portao`, `Espinhos`, `Ladrilhos` | 2D Dungeon Asset Pack v5.2 | Chão e tijolos das salas (a cor do andar só tinge de leve), portão de grade nas portas e na tranca, espinhos, buraco do alçapão. Da folha `Objetos`: frasco (coração), moeda, chave, altar do pedestal, mesas da loja, estandartes das portas especiais, gema do tiro dos inimigos e o ícone de cada item passivo |
| `Efeitos/Poeira`, `ExplosaoPequena`, `Respingo`, `Chama` e `Personagens/Projeteis/MagiaVerde` | Tiny Swords Free Pack (Particle FX) e Tiny RPG Pack 01 v2.0 (magia do necromante) | Impacto e rastro das flechas especiais e dos tiros dos inimigos (seção 9) |
| `Masmorra/Bau`, `ChaveDourada` e `Masmorra/BauDeFerro` | 2D Dungeon Asset Pack v5.2 (items_animation) e 2D Pixel Dungeon Asset Pack v2.0 (chest e chest_open juntos numa tira) | Baú de madeira abrindo, chave dourada girando que o chefe deixa (`Itens/ChaveDoChefe.cs`) e baú de ferro trancado respirando e abrindo com brilho (`Itens/Bau.cs`) |
| `Masmorra/Temas` | 2D Pixel Dungeon Asset Pack v2.0 e 2D Dungeon Asset Pack v5.2 (ladrilhos recortados e juntados) | Chão e parede de cada tema de andar (`Andar/TemaDoAndar.cs`): `ChaoPorao`/`ParedePorao` (tijolos marrons do v2.0), `ChaoCripta`/`ParedeCripta` (laje rachada e friso azul do v5.2), `ChaoAbismo`/`ParedeAbismo` (pedra lisa e friso vermelho do v5.2) e a `Runa` vermelha do chão do Abismo. As Catacumbas usam o `Chao`/`Parede` padrão |
| `TinySwords` | Tiny Swords (Update 010) e Tiny Swords Free Pack, da Pixel Frog | Goblins da tocha e da dinamite, barril de TNT, arqueiro sombrio, flecha, dinamite, explosão (bomba do jogador também), caveira de morte e enfeites de chão nas salas (`Sala.Enfeitar`); o jogador é o arqueiro azul (`TopDown/ArqueiroDoJogador.cs`) e atira flechas; pedras das salas; a dinamite é a bomba |

### Menus

Todas as telas são montadas por código (`UI/TelaSimples.cs`) e os botões vêm de
`UI/MenuDeBotoes.cs`: W/S ou setas escolhem, Enter/Espaço/A apertam, o mouse escolhe e
clica. Cada botão mostra o atalho do lado. `UI/TransicaoDeTela.cs` escurece a tela antes
de recarregar a cena (jogar de novo, voltar ao menu, sair).

| Tela | Botões | Atalhos |
|---|---|---|
| Menu inicial (`TelaDeInicio`) | Jogar, Configurações, Sair do jogo | Enter jogar, O configurações, Esc vai pro Sair (de novo: sai), A/D troca o herói |
| Pausa (`TelaDePausa`) | Continuar, Reiniciar partida, Configurações, Menu principal, Sair do jogo | Esc, R, O, Q |
| Fim de jogo (`TelaDeFimDeJogo`) | Tentar de novo, Menu principal, Sair do jogo | R, Q, Esc |
| Configurações (`TelaDeOpcoes`) | volumes, tela cheia, resolução, Voltar | Esc volta |

Na morte a tela pisca vermelha, o jogo entra em câmera lenta enquanto o herói cai e depois
aparece o resumo: herói, andar, tempo, inimigos derrotados, salas exploradas
(`UI/ResumoDaPartida.cs`), itens e semente.

Os textos do jogo têm acento. Onde um nome vira chave salva ou código (herói liberado no
`Progresso`, ícone do item em `ArteImportada.IconeDoItem`), `FonteDoJogo.SemAcentos` tira
os acentos, então o que já estava salvo continua valendo.

Cada tira de personagem tem quadros de 100×100 com o bicho (uns 20 px) no meio. O
recorte usado é 64×48 em volta do corpo, com o pivô no centro do corpo.
As folhas do Tiny Swords são grades (uma linha por animação, células de 192, 128 ou 64 px);
`ArteImportada.Linha` recorta uma linha com o pivô no centro do corpo. A explosão fica
com o tamanho do raio de dano (`Combate/Explosao.cs`).
Se uma imagem sumir, `ArteImportada` devolve null e cada tela volta ao desenho antigo.

---

## 9. Flechas e projéteis

Pasta `Assets/Scripts/Projeteis/`.

**Flechas do jogador.** Cada tipo de flecha sai de um item do mesmo nome (o `CatalogoDeItens`
cria um item por flecha, campo `ItemPassivo.flecha`). A flecha fica na aljava pelo resto da
partida: pegar uma nova já troca pra ela, e `Q` (ou `LT` no controle; a bomba no controle fica só no `LB`) passa pra próxima,
voltando na normal. O painel no canto de baixo à direita mostra a flecha em uso (o canto esquerdo é da linha de teclas da HUD).

| Flecha | Desenho | O que faz |
|---|---|---|
| Rápida | flecha de pena dourada (arqueiro do Tiny RPG), rastro | 1,6x velocidade, 1,25x cadência, 0,8x dano |
| Pesada | flecha grossa do soldado, balançando | 1,8x dano, 3x empurrão, maior e mais lenta |
| Explosiva | flecha do demônio com pavio piscando e chama | explode ao bater ou cair (raio 1,1; só machuca inimigo) |
| Perfurante | flecha do esqueleto com rastro de luz | atravessa todos os inimigos, voa mais longe |
| de Gelo | estrela de cristal do mago girando | inimigo fica com 45% da velocidade por 2,5 s |
| Venenosa | raio verde do necromante | 3 mordidas de veneno depois do golpe |
| Ricochete | flecha do Tiny Swords dourada | quica nas paredes até 3 vezes |

| Arquivo | Assunto |
|---|---|
| `TipoDeFlecha.cs` | Os tipos, os números e a aparência de cada um (`CatalogoDeFlechas`) |
| `TrocaDeFlecha.cs` | Aljava do jogador, troca com Q/RB e o painel da HUD |
| `EfeitoDaFlecha.cs` | Explodir, gelar, envenenar e quicar (a `Lagrima` chama) |
| `CondicaoDoInimigo.cs` | Gelo e veneno no inimigo (`InimigoDeSala.MultiplicadorDeVelocidade`) |
| `AparenciaDoProjetil.cs` / `VisualDoProjetil.cs` | Desenho animado de qualquer projétil: quadros, apontar, girar, pulsar, piscar, rastro, faíscas e impacto |

**Tiros dos inimigos.** O `TiroDaSala` descobre sozinho o estilo de quem atirou
(`EstiloDeTiro.cs`): primeiro um `EstiloDoAtirador` no bicho, se tiver, senão pelo nome
que a `FabricaDeInimigos` dá. Bruxo: orbe roxo ondulando. Demônia: bola de fogo que
acelera. Fogo fátuo: chama azul em zigue-zague. Necromante: raio verde que persegue um
pouco (o do chefe não persegue). Cavaleiro do canhão: bala girando. Monstro de sangue:
gota que freia. Arqueiros: cada um com a própria flecha. Chefes: pedra (golem), brasa e
bolha (demônio do martelo), raio verde (Rei Necromante), onda de choque (Minotauro),
estrela vermelha (Olho do Abismo). Inimigo sem estilo continua com a gema. Pra dar estilo a
um inimigo novo: `EstiloDoAtirador.Marcar(obj, EstiloDeTiro.X)` ou uma linha em
`EstilosDeTiro.porNome`.

---

## 10. Música e efeitos sonoros

Pasta `Assets/Scripts/Audio/`. Os volumes de música e de efeitos (`Musica.Volume`,
`Sons.Volume`) são os do menu de opções e do M/N.

**Música** (`Musica.Tocar(TemaMusical.X)`): cada tema é o arquivo
`Resources/Musica/<tema>.ogg`, em loop sem emenda e com troca suave entre uma e outra.
Trocar uma música é só pôr outro arquivo com o mesmo nome. Sem o arquivo, a música é
composta por código no estilo chiptune (como era antes).

| Música | Quando toca |
|---|---|
| `Porao`, `Catacumbas`, `Cripta`, `Abismo` | No mundo de cada tema (`TemaDoAndar.Musica`, via `Andar.MusicaDoAndar`) |
| `Chefe` / `ChefeFinal` | Na sala do chefe enquanto ele vive, e nas ondas da sala de desafio (`Chefe`) |
| `Loja` | Dentro da loja |
| `Menu` | Menu inicial |
| `Vitoria` | Depois de vencer o chefe final |
| `FimDeJogo` | Quando o herói morre |

**Efeitos** (`Sons.Tocar(Som.X)`): cada `Som` é o arquivo `Resources/Sons/<Som>`;
`<Som>_2`, `<Som>_3`... são variações sorteadas a cada toque (`Acerto`, `MorteInimigo`,
`MorteDemonio`, `Rugido`). Sem o arquivo, o som é sintetizado (`Sintetizador.cs`).
Vinhetas (`Item`, `Vitoria`, `Segredo`, `Coracao`, `Cura`, `BauAbre`) tocam sempre no tom
certo; os outros variam um pouco o tom pra não cansar.

- Cada inimigo morre com o som do seu tipo (`FabricaDeInimigos.SomDeMorte`): ossos pros
  esqueletos, gosma pras gosmas, bolhas e monstro de sangue, grito pros demônios, urro pras
  feras, o do chefe pros chefes e um gemido pros outros.
- Cada estilo de tiro inimigo tem o seu disparo (`EstilosDeTiro.SomDoDisparo`): flecha,
  fogo, canhão, gosma ou magia. O mago e o padre atiram com som de magia
  (`AtiradorTopDown.DefinirSomDoTiro`).
- Também têm som: o tiro do herói estourando, o pavio da bomba e do barril, o goblin
  jogando a dinamite, a mordida do vampiro, o feitiço do necromante, o alçapão abrindo, o
  Prego Enferrujado, as curas e a morte do herói.

Os arquivos de som e música foram gerados pelos scripts de `Ferramentas/Audio` (síntese,
trechos do Freedoom e notas tocadas no soundfont; o `LEIAME.md` de lá explica como gerar
de novo). Os créditos estão no `CREDITOS-AUDIO.txt`.
