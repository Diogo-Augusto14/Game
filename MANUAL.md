# MANUAL DAS PEÇAS

Tutorial vivo do Diogo. Cada peça que aparece nas aulas ganha uma entrada aqui,
no formato: **o que é → pra que serve → regras e pegadinhas → onde tá no meu código**.
Bateu dúvida em casa? Procura aqui antes de procurar em outro lugar.

*(alimentado pelo Claude a cada peça ensinada — se faltar alguma, cobrar)*

---

## Fundamentos (a Fase 1 inteira, registrada depois que o manual nasceu)

### O esqueleto de todo script — MonoBehaviour e Update
- **O que é:** `public class X : MonoBehaviour` é o casco que transforma uma
  classe C# em COMPONENTE pendurável num GameObject. Script solto na pasta não
  roda: só executa **pendurado** num objeto da cena.
- **`void Update()`:** eu nunca chamo — **a Unity chama**, uma vez POR QUADRO
  (~60×/segundo). Tudo que é "escutar tecla / mexer todo instante" mora nele.
  Família de eventos: `Update` (todo quadro), `OnCollisionEnter2D` (na trombada) —
  a Unity liga pra eles, nunca o contrário.
- **Pegadinha:** o NOME do arquivo tem que ser igual ao da classe
  (`Movimento.cs` ↔ `class Movimento`), senão a Unity recusa pendurar.

### Campos `public`/`private` e o Inspector
- **`public` no topo da classe** = caixinha visível no Inspector (números
  ajustáveis com o jogo rodando, arrastões de objeto) E porta aberta pra
  outros scripts lerem. **`private`** = gaveta só do dono: nem Inspector,
  nem vizinho enxergam.
- **`float` pede o `f`:** `5f`, `0.5f` — sem o f o número é `double` e o
  compilador reclama da mistura.
- **PEGADINHA GRANDE:** depois que um campo `public` aparece no Inspector,
  o valor DO INSPECTOR vence o do código. Mudar `velocidade = 5f` pra `8f`
  no código NÃO muda o que o Inspector já gravou — ajustar lá, ou
  Reset no componente.

### Rigidbody2D e `rb.linearVelocity` — o motor físico
- **O que é:** o componente que entrega o corpo ao motor de física (gravidade,
  inércia, colisão). `linearVelocity` é a caixa da velocidade: um `Vector2`
  (x = lado, y = vertical). Em versões antigas da Unity chama só `velocity`.
- **A receita das 3 irmãs (preservar o eixo alheio):** mexer num eixo SEM
  apagar o outro — o eixo que não é meu volta como está:
  andar: `new Vector2(velocidade * lado, rb.linearVelocity.y)` (linha 19)
  pular: `new Vector2(rb.linearVelocity.x, forcaPulo)` (linha 22)
  cortar: `new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f)` (linha 28)
- **Coleira sempre:** `linearVelocity` mora no rb → `rb.linearVelocity`,
  nunca solto (CS0103).

### `transform.localScale` — tamanho e ESPELHO
- **O que é:** a escala do objeto (Vector3). Números maiores = maior;
  **x NEGATIVO espelha o sprite** — o flip de direção é
  `direcao * escalaBase` com direcao valendo +1 ou -1 (linha 52).
- **Ternário `? :`** — if-else de uma linha:
  `float escalaY = pequeno ? escalaAgachado : escalaBase;`
  (lê-se: pequeno? então agachado, senão base).

### Prefab — o molde azul (a peça 2, feita no editor)
- **O que é:** um GameObject salvo como MOLDE na pasta Assets. Receita:
  montar o objeto na cena (componentes, script) → **arrastar da Hierarchy
  pra pasta Assets** → ícone fica AZUL = prefab → apagar o da cena.
- **Pra que serve:** o `Instantiate` carimba cópias dele; cada cópia nasce
  na cena com nome `(Clone)`.
- **Ligação com o código:** arrastar o prefab na caixinha `public GameObject`
  do Inspector — mesmo gesto do `rb`.

### `Debug.Log("...")` — a prova no Console
- Imprime no Console da Unity (aba Console, em casa). É o `print()` de lá:
  prova que um caminho do código rodou. O do stub não mostra nada — no
  trabalho ele só compila.

---

## Unity / C#

### `Input.GetButtonDown("Jump")` — a campainha de APELIDO
*(03/09/2026 — usada em `aula_8/Assets/Scripts/Movimento.cs`, linha 19)*

- **O que é:** a Unity tem uma lista de botões batizados com apelido em
  Edit → Project Settings → **Input Manager**: "Jump", "Horizontal", "Fire1"...
  Cada apelido aponta pra teclas reais ("Jump" → espaço, "Horizontal" → A/D e setas).
  `GetButtonDown` pergunta: *"o apelido tal foi apertado NESTE quadro?"*
- **Pra que serve:** desacoplar a ação da tecla. Quiser o pulo no W, ou num controle
  de Xbox? Muda **na lista** do Input Manager, sem tocar no código.
- **Regras / pegadinhas:**
  - Entre as aspas só entra **apelido que existe na lista**. `"s"` não é apelido
    de nada → a Unity **não dá erro, só fica muda pra sempre**. Erro silencioso.
  - Pra perguntar por uma **tecla crua**, sem apelido, a porta é outra:
    `Input.GetKeyDown(KeyCode.S)`.
- **A família** (vale pros dois mundos, Button e Key):
  | forma | pergunta | dispara |
  |---|---|---|
  | `...Down` | apertou AGORA, neste quadro? | 1 vez por aperto |
  | sem sufixo (`GetButton` / `GetKey`) | está SEGURANDO? | todo quadro enquanto segura |
  | `...Up` | soltou AGORA? | 1 vez, no soltar |
- **No meu código:** linha 19 usa apelido (`GetButtonDown("Jump")` no pulo);
  linhas 29 e 33 usam tecla crua (`GetKeyDown`/`GetKeyUp` com `KeyCode.S` no agachar).

### A lista de apelidos de fábrica (Input Manager)
*(03/09/2026 — a lista que faltou na peça do Jump)*

**CAMPAINHAS** — respondem sim/não → família `GetButton/Down/Up`:
| apelido | tecla de fábrica |
|---|---|
| `"Jump"` | Espaço |
| `"Fire1"` | Ctrl esq. *(+ clique esquerdo)* |
| `"Fire2"` | Alt esq. *(+ clique direito)* |
| `"Fire3"` | Shift esq. *(+ clique do meio)* |
| `"Submit"` | Enter |
| `"Cancel"` | Esc |

**RÉGUAS** — respondem um NÚMERO → família `GetAxis/GetAxisRaw`:
| apelido | o que mede |
|---|---|
| `"Horizontal"` | A/D e ←→ (-1, 0, +1 no Raw) |
| `"Vertical"` | W/S e ↑↓ (idem) |
| `"Mouse X"` / `"Mouse Y"` | movimento do mouse |
| `"Mouse ScrollWheel"` | a rodinha |

- **Apelido em string é senha exata que o compilador NÃO CONFERE:** `"fire1"`
  compila feliz e explode em casa rodando ("Input Button fire1 is not setup").
  Os 3 tipos de erro: compilação (grita no build), silencioso (nome de evento
  errado — nunca chama), e de STRING (só explode rodando). Conferir maiúsculas
  na tabela acima.
- **Receita de COMBO (ex.: deslizada = S + Dash):** dois `Down` juntos exigem
  os dois apertos no MESMO quadro (1/60s) — dedo humano não acerta. O
  modificador fica SEGURADO (`GetKey`, sem sufixo) e só o gatilho usa `Down`:
  `if (Input.GetButtonDown("Dash") && Input.GetKey(KeyCode.S))`.
- **Onde ver em casa:** Edit → Project Settings → **Input Manager** → abrir "Axes".
  Cada apelido com suas teclas, duplicado pra controle de videogame.
- **A lista é minha:** dá pra criar apelido novo lá (um `"Dash"` no futuro).
- **Campainha ≠ régua:** régua se lê com `GetAxisRaw` (minha linha 17),
  não com `GetButtonDown`.

### `OnCollisionEnter2D(Collision2D colisao)` — o alarme de trombada
*(03/09/2026 — usada em `aula_8/Assets/Scripts/Movimento.cs`, linhas 40-46)*

- **O que é:** método de **evento**, família do `Update`: eu nunca chamo —
  **a Unity chama por mim**, no quadro em que o collider deste objeto ENCOSTA
  em outro collider. A família completa:
  | método | a Unity chama quando | frequência |
  |---|---|---|
  | `OnCollisionEnter2D` | encostou AGORA | 1 vez, no toque |
  | `OnCollisionStay2D` | continua encostado | todo quadro do contato |
  | `OnCollisionExit2D` | desencostou AGORA | 1 vez, no soltar |
  (mesmo trio Down / segurando / Up das teclas, só que pra toques físicos)
- **O `Collision2D colisao`:** o **boletim de ocorrência** da trombada — a Unity
  preenche e entrega: quem encostou (`colisao.gameObject`), onde, com que força.
  Regra de sempre: assinatura DECLARA tipo + apelido, corpo USA o apelido
  (o apelido é meu: usei `collision`, valia qualquer nome).
- **Pra que serve:** reagir ao mundo físico — pisar no chão (meu ground check),
  tomar dano em espinho, quebrar caixa.
- **Regras / pegadinhas:**
  - O nome é **senha exata**, com o **2D no fim** (a versão sem 2D é do mundo 3D).
    Errou uma letra? **Sem erro nenhum — a Unity só nunca chama.** Erro silencioso,
    o mesmo veneno do `"s"` no GetButtonDown.
  - Só dispara se os DOIS objetos têm collider E pelo menos um tem **Rigidbody2D**
    (normalmente o que se mexe).
  - `CompareTag("Ground")`: Tag é a etiqueta colada no objeto no topo do Inspector.
    Precisa **existir na lista E estar colada no chão** — chão sem etiqueta =
    o alarme toca mas o `if` ignora.
- **No meu código:** usei só o Enter e desliguei `estaNoChao` direto no pulo
  (linha 22) — menos código, mata o pulo infinito. **Furo conhecido pra testar:**
  cair da beirada SEM pular deixa `estaNoChao` ligado → dá um pulo no ar durante
  a queda. É o buraco que o `OnCollisionExit2D` taparia.

### `rb.linearVelocity` também se LÊ — pulo com altura controlada
*(03/09/2026 — peça do toque=pulinho / segurar=pulão, em `Movimento.cs`)*

- **O que é:** até aqui eu só ESCREVIA na velocidade. Mas ela se lê como qualquer
  número, e conta a história do boneco a cada quadro:
  - `rb.linearVelocity.y > 0` → subindo
  - `rb.linearVelocity.y < 0` → caindo
- **Pra que serve:** o pulo de metroidvania — no instante em que o botão é SOLTO
  (`Input.GetButtonUp("Jump")`, o Up da família), se ainda estiver subindo,
  **encolher** a velocidade de subida (multiplicar por uma fração tipo `0.5f`).
  A subida perde força, o pulo morre mais baixo, a gravidade faz o resto.
  Soltou cedo = pulinho; segurou = pulão.
- **Regras / pegadinhas:**
  - O guarda `> 0` é obrigatório: sem ele o corte dispara também na QUEDA, e
    fração de número negativo = queda mais lenta = boneco-pena flutuando.
  - A escrita continua preservando o eixo alheio (x fica como está) — terceira
    irmã das linhas 18 e 21.
  - Fração no código é número mágico — campo `public float` bota ela no Inspector.

### Eixos: x é pro LADO, y é pra CIMA
*(03/09/2026 — dúvida minha respondida)*

- **x = horizontal** (deitado: esquerda/direita) · **y = vertical** (em pé: cima/baixo).
  Mesmo desenho do gráfico da escola.
- **`Vector2(x, y)`: a ordem é sempre primeiro o lado, depois a altura.**
- **No meu código:** linha 18, o andar preenche a 1ª casinha (`velocidade * lado`, x);
  linha 21, o pulo preenche a 2ª (`forcaPulo`, y).

### `transform.localPosition` — propriedade é CAIXA, não função
*(03/09/2026 — erro CS1955 no dash v1)*

- **O que é:** a posição do objeto guardada numa caixa (propriedade), como
  `rb.linearVelocity` e `transform.localScale`. Caixa NÃO se chama com
  parênteses — se **atribui** com `=` e carimbo `new Vector3(x, y, z)`:
  errado: `transform.localPosition(x, y, 0);` → CS1955 "membro não invocável"
  certo: o mesmo formato da minha linha 51 (`localScale = new Vector3(...)`).
- **Pra que serve:** teleporte/reposicionamento direto — meu dash v1 soma
  `distanciaDash` no x.
- **Regras / pegadinhas:**
  - Coleira sempre: dentro do carimbo é `transform.localPosition.x`, não
    `localPosition.x` solto (CS0103).
  - **Teleporte ignora a física**: atravessa parede — mudar posição na mão
    não consulta colisor. Dash "de verdade" = velocidade + tempo (peça futura).
  - `if` sem chaves agarra SÓ a primeira linha seguinte; chaves vazias depois
    viram bloco órfão (prima do `if();`). Abraçar o corpo sempre.
  - Dash com `+` fixo vai sempre pra direita — a variável que sabe pra onde
    olho (direcao) resolve com uma multiplicação.

### `public GameObject` — caixinha de Inspector que guarda um OBJETO inteiro
*(03/09/2026 — peça 1 da Fase 2: o ouvido do ataque, `ataques/Ataque.cs`)*

- **O que é:** campo `public` não é só pra número: o tipo `GameObject` faz a
  caixinha do Inspector aceitar um **objeto/molde inteiro** — arrasta lá,
  igual fiz com o Rigidbody na caixinha `rb`.
- **Pra que serve:** dar ao código o MOLDE do golpe (prefab) na mão, pra
  carimbar cópias com `Instantiate` (peça futura).
- **Regras / pegadinhas:**
  - Caixinha vazia (esqueci de arrastar) = `NullReferenceException` na hora
    de usar — a Unity aponta a linha.
  - Quem escuta o botão de ataque é quem está SEMPRE vivo em cena (o player);
    a espada não pode esperar o próprio nascimento pra ouvir o teclado.
    Player escuta e manda nascer; o script do objeto nascido cuida do dano.
### `Instantiate(molde, onde, rotação)` — a máquina de carimbar
*(03/09/2026 — peça 3 da Fase 2, em `ataques/Ataque.cs`)*

- **O que é:** entrega um prefab, um lugar e uma rotação → nasce uma CÓPIA viva
  na cena naquele instante. É assim que nasce tudo em tempo de jogo: balas,
  moedas, inimigos, golpes.
- **Os 3 ingredientes em ordem:**
  1. molde — o que está na caixinha (`golpePrefab`)
  2. onde — `transform.position` = a posição de QUEM FALA (crachá do dono
     do script, mesma família do `transform.localScale`)
  3. rotação — `Quaternion.identity` = "reto, do jeito que o molde é"
     (decorar como carimbo-sem-girar por enquanto)
- **Regras / pegadinhas:**
  - Caixinha `golpePrefab` vazia no Inspector = `NullReferenceException`
    na linha do Instantiate.
  - Carimbo NÃO tem borracha: cada cópia fica na cena até alguém `Destroy`
    (peça 4). Hierarchy enchendo de `(Clone)` = sintoma esperado da v1.
  - Nascer NA FRENTE (com direção) = peça 3.5, exige dois scripts conversando.
### Dois scripts conversando — caixinha de vizinho + porta `public`
*(03/09/2026 — peça 3.5: o golpe nasce NA FRENTE)*

- **O que é:** um script meu é um TIPO como Rigidbody2D: `public Movimento movimento;`
  no Ataque.cs cria uma caixinha que segura o script do vizinho — arrasta o
  Boneco nela no Inspector (a Unity acha o componente). Depois,
  `movimento.direcao` lê o campo dele com coleira normal.
- **A porta:** vizinho só lê campo `public`. `private float direcao` é gaveta
  trancada → virar `public float direcao` pro Ataque enxergar.
- **A conta do "na frente"** (a MESMA fórmula do meu dash, linha 34):
  `transform.position.x + distanciaGolpe * movimento.direcao` no x,
  y intacto, 0 no z.
- **Pegadinha:** caixinha `movimento` vazia no Inspector = NullReference no
  aperto, igual à do prefab.

- **Roadmap da Fase 2 (combate em tempo real, decidido 03/09):**
  1. ouvido (Ataque.cs no player) → 2. molde (prefab, editor) →
  3. carimbo (Instantiate + direcao) → 4. toque fantasma (OnTriggerEnter2D
  no Espada.cs) → 5. inimigo com vida (Levar/piso renascem do Motor).

---

## Animação

### Onde mora a animação — quadro a quadro × osso
*(16/09/2026 — a conversa de por que este projeto não usa Animator)*

- **As duas famílias:** em **quadro a quadro** (*frame by frame*) a animação mora
  **no desenho** — alguém desenhou 20 poses da corrida e o jogo só troca a
  imagem. Muita imagem, animação pronta. Em **rigging** ela mora **no esqueleto**
  — uma imagem cortada em partes, ossos dentro dela, e eu animo girando osso.
  Pouca imagem, animação feita por mim.
- **O que decide NÃO é 2D × 3D, é o estilo da arte:** arte pintada ou vetorial
  deforma bem → osso brilha. **Pixel art não deforma** — girar e esticar pixel
  quebra a grade de pixels, e fica borrado ou serrilhado.
- **Onde tá no meu projeto:** quadro a quadro. 43 folhas em `Assets/player`,
  51 clipes gerados. Osso está fora porque é pixel art — não por ser 2D.

### Animator / Mecanim — a máquina de estados visual (e de onde ela veio)
*(16/09/2026)*

- **O que é:** o componente `Animator` + um asset `AnimatorController`: uma
  máquina de estados DESENHADA, com estados (um clipe cada) e transições
  disparadas por parâmetros (`SetBool`, `SetTrigger`, `SetFloat`).
- **Pra que serve:** é o caminho oficial e documentado — todo tutorial usa. É
  visual, então artista e designer ajustam sem tocar em código. E faz o que o meu
  animador não faz: blend entre animações, layers, máscaras, root motion, animar
  QUALQUER propriedade (cor, posição, um campo do meu script), e é quem a
  Timeline dirige.
- **De onde ele veio:** nasceu pra humanoide 3D. A lista do que ele tem de mais
  pesado — Avatar, *retargeting*, root motion, IK, máscara de avatar, layers,
  blend tree — é toda de conceito de esqueleto 3D. Usando ele só pra trocar
  sprite, eu uso a máquina de estados e **ignoro uns 80% do resto**.
- **Pegadinha (a que pesou aqui):** com a lógica já sendo uma máquina de estados
  em C#, o Animator vira uma SEGUNDA máquina que tem que concordar com a
  primeira, sincronizada por parâmetro. É daí que nasce o bug clássico: "o
  parâmetro existe, mas a transição tem exit time e o clipe sai atrasado".
- **Pegadinha 2 — Animation Event:** marcar evento no clipe (ligar/desligar
  hitbox) é na mão, clipe por clipe. E se o clipe é CORTADO no meio, o evento que
  **desliga** não dispara → a hitbox fica ligada.
- **Onde tá no meu projeto:** não está. Quem anima é o `AnimadorDeSprites`, que
  pede o clipe pelo nome e sabe o quadro exato e o progresso de 0 a 1. Se sobrar
  um `Animator` no mesmo objeto, ele o desliga sozinho e avisa no Console — os
  dois escrevem em `SpriteRenderer.sprite` e brigariam a cada quadro.

### Rigging 2D com osso — o Skinning Editor
*(16/09/2026)*

- **O que é:** pacote `com.unity.2d.animation`. Acrescenta o **Skinning Editor**
  dentro do Sprite Editor, com três passos: criar os ossos (*Create Bone*), gerar
  a malha (*Auto Geometry*) e pintar os pesos (*Auto Weights* = quanto cada osso
  puxa cada pedaço da malha). O objeto ganha um `SpriteSkin` e uma penca de
  Transform de osso como filhos.
- **Como anima:** eu animo os **Transforms de osso** na janela Animation → sai um
  `.anim` → tocado por um **Animator**. Ou seja: osso termina no Animator.
- **Pra que serve:** criar animação nova SEM desenhar arte nova; interpolação
  suave de verdade (é animação de transform, então blend passa a fazer sentido);
  IK (o pé grudar no chão, a mão apontar pro alvo); reaproveitar o mesmo
  esqueleto em personagens diferentes.
- **O que pede:** arte em PARTES separadas, com sobra atrás das juntas. Sprite
  achatado de 46×55 não rigga.
- **Pegadinha:** esse pacote (e o `com.unity.2d.psdimporter`, que é o par dele pra
  trazer PSD em camadas) vem instalado **por padrão no template 2D**. Estar
  instalado não quer dizer nada sobre eu dever usar.
- **Quando valeria pra mim:** um chefe grande com poucos desenhos que precisa se
  mexer muito, ou um braço que segue a direção da mira.

### Timeline — a régua da cutscene
*(16/09/2026)*

- **O que é:** pacote `com.unity.timeline`. Um editor de linha de tempo igual ao
  de programa de vídeo: régua, cursor tocando e faixas empilhadas. Um asset de
  Timeline + um `PlayableDirector` na cena que toca ele.
- **As faixas:** *Animation* (toca num `Animator`), *Activation* (liga/desliga
  objeto na hora marcada), *Audio* (som na hora exata), *Signal/Marker* (dispara
  evento = **chama um método meu**), *Control* (aninha outra Timeline, controla
  partícula).
- **Pra que serve:** cutscene e momento roteirizado — a porta abre, o chefe cai do
  teto, a câmera passeia, o controle volta pra mim. Coisa com começo, meio e fim
  que acontece sempre igual. **Não** é pra gameplay.
- **Não tem nada de 3D:** é sequenciador, não sabe nem se a cena tem profundidade.
  Cutscene 2D usa igual.
- **Conviver com o meu animador:** a Animation Track não conhece o
  `AnimadorDeSprites`. Saída: faixa de **Signal** chamando
  `Tocar("nome_do_clipe")`, que é público e recebe string. Perco a
  pré-visualização dentro da Timeline (fica um marcador em vez do bloco), mas
  funciona.
- **Pegadinha:** pra sequência curta, uma coroutine em C# resolve com menos peça
  móvel. Timeline começa a valer quando várias coisas acontecem em PARALELO e eu
  quero VER o encaixe, em vez de contar segundos no código.

### Os três grupos de peças da Unity — como ler qualquer tutorial
*(16/09/2026 — o modelo mental que decidiu a discussão acima)*

- **Feitas para 2D:** Sprite Renderer, Sprite Editor, Tilemap, SpriteShape,
  física 2D, luzes 2D do URP.
- **Feitas para 3D, que o 2D reaproveita:** Animator/Mecanim, rigging com osso.
- **Neutras:** Timeline, Playables, Input System, UI.
- **Pra que serve saber:** quando um tutorial recomendar algo do grupo **do meio**,
  a pergunta certa é "o que dessa ferramenta eu vou realmente usar?". Se a
  resposta for "só a máquina de estados — e eu já tenho uma em C#", o custo dela
  continua de pé e o benefício não.
- **E o contrário também vale:** "isso é coisa de 3D" seria conclusão errada e
  larga — muito jogo 2D usa Mecanim e está tudo bem. A conclusão certa é
  estreita: *pra sprite quadro a quadro, as forças do Mecanim ficam paradas.*

---

## Ferramentas (VS Code)

### O ponto que aceita sugestão sozinho
*(03/09/2026 — o capeta dos usings fantasmas)*

- **O que acontece:** com a caixinha de sugestões aberta, digitar `.` significa
  "aceita a sugestão marcada E põe o ponto" — cola bobagem que ninguém pediu.
  Foi ele que criou o `using System.Numerics` fantasma e o `== cenas` do Python.
- **Piora no trabalho:** o csproj da Unity aponta DLLs que só existem no PC de casa
  → o VS Code não conhece `Input` de verdade e chuta palavras soltas.
- **Conserto (fazer nos DOIS PCs, a config não viaja no git):**
  `Ctrl + ,` → pesquisar `commit character` →
  **desmarcar** "Editor: Accept Suggestion On Commit Character".
  Aceitar sugestão vira ato voluntário: só com `Tab`/`Enter`.
