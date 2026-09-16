# Tutorial — do primeiro Play até a sua própria fase

Arquivo separado de propósito: o `MANUAL.md` é o seu caderno de estudo das peças da Unity,
o `ARQUITETURA.md` é a referência de "onde está o quê". **Este aqui é passo a passo.** Faça
na ordem, uma vez, e você sabe mexer no jogo inteiro.

Tempo: uns 40 minutos, sem pressa.

---

## Passo 0 — Abrir

1. Abra o projeto na Unity.
2. Abra a cena `Assets/Scenes/SampleScene.unity` (duplo clique).
3. Espere a barrinha de compilar sumir no canto de baixo à direita.

**Na primeira vez**, o editor gera sozinho o arquivo de animações
(`Assets/Resources/BibliotecaDeAnimacoes.asset`). Ele lê as 43 folhas de sprite da pasta
`player` e recorta 51 clipes. Leva alguns segundos e aparece uma barrinha de progresso.

Se não aparecer nada e depois der erro no Console falando de biblioteca, rode na mão:
**`Tools ▸ Jogo ▸ Preparar projeto`**.

---

## Passo 1 — Aperte Play

Só isso. Não precisa arrastar nada.

O que aconteceu: um componente chamado **Bootstrap** se instalou sozinho e montou o boneco
completo, os inimigos, a câmera, a HUD e algumas peças de fase para você testar as
mecânicas. O boneco antigo da cena foi desativado (o arquivo da cena não foi mexido).

O que você deve ver:

- o boneco parado, respirando (animação de idle rodando);
- em cima à esquerda, a **barra de vida vermelha**, uma barrinha fina de cura embaixo dela,
  e **três quadradinhos azuis** (os frascos);
- em baixo à esquerda, a **lista de controles**;
- alguns inimigos no chão, andando de um lado para o outro.

No Console deve ter uma linha assim:

```
[Bootstrap] boneco montado | fase completada (...) | 3 inimigo(s) | HUD montada
```

> **Se o boneco estiver invisível:** pare o Play, rode `Tools ▸ Jogo ▸ Preparar projeto`,
> e dê Play de novo. Quer dizer que a biblioteca de animações não existia.

---

## Passo 2 — Conhecer o boneco

Você está num campo de treino. Cada peça que o Bootstrap montou serve para testar uma
mecânica. **Faça na ordem** — cada exercício usa o que você aprendeu no anterior.

### 2.1 Andar e correr

- `A` e `D` (ou as setas) para os lados.
- Segure `Ctrl` enquanto anda: ele **anda devagar** em vez de correr.

Olhe os pés. Ao sair do zero tem uma animação de arranque; ao soltar a tecla em velocidade,
uma de frear; ao inverter o lado correndo, uma de virar (a derrapada). São animações
diferentes, não a mesma acelerada.

### 2.2 Pular

- `Espaço` pula. **Aperte de novo no ar** e ele dá o segundo pulo (pulo duplo).
- Solte o `Espaço` cedo, ainda subindo: o pulo fica baixinho. Segure: fica alto.
  Isso se chama corte do pulo, e é o que faz o controle parecer preciso.
- Ande até a beirada de uma plataforma e pule **um tiquinho depois** de já ter saído dela.
  Ainda pula. Isso é o *coyote time* (0,1 s de tolerância).
- Caia de bem alto: ele **pousa rolando** em vez de parar seco.

### 2.3 Agachar e escorregar

Ande para a **direita** até o primeiro bloco na altura da cintura — é o **túnel**.

- `Baixo` agacha. Agachado ele anda devagar e o colisor encolhe.
- Segure `Baixo` e aperte `Shift`: ele **escorrega**. É assim que se passa por baixo do
  túnel — só agachado não cabe.
- Escorregue para dentro do túnel e **solte o `Baixo` no meio dele**. Ele continua agachado,
  porque não cabe de pé. Só levanta quando sair.

### 2.4 Dash e esquiva

- `Shift` sozinho: **dash**, uma arrancada em linha reta. Funciona no chão e no ar.
- Durante o dash ele é **invencível** — dá para atravessar inimigo.
- `Shift` segurando a direção **contrária** à que ele olha: **esquiva para trás**. Ele pula
  para trás continuando de frente para o inimigo.
- O dash tem recarga (0,35 s). Apertar duas vezes seguidas não dá dois dashes.

### 2.5 Pendurar na beirada

Continue para a direita até o **bloco alto azul escuro**.

- Pule e caia **raspando na quina de cima** do bloco. Ele se **pendura**.
- Pendurado: `Cima` (ou `Espaço`) para **subir**, `Baixo` para **soltar**.

Se não pendurar, você está passando longe da quina. Tem que encostar o peito na parede com
a cabeça livre acima.

### 2.6 Wall jump

Mais à direita tem **duas paredes azuis de frente uma para a outra**. É um corredor.

- Pule para uma delas **segurando a direção da parede**. Ele gruda e **desce devagar**
  (wall slide).
- Grudado, aperte `Espaço`: ele pula para o **lado contrário**.
- Segure a direção da outra parede, `Espaço` de novo. E de novo. Você sobe o corredor em
  ziguezague até a plataforma de saída.

O detalhe que faz funcionar: logo depois do wall jump o teclado fica travado por 0,16 s.
Sem isso, você estar segurando a direção da parede cancelaria o próprio pulo.

### 2.7 Escada

Agora vá para a **esquerda** do ponto onde você nasceu. Tem uma barra marrom vertical.

- Entre nela e aperte `Cima`: ele **sobe**. `Baixo` desce.
- Parado na escada a animação congela (ele não sobe sem sair do lugar).
- `Espaço` na escada: desgruda e pula.
- `Baixo` + `Shift` na escada: **escorrega** para baixo rápido, estilo bombeiro no cano.
- Subindo até o topo, ele **sai sozinho** e pisa na plataforma do lado.

### 2.8 Combate

Ache um inimigo.

- `J` ou **botão esquerdo do mouse**: golpe. Aperte **três vezes no ritmo** — sai um combo
  de 3, e o terceiro é bem mais forte (22 de dano contra 12 do primeiro).
- Se você demorar entre os golpes, o combo **volta para o primeiro**. A janela para emendar
  abre em 55% do golpe anterior.
- No ar, o combo é de 2 golpes (animações diferentes).
- `J` **no meio de um dash**: golpe de dash, que avança junto.
- No ar, segurando `Baixo`, `J`: **mergulho** — ele desce de lâmina e só para no chão.
- Levou golpe no meio do seu golpe? O golpe é **cortado**.

Repare no inimigo: ele para, **telegrafa** (fica em guarda por 0,3 s) e só então bate. Essa
janelinha é de propósito — é ela que te dá chance de reagir.

### 2.9 Curar

- Perca vida primeiro (deixe um inimigo te acertar).
- **Segure `E`**, parado no chão. A barrinha fina embaixo da vida enche.
- Enchendo até o fim: gasta um frasco (um quadradinho azul apaga) e recupera 34 de vida.
- Agora tente de novo e **ande no meio**, ou pule, ou leve um golpe. Cancela — e o frasco
  **não** é gasto.

### 2.10 Morrer

Deixe os inimigos te matarem, ou caia num buraco.

Ele toca a animação de morte, espera 1,6 s, **renasce** onde a fase começou com vida cheia,
frascos cheios e um instante de invencibilidade. A câmera pula direto para lá, sem voar pela
fase.

---

## Passo 3 — Mexer nos números

**Com o jogo rodando**, selecione o objeto `Jogador` na Hierarchy e ache o componente
**Movimento** no Inspector. Mexa e sinta na hora:

| Campo | Padrão | Tente |
|---|---|---|
| `Velocidade Maxima` | 3.2 | 6 — corrida de jogo de ação |
| `Jump` | 6.5 | 9 — pulo de lua |
| `Maximo De Pulo` | 2 | 3 — pulo triplo |
| `Gravidade Na Queda` | 1.5 | 3 — queda seca, pulo "pesado" |
| `Dash Velocidade` | 7.5 | 14 — teleporte |
| `Dash Recarga` | 0.35 | 0 — dash infinito |

> **Atenção:** valor mexido durante o Play **volta ao normal** quando você para. É de
> propósito (é assim que se acerta um número sem estragar o projeto). Para guardar de
> verdade, pare o Play e mexa com o jogo parado.

Outros componentes que vale abrir: **Ataque** (dano e alcance de cada golpe do combo),
**Vida** (vida, defesa, invencibilidade), **Cura** (quantos frascos), **Inimigo** (visão,
patrulha, telegrafo).

---

## Passo 4 — Sair do automático

Até aqui o Bootstrap montou tudo. Agora a cena vai ser sua.

**Pare o Play.** Rode:

```
Tools ▸ Jogo ▸ Montar na cena ▸ Kit completo
```

Aparece uma janelinha explicando o que foi feito. O que mudou na sua cena:

- um objeto **`Jogador`** de verdade, completo, que você pode mover e ajustar;
- um objeto **`Inimigo`** igual;
- um **`Kit de cenario`** com um bloco de cada tipo, lado a lado;
- uma **`HUD`**;
- um objeto **`Bootstrap (desligado)`**;
- e em `Assets/Prefabs/` os **prefabs** de tudo isso.

**Aperte Play.** Deve funcionar igual — mas agora é a sua cena rodando, não a montada por
código.

> ⚠️ **Não apague o objeto `Bootstrap (desligado)`.** É a presença dele na cena que impede o
> automático de instalar outro. Se apagar, ele volta no próximo Play e você fica com dois
> bonecos.

### A paleta

A **cor diz o comportamento**. Família cinza: pisa em cima. Família azul: é parede.

| Peça | Cor | Serve para |
|---|---|---|
| `Chao` | cinza | chão normal |
| `Plataforma` | cinza claro | igual, só mais fina |
| `Tunel` | cinza esverdeado | teto baixo, para passar escorregando |
| `Parede` | azul | wall slide e wall jump |
| `Beirada` | azul escuro | pendurar na quina e subir |
| `Escada` | marrom | subir com `Cima` |

A diferença entre cinza e azul é só a **camada** (Layer) do objeto. O boneco procura chão
na camada de chão e parede na camada de parede. Trocar a camada de um bloco troca o que ele
faz — é literalmente a única diferença entre uma plataforma e uma parede.

---

## Passo 5 — Sua primeira sala

Agora o exercício de verdade. Vamos montar uma sala pequena, do zero.

### 5.1 Esticar um bloco

1. Na Hierarchy, abra `Kit de cenario` e clique em **`Chao`**.
2. Na barra de ferramentas, escolha a **ferramenta de retângulo** (o ícone quadrado, atalho
   `T`). Aparecem alças nas bordas do bloco.
3. **Arraste uma alça do lado.** O bloco estica e a textura **repete** em vez de deformar.

Olhe o `Box Collider 2D` no Inspector enquanto arrasta: o `Size` dele **acompanha sozinho**.
É o componente `BlocoDoCenario` fazendo isso. Sem ele, você esticaria o desenho e a colisão
ficaria do tamanho antigo — o boneco atravessaria o chão e você levaria meia hora para
descobrir por quê.

### 5.2 Duplicar

1. Com o `Chao` selecionado, `Ctrl+D`.
2. Arraste a cópia para o lado (ferramenta de mover, atalho `W`).

É esse o fluxo: **clonar e esticar**. Você não monta bloco novo, você copia um que já está
certo.

### 5.3 A sala

Monte isto (as medidas são sugestão, o olho manda):

1. **Chão**, 14 de largura. Deixe o `Jogador` em pé na esquerda dele.
2. Duas **plataformas** subindo em escada: a primeira a 0,8 de altura, a segunda a 1,7.
   Um pulo alcança a primeira; a segunda só com pulo duplo.
3. Um **túnel** no meio do chão. Regra: o vão entre o chão e a base do túnel tem que ser de
   mais ou menos **0,35** — menos que a altura do boneco de pé (0,52) e mais que a dele
   agachado (0,29). Só passa escorregando.
4. Duas **paredes** de frente uma para a outra, com **1,2 de vão** entre elas e umas 3 de
   altura. Esse é o corredor de wall jump.
5. Uma **beirada** (bloco alto) num canto, para pendurar.
6. Uma **escada**, e uma **plataforma ao lado dela** com o topo **na mesma altura** do topo
   da escada.

> **Sobre a escada:** a plataforma vai **ao lado**, nunca em cima. Em cima, o boneco bateria
> a cabeça nela antes de chegar no último degrau. Ao sair pelo topo, ele procura piso na
> posição dele e um passo para cada lado — e pisa no primeiro que achar.

### 5.4 Salvar

`Ctrl+S`. Pronto, a sala é sua.

Se você exagerou e quer começar de novo: `Ctrl+Z` desfaz o comando do menu inteiro de uma
vez (ele registra tudo como um único passo).

---

## Passo 6 — Inimigos

1. Arraste `Assets/Prefabs/Inimigo.prefab` da janela Project para a cena.
2. Posicione em cima do chão.
3. Repita quantas vezes quiser.

Com o inimigo selecionado, os números que mais mudam o jogo:

| Campo | Padrão | O que faz |
|---|---|---|
| `Raio De Visao` | 4.5 | de quão longe ele te percebe |
| `Raio De Alerta Por Tras` | 1.5 | de quão longe ele percebe **pelas costas** |
| `Diferenca De Altura Maxima` | 1.6 | acima disso ele ignora (não agro de outra plataforma) |
| `Alcance Da Patrulha` | 2.5 | quanto ele anda para cada lado quando está de boa |
| `Tempo De Preparo` | 0.3 | o telegrafo — aumente para deixar mais fácil |
| `Chance De Combo` | 0.45 | chance de emendar um segundo golpe |
| `Memoria De Agro` | 3 | quanto tempo ele te persegue depois de te perder de vista |

Selecione o inimigo e olhe a **Scene**: os círculos amarelo (visão), laranja (ouvido), a
linha vermelha (alcance do golpe), a azul (patrulha) e a verde (o sensor de beirada). Dá
para ajustar tudo de olho, sem chutar.

**Vida e dano** ficam no componente `Vida` do inimigo (55 de vida, 1 de defesa) e nos campos
`Dano` / `Forca Empurrao` do `Inimigo`.

---

## Passo 7 — Mexer nas animações

Abra `Assets/Resources/BibliotecaDeAnimacoes.asset` e olhe o Inspector. São 51 clipes, cada
um com:

- `Quadros Por Segundo` — a velocidade;
- `Em Loop` — se repete para sempre;
- `Manter Ultimo Quadro` — se congela no fim ou desaparece.

**Mexa nesses três à vontade**, vale na hora. Exemplo: achou a corrida lenta? aumente o
`Quadros Por Segundo` de `correr`.

Para mudar **qual folha** ou **quais quadros** um clipe usa, é na tabela de receitas em
`Assets/Editor/ConstrutorDeAnimacoes.cs`:

```csharp
new Receita(NomesDeAnimacao.Correr, PLAYER + "run/sprite sheets/run.png", 4, 5, 20f, true),
//          nome do clipe           folha                             col lin fps  loop
```

Mexeu? Rode **`Tools ▸ Jogo ▸ Reconstruir animações`**.

Um exemplo útil: trocar a arte do inimigo. As receitas `inimigo_*` apontam para
`LightBandit.png`. Troque para `HeavyBandit.png` (mesma grade, 8×5) e o inimigo inteiro muda
de aparência.

> As folhas de arte **não** são alteradas por nada disso. A biblioteca guarda "textura +
> retângulo + pivô" e os quadros são recortados na hora de rodar. Os cortes que você já fez
> nas folhas continuam intactos.

---

## Quando algo dá errado

| Sintoma | Causa quase sempre | Conserto |
|---|---|---|
| Boneco invisível | biblioteca de animações não existe | `Tools ▸ Jogo ▸ Preparar projeto` |
| Console: `nao existe o clipe "x"` | a receita não achou a folha de sprite | confira o caminho na tabela de receitas |
| **Dois bonecos** na cena | Bootstrap ligado + jogador montado à mão | desmarque `Ativo` no Bootstrap |
| Bootstrap "voltou" sozinho | o objeto desligado foi apagado | rode `Montar na cena ▸ Desligar o Bootstrap` |
| Boneco atravessa um bloco | o bloco está na camada errada, ou sem collider | camada de chão ou de parede; confira o `Box Collider 2D` |
| Esticou o bloco e o boneco atravessa | falta o `BlocoDoCenario` no bloco | adicione o componente (ou copie um bloco da paleta) |
| Não gruda na parede | bloco na camada de **chão**, não de parede | troque a camada para parede |
| Não pendura na beirada | bloco não é de parede, ou tem coisa acima da quina | camada de parede, e nada em cima |
| Inimigo não me vê | longe demais, ou diferença de altura > 1.6 | aumente `Raio De Visao` ou `Diferenca De Altura Maxima` |
| Inimigo golpeia o ar | `Distancia De Ataque` maior que o alcance da hitbox | diminua a distância, ou aumente `Tamanho Da Hitbox` |
| Boneco cai para sempre | sem chão embaixo, e `Altura Da Morte` longe | ponha chão, ou ajuste `Altura Da Morte` no `Player` |
| Boneco desaparece ao morrer | `Destruir Ao Morrer` ligado no `Vida` dele | **desmarque** — só inimigo é destruído |
| Sprite piscando | sobrou um `Animator` no objeto | o `AnimadorDeSprites` desliga sozinho e avisa no Console |
| Mudei um número e voltou | você mexeu **durante** o Play | pare o Play e mexa com o jogo parado |

---

## Colinha dos controles

| Tecla | Ação |
|---|---|
| `A` / `D` | andar / correr |
| `Ctrl` | andar devagar |
| `Espaço` | pular (2×) · na parede = wall jump |
| `Shift` | dash |
| `Shift` + `Baixo` | escorregar |
| `Shift` + direção contrária | esquiva para trás |
| `J` / mouse | golpe (combo 1-2-3) |
| `J` no ar + `Baixo` | mergulho |
| `Baixo` | agachar · soltar beirada · descer escada |
| `Cima` | entrar na escada · subir da beirada |
| `E` (segurar) | curar |

---

## Para onde ir depois

Você já sabe: rodar, testar cada mecânica, ajustar números, montar fase à mão e mexer nas
animações. Dá para fazer um jogo inteiro só com isso.

Quando quiser entender **como** funciona por dentro — por que não tem Animator Controller,
como o pivô das animações é calculado, como o sistema de dano conversa com o movimento —
está tudo no `ARQUITETURA.md`.

E quando quiser mexer no código, a ordem de leitura que faz mais sentido é:
`Entrada.cs` (o que o teclado diz) → `Movimento.cs` (a máquina de estados) →
`AnimacaoDoJogador.cs` (estado vira desenho) → `Ataque.cs` (o combo) →
`Inimigo.cs` (a IA). Nessa ordem cada arquivo só usa coisas que você já viu.
