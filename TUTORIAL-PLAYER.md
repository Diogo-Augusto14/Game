# Tutorial do player

Arquivo separado, só sobre o boneco: como ele pensa, como observar cada estado rodando,
o que cada número faz de verdade, e **receitas prontas** para mudar a sensação do controle.

Se você ainda não fez o `TUTORIAL.md`, faça primeiro. Este aqui é o aprofundamento.

---

## Passo 1 — Como ele pensa

O boneco é uma máquina de estados com **11 estados**. A regra que vale para o arquivo
inteiro: **um estado manda por quadro**. Cada um tem o seu `Atualizar...()`, e os outros nem
rodam. É por isso que dash, agachar, beirada e escada não brigam entre si — não existe "ele
está agachado e dando dash ao mesmo tempo".

```
                    ┌──────────────── Normal ────────────────┐
                    │   (andar, correr, pular, cair)          │
                    │                                         │
        Baixo ──────┤                                         ├────── Shift
                    ▼                                         ▼
                Agachado                                     Dash
                    │                                    (0,16 s, invencível)
        Shift+Baixo │                                         │
                    ▼                                    Shift + trás
              Escorregando ◄──────────────────────────► EsquivaTras
                (0,38 s)

   Normal ──encostou na parede caindo──► ParedeDeslizando ──Espaço──► wall jump
                                                 │
                                          quina livre acima
                                                 ▼
                                            Pendurado ──Cima──► SubindoBeirada
                                                 │                    │
                                              Baixo                0,45 s
                                                 ▼                    ▼
                                              Normal              em cima do bloco

   Normal ──Cima dentro de uma escada──► Escada ──topo / Espaço / Baixo──► Normal

   qualquer estado ──levou golpe──► Atordoado ──► Normal
   qualquer estado ──vida = 0────► Morto ──1,6 s──► renasce
```

E uma regra que não está no desenho: **atacar não é um estado**. Você anda, pula e cai
enquanto golpeia — estilo Hollow Knight. O `Ataque` é um componente separado que roda por
cima do movimento.

---

## Passo 2 — Ver cada estado acontecendo

Aperte **Play**, selecione o `Jogador` na Hierarchy e ache o componente **Movimento**. No
topo dele aparece o painel **Estado ao vivo**: estado, no chão, na parede, invencível, pulos
gastos, velocidade, vida, frascos, golpe e progresso do golpe. Só existe dentro do Play, e é
só leitura.

Agora faça a lista e olhe o painel:

| Faça isso | Estado | O que mais olhar no painel |
|---|---|---|
| Andar | `Normal` | `Velocidade X` subindo até 3,20 |
| Segurar `Ctrl` e andar | `Normal` | `Velocidade X` para em 1,40 |
| Pular | `Normal` | `Pulos usados` vira `1 de 2`; `Velocidade Y` = 6,50 |
| Pular de novo no ar | `Normal` | `Pulos usados` vira `2 de 2` |
| Andar para fora de uma plataforma | `Normal` | `Pulos usados` já vira `1 de 2` — o pulo do chão foi gasto |
| `Baixo` no chão | `Agachado` | `Velocidade X` cai para 1,44 no máximo |
| `Shift` | `Dash` | `Invencivel` = **sim** |
| `Shift` + `Baixo` | `Escorregando` | `Invencivel` = **sim** |
| `Shift` + direção contrária | `EsquivaTras` | `Olhando pra` **não** muda |
| Grudar numa parede caindo | `ParedeDeslizando` | `Na parede` = sim, `Velocidade Y` presa em -1,60 |
| Cair raspando numa quina | `Pendurado` | `Velocidade Y` = 0 (ele vira Kinematic) |
| `Cima` pendurado | `SubindoBeirada` | dura 0,45 s e ele aparece em cima |
| `Cima` dentro de uma escada | `Escada` | `Escada encostada` mostra o nome do objeto |
| Levar um golpe | `Atordoado` | dura 0,18 s (leve) ou 0,45 s (forte) |
| Morrer | `Morto` | `Vida` = 0, e 1,6 s depois renasce |

Um detalhe do quinto item que vale entender: ao **cair de uma plataforma sem pular**, sobra
exatamente **um** pulo no ar — não dois. Se sobrassem dois, andar para fora de uma beirada
seria melhor que pular, e aí ninguém pularia.

---

## Passo 3 — As três camadas

O boneco não é um script, são três responsabilidades separadas:

```
Entrada.cs           lê o teclado e GUARDA os apertos por um instante
   ↓ (só leitura)
Movimento.cs         a máquina de estados: decide e mexe na física
   ↓ (só leitura)
AnimacaoDoJogador.cs traduz estado em nome de clipe, por prioridade
```

### Por que a entrada guarda os apertos

`Entrada.cs` não é frescura. Ele guarda cada aperto por um tempinho:

| Aperto | Guardado por |
|---|---|
| Pular | 0,12 s |
| Dash | 0,12 s |
| Golpe | 0,20 s |

O motivo é técnico e o efeito é sentido: `Input.GetButtonDown` vale por **um quadro de
`Update`**, e a física roda no `FixedUpdate`, que pode não rodar naquele quadro. Ler o
teclado direto no `FixedUpdate` **perde apertos** de vez em quando — e "de vez em quando" é
o pior tipo de bug de controle, porque parece que o jogador errou.

Com o buffer: apertar pular um tiquinho **antes** de encostar no chão ainda pula. Junto com
o `Coyote Time` (apertar um tiquinho **depois** de sair da beirada), é o que faz o controle
parecer justo.

### Por que a animação é decidida em outro arquivo

`AnimacaoDoJogador` recalcula o clipe certo **todo quadro**, de cima para baixo por
prioridade: morto ganha de apanhar, que ganha de atacar, que ganha de andar. Não existe
transição pendente nem estado preso.

E tem uma **trava curta**: clipes de transição (arrancar, frear, virar, pousar rolando,
pular da parede) precisam terminar para fazer sentido, então seguram o desenho pela duração
deles — mas só contra clipes de prioridade menor. Levar um golpe no meio de uma freada corta
a freada, como deve ser.

---

## Passo 4 — Andar e correr por dentro

| Campo | Padrão | O que faz |
|---|---|---|
| `Velocidade Maxima` | 3.2 | velocidade de corrida |
| `Velocidade De Caminhada` | 1.4 | velocidade segurando `Ctrl` |
| `Aceleracao` | 26 | quão rápido chega na velocidade alvo |
| `Desaceleracao` | 42 | quão rápido **para** ou **inverte** |
| `Controle No Ar` | 0.8 | fração da aceleração que vale no ar |

A aceleração e a desaceleração serem **números diferentes** é o segredo. Acelerar devagar dá
peso; parar rápido dá precisão. Igualar as duas deixa o controle "escorregadio" (desaceleração
baixa) ou "grudado no chão" (aceleração alta).

`Controle No Ar` em 0.8 quer dizer que no ar você tem 80% da autoridade que tem no chão.
Em 1, o pulo fica totalmente controlável (estilo Celeste). Em 0.3, você se compromete com
a direção ao pular (estilo Castlevania antigo).

**As quatro animações de transição** que você vê nos pés não são a mesma acelerada:
`andar_inicio` / `correr_inicio` (saiu do zero), `correr_parar` (soltou a tecla em
velocidade), `correr_virar` (inverteu o lado correndo). Quem dispara é o
`AnimacaoDoJogador`, comparando a velocidade e o lado pedido deste quadro com os do quadro
anterior.

---

## Passo 5 — O pulo por dentro

| Campo | Padrão | O que faz |
|---|---|---|
| `Jump` | 6.5 | velocidade vertical do pulo |
| `Maximo De Pulo` | 2 | pulos por ciclo, contando o do chão |
| `Corte Do Pulo` | 0.45 | soltou subindo → a subida é multiplicada por isso |
| `Coyote Time` | 0.1 | tolerância para pular depois de sair da beirada |
| `Gravidade Na Queda` | 1.5 | multiplica a gravidade enquanto cai |
| `Velocidade Maxima De Queda` | 14 | teto da queda |
| `Altura Do Pouso Rolado` | 2.2 | caiu mais que isso → pousa rolando |

### A conta do pulo

Vale fazer uma vez, porque depois você acerta qualquer pulo de cabeça.

A gravidade efetiva é a do projeto (`9.81`) vezes o `Gravity Scale` do Rigidbody2D (`2`) =
**19,62**. A altura do pulo é `Jump² / (2 × gravidade)`:

```
6,5² / (2 × 19,62) = 42,25 / 39,24 = 1,08 unidades
```

O boneco tem 0,52 de colisor. Então o pulo dele sobe **duas vezes a própria altura** — que é
a proporção clássica de plataforma. Subir leva `6,5 / 19,62 = 0,33 s`; cair leva menos,
porque a queda tem 1,5× de gravidade. O pulo inteiro dura ~0,6 s, e correndo ele cobre
**1,9 unidade** de distância.

Quer um pulo de 2 unidades de altura? `Jump = raiz(2 × 19,62 × 2) = 8,9`.

### A queda mais rápida que a subida

`Gravidade Na Queda` em 1.5 é o truque mais barato de "game feel" que existe. Subida lenta dá
tempo de ver para onde você vai; queda rápida evita aquela sensação de flutuar. Ponha em 1 e
sinta o pulo ficar molenga; ponha em 3 e sinta ele ficar seco.

### O corte do pulo

Soltar o `Espaço` ainda subindo multiplica a velocidade vertical por `Corte Do Pulo` (0,45).
É isso que dá **pulo baixo e pulo alto com o mesmo botão** — o jogador controla a altura pelo
tempo que segura, sem precisar de dois botões.

---

## Passo 6 — Um botão, três movimentos

`Shift` faz três coisas diferentes, e a escolha está num lugar só (`TentarDash`):

| Você está | Sai |
|---|---|
| segurando `Baixo`, no chão | **Escorregar** (0,38 s, 6,5 de velocidade → 2,5 unidades) |
| segurando a direção **contrária** à que olha, no chão | **Esquiva para trás** (0,22 s → 1,2 unidade) |
| qualquer outro caso | **Dash** (0,16 s, 7,5 de velocidade → 1,2 unidade) |

Os três são **invencíveis** enquanto duram (`Dash Invencivel` ligado). É bastante poder —
0,38 s de invencibilidade no escorregar é generoso. Se quiser um jogo mais duro, desmarque
`Dash Invencivel`, ou reduza as durações.

**Diferenças que importam:**

- O **dash** zera a gravidade: linha reta de verdade, sem cair no meio.
- O **escorregar** mantém a gravidade (senão ele "voaria" numa rampa) e encolhe o colisor —
  e só levanta no fim **se couber**. Preso num túnel, ele continua agachado.
- A **esquiva** anda para trás mantendo a cara para frente. É por isso que ela é a defesa
  natural num duelo: você recua sem perder o inimigo de vista.
- O dash **atravessa inimigos** (o campo `Dash Atravessa` já vem com a camada Inimigo).
- `Dashes No Ar` = 1: um dash aéreo por vez no ar, recarrega ao tocar o chão.
- `Dash Recarga` = 0,35 s: não dá para metralhar dashes.

---

## Passo 7 — Parede, beirada e escada

### Parede

| Campo | Padrão |
|---|---|
| `Velocidade Deslizada` | 1.6 |
| `Wall Jump Forca X` | 4.5 |
| `Wall Jump Forca Y` | 6.2 |
| `Trava Apos Wall Jump` | 0.16 |
| `Wall Jump Recarrega Pulos` | ligado |

Para grudar, três coisas: estar no ar, ter parede no `WallCheck`, **e estar segurando a
direção da parede**. Soltar a direção desgruda.

A `Trava Apos Wall Jump` é o campo que parece opcional e não é: por 0,16 s depois do pulo o
teclado não manda no horizontal. Sem ela, você estar segurando a direção da parede
**cancelaria o próprio pulo** e o boneco ficaria colado, subindo no lugar.

### Beirada

| Campo | Padrão |
|---|---|
| `Precisa Segurar Para Agarrar` | desmarcado |
| `Avanco Subida` | 0.22 |
| `Sem Agarrar Apos Soltar` | 0.3 |
| `Duracao Da Subida` | 0.45 |

Agarra quando: no ar, parede no peito (`WallCheck`), **nada na cabeça** (`LedgeCheck`), e não
está subindo. Aí ele vira Kinematic e encosta as mãos no topo do bloco.

A subida é um **teletransporte curto no fim** da animação: durante os 0,45 s ele fica onde
está e o desenho mostra o movimento. Teletransportar no começo faria o boneco aparecer em
cima antes de ter subido. O `AnimacaoDoJogador` estica o clipe para durar exatamente
`Duracao Da Subida` — mude um e o outro acompanha.

`Sem Agarrar Apos Soltar` evita o loop de grudar no mesmo lugar assim que você solta.

### Escada

| Campo | Padrão |
|---|---|
| `Escada Velocidade` | 1.8 |
| `Escada Velocidade Escorregando` | 5 |

Ele se centraliza no degrau ao entrar (subir torto engancha no colisor) e a gravidade vira
zero. Ao sair pelo topo, ele procura piso na posição dele e **um passo para cada lado** —
por isso a plataforma do topo pode ficar ao lado da escada, que é onde ela tem que ficar.

---

## Passo 8 — O combate por dentro

Três sequências, no componente **Ataque**:

| Sequência | Golpes | Dano | Quando sai |
|---|---|---|---|
| `Combo No Chao` | 3 | 12 / 14 / **22** | padrão no chão |
| `Combo No Ar` | 2 | 12 / 16 | no ar |
| `Golpes De Dash` | 2 | 16 / 20 | `J` durante o dash |
| `Golpe De Mergulho` | 1 | 24 | no ar + `Baixo` |

Cada golpe tem os mesmos campos:

```
        0%          30%        55%   60%        100%
        ├────────────┬──────────┬─────┬──────────┤
                     │  hitbox  │
                     └──────────┘
                                └─ daqui pra frente, apertar de novo
                                   emenda no golpe seguinte
```

- `Inicio Da Janela` / `Fim Da Janela` — quando a hitbox liga e desliga, em **fração do
  clipe** (não em segundos).
- `Inicio Do Cancelamento` — a partir daqui o próximo aperto emenda o combo.
- `Avanco` / `Trava Do Avanco` — o passo para frente junto com a lâmina.
- `Centro Da Hitbox` / `Tamanho Da Hitbox` — a geometria. O terceiro golpe alcança mais
  (0,6 de largura contra 0,45).
- `Velocidade Do Clipe` — 1,2 deixa o golpe mais seco sem mexer na animação.

Fração em vez de segundos é de propósito: mudar o fps da animação ajusta a janela do golpe
junto, sozinho. É o erro clássico de sincronizar hitbox com animação.

**O combo:** o aperto fica guardado 0,2 s, então apertar um pouco cedo também emenda — é por
isso que o combo "nunca falha". Demorar mais de `Tempo Para Zerar Combo` (0,8 s) volta para o
primeiro golpe.

**O que cancela o quê:**

| Situação | O que acontece |
|---|---|
| Levou golpe atacando | o golpe é **cortado** |
| Apertou golpe curando | cancela a cura, e o golpe sai no aperto seguinte |
| Tentou atacar pendurado / na escada / subindo beirada | não sai |
| Tentou atacar escorregando | não sai |
| Atacou dashando | sai o **golpe de dash** |

---

## Passo 9 — Vida, cura, dano e morte

| Componente | Campo | Padrão |
|---|---|---|
| `Vida` | vida máxima | 120 |
| | `Defesa` | 0 |
| | `Tempo Invencivel` | 0.35 |
| | `Trava Golpe Leve` / `Forte` | 0.18 / 0.45 |
| | `Fracao Para Golpe Forte` | 0.2 |
| | `Piscar Na Invencibilidade` | ligado |
| | `Destruir Ao Morrer` | **desmarcado** (ele renasce!) |
| `Cura` | `Tempo Para Curar` | 0.85 |
| | `Quantidade Curada` | 34 |
| | `Frascos Maximos` | 3 |
| `Player` | `Tempo Ate Renascer` | 1.6 |
| | `Invencibilidade Ao Renascer` | 1.2 |
| | `Altura Da Morte` | -20 |

**A conta:** o inimigo tira 14 e você tem 120 → **9 golpes** para morrer. Com os frascos
(3 × 34 = 102) dá umas 16 pancadas por vida. Um golpe é classificado como **forte** quando
tira mais de `0.2 × 120 = 24` — o inimigo tira 14, então os golpes dele são leves (0,18 s de
trava). Só o segundo golpe do combo dele conta como forte.

**O piscar** durante a invencibilidade não é enfeite: sem ele o jogador não sabe que está
protegido e leva dano "de graça" achando que foi injusto.

**`Destruir Ao Morrer` tem que ficar DESMARCADO no boneco.** É o erro mais fácil de cometer:
se marcar, ele é apagado da cena em vez de renascer, e o jogo fica sem jogador. No inimigo é
o contrário — lá fica marcado.

**O buraco:** cair abaixo de `Altura Da Morte` mata. Sem isso o boneco cai para sempre e o
jogo parece travado sem estar. Se você montar a fase numa altura muito diferente de zero,
ajuste esse número.

---

## Passo 10 — Receitas de sensação

Como nas receitas de inimigo: **você não precisa de código novo para mudar o jogo**. Copie
os valores no componente `Movimento` (e onde indicado, no `Rigidbody2D`).

### Preciso e rápido (estilo Celeste)

| Campo | Valor |
|---|---|
| `Velocidade Maxima` | 4.5 |
| `Aceleracao` | 60 |
| `Desaceleracao` | 80 |
| `Controle No Ar` | 1 |
| `Jump` | 7 |
| `Corte Do Pulo` | 0.3 |
| `Coyote Time` | 0.12 |
| `Gravidade Na Queda` | 2.2 |
| `Dash Velocidade` | 11 |
| `Dash Duracao` | 0.13 |
| `Dash Recarga` | 0.1 |

Aceleração altíssima = responde no quadro. Queda seca. Dash quase sem recarga.

### Pesado e metódico (estilo Hollow Knight)

| Campo | Valor |
|---|---|
| `Velocidade Maxima` | 2.8 |
| `Aceleracao` | 18 |
| `Desaceleracao` | 24 |
| `Controle No Ar` | 0.7 |
| `Jump` | 6 |
| `Corte Do Pulo` | 0.5 |
| `Gravidade Na Queda` | 1.4 |
| `Dash Recarga` | 0.6 |

Você sente o peso do boneco. O dash é recurso, não transporte.

### Flutuante e exploratório (estilo Metroid)

| Campo | Valor |
|---|---|
| `Jump` | 6.5 |
| `Gravidade Na Queda` | 1.0 |
| `Velocidade Maxima De Queda` | 8 |
| `Controle No Ar` | 1 |
| `Maximo De Pulo` | 2 |

Queda lenta com teto baixo = muito tempo no ar para mirar onde cair.

### Arcade solto

| Campo | Valor |
|---|---|
| `Velocidade Maxima` | 6 |
| `Aceleracao` | 40 |
| `Jump` | 8 |
| `Maximo De Pulo` | 3 |
| `Dash Recarga` | 0.15 |

### Mais fácil / mais difícil

Se quiser deixar o jogo acessível sem mexer em dano, os campos de tolerância são estes:

| Mais fácil | Mais difícil |
|---|---|
| `Coyote Time` 0.18 | `Coyote Time` 0 |
| buffers da `Entrada` 0.2 | buffers 0.05 |
| `Tempo Invencivel` 0.6 | `Tempo Invencivel` 0.15 |
| `Frascos Maximos` 5 | `Frascos Maximos` 1 |
| `Dash Invencivel` ligado | `Dash Invencivel` desmarcado |

---

## Quando algo dá errado

| Sintoma | Causa quase sempre | Conserto |
|---|---|---|
| Não pula às vezes | `Coyote Time` ou buffer do pulo em 0 | volte para 0.1 / 0.12 |
| Pulo alto demais / de menos | é `Jump` com o `Gravity Scale` — veja a conta do Passo 5 | ajuste `Jump`, não a gravidade |
| Anda "no gelo" | `Desaceleracao` baixa | suba para 40+ |
| Não gruda na parede | bloco na camada de **chão**, ou não está segurando a direção | camada de parede; segure a direção |
| Fica preso subindo a parede no lugar | `Trava Apos Wall Jump` em 0 | volte para 0.16 |
| Não pendura na beirada | falta o filho `LedgeCheck`, ou tem coisa acima da quina | confira o filho; o topo do bloco tem que estar livre |
| Sobe a beirada e cai | `Avanco Subida` pequeno | suba para mais da metade da largura do colisor |
| Atravessa o chão caindo rápido | `Velocidade Maxima De Queda` alta demais | baixe, ou use `Continuous` no Rigidbody2D (já vem) |
| Não agacha | falta o `BoxCollider2D` no boneco | agachar e escorregar dependem dele |
| Some ao morrer | `Destruir Ao Morrer` marcado no `Vida` | **desmarque** |
| Cai para sempre | sem chão e `Altura Da Morte` longe | ajuste `Altura Da Morte` no `Player` |
| Combo sempre volta pro primeiro | demorando mais de 0,8 s entre golpes | suba `Tempo Para Zerar Combo` |
| Golpe não acerta | `Centro`/`Tamanho Da Hitbox` curtos para a distância | aumente, e confira o gizmo vermelho na Scene |
| Cura nunca completa | você está andando, ou não está no chão | pare; ou desmarque `Andar Cancela` |
| Sprite piscando | sobrou um `Animator` no objeto | o `AnimadorDeSprites` desliga sozinho e avisa |
| Número voltou ao normal | mexeu **durante** o Play | pare o Play e mexa com o jogo parado |

---

## Colinha das teclas

| Tecla | Ação |
|---|---|
| `A` / `D` | andar / correr |
| `Ctrl` | andar devagar |
| `Espaço` | pular (2×) · solte cedo = pulo baixo · na parede = wall jump |
| `Shift` | dash |
| `Shift` + `Baixo` | escorregar |
| `Shift` + direção contrária | esquiva para trás |
| `J` / mouse esquerdo | golpe — combo 1-2-3 no chão, 1-2 no ar |
| `J` durante o dash | golpe de dash |
| `J` no ar + `Baixo` | mergulho |
| `Baixo` | agachar · soltar beirada · descer escada |
| `Cima` | entrar na escada · subir da beirada |
| `E` (segurar) | curar |

As teclas de `Shift`, `J`, `E` e `Ctrl` são campos do componente **Entrada** — troque lá.
`A`/`D`/setas e `Espaço` vêm do Input Manager da Unity (`Horizontal`, `Vertical`, `Jump`).

---

## Onde está o código

| Arquivo | Assunto |
|---|---|
| [Entrada.cs](Assets/Scripts/Jogador/Entrada.cs) | teclas e buffers |
| [Movimento.cs](Assets/Scripts/Movimento.cs) | a máquina de estados |
| [AnimacaoDoJogador.cs](Assets/Scripts/Jogador/AnimacaoDoJogador.cs) | estado vira desenho |
| [Ataque.cs](Assets/Scripts/ataques/Ataque.cs) | combos e janelas |
| [Espada.cs](Assets/Scripts/ataques/Espada.cs) | a hitbox |
| [Vida.cs](Assets/Scripts/combate/Vida.cs) | vida, empurrão, invencibilidade |
| [Cura.cs](Assets/Scripts/Cura.cs) | frascos |
| [Player.cs](Assets/Scripts/Player.cs) | morte, renascimento, checkpoint, buraco |

Lendo nessa ordem, cada arquivo só usa coisas que você já viu no anterior.
