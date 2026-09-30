# Arquitetura do jogo

Como as peças se encaixam, o que cada script faz e onde mexer quando quiser ajustar algo.
O `MANUAL.md` continua sendo o caderno de estudo das peças da Unity; este arquivo é sobre
o **código deste jogo**.

---

## 0. Só dar Play

O jogo é um roguelike visto de cima, no estilo do Isaac. A cena do jogo é
`Assets/Scenes/Jogo.unity`: uma câmera, a luz 2D e um objeto com o componente `Andar`, que
monta tudo por código (andar, salas, jogador, menu, HUD, música).

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
| `Esc` | Pausa |
| `M` / `N` | Liga/desliga música / efeitos |
| `I` (Select no controle) | Mostra o efeito dos itens pegos, um por um (ou passe o mouse no item) |

| Script | O que faz |
|---|---|
| `TopDown/MovimentoTopDown.cs` | Andar com aceleração/freio, gravidade zero, recebe empurrão do `Vida` |
| `TopDown/AtiradorTopDown.cs` | Dano, alcance, cadência, velocidade do tiro; o desenho do tiro vem do herói |
| `TopDown/Lagrima.cs` | O projétil: voa até o alcance, bate em `IDanificavel` ou parede e estoura |
| `TopDown/Herois.cs` | Os heróis jogáveis e os números de cada um |
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
| `Sala.cs` | Chão, 4 paredes, portas; fecha ao entrar, abre quando morre o último inimigo |
| `Porta.cs` | Aberta / fechada / não existe (vira parede); avisa `AoAtravessar` |
| `InimigoDeSala.cs` | Base: dormir, acordar, dano por encostar, empurrão na direção do golpe |
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

### Tema de cada andar

`Andar/TemaDoAndar.cs` dá a cara de cada andar: nome (aparece no aviso "Andar 2: Catacumbas"
e na pausa), chão, parede, tom, quantos ossos/runas/candelabros enfeitam as salas e a lista
de inimigos comuns com peso. O `Andar` escolhe o tema antes de montar as salas e
`ArteGerada.Chao`/`Tijolo` desenham o chão e a parede dele.

| Andar | Tema | Inimigos comuns |
|---|---|---|
| 1 | Porão | goblins da tocha, barril, gosmas, bolhas, morcegos, cão infernal, orc, bruxo, minotauro, monstro de sangue |
| 2 | Catacumbas | saltador, orcs, goblin da dinamite, arqueiro sombrio, lobisomem, esqueletos, cavaleiro do escudo, sentinela |
| 3 | Cripta | esqueletos (foice, espadão, arqueiro...), vampiro, necromante, fogo-fátuo, cavaleiros, sentinela, urso |
| último | Abismo | demônios e demônias, monstro de sangue, cão infernal, bolha, fogo-fátuo, orc elite, minotauro, vampiro |

Chefes, salas especiais e desbloqueios não mudam com o tema. A sala de desafio usa a
lista do tema nas ondas.

## 8. Arte importada (pacotes)

Imagens de pacote ficam em `Assets/Arte/Resources/` e são lidas por
`Arte/ArteImportada.cs` (sem fatiar no Sprite Editor: os recortes estão no código). O
import de cada PNG já vem no `.meta`: Sprite, filtro Point, sem compressão, sem mipmap.

| Pasta | Pacote | Onde aparece |
|---|---|---|
| `Personagens/*` (20 bichos) e `Personagens/Projeteis` | Tiny RPG Character Asset Pack 02 (versão com sombra quando existe) | Todos os inimigos antigos (perseguidor = cão infernal, atirador = bruxo, investidor = minotauro, saltador = gosma, sentinela = cavaleiro do canhão, divisor = bolha), os chefes (golem, demônio do martelo, monstro-olho), o vendedor da loja e as variações `Morcego`, `CavaleiroLanca`, `CavaleiroEscudo`, `DemonioTridente`, `DemonioLaminas`, `DemonioArqueiro`, `Demonia`, `FogoFatuo`, `DemoniaFoice`; do Pack 01 v2.0: `Orc`, `OrcBlindado`, `OrcElite`, `OrcMontado`, `EsqueletoGuerreiro`, `EsqueletoBlindado`, `EsqueletoEspadao`, `EsqueletoArqueiro`, `Geleia`, `Morceguinho`, `Lobisomem`, `Urso` e `Necromante`. Animados por `Animacao/AnimacaoDePersonagem.cs`, que toca o ataque sozinho no telégrafo |
| `Personagens/Herois/*` e os tiros `FlechaDoSoldado`, `FlechaDoArqueiro`, `Dardo`, `BolaDeFogo`, `Cristais` | Tiny RPG Character Asset Pack 01 v2.0 | Os 9 heróis jogáveis (soldado, cavaleiro, templário, lanceiro, espadachim, machadeiro, arqueiro, mago, padre). `TopDown/Herois.cs` guarda vida, velocidade e tiro de cada um; o menu (`UI/TelaDeInicio.cs`) escolhe com esquerda/direita; o padre cura sozinho (`TopDown/CuraDoHeroi.cs`) |
| `InterfacePixel` | Pixel UI pack 3 | Corações da vida, barras (vermelha/amarela) do chefe, placa de preço da loja |
| `InterfaceDragao` | Tiny RPG Dragon Regalia GUI (CC0) | Molduras douradas do menu/pausa/fim de jogo, faixas de título e do aviso de andar, botão de jogar, setas e ponteiro da escolha de herói, moldura da barra do chefe, cursor do mouse (`ArteDaInterface.cs`) |
| `Teclas` | Controllers and Keyboard (Vryell) | Desenho das teclas nas dicas de controle do menu, da pausa, do fim de jogo e da HUD (`TelaSimples.LinhaDeTeclas`, `IconeDeTecla`) |
| `Sons` | Universal UI Soundpack (Nathan Gibson, CC BY 4.0: crédito no menu) | Sons de menu: navegar, confirmar, abrir/fechar pausa, herói bloqueado, herói liberado. `Sons.Tocar` usa `Resources/Sons/<Som>` quando existe |
| `Masmorra/Esqueleto`, `EsqueletoFoice`, `Vampiro` | Enemy Animations Set (tiras de 32×32) | Inimigos `Esqueleto`, `EsqueletoFoice` e `Vampiro`, com a própria animação de morte |
| `Masmorra/Tocha`, `Candelabro`, `Objetos` | 2D Dungeon Asset Pack v5.2 e 2D Pixel Dungeon Asset Pack v2.0 | Tochas acesas na parede de cima de toda sala, candelabro num canto, caveira e ossos entre os enfeites de chão |
| `Masmorra/Chao`, `Parede`, `Portao`, `Espinhos`, `Ladrilhos` | 2D Dungeon Asset Pack v5.2 | Chão e tijolos das salas (a cor do andar só tinge de leve), portão de grade nas portas e na tranca, espinhos, buraco do alçapão. Da folha `Objetos`: frasco (coração), moeda, chave, altar do pedestal, mesas da loja, estandartes das portas especiais, gema do tiro dos inimigos e o ícone de cada item passivo |
| `Masmorra/Temas` | 2D Pixel Dungeon Asset Pack v2.0 e 2D Dungeon Asset Pack v5.2 (ladrilhos recortados e juntados) | Chão e parede de cada tema de andar (`Andar/TemaDoAndar.cs`): `ChaoPorao`/`ParedePorao` (tijolos marrons do v2.0), `ChaoCripta`/`ParedeCripta` (laje rachada e friso azul do v5.2), `ChaoAbismo`/`ParedeAbismo` (pedra lisa e friso vermelho do v5.2) e a `Runa` vermelha do chão do Abismo. As Catacumbas usam o `Chao`/`Parede` padrão |
| `TinySwords` | Tiny Swords (Update 010) e Tiny Swords Free Pack, da Pixel Frog | Goblins da tocha e da dinamite, barril de TNT, arqueiro sombrio, flecha, dinamite, explosão (bomba do jogador também), caveira de morte e enfeites de chão nas salas (`Sala.Enfeitar`); o jogador é o arqueiro azul (`TopDown/ArqueiroDoJogador.cs`) e atira flechas; pedras das salas; a dinamite é a bomba |

Cada tira de personagem tem quadros de 100×100 com o bicho (uns 20 px) no meio. O
recorte usado é 64×48 em volta do corpo, com o pivô no centro do corpo.
As folhas do Tiny Swords são grades (uma linha por animação, células de 192, 128 ou 64 px);
`ArteImportada.Linha` recorta uma linha com o pivô no centro do corpo. A explosão fica
com o tamanho do raio de dano (`Combate/Explosao.cs`).
Se uma imagem sumir, `ArteImportada` devolve null e cada tela volta ao desenho antigo.
