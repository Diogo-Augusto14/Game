# Tutorial do inimigo

Arquivo separado, só sobre o inimigo: como ele pensa, como observar cada estado rodando,
como afinar os números, como fazer **tipos diferentes** de inimigo a partir do mesmo
componente, e o que ele ainda não sabe fazer.

Se você ainda não fez o `TUTORIAL.md`, faça primeiro — este aqui assume que você já
consegue rodar o jogo e mexer no Inspector.

---

## Passo 1 — Como ele pensa

O inimigo é uma máquina de estados. **Um estado manda por quadro**, e cada um tem uma saída
clara. Não existe "ele está perseguindo e atacando ao mesmo tempo".

```
  Parado / Patrulhando  ──viu você──►  Alerta  ──0,35 s──►  Perseguindo
         ▲                                                       │
         │                                                 chegou perto
    perdeu você                                                  ▼
    (3 s de memória)                                        Preparando
         │                                                       │
         │                                                  0,3 s  (telegrafo)
         │                                                       ▼
         └──────────  Recuperando  ◄──0,6 s──  Atacando ◄──┘
                            │                     │
                            └─── 45% de chance ───┘  (segundo golpe do combo)

   qualquer estado ──levou golpe──► Atordoado ──► volta a perseguir
   qualquer estado ──vida = 0────► Morto (para tudo, some em 1,1 s)
```

Duas coisas foram desenhadas de propósito e vale saber **por quê**:

**O `Alerta`.** Quando ele te vê, não sai correndo na hora: fica 0,35 s parado, virado para
você, tocando a animação de guarda. Isso existe para *você ver que foi notado*. Inimigo que
dispara no mesmo quadro em que aparece na tela parece injusto, mesmo quando é justo.

**O `Preparando`.** Antes de bater ele para e telegrafa por 0,3 s. Só depois a hitbox abre.
Do momento em que ele decide atacar até a lâmina existir passa quase meio segundo — essa é
a sua janela de reação. É a diferença entre um jogo difícil e um jogo chato.

---

## Passo 2 — Ver cada estado acontecendo

Aperte **Play** e selecione um inimigo na Hierarchy. No topo do Inspector aparece um painel
**Estado ao vivo** — estado, no chão, velocidade, vida, progresso do golpe — atualizando em
tempo real. Ele só existe dentro do Play e é só leitura.

Olhe o **Estado** mudando enquanto você faz o seguinte.

| Faça isso | Estado que aparece | O que olhar na tela |
|---|---|---|
| Fique longe, quieto | `Patrulhando` | ele anda de um lado para o outro e pausa nas pontas |
| Chegue devagar até ~4 de distância | `Alerta` | ele **para** e fica em guarda, virado para você |
| Espere | `Perseguindo` | vem correndo (mais rápido que a patrulha) |
| Deixe ele chegar | `Preparando` → `Atacando` | para, telegrafa, **depois** bate |
| Depois do golpe | `Recuperando` | fica em guarda respirando |
| Bata nele | `Atordoado` | ele voa para trás e pisca vermelho |
| Fuja e se esconda atrás de uma parede | `Perseguindo` por 3 s, depois `Patrulhando` | a memória de agro expirando |
| Mate ele | `Morto` | cai, os colliders desligam, some em 1,1 s |

### Os gizmos

Com o inimigo selecionado, a **Scene** desenha o que ele sente. É por aqui que se ajusta
sem chutar:

| Desenho | O que é |
|---|---|
| Círculo **amarelo** | `Raio De Visao` — de quão longe ele te percebe de frente |
| Círculo **laranja** | `Raio De Alerta Por Tras` — de quão longe ele percebe **pelas costas** |
| Linha **vermelha** | `Distancia De Ataque` — onde ele para de andar e começa a bater |
| Linha **azul** | `Alcance Da Patrulha` — o trecho que ele patrulha |
| Linha **verde** para baixo | o sensor de beirada — se não achar chão aí, ele não avança |

---

## Passo 3 — O que ele sente

### Visão

Três condições, todas têm que passar:

1. distância menor que `Raio De Visao` (4.5);
2. diferença de altura menor que `Diferenca De Altura Maxima` (1.6) — **é isso que impede
   ele de te agro de outra plataforma**;
3. sem parede no meio (`Parede Esconde` ligado faz um traço do peito dele até o seu).

**Pelas costas** o raio cai para `Raio De Alerta Por Tras` (1.5). Ou seja: dá para chegar
por trás — mas não muito perto. É o "ouvido" dele.

### Não cair da plataforma

O filho `ChecadorDeBorda` fica na frente e um pouco abaixo dos pés. Antes de avançar, o
inimigo lança um raio de 0,35 para baixo dali. Sem chão, ele **para e espera você chegar**
em vez de se jogar.

Isso vale tanto na patrulha (aí ele dá meia-volta) quanto na perseguição (aí ele fica na
beirada, esperando). Se quiser um inimigo kamikaze que se joga atrás de você, desmarque
`Nao Cair Da Borda`.

### Memória

`Memoria De Agro` (3 s) é quanto tempo ele continua atrás de você depois de te perder de
vista. Enquanto essa memória durar, ele corre para a sua última posição conhecida. Zerando
ela, ele desiste no mesmo quadro em que você sai da linha de visão — o que faz esconder
atrás de uma quina ficar exageradamente eficiente.

---

## Passo 4 — Como o golpe funciona por dentro

O ciclo completo de um ataque:

```
 Preparando          Atacando (0,45 s)                  Recuperando
 ├─ 0,3 s ─┤ ├──────────────────────────────┤ ├──────── 0,6 s ────────┤
            0%     25%        55%        100%
                   ├─ hitbox ─┤
                    (0,135 s de lâmina no ar)
```

- `Tempo De Preparo` — o telegrafo, antes de qualquer dano.
- `Duracao Do Golpe` — o golpe inteiro.
- `Inicio Da Janela` / `Fim Da Janela` — **frações do golpe** (0 a 1), não segundos. A
  hitbox liga em 25% e desliga em 55%.
- `Tempo De Recuperacao` — o respiro depois.

Fração em vez de segundos é de propósito: se você mudar a velocidade da animação do golpe,
a janela acompanha sozinha. É o erro clássico de sincronizar hitbox com animação — e a
animação do inimigo é esticada para durar exatamente `Duracao Do Golpe`, então a lâmina
aparece no desenho no mesmo instante em que passa a machucar.

**O combo:** ao terminar o golpe, se você ainda estiver por perto, ele tem
`Chance De Combo` (45%) de emendar um segundo golpe **sem recuperar**. O segundo usa outra
animação e conta como golpe **forte** (te trava por mais tempo).

**O avanço:** `Avanco Do Golpe` (1.2) dá um passo para frente junto com a lâmina — é o que
impede você de escapar andando um centímetro para trás. Depois de 30% do golpe ele freia,
para o ataque não deslizar pela tela.

> ⚠️ **Regra que quebra o combate se você errar:** `Distancia De Ataque` tem que ser
> **menor** que o alcance da hitbox. O alcance é `Centro Da Hitbox.x + Tamanho Da Hitbox.x / 2`
> — com os valores padrão, `0.34 + 0.3 = 0.64`. A distância de ataque é `0.6`. Se você
> aumentar a distância para 1.0 sem aumentar a hitbox, ele para longe e **golpeia o ar** para
> sempre.

---

## Passo 5 — Quatro receitas de inimigo

Este é o ponto: **você não precisa de um script novo para ter inimigos diferentes**. Os
campos do componente já dão personalidades bem distintas. Copie os valores.

### Sentinela — guarda um lugar, bate forte, reage devagar

| Campo | Valor |
|---|---|
| `Patrulhar` | desmarcado |
| `Raio De Visao` | 6 |
| `Tempo De Alerta` | 0.5 |
| `Velocidade De Perseguicao` | 1.2 |
| `Tempo De Preparo` | 0.45 |
| `Chance De Combo` | 0.2 |
| `Vida` (no componente Vida) | 70, defesa 2 |

Fica parado no posto, te vê de longe, vem devagar. Fácil de evitar, chato de trocar golpe.

### Corredor — rápido, agressivo e frágil

| Campo | Valor |
|---|---|
| `Velocidade De Patrulha` | 1.2 |
| `Velocidade De Perseguicao` | 3.2 |
| `Aceleracao` | 20 |
| `Tempo De Alerta` | 0.15 |
| `Tempo De Preparo` | 0.15 |
| `Tempo De Recuperacao` | 0.35 |
| `Chance De Combo` | 0.6 |
| `Dano` | 9 |
| `Vida` | 30, defesa 0 |

Corre mais que você (3.2 contra 3.2 — empata, e ele encosta). Telegrafo curtíssimo. Morre
em dois golpes. Bom em grupo.

### Tanque — lento, duro, não se abala

| Campo | Valor |
|---|---|
| `Velocidade De Perseguicao` | 1.0 |
| `Aceleracao` | 6 |
| `Tempo De Preparo` | 0.55 |
| `Duracao Do Golpe` | 0.6 |
| `Dano` | 24 |
| `Forca Empurrao` | 7 |
| `Avanco Do Golpe` | 2 |
| `Tamanho Da Hitbox` | 0.8 × 0.5 |
| `Distancia De Ataque` | 0.75 |
| `Tempo Atordoado Leve` | 0.1 |
| `Tempo Atordoado Forte` | 0.2 |
| `Vida` | 140, defesa 4, `Fracao Para Golpe Forte` 0.5 |

O detalhe que faz ele ser um tanque não é a vida: é o **atordoamento curto**. Ele continua
atacando enquanto você bate nele. Repare que aumentei a hitbox junto com a distância de
ataque — a regra do Passo 4.

### Emboscada — parado, invisível, pula em cima

| Campo | Valor |
|---|---|
| `Patrulhar` | desmarcado |
| `Raio De Visao` | 1.8 |
| `Raio De Alerta Por Tras` | 1.8 |
| `Tempo De Alerta` | 0.1 |
| `Velocidade De Perseguicao` | 2.6 |
| `Memoria De Agro` | 6 |
| `Chance De Combo` | 0.8 |

Ignora você até você chegar quase em cima. Aí não solta mais.

### Salvando as receitas — variantes de prefab

Não repita esses valores na mão em cada inimigo. Faça assim:

1. Na janela Project, clique com o botão direito em `Assets/Prefabs/Inimigo.prefab`.
2. **Create ▸ Prefab Variant**. Renomeie para `Inimigo - Tanque`.
3. Duplo clique na variante, ajuste os campos, salve.
4. Arraste a variante para a cena quantas vezes quiser.

A vantagem: se depois você consertar algo no `Inimigo.prefab` original, **todas as variantes
herdam o conserto** — menos nos campos que você mudou de propósito.

---

## Passo 6 — Balanceamento (a conta)

Vale fazer essa conta uma vez para entender o que está acontecendo.

**Quantos golpes para matar o inimigo?** O boneco tira 12 / 14 / 22 no combo de 3. O
inimigo tem 55 de vida e 1 de defesa, então chegam 11 / 13 / 21 = **45 num combo completo**.
Faltam 10 → um golpe qualquer termina. Ou seja: **combo completo + 1 golpe.**

**Quantos golpes para te matar?** Ele tira 14, você tem 120 e 0 de defesa → **9 golpes**.
Com os frascos (3 × 34 = 102 de cura), na prática uns 16.

**Uma coisa que talvez você queira mudar.** O `Vida` classifica um golpe como *forte*
quando ele tira mais que `Fracao Para Golpe Forte` da vida máxima. No inimigo isso é
`0.2 × 55 = 11` — e o seu golpe mais fraco tira exatamente 11. Resultado: **todo** golpe seu
conta como forte e atordoa ele por 0,55 s, que é mais do que o intervalo entre os seus
golpes. Na prática o inimigo fica travado durante o combo inteiro e não consegue revidar.

Isso não é um bug — é como muitos jogos de ação funcionam de propósito (combo = janela de
segurança). Mas se você quer que ele **interrompa** você:

- suba `Fracao Para Golpe Forte` do inimigo para **0.5** (aí só o terceiro golpe é forte); ou
- baixe `Tempo Atordoado Forte` para **0.25**; ou
- os dois, para um inimigo que revida de verdade.

Experimente e sinta. É o ajuste que mais muda a sensação do combate.

---

## Passo 7 — Trocar a arte

O inimigo usa a folha `LightBandit.png`, cortada numa grade 8×5 de 48×48. As oito animações
saem de faixas de quadros dessa folha:

| Clipe | Quadros | fps |
|---|---|---|
| `inimigo_parado` | 0–3 | 6 |
| `inimigo_alerta` | 4–7 | 8 |
| `inimigo_correr` | 8–15 | 12 |
| `inimigo_ataque1` | 16–23 | 16 |
| `inimigo_ataque2` | 24–31 | 16 |
| `inimigo_dano` | 32–33 | 10 |
| `inimigo_no_ar` | 34 | 8 |
| `inimigo_morrer` | 35 | 4 |

**Troca fácil (mesmo pacote de arte):** em `Assets/Editor/ConstrutorDeAnimacoes.cs`, troque
`LightBandit.png` por `HeavyBandit.png` nas 8 receitas `inimigo_*` — é a mesma grade — e
rode `Tools ▸ Jogo ▸ Reconstruir animações`. O inimigo inteiro muda de aparência.

**Velocidade de uma animação:** abra `Assets/Resources/BibliotecaDeAnimacoes.asset` e mexa
no `Quadros Por Segundo` do clipe. Vale na hora, sem reconstruir.

**Uma melhoria que dá para fazer agora:** a morte é **um quadro só** — é tudo que esse
pacote tem. Na receita de `inimigo_morrer`, troque `35, 1` por `32, 4` e baixe para 8 fps:
aí ele cambaleia e cai, em vez de piscar direto para o chão. O quadro do meio é uma pose de
pulo; se ficar estranho, volte para `35, 1`.

**Outro pacote de arte:** a pasta `Assets/Pixel Adventure 2` tem 20 bichos. Para usar um,
acrescente receitas novas apontando para a folha dele e ajuste `colunas`/`linhas` para a
grade daquela folha. O `ARQUITETURA.md` explica o formato da receita.

---

## Passo 8 — O que ele ainda não sabe fazer

Sendo honesto sobre os limites, para você não caçar um campo que não existe:

| Não faz | Se você quiser |
|---|---|
| **Pular** | não existe pulo na IA; ele para nas beiradas. Precisa de um estado novo |
| **Atirar** | é corpo a corpo. Um inimigo de longe precisa de projétil (componente novo) |
| **Barra de vida em cima da cabeça** | só tem o flash vermelho. Daria um script pequeno |
| **Voar / nadar** | usa gravidade e chão. Um voador precisa de outro controlador |
| **Manter distância** | ele só aproxima. Não recua para forçar você a ir até ele |
| **Morte com mais de 1 quadro** | limitação da arte (veja o Passo 7) |
| **Chamar os outros** | o evento `Ao Notar Jogador` existe e está vazio — dá para ligar nele |

Esse último é o gancho mais fácil e mais divertido: no Inspector, o `Ao Notar Jogador` é um
`UnityEvent`. Arraste outro inimigo para lá e chame um método público dele — e você tem
inimigos que alertam os vizinhos, sem escrever um script.

---

## Quando algo dá errado

| Sintoma | Causa quase sempre | Conserto |
|---|---|---|
| Golpeia o ar, nunca acerta | `Distancia De Ataque` maior que o alcance da hitbox | diminua a distância ou aumente `Tamanho Da Hitbox` |
| Não me vê | longe demais, ou diferença de altura > 1.6 | suba `Raio De Visao` / `Diferenca De Altura Maxima` |
| Me vê atravessando parede | `Parede Esconde` desmarcado | marque |
| Ignora quem está logo acima numa plataforma | é de propósito (`Diferenca De Altura Maxima`) | suba o valor se quiser agro vertical |
| Cai da plataforma | `Nao Cair Da Borda` desmarcado, ou sem `ChecadorDeBorda` | marque; confira se o filho existe e está na frente dos pés |
| Some sem animação de morte | `Atraso Para Destruir` menor que a animação | suba o atraso no `Vida` |
| Não morre nunca | `Destruir Ao Morrer` desmarcado no `Vida` | marque (no inimigo sim; no jogador **não**) |
| Um inimigo mata o outro | não deveria acontecer — a hitbox ignora a mesma camada | confira se o inimigo está na camada `Inimigo` |
| Empurra o jogador só de encostar | é a colisão dos corpos, não dano | normal; para atravessar, use a camada no `Dash Atravessa` do boneco |
| Trava no lugar tremendo | dois inimigos no mesmo ponto | separe; nascer dentro de um bloco também faz isso |
| Fica em guarda e nunca ataca | está numa beirada esperando | é de propósito — vá até ele |

---

## Colinha dos campos

| Grupo | Campo | Padrão |
|---|---|---|
| Movimento | `Velocidade De Patrulha` | 0.7 |
| | `Velocidade De Perseguicao` | 1.8 |
| | `Aceleracao` | 12 |
| Patrulha | `Patrulhar` | ligado |
| | `Alcance Da Patrulha` | 2.5 |
| | `Pausa Na Patrulha` | 0.8 |
| Visão | `Raio De Visao` | 4.5 |
| | `Raio De Alerta Por Tras` | 1.5 |
| | `Diferenca De Altura Maxima` | 1.6 |
| | `Parede Esconde` | ligado |
| | `Memoria De Agro` | 3 |
| | `Tempo De Alerta` | 0.35 |
| Ataque | `Distancia De Ataque` | 0.6 |
| | `Tempo De Preparo` | 0.3 |
| | `Duracao Do Golpe` | 0.45 |
| | `Inicio / Fim Da Janela` | 0.25 / 0.55 |
| | `Tempo De Recuperacao` | 0.6 |
| | `Chance De Combo` | 0.45 |
| | `Dano` | 14 |
| | `Forca Empurrao` | 4.5 |
| | `Avanco Do Golpe` | 1.2 |
| Hitbox | `Centro Da Hitbox` | (0.34, 0.2) |
| | `Tamanho Da Hitbox` | (0.6, 0.36) |
| Sensores | `Nao Cair Da Borda` | ligado |
| | `Alcance Do Checador` | 0.35 |
| Dano | `Tempo Atordoado Leve` | 0.25 |
| | `Tempo Atordoado Forte` | 0.55 |
| | `Vira Para Quem Bateu` | ligado |
| Vida | vida / defesa | 55 / 1 |
| | `Destruir Ao Morrer` | ligado, atraso 1.1 |

O código está em [Inimigo.cs](Assets/Scripts/Inimigo.cs) (comportamento) e
[AnimacaoDoInimigo.cs](Assets/Scripts/Inimigos/AnimacaoDoInimigo.cs) (estado vira desenho).
Vida, empurrão, flash e morte não estão lá — são do `Vida`, que é o mesmo componente do
jogador. É por isso que dá para trocar a arte, a vida ou a animação do inimigo sem mexer
numa linha de IA.
