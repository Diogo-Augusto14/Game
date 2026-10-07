# Arquitetura do jogo

Como as peças se encaixam, o que cada script faz e onde mexer quando quiser ajustar algo.
O `MANUAL.md` continua sendo o caderno de estudo das peças da Unity; este arquivo é sobre
o **código deste jogo**.

---

## 0. Só dar Play

O jogo é um roguelike visto de cima, no estilo do Isaac, com o tiro do Gungeon: mira livre com o
mouse e armas de fogo com pente, recarga e munição (seção 11). A cena do jogo é
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
| Mouse | Mira em qualquer ângulo; o botão esquerdo atira (o cursor vira uma mira) |
| `Setas` | Atirar sem mouse (só as 4 retas; vale a última seta apertada; mandam sobre o mouse) |
| `R` (controle: clique no analógico esquerdo) | Recarrega a arma de fogo |
| `Q` ou roda do mouse (controle: `LT`) | Próxima arma; com o herói na mão, anda primeiro pelas flechas dele |
| `1` a `9` | Escolhe a arma pelo lugar |
| `F` (controle: clique no analógico direito) | Vazio: apaga os tiros inimigos e empurra os inimigos |
| `E` | Bomba |
| `Shift` ou botão direito do mouse (controle: `RB`) | Dash: arrancada curta, sem tomar dano durante ela, com recarga |
| `Esc` | Pausa |
| `M` / `N` | Liga/desliga música / efeitos |
| `I` (Select no controle) | Mostra o efeito dos itens pegos, um por um (ou passe o mouse no item) |

| Script | O que faz |
|---|---|
| `TopDown/MovimentoTopDown.cs` | Andar com aceleração/freio, gravidade zero, recebe empurrão do `Vida`; o dash (velocidade, duração, recarga e a proteção que faz dele esquiva) |
| `TopDown/RastroDoDash.cs` | O que se vê no dash: poeira do Tiny Swords, rastro azulado do herói e o brilho quando a recarga acaba |
| `TopDown/GolpeDeEspada.cs` | Heróis de espada/machado: brilho de lâmina a cada golpe |
| `TopDown/AnimacaoDoTiro.cs` | Tiro animado: a onda de corte tremula e se abre ao sair |
| `TopDown/AtiradorTopDown.cs` | Dano, alcance, cadência, velocidade do tiro; o desenho do tiro vem do herói. Com arma de fogo na mão (`DefinirArma`) atira as balas dela (`AtirarComArma`) |
| `TopDown/MiraNaTela.cs` | Troca o cursor do mouse por uma mira enquanto o mouse mira e o jogo roda; nos menus volta o cursor dourado |
| `Armas/*` | As armas de fogo: dados e catálogo, arsenal, arma na mão, painel de munição e o vazio (seção 11) |
| `TopDown/Lagrima.cs` | O projétil: voa até o alcance, bate em `IDanificavel` ou parede e estoura |
| `TopDown/Herois.cs` | Os heróis jogáveis e os números de cada um. Cavaleiro, templário, espadachim e machadeiro (`OndaDeCorte`) disparam ondas de corte que atravessam inimigos e golpeiam em combo (ataque 1, 2 e 3 da folha) |
| `TopDown/Progresso.cs` | O que fica salvo entre partidas (PlayerPrefs): vitórias e heróis liberados. Só o Arqueiro começa livre; os outros saem vencendo chefes e zerando (condições em `Herois.cs`). `Tools ▸ Jogo ▸ Apagar progresso salvo` bloqueia tudo de novo |
| `TopDown/BootstrapTopDown.cs` | `CriarJogador` (usado pelo `Andar`) e a sala de treino |
| `combate/Vida.cs` | Vida, dano, invencibilidade e empurrão de tudo que apanha |
| `Jogador/Entrada.cs` | Teclado (WASD e setas separados), mouse e controle. `Tiro` é a direção de quem atira; `Mira` é pra onde o jogador aponta (segue o cursor, ou fica onde as setas e o analógico deixaram) |
| `UI/Hud.cs` | Corações (numa área de altura fixa: vida a mais encolhe os corações em vez de empurrar o resto) e a lista de controles |
| `Itens/HudDoInventario.cs` | Moedas, chaves e bombas em coluna embaixo da vida, e o aviso do item pego |
| `Itens/PainelDeItens.cs` | Itens pegos na lateral direita; mouse ou `I`/Select mostra o nome e o efeito (`ItemPassivo.Descricao`) |
| `Itens/ItemAtivoDoJogador.cs` | Item ativo (um por vez): Espaço/RT usa, cada sala limpa recarrega uma carga; pegar outro deixa o antigo no pedestal. Os seis ativos estão em `CatalogoDeAtivos` |
| `Itens/Sinergias.cs` | Duplas de itens que dão um bônus a mais (um `ItemPassivo` invisível somado pelo `EstatisticasDoJogador`). A descrição de cada item lista com quem combina |
| `combate/Impacto.cs` | Peso dos golpes: tremor de câmera, congelamento curto (hitstop) e números de dano. Tremor e números desligam nas Configurações |
| `Chefe/ViradaDeFase.cs` | O momento em que um chefe muda de fase: rugido, tremor, congelamento, os tiros no ar somem e uma frase aparece |
| `Progresso/Registro.cs` | Estatísticas de todas as partidas, bestiário (visto, derrotados, quem mais matou) |
| `Progresso/Conquistas.cs` | As conquistas; algumas liberam itens no sorteio (`ItemLiberado`). Aviso no canto (`AvisoDeConquista`) |
| `Progresso/TelaDeProgresso.cs` | Menu inicial ▸ Progresso (P / Y): abas Estatísticas, Conquistas e Bestiário (`Bestiario.cs` tem nome e descrição de cada inimigo) |
| `Progresso/Salvamento.cs` | Partida salva no começo de cada fase (herói, fase, semente, vida, itens, ativo, moedas...). Pausa ▸ Salvar e sair; menu ▸ Continuar |
| `UI/TelaDeNovidades.cs` | Na primeira abertura de uma versão nova, mostra o `novidades.md` que o `gerar-versao.ps1` põe ao lado do exe (vem de `Lancador/notas/<versão>.md`) |

---

## 7. Sala estilo Isaac (visão de cima)

Pasta `Assets/Scripts/Sala/`. Nada aqui usa gravidade.

**Testar:** `Tools ▸ Jogo ▸ Sala ▸ Criar cena de teste` e Play. WASD anda, setas atiram.
Saindo por uma porta aberta, a demo troca por uma sala nova com um inimigo a mais.

| Arquivo | Assunto |
|---|---|
| `Sala.cs` | Chão, 4 paredes, portas; fecha ao entrar, abre quando morre o último inimigo. Na sala pronta do Old Prison um véu (`VeuDoChao`, `ContrasteTiradoDoPiso`) puxa o piso pra cor média do tema e baixa o contraste das manchas de terra; ossos de enfeite ficam apagados e o esqueleto inteiro mais ainda, pra não parecer inimigo |
| `Porta.cs` | Aberta / fechada / não existe (vira parede); avisa `AoAtravessar`. Abrir e fechar são animados (o portão sobe e desce quadro a quadro, com tremor e um pulinho no fim); fechando bloqueia na hora, abrindo só deixa passar quando o portão chegou em cima (`Passavel`). `AbrirNaHora` é a montagem sem animação. Na sala do Old Prison as portas dos lados ganham um pouco de luz no vão, um fio claro em cada batente e um brilho fraco no chão (`MontarLuzDoVao`), senão a saída sumia na parede |
| `InimigoDeSala.cs` | Base: dormir, acordar, dano por encostar, empurrão na direção do golpe. `JeitoDeChegar` (`Aproximar`): um jeito de chegar no jogador por espécie, nenhum repetido (zigue-zague, passo pesado, marcha, finta, pulsante, flanco, revoada, interceptar, cerco, espiral, cambaleante, arrasto, vaivém, guarda, embalo) |
| `ContornoClaro.cs` | Borda clara de 1 pixel em volta de todo inimigo (`FabricaDeInimigos` põe), pra bicho escuro não sumir no chão. Sem shader: 4 `SpriteMask` com o quadro do desenho, deslocadas 1 pixel, recortam um retângulo claro logo atrás dele. Some na morte e enquanto o barril está disfarçado (`InimigoDeSala.Disfarcado`) |
| `InimigoPerseguidor.cs` | Vai atrás do jogador em zigue-zague |
| `InimigoAtirador.cs` | Mantém distância, telegrafa (incha) e atira (base antiga; as espécies usam as classes abaixo) |
| `TiroDaSala.cs` | Projétil dos dois lados (inimigo acerta só o jogador e vice-versa). Conta as balas inimigas no ar (`NoAr`) e apaga todas (`LimparDosInimigos`) |
| `PadroesDeBala.cs` | Os desenhos que as balas fazem: anel, anel com buraco, leque, cortina, rosa. Com teto de 160 balas no ar |
| `AtiradorDePadroes.cs` | Faz um inimigo soltar padrões de bala, com aviso de carga e os padrões no tempo (rajada, espiral); sozinho ou chamado pelo inimigo |
| `InimigoDemonio.cs` | Demônio (arte importada): persegue, ergue a espada e corta à frente |
| `InimigoDeSangue.cs` | Monstro de Sangue (arte importada): lento, espirra um anel de gotas |
| `InimigoComArte.cs` | Base dos inimigos com arte animada: animação, caveira ao morrer, manter distância |
| `InimigoDeGolpe.cs` | Corpo a corpo com arte: orc, esqueleto guerreiro e a base dos lutadores com jeito próprio |
| `InimigoEsqueletoFoice.cs` | Demônia da foice: perto, gira a foice duas vezes em volta de si |
| `InimigoArqueiro.cs` | Arqueiros: puxam o arco e soltam flecha. `UsarMira`: esqueleto arqueiro (`Alinhada`) corre pra mesma linha/coluna do jogador e só atira dali, reto pelo corredor; demônio arqueiro (`Leque`) solta três flechas abertas |

| `InimigoSaltador.cs` | Gosma: pula atrás do jogador. `VirarGeleia`: pula sem rumo, mais vezes e mais curto. Os dois sem animação de ataque (só pulam) |
| `InimigoBruxo.cs` | Bruxo: aparece, solta o próximo padrão do ciclo dele (leque que cresce com a fase, anel com buraco, rajada; seção 12), some (intangível) e reaparece noutro ponto perto do jogador |
| `InimigoNecromante.cs` | Necromante: foge do jogador e levanta esqueletos guerreiros (até 2 por vez); com a sala cheia, joga maldição lenta |
| `InimigoFogoFatuo.cs` | Fogo-fátuo: orbita o jogador num raio que respira, apaga (intangível) e reacende soltando o próximo padrão do ciclo (cruz, anel com buraco, espiral) |
| `InimigoDemonia.cs` | Demônia: circula o jogador sem parar e atira o próximo padrão do ciclo (leque que cresce, cortina, rosa) |
| `InimigoMorcego.cs` | Morcego (`Mergulho`): circula e mergulha em linha reta; morceguinho (`Enxame`): voo errático, aos trancos |
| `InimigoCao.cs` | Cão infernal: persegue e, perto, dá um bote |
| `InimigoLobisomem.cs` | Lobisomem: rodeia o jogador à espreita e de repente arranca em disparada |
| `InimigoLanceiro.cs` | Cavaleiro da lança: se alinha com o jogador, avisa e investe com a lança |
| `InimigoEscudeiro.cs` | Cavaleiro do escudo: bloqueia tiro de frente e vira devagar; tem que flanquear |
| `InimigoUrso.cs` | Urso: não para com tiro; pisoteia o chão soltando um anel de pedras |
| `InimigoFurioso.cs` | Orc de elite: com metade da vida entra em fúria (vermelho, mais rápido, golpes mais seguidos) |
| `InimigoBlindado.cs` | Orc e esqueleto blindados: armadura segura parte do dano, nada interrompe o golpe |
| `InimigoDuelista.cs` | Demônio das lâminas: avança em 3 arrancadas seguidas e descansa |
| `InimigoTridente.cs` | Demônio do tridente: a meia distância treme e arremessa o tridente reto |
| `InimigoEspadao.cs` | Esqueleto do espadão: o golpe solta uma onda de corte que atravessa a sala, de perto ou de longe |
| `Chefe/ChefeLobisomem.cs` | Lobisomem Alfa: botes em sequência, garras em leque, cerco em volta do jogador e uivo que chama cães |
| `Chefe/ChefeOrc.cs` | Senhor da Guerra: machadada com onda de choque, machados que vão e voltam (`MachadoBumerangue`), investida soltando pedras e grito que chama orcs |
| `Andar/SalaEscura.cs` | Variação de sala comum: só se vê em volta do herói até limpar |
| `Andar/SalaDeEmboscada.cs` | Variação de sala comum: vazia até entrar; depois, ondas que nascem de marcas no chão |
| `Andar/LaminaGiratoria.cs` | Serra que anda num trilho (a partir da fase 2) e corta o herói |
| `Andar/TroncoRolante.cs` | Tronco com estacas do Old Prison rolando de parede a parede (a partir da fase 2, nas salas sem serra), com alavanca e engrenagem na parede de cima; corta o herói, some quando a sala é limpa |
| `Sala/Mesa.cs` | Mesa do Old Prison no lugar de algumas pedras: o primeiro tiro (de qualquer um) tomba ela de lado e ela vira barricada fina; cada tiro depois racha até quebrar. `Lagrima` e `TiroDaSala` chamam `Acertar`; bomba quebra pela `Vida` |
| `Sala/Baratas.cs` | Enfeite: bando de baratas indo e vindo no pé de uma parede (`Sala.EnfeitarComOldPrison`, que também põe as peças grandes, o sangue pingando e as velas de chama mágica) |
| `Andar/AltarDeSangue.cs` | Variação da sala amaldiçoada: um coração por prêmio, cada vez melhor, sem nunca matar |
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

Cada espécie é de **um mundo só** (nenhuma se repete entre listas, e nada vem do mundo anterior):

| Mundo | Tema | Inimigos (os três primeiros são os básicos) |
|---|---|---|
| 1 | Porão | orc, gosma, morceguinho, geleia, bolha, morcego, orc blindado |
| 2 | Catacumbas | lobisomem, cavaleiro da lança, cavaleiro do escudo, minotauro, orc montado, urso, cavaleiro canhão, orc de elite |
| 3 | Cripta | esqueleto guerreiro, esqueleto blindado, fogo-fátuo, esqueleto arqueiro, bruxo, esqueleto do espadão, necromante |
| 4 (último) | Abismo | demônio, cão infernal, demônio das lâminas, demônio do tridente, demônio arqueiro, demônia, demônia da foice, monstro de sangue |

Chefes, salas especiais e desbloqueios não mudam com o tema. A sala de desafio usa a
lista do tema nas ondas, e os ajudantes que os chefes chamam saem dos três básicos do mundo
(`TemaDoAndar.Lacaio`).

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
(`CustoNoBando`): bicho fraco vem em mais, pesado em menos. Necromante e
sentinela têm teto por sala (`MaximoNaSala`). Cada um nasce num ponto seu, longe das portas e dos
outros, com um pouco mais ou menos de velocidade.

Movimento: quem persegue contorna pedra, bloco e buraco por um mapa de distâncias nos ladrilhos da
sala (`Sala.ProximoPasso`, via `InimigoDeSala.PeloCaminho`); quem atira (bruxo, arqueiros...) passeia pela sala entre um tiro e outro (`InimigoDeSala.Passear`) e só recua quando o
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
| `Personagens/Herois/*` e os tiros `FlechaDoSoldado`, `FlechaDoArqueiro`, `Dardo`, `BolaDeFogo`, `Cristais` | Tiny RPG Character Asset Pack 01 v2.0 | Os 9 heróis jogáveis (arqueiro, que começa livre, soldado, cavaleiro, templário, lanceiro, espadachim, machadeiro, mago, padre). `TopDown/Herois.cs` guarda vida, velocidade e tiro de cada um; o menu (`UI/TelaDeInicio.cs`) escolhe com esquerda/direita; o padre cura sozinho (`TopDown/CuraDoHeroi.cs`) |
| `InterfacePixel` | Pixel UI pack 3 | Corações da vida, barras (vermelha/amarela) do chefe, placa de preço da loja |
| `InterfaceDragao` | Tiny RPG Dragon Regalia GUI (CC0) | Molduras douradas do menu/pausa/fim de jogo, faixas de título e do aviso de andar, botões dos menus, ícones redondos de configurações e de sair, setas e ponteiro, moldura da barra do chefe, cursor do mouse (`ArteDaInterface.cs`) |
| `Fontes` | Jersey 15 e Jacquard 12 (Google Fonts, licença OFL: `LICENCA-*.txt` do lado) | Jersey 15 em todo texto do jogo (números bem legíveis; o arquivo tem o unitsPerEm diminuído pra ficar do tamanho da fonte antiga); Jacquard 12 (gótica pixelada) nos títulos grandes: nome do jogo, Pausado, Você morreu, Configurações (`UI/FonteDoJogo.cs`) |
| `Teclas` | Controllers and Keyboard (Vryell) | Desenho das teclas nas dicas de controle do menu, da pausa, do fim de jogo e da HUD (`TelaSimples.LinhaDeTeclas`, `IconeDeTecla`) |
| `Sons` (os `.wav`) | Universal UI Soundpack (Nathan Gibson, CC BY 4.0: crédito no menu) | Sons de menu: navegar, confirmar, abrir/fechar pausa, herói bloqueado, herói liberado, aviso de fase |
| `Sons` (os `.ogg`) e `Musica` | Feitos pro jogo; vozes e monstros do Freedoom (BSD); músicas gravadas no soundfont MuseScore General (MIT) | Os efeitos de jogo e as 10 músicas (seção 10). Créditos e licenças em `Assets/StreamingAssets/CREDITOS-AUDIO.txt`, que vai junto no build |
| `Masmorra/Tocha`, `Objetos` | 2D Dungeon Asset Pack v5.2 e 2D Pixel Dungeon Asset Pack v2.0 | Tochas das salas grandes (as prontas do Old Prison já trazem as arandelas) e a folha de objetos (abaixo) |
| `Masmorra/Chao`, `Parede`, `Portao`, `Ladrilhos` | 2D Dungeon Asset Pack v5.2 | Chão, tijolos e portão de grade só das salas grandes (as de tamanho padrão são as prontas do Old Prison). Da folha `Objetos`: a chave da tranca e do baú, a gema do tiro dos inimigos, os ícones do minimapa e o de cada item passivo |
| `Efeitos/Poeira`, `ExplosaoPequena`, `Respingo`, `Chama` e `Personagens/Projeteis/MagiaVerde` | Tiny Swords Free Pack (Particle FX) e Tiny RPG Pack 01 v2.0 (magia do necromante) | Impacto e rastro das flechas especiais e dos tiros dos inimigos (seção 9) |
| `Masmorra/ChaveDourada` | 2D Dungeon Asset Pack v5.2 (items_animation) | Chave dourada girando que o chefe deixa (`Itens/ChaveDoChefe.cs`). Os baús são do Old Prison (`Masmorra/Prisao`) |
| `Masmorra/Temas` | 2D Pixel Dungeon Asset Pack v2.0 e 2D Dungeon Asset Pack v5.2 (ladrilhos recortados e juntados) | Chão e parede de cada tema de andar (`Andar/TemaDoAndar.cs`): `ChaoPorao`/`ParedePorao` (tijolos marrons do v2.0), `ChaoCripta`/`ParedeCripta` (laje rachada e friso azul do v5.2), `ChaoAbismo`/`ParedeAbismo` (pedra lisa e friso vermelho do v5.2) e a `Runa` vermelha do chão do Abismo. As Catacumbas usam o `Chao`/`Parede` padrão |
| `Masmorra/Prisao` | Epic RPG World – Old Prison (Rafael Matos) | Pedras das salas e dos tiros de pedra (`PedraAleatoria`, `Pedra`), miudezas de chão (`EnfeiteAleatorio`), baú de madeira e baú de pedra trancado abrindo (`Bau`), serra giratória e o trilho dela (`LaminaGiratoria`), barril e caixote da loja. Também: `Espinhos` (lanças no chão de sangue do tileset), `Vortice` (o alçapão), `PortaoDaCela` (a `Tranca`), `PortaoDeCaveira` e `PortaDeMadeira1/2` (porta de cima do chefe, da loja e do item: `Porta.UsarPortaoEspecial`), `Mesa` (tomba e quebra, `Sala/Mesa.cs`), `Tronco`/`TroncoEmPe`, `Alavanca` e `Engrenagem` (`Andar/TroncoRolante.cs`), `Sangue`, `Baratas`, `ChamaMagica` e `PoeiraDoFosso` (enfeites que mexem). Em `Pecas`, as peças soltas do atlas já recortadas, pivô no pé (`PecaDaPrisao`, `PecaSorteada`): túmulo do pedestal e do altar de sangue, mesa da loja, balcão, canecas, ouro e prata, donzela de ferro (sala amaldiçoada), estandarte (desafio), gaiola, guilhotina, pelourinho, caveiras, bolas de espinhos, baldes, sacos, velas, lança do Prego Enferrujado, retratos e correntes |
| `TinySwords` | Tiny Swords (Update 010) e Tiny Swords Free Pack, da Pixel Frog | Só efeitos sem par no Tiny RPG nem no Old Prison: dinamite (a bomba do jogador), explosão e poeira. Nenhum personagem nem cenário |

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
- Também têm som: o tiro do herói estourando, o pavio da bomba, o feitiço do necromante, o alçapão abrindo, o
  Prego Enferrujado, as curas e a morte do herói.

Os arquivos de som e música foram gerados pelos scripts de `Ferramentas/Audio` (síntese,
trechos do Freedoom e notas tocadas no soundfont; o `LEIAME.md` de lá explica como gerar
de novo). Os créditos estão no `CREDITOS-AUDIO.txt`.

---

## 11. Armas de fogo (o tiro do Gungeon)

Pasta `Assets/Scripts/Armas/`. O jogo continua um roguelike de salas, mas o tiro é de Gungeon: o
jogador **mira com o mouse em qualquer ângulo**, atira com o botão esquerdo, e a arma na mão tem
**pente que acaba e pede recarga** e **munição que acaba de vez**.

**Mira.** `Jogador/Entrada.cs` calcula `Mira` (do corpo até o cursor, na mesma conta de
`Camera.ScreenToWorldPoint`) e `Tiro` (a direção de quem está atirando). O mouse só retoma a mira
quando mexe ou clica; setas e analógico direito seguram a mira onde estiverem. O clique só atira se
começou com o jogo rodando (o clique que aperta "Jogar" no menu não vira tiro, porque o menu congela
`Time.timeScale`). `TopDown/MiraNaTela.cs` troca o cursor por uma mira de 32x32 (`ArteDasArmas.TexturaDaMira`)
e devolve o cursor dourado nos menus, na pausa e depois da morte.

| Arquivo | O que faz |
|---|---|
| `ArmaDeFogo.cs` | Os números de uma arma (dano por bala, tiros por segundo, balas por tiro, abertura, imprecisão, pente, recarga, munição, efeito) e o `CatalogoDeArmas` com todas |
| `ArsenalDoJogador.cs` | As armas que o jogador carrega: lugar 0 é a arma do herói (infinita, sem pente), lugar 1 em diante as de fogo. Pente, munição, recarga (R, ou sozinha quando o pente acaba) e troca de arma |
| `ArmaNaMao.cs` | A arma desenhada na mão: gira pra mira (e vira de ponta-cabeça pro outro lado), coiceia, solta clarão na boca do cano, mostra a barrinha de recarga e vira o corpo do herói pro lado da mira |
| `HudDasArmas.cs` | Painel no canto de baixo à direita: arma, pente (uma bala desenhada por tiro), munição que sobra e "Recarregando..." |
| `ArteDasArmas.cs` | A pixel art das armas, da bala, da caixa de munição, do vazio e da mira, desenhada em texto (uma letra por pixel) e transformada em sprite. Não precisa de imagem no projeto |
| `Vazio.cs` | O vazio: apaga os tiros inimigos da sala, empurra os inimigos acordados e protege o jogador por 0,6 s |

**Como o tiro sai.** Com a arma do herói na mão tudo é como era (lágrima, flecha, onda de corte).
Com arma de fogo, o `AtiradorTopDown` pergunta `ArsenalDoJogador.PodeDisparar`, solta as balas da boca
do cano (em linha reta com a mira, sem herdar a velocidade do jogador) e avisa `RegistrarDisparo`.
Cada bala é uma `Lagrima`, então parede, mesa que tomba, pedra, empurrão e atravessar funcionam igual. Os
itens passivos continuam valendo: o `EstatisticasDoJogador` calcula o quanto os itens subiram o dano, a
cadência e o alcance do herói (`DefinirFatores`) e a arma de fogo multiplica os números dela por isso.
Lágrimas extras viram balas extras em leque, e atravessar, perseguir, golpe pesado, explodir ao acertar
e a cor de bala dos itens valem pras balas também.

**Efeito da arma.** `ArmaDeFogo.efeito` usa os mesmos efeitos das flechas especiais
(`Projeteis/EfeitoDaFlecha.cs`): `Explosiva`, `Gelo`, `Venenosa` e `Ricochete`. Só o efeito vale; os
multiplicadores de dano e cadência da flecha não.

**De onde vêm as armas.** Toda partida começa com a **Pistola** (`ArmaDeFogo.inicial`: munição infinita,
pente de 10) na mão, e a arma do herói fica no lugar 0 (`Q` chega nela). As outras viram itens
(`ItemPassivo.arma`, uma por arma do catálogo, menos a inicial) e saem de baú, loja e pedestal como
qualquer item; pegar uma já passa pra ela. Pegar uma repetida enche metade da munição dela. O
`Herois.Aplicar` chama `ArsenalDoJogador.ComecarPartida`, que volta o arsenal pro começo.

**Munição e vazio.** `TipoDeColetavel.Municao` (caixa: enche 35% da munição máxima de cada arma de fogo,
e só é pega se alguma precisar) e `TipoDeColetavel.Vazio` caem dos inimigos pela `TabelaDeDrops`. A
caixa só entra na roda quando o jogador tem arma de munição limitada, e com mais chance quando ela está
baixa. O jogador começa com 2 vazios (`Inventario`, tecla `F`); a HUD mostra o contador embaixo das
bombas. O salvamento guarda os vazios e as armas voltam junto com os itens, com pente e munição cheios.

**Teclas de arma.** `Q` (e `LT`) anda pelas flechas do herói e depois pelas armas; a roda do mouse e `1` a
`9` escolhem direto; `R` recarrega. Trocar de arma cancela a recarga. `Tab` é do minimapa.

**Acrescentar uma arma.** Uma entrada nova em `CatalogoDeArmas.Montar` (e, se quiser um desenho novo, um
`EstiloDeArma` com as linhas dele em `ArteDasArmas`). Ela vira item sozinha. Os números pra equilibrar são
a munição máxima e o dano por bala; o resto é sensação (`recuo`, `tremor`, `som`).

---

## 12. Bullet hell (os padrões de bala dos inimigos)

O outro lado do Gungeon: em vez de um tiro solto de vez em quando, os inimigos soltam **padrões**: anéis,
leques, espirais, cortinas com um buraco pra passar. A resposta é desviar, rolar (dash) ou gastar um vazio.
Quase tudo fica em `Sala/` e `Andar/`.

| Arquivo | O que faz |
|---|---|
| `Sala/PadroesDeBala.cs` | Os desenhos que saem de uma vez: `Anel`, `AnelComBuraco`, `Leque`, `Cortina`, `Rosa`. Respeita o teto de 160 balas inimigas no ar |
| `Sala/AtiradorDePadroes.cs` | Componente que faz um inimigo soltar um ciclo de `AtaqueDeBalas`. Cuida do aviso (um anel claro que fecha em volta do inimigo) e dos padrões no tempo (`Rajada`, `Espiral`). Funciona **sozinho** (conta o próprio intervalo, só com o inimigo livre, sem atirar no meio de um golpe) ou **chamado** (o inimigo manda, como a sentinela que já avisava incha) |
| `Andar/PerfilDeBalas.cs` | A tabela de quem atira o quê e como isso fica mais denso a cada fase. O `Andar.Fortalecer` chama `Equipar` em todo inimigo comum |
| `Sala/TiroDaSala.cs` | Conta as balas inimigas (`NoAr`), apaga todas (`LimparDosInimigos`) e dá à bala de padrão a cara própria (abaixo) |

**Quem atira.** Os quatro atiradores de antes (bruxo, sentinela, fogo-fátuo, demônia) continuam mandando no
aviso e no andar deles, mas agora soltam o próximo padrão do ciclo em vez do leque ou da cruz fixos. Alguns
bichos que só perseguiam ganharam um ataque no ritmo deles, e os outros ficaram como eram: nem toda sala é de
tiro. Todo campeão ganha um padrão da cor dele (vermelho: anel; amarelo: rajada; roxo: espiral).

| Mundo | Quem atira | Padrões |
|---|---|---|
| 1 Porão | orc, gosma, bolha | rajada de 2 mirada; anel de 6; anel de 6 |
| 2 Catacumbas | cavaleiro da lança, orc de elite, cavaleiro do canhão | rajada; leque de 5; anel de 8, cruz e espiral |
| 3 Cripta | bruxo, fogo-fátuo | leque, anel com buraco, rajada; cruz, anel com buraco, espiral |
| 4 Abismo | demônio, demônia, demônia da foice | leque; leque, cortina, rosa; espiral |

Espiral, cortina e rosa só do mundo 2 em diante (`DificuldadeDaFase.PadroesComplicados`); o primeiro mundo só tem
anel, leque e rajada.

**Dificuldade.** `DificuldadeDaFase` guarda três números: `DensidadeDeBalas` (+7% por fase, no máximo 1,6x: mais
que isso o anel fecha e não sobra buraco), `PressaDasBalas` (+1,5% por fase: bala lenta é o que deixa o padrão
legível) e `RitmoDeTiro` (+3% por fase, e o intervalo nunca passa de 1,8 s pra baixo). O dano da bala continua
o do jogo (`Vida.MultiplicadorDeDanoRecebido`: meio coração, e coração inteiro no mundo 4).

**Legibilidade.** A bala de padrão não usa o estilo de quem atirou (a bala de ferro da sentinela e as chamas escuras
sumiam no chão escuro): tem sempre miolo claro, contorno escuro e um brilho na cor dela. Ao bater na parede ou ser
apagada, estoura em poeira. Quando a sala é vencida (`Sala.Limpar`) todas as balas inimigas no ar somem.

**Acrescentar.** Um padrão novo: uma entrada em `PadraoDeBala`, o `case` em `AtiradorDePadroes.Disparar` e, se sair
de uma vez, uma função em `PadroesDeBala`. Dar tiro a uma espécie: um `case` em `PerfilDeBalas.CicloDoBicho`.

**Os chefes.** Cada chefe já tinha anéis, leques, espirais, muralhas e ondas, com aviso e duas fases (o Olho,
três), e isso continua como era. Cada um ganhou **mais um ataque**, `Extra`, que usa a biblioteca de padrões e
fica mais denso na segunda fase. A fila de padrões de cada um está em `PerfilDeBalas.ExtraDoChefe`; cada
chefe só liga o `Chefe/ExtraDoChefe.cs` (criar no `Awake`, avisar, `Soltar`, esperar `Ocupado` acabar).

| Chefe | Ataque extra (primeira fase; na segunda fica mais cheio) |
|---|---|
| Golem de Magma | Florescer: uma rosa de tiros (duas, encaixadas nos vãos) |
| Demônio do Martelo | Muralha de brasa: cortinas largas com buraco, indo pro jogador (três) |
| Rei Necromante | Maldição girando: espiral de 3 fios (4, e fecha com um anel com buraco) |
| Minotauro Furioso | Ondas duplas: dois anéis com buraco, cada um no seu lugar (três) |
| Lobisomem Alfa | Uivo da lua: espiral rápida de 2 fios (3, e uma rajada mirada no fim) |
| Senhor da Guerra | Chuva de lanças: 3 leques seguidos de 5, cada um mirado de novo (4 de 7) |
| Olho do Abismo | Desabrochar: uma flor, um anel com buraco e outra flor encaixada (e uma espiral junto) |

As balas dos chefes (e a de todo inimigo com estilo próprio) ganharam o brilho de legibilidade; as de padrão de
chefe passam do teto dos bichos comuns (mais 80 balas), pra o chefe nunca ficar sem padrão por causa dos lacaios.
O `Andar` passa a dificuldade da fase ao chefe (`AtiradorDePadroes.Dificuldade`), que escala a densidade.
