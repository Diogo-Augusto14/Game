# Arquitetura do jogo

Como as peças se encaixam, o que cada script faz e onde mexer quando quiser ajustar algo.
O `MANUAL.md` continua sendo o caderno de estudo das peças da Unity; este arquivo é sobre
o **código deste jogo**.

---

## 0. Cena top-down (estilo Isaac)

O jogo está virando um roguelike visto de cima. A cena nova é `Assets/Scenes/TopDown.unity`:
abra e dê Play. O único objeto dela é o `BootstrapTopDown`, que monta tudo por código — sala
13 × 7 com paredes e pedras, o jogador, três bonecos de treino, a câmera parada e a HUD.
Estando na cena, ele impede o `Bootstrap` do plataforma de se instalar.

| Tecla | Ação |
|---|---|
| `W A S D` | Andar em 8 direções |
| `Setas` | Atirar lágrima (só as 4 retas; vale a última seta apertada) |

Andar e atirar são independentes: dá pra fugir pra um lado atirando pro outro.

| Script | O que faz |
|---|---|
| `TopDown/MovimentoTopDown.cs` | Andar com aceleração/freio, gravidade zero, recebe empurrão do `Vida` |
| `TopDown/AtiradorTopDown.cs` | Dano, alcance, cadência, velocidade da lágrima; alterna olho esquerdo/direito |
| `TopDown/Lagrima.cs` | O projétil: voa até o alcance, bate em `IDanificavel` ou parede e estoura |
| `TopDown/AlvoDeTreino.cs` | Saco de pancada com barra de vida; morre e volta |
| `TopDown/BootstrapTopDown.cs` | Monta a cena |
| `TopDown/FormasTopDown.cs` | Quadrado e círculo gerados por código (até ter arte) |

Reaproveitado do plataforma, sem cópia: `Vida` / `DanoInfo` / `IDanificavel` (dano),
`Entrada` (com `Modo Top Down` ligado, lê WASD e setas separados) e `Hud`.

O código do plataforma continua inteiro em `Assets/Scripts/Plataforma/`, e a
`SampleScene` funciona como antes.

---

## 1. Só dar Play

Não precisa preparar nada. Ao entrar no Play:

1. `ConfiguradorDoProjeto` (Editor) confere se a biblioteca de animações existe. Se não
   existir, ele gera na hora.
2. `Bootstrap` se instala sozinho na cena (`RuntimeInitializeOnLoadMethod`) e monta:
   - o **boneco completo** (corpo, sensores, hitbox, movimento, combate, cura, animação);
   - os **inimigos completos** (nas posições dos inimigos que já estavam na cena, e mais
     alguns até chegar no mínimo configurado);
   - as **peças de fase que faltam** pra testar cada mecânica (túnel de escorregar,
     plataformas, bloco de beirada, corredor de wall jump, escada);
   - a **câmera** que segue o boneco, com limites da fase;
   - a **HUD** (vida, frascos, lista de controles).

O boneco e os inimigos que já estavam na cena são **desativados durante o jogo** (o arquivo
da cena não é alterado). O motivo: quando o Bootstrap entra em ação, o `Awake` dos
componentes antigos já rodou com referências resolvidas pela metade — montar do zero, na
ordem certa de dependência, é o que garante que toda mecânica funcione no primeiro Play.

Para desligar qualquer parte disso, crie um objeto vazio na cena, adicione o componente
`Bootstrap` e desmarque o que não quiser.

### Menus de editor

| Menu | O que faz |
|---|---|
| `Tools ▸ Jogo ▸ Preparar projeto` | Cria camadas/tags, arruma a matriz de colisão 2D e gera a biblioteca |
| `Tools ▸ Jogo ▸ Reconstruir animações` | Regera só a biblioteca (depois de mexer na tabela de receitas) |
| `Tools ▸ Jogo ▸ Conferir a cena` | Lista o que está faltando na cena aberta |
| `Tools ▸ Jogo ▸ Montar na cena ▸ Kit completo` | Passa pro modo manual: monta tudo na cena, salva prefabs e desliga o Bootstrap |
| `Tools ▸ Jogo ▸ Montar na cena ▸ Só o jogador / inimigo / peças` | Monta uma parte só |
| `Tools ▸ Jogo ▸ Montar na cena ▸ Desligar o Bootstrap desta cena` | Só a chave mestra, sem montar nada |

---

## 1.1 Modo manual — montar a fase à mão

O automático é para o jogo rodar sem ninguém preparar a cena. Quando você quiser tomar
conta da cena, rode **`Tools ▸ Jogo ▸ Montar na cena ▸ Kit completo`**. Ele:

- monta **Jogador** e **Inimigo** completos na cena, como objetos de verdade;
- salva os dois como **prefab** em `Assets/Prefabs/` (a instância da cena fica ligada ao prefab);
- monta uma **paleta de blocos**, um de cada tipo, lado a lado, para você duplicar e esticar;
- ajusta a câmera, cria a HUD e **desliga o Bootstrap** na cena.

A paleta (a cor indica o comportamento — família cinza pisa em cima, família azul é parede):

| Peça | Camada | Serve para |
|---|---|---|
| `Chao` | chão | chão normal |
| `Plataforma` | chão | igual, só mais fina |
| `Tunel` | chão | teto baixo; deixe ~0,35 de vão para só passar escorregando |
| `Parede` | parede | wall slide e wall jump |
| `Beirada` | parede | pendurar na quina de cima e subir |
| `Escada` | (trigger) | subir com `Cima` |

**Como esticar:** selecione o bloco e use a ferramenta de retângulo do SpriteRenderer. O
componente `BlocoDoCenario` copia o tamanho do desenho para o BoxCollider2D a cada quadro
**fora do Play** — então desenho e colisão nunca ficam de tamanhos diferentes, que é o erro
mais chato de montar fase à mão. O desenho usa modo *Tiled*: a textura repete em vez de
esticar, como pixel art deve se comportar.

O desenho dos blocos é `Assets/Sprites/bloco.png`, gerado na primeira vez que você roda o
comando. Troque esse arquivo pelo seu tileset e todos os blocos mudam de cara juntos.

### Desligar o Bootstrap — três níveis

1. **Por parte:** desmarque só o que você já montou (`Montar Jogador`, `Montar Inimigos`,
   `Completar Fase`, `Montar Hud`, `Ajustar Camera`, `Ajustar Gravidade`).
2. **Chave mestra:** desmarque `Ativo`. Ele não faz absolutamente nada — mas **não apague o
   objeto**: é a presença dele na cena que impede o automático de instalar outro.
3. **No projeto inteiro:** adicione o símbolo `JOGO_SEM_BOOTSTRAP` em
   `Project Settings ▸ Player ▸ Scripting Define Symbols`. Aí a auto-instalação nem existe,
   e o Bootstrap só roda se você colocar o componente na cena.

### Montando um jogador do zero, sem o comando

Se preferir montar na mão, a ordem importa — o `Vida` procura o `Movimento` no próprio
`Awake` e guarda a referência uma vez só. No objeto raiz, nesta ordem:
`Rigidbody2D` → `BoxCollider2D` → `Entrada` → `Movimento` → `Vida` → `Ataque` → `Cura` →
`EfeitoDeImpacto` → `AnimacaoDoJogador` → `Player`.

Mais os filhos, com estes nomes exatos: `GroundCheck`, `WallCheck`, `LedgeCheck` (vazios),
`Hitbox` (BoxCollider2D com `Is Trigger`, desligado, + `Espada`, camada `Golpe`) e `Visual`
(`SpriteRenderer` + `AnimadorDeSprites`). No `Vida`, desmarque `Destruir Ao Morrer` — senão
o boneco é apagado da cena em vez de renascer. Tag `Player`, camada `Player`.

A receita exata está em [Construtor.cs](Assets/Scripts/Plataforma/Bootstrap/Construtor.cs) — é o mesmo
arquivo que o Bootstrap e o comando de editor usam, então ele nunca fica desatualizado em
relação ao que funciona no Play.

---

## 2. Controles

| Tecla | Ação |
|---|---|
| `A` / `D` (ou setas) | Andar / correr |
| `Ctrl` (segurar) | Andar devagar em vez de correr |
| `Espaço` | Pular · aperte de novo no ar = pulo duplo · encostado na parede = wall jump |
| `Espaço` (soltar cedo) | Corta a subida (pulinho × pulão) |
| `Shift` | Dash (no chão e no ar) |
| `Shift` + `Baixo` | Escorregar (passa por baixo do túnel) |
| `Shift` + direção contrária | Esquiva pra trás |
| `J` ou botão esquerdo do mouse | Golpe — combo de 3 no chão, de 2 no ar |
| `J` no meio do dash | Golpe de dash |
| `J` no ar + `Baixo` | Mergulho (ataque descendo) |
| `Baixo` | Agachar |
| `Cima` | Entrar na escada · subir da beirada |
| `Baixo` na escada / na beirada | Descer / soltar |
| `Shift` + `Baixo` na escada | Escorregar pela escada |
| `E` (segurar) | Curar (gasta um frasco; qualquer coisa cancela) |

---

## 3. As camadas do código

O projeto segue uma regra: **cada script tem um assunto só**. Movimento não anima, animação
não decide física, vida não conhece o boneco.

```
Entrada          lê teclado e GUARDA os apertos por um instante (buffer)
   ↓
Movimento        máquina de estados: anda, pula, dash, agacha, parede, beirada, escada
   ↓ (só leitura)
AnimacaoDoJogador   traduz estado → nome de clipe, por prioridade
   ↓
AnimadorDeSprites   toca o clipe quadro a quadro no SpriteRenderer
   ↑
BibliotecaDeAnimacoes   asset com todos os clipes (textura + retângulos + pivô + fps)
```

```
Espada (hitbox)  →  IDanificavel.TomarDano(DanoInfo)  →  Vida
                                                          ↓
                                    IControladorDeMovimento.AplicarImpulsoExterno
                                                          ↓
                                                       Movimento (atordoado + empurrão)
```

### Arquivos

| Pasta | Arquivo | Assunto |
|---|---|---|
| `Nucleo` | `Cronometro.cs` | Contador regressivo (`Armar`, `Ativo`, `Consumir`) |
| | `Camadas.cs` | Nomes de camada com apelidos e cache de máscara |
| `Animacao` | `ClipeDeSprites.cs` | Um clipe: textura + retângulos + pivô + fps + loop |
| | `BibliotecaDeAnimacoes.cs` | Catálogo de clipes (asset em `Resources`) |
| | `AnimadorDeSprites.cs` | Toca clipes; substitui o Animator Controller |
| | `NomesDeAnimacao.cs` | Constantes com os nomes dos clipes |
| | `EfeitoAnimado.cs` | Faísca: toca um clipe e se desliga (usado em pool) |
| `Jogador` | `Entrada.cs` | Teclas e buffers de aperto |
| | `AnimacaoDoJogador.cs` | Estado → clipe, com prioridade e trava curta |
| (raiz) | `Movimento.cs` | A máquina de estados de plataforma |
| | `Player.cs` | Morte, renascimento, checkpoint, buraco |
| | `Cura.cs` | Frascos estilo Hollow Knight |
| | `Cameramov.cs` | Câmera com zona morta, antecipação, limites e tremida |
| | `Inimigo.cs` | IA completa (patrulha, alerta, perseguir, golpear, recuperar) |
| `Inimigos` | `AnimacaoDoInimigo.cs` | Estado do inimigo → clipe |
| `ataques` | `Ataque.cs` | Combos, janelas de hitbox, mergulho |
| | `Espada.cs` | Hitbox permanente, um acerto por alvo por golpe |
| `combate` | `DanoInfo.cs` | O que um golpe carrega (quanto, de onde, com que peso) |
| | `Vida.cs` | Vida, defesa, invencibilidade, empurrão, flash, morte |
| | `EfeitoDeImpacto.cs` | Pool de faíscas no ponto do impacto |
| | `IDanificavel.cs` / `IControladorDeMovimento.cs` | Os dois contratos do sistema de dano |
| `Mundo` | `Escada.cs` | Zona de escada (trigger) |
| | `BlocoDoCenario.cs` | Colisor acompanha o tamanho do sprite ao esticar |
| `UI` | `Hud.cs` | Barra de vida, frascos e controles, montados por código |
| `Bootstrap` | `Construtor.cs` | A receita de como montar jogador, inimigo e blocos |
| | `Bootstrap.cs` | Monta o jogo ao apertar Play (e a chave pra desligar) |
| `Editor` | `ConstrutorDeAnimacoes.cs` | Lê as folhas de sprites e gera a biblioteca |
| | `ConfiguradorDoProjeto.cs` | Camadas, tags, matriz de colisão, automação |
| | `MontadorDeCena.cs` | Monta o kit na cena e salva os prefabs (modo manual) |

---

## 4. Por que não tem Animator Controller

Com ~45 estados, a máquina de estados visual do Animator vira um novelo de transições — e
quem manda de verdade é o código, que já sabe em que estado está. O `AnimadorDeSprites`
pede o clipe pelo nome e pronto: sem parâmetro faltando, sem transição com tempo errado,
sem clipe que não dispara. De graça ainda vêm três coisas que o Animator não dá fácil:

- saber o **quadro exato** (a hitbox abre numa fração do clipe, não num tempo digitado);
- saber o **progresso de 0 a 1** (o combo emenda a partir de 55% do golpe);
- **esticar o clipe** pra caber na duração de um estado (a subida da beirada, o golpe do
  inimigo).

Se um `Animator` estiver no mesmo objeto, o `AnimadorDeSprites` o desliga no `Awake` — os
dois escrevem em `SpriteRenderer.sprite` e brigariam a cada quadro.

---

## 5. Como as animações são geradas

`ConstrutorDeAnimacoes` tem uma **tabela de receitas**: uma linha por clipe, com o caminho
da folha, a grade (colunas × linhas), o fps, se é loop e qual faixa de quadros usar.

```csharp
new Receita(NomesDeAnimacao.Correr, PLAYER + "run/sprite sheets/run.png", 4, 5, 20f, true),
//          nome do clipe           folha                             col lin fps  loop
```

Para cada receita o construtor:

1. **recorta por célula** (grade explícita) em vez de usar o corte automático da Unity —
   corte automático deixa cada quadro com um tamanho diferente e o boneco tremelica;
2. **descarta células vazias** (a última linha das folhas quase sempre é incompleta);
3. **calcula o pivô nos pés**: varre os pixels, acha a linha mais baixa com desenho e o
   centro horizontal da base do corpo. É isso que impede o boneco de "escorregar" ao trocar
   de animação — a folha de ataque tem 160 px de largura, a de parado tem 46;
4. guarda no asset **textura + retângulo + pivô**. Os `Sprite` são criados em tempo de
   execução, então **os cortes que você já tem nas folhas não são alterados**.

### Ajustar uma animação

- **Velocidade ou loop de um clipe:** abra `Assets/Resources/BibliotecaDeAnimacoes.asset`
  no Inspector e mexa em `Quadros Por Segundo` / `Em Loop`. Vale na hora.
- **Faixa de quadros, grade ou trocar a folha:** mexa na linha da tabela em
  `ConstrutorDeAnimacoes.cs` e rode `Tools ▸ Jogo ▸ Reconstruir animações`.
- **Trocar a arte do inimigo:** as receitas `inimigo_*` apontam para
  `LightBandit.png`. Trocando o caminho para `HeavyBandit.png` (mesma grade) o inimigo
  inteiro muda de aparência.

> Duas animações não existiam na pasta de arte e foram montadas a partir do que existe:
> `pular` e `cair` são as duas metades da folha `jump.png` (24 quadros), e `morrer` usa a
> folha `hard hit.png` inteira (o fim dela é o boneco no chão). Se aparecerem folhas
> próprias pra isso, é uma linha na tabela.

---

## 6. Ajustes mais comuns

| Quero mudar | Onde |
|---|---|
| Velocidade, pulo, gravidade, dash | Componente `Movimento` no boneco (ou os padrões no arquivo) |
| Dano, alcance e janela de cada golpe | Componente `Ataque` → listas `Combo No Chao` / `No Ar` / `De Dash` |
| Vida, defesa, invencibilidade, empurrão | Componente `Vida` |
| Quantos frascos e quanto curam | Componente `Cura` |
| Visão, patrulha, telegrafo e combo do inimigo | Componente `Inimigo` |
| Zoom da câmera, zona morta, limites | `Bootstrap ▸ Tamanho Da Camera` e o componente `Cameramov` |
| Quantidade de inimigos / peças de fase | Componente `Bootstrap` |

Os valores que o Bootstrap usa ao montar (vida 120 do boneco, vida 55 do inimigo, zoom da
câmera) estão em `Bootstrap.cs` — são os números do "jogo pronto pra rodar", não um limite.

---

## 7. Sala estilo Isaac (visão de cima)

Pasta `Assets/Scripts/Sala/`. Independe do código de plataforma: nada aqui usa gravidade,
o `Movimento` ou o `Bootstrap`.

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

## 8. Arte importada (pacotes)

Imagens de pacote ficam em `Assets/Arte/Resources/` e são lidas por
`Arte/ArteImportada.cs` (sem fatiar no Sprite Editor: os recortes estão no código). O
import de cada PNG já vem no `.meta`: Sprite, filtro Point, sem compressão, sem mipmap.

| Pasta | Pacote | Onde aparece |
|---|---|---|
| `Personagens/Demonio`, `Personagens/MonstroDeSangue` | Tiny RPG Character Asset Pack 02 (versão com sombra) | Inimigos `Demonio` e `MonstroDeSangue`, animados por `Animacao/AnimacaoDePersonagem.cs` |
| `InterfacePixel` | Pixel UI pack 3 | Corações da vida, barra do chefe, painéis do menu/pausa/fim de jogo, placa de preço da loja |

Cada tira de personagem tem quadros de 100×100 com o bicho (uns 20 px) no meio. O
recorte usado é 64×48 em volta do corpo, com o pivô no centro do corpo.
Se uma imagem sumir, `ArteImportada` devolve null e cada tela volta ao desenho antigo.
