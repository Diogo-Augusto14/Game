# Tutorial do Bootstrap

Arquivo separado, só sobre o Bootstrap: o que ele é, por que existe, o que ele faz **na
ordem exata**, como controlar cada parte, e como aposentar ele quando a cena virar sua.

É o único dos quatro tutoriais que fala de uma peça que você provavelmente vai **desligar**
um dia. Vale entender antes, para desligar sabendo o que perde.

---

## Passo 1 — Qual problema ele resolve

Num projeto Unity normal, para o jogo rodar alguém precisa ter preparado a cena: arrastado o
prefab do boneco, posto a tag certa, criado os objetos de sensor com os nomes certos,
arrastado a câmera, ligado a HUD. Se qualquer uma dessas coisas faltar, o Play abre e **nada
funciona** — e o erro no Console raramente aponta para a peça que falta.

O Bootstrap monta tudo isso por código, no primeiro quadro. O resultado prático:

- você clona o repositório e aperta Play. Funciona.
- você abre uma cena vazia e aperta Play. Funciona.
- você quebra a cena mexendo em algo. Aperta Play. Funciona.

Ele é um **andaime**, não uma peça permanente do jogo. Andaime bom é andaime que se tira.

### Por que ele constrói um boneco novo em vez de consertar o da cena

Essa decisão gera a pergunta mais comum ("por que tem dois bonecos na Hierarchy?"), então
vale a explicação.

Quando o Bootstrap entra em ação, o `Awake` de todos os componentes da cena **já rodou**. E
vários deles resolvem referências uma vez só, no `Awake`: o `Vida` procura o controlador de
movimento, o `Ataque` procura a hitbox, o `Movimento` procura os filhos de sensor. Se o
objeto da cena estava incompleto naquele instante — uma hitbox que não existia, um sensor com
o nome errado — **a referência ficou nula para sempre**, e consertar o objeto depois não
desfaz isso.

Foi exatamente o que acontecia nesta cena: o filho de sensor estava escrito `GoundCheck `
(sem o `r`, e com um espaço no fim). O `Movimento` não achava, logava erro e **se desligava
inteiro**. Um jogo que não anda por causa de uma letra.

Montar do zero, na ordem certa de dependência, é a única forma de garantir que toda mecânica
funciona no primeiro Play. O boneco velho **não é apagado** — só fica desativado durante o
jogo, e o arquivo da cena continua intacto.

---

## Passo 2 — O que ele faz, na ordem

Essa ordem não é arbitrária; cada passo depende do anterior.

```
 1. Gravidade do mundo                    (se Ajustar Gravidade)
 2. Esquece o cache de camadas
 3. Confere se a biblioteca de animações existe   → erro claro no Console se faltar
 4. Descobre ONDE o boneco nasce
 5. Monta o boneco                        (se Montar Jogador)
 6. Procura o chão embaixo do nascimento  → raio de 40 unidades para baixo
 7. Sem chão? cria um de emergência       (se Chao De Emergencia)
 8. Acrescenta as peças de fase           (se Completar Fase)
 9. Monta os inimigos                     (se Montar Inimigos)
10. Ajusta a câmera e calcula os limites  (se Ajustar Camera)
11. Monta a HUD                           (se Montar Hud)
12. Define a altura da morte do boneco
13. Escreve o relatório no Console        (se Relatorio No Console)
```

Dois "por quês" dessa ordem:

- **o boneco antes do chão** (5 antes de 6): a posição de nascimento é o ponto de partida do
  raio que procura o chão. Sem saber onde o boneco vai nascer, não há onde procurar.
- **a fase antes dos inimigos** (8 antes de 9): os inimigos são colocados em pontos do chão
  escolhidos para **não** cair dentro do túnel, das paredes ou do bloco da beirada. Se os
  inimigos viessem primeiro, um deles nasceria dentro de um bloco e sairia voando pro lado.

---

## Passo 3 — Ver acontecendo

Aperte **Play** e olhe o Console. Deve ter uma linha assim:

```
[Bootstrap] boneco montado | boneco antigo desativado (1) | fase completada (...) | 3 inimigo(s) | HUD montada
```

Esse relatório é a forma mais rápida de saber o que ele fez. Se algo que você esperava não
está na linha, a caixinha correspondente está desmarcada.

Agora olhe a **Hierarchy durante o Play**. Você consegue separar o que é seu do que é dele
pelos nomes:

| Objeto | Quem criou |
|---|---|
| `Jogador` | o Bootstrap |
| `Inimigo 1`, `Inimigo 2`... | o Bootstrap |
| `Fase (Bootstrap)` (com as peças dentro) | o Bootstrap |
| `HUD` | o Bootstrap |
| `Faiscas (Jogador)`, `Faiscas (Inimigo 1)` | os pools de efeito de impacto |
| `Bootstrap (automatico)` | a auto-instalação |
| `Player` e `LightBandit_0` **desativados** | eram da cena; ele desligou |
| `Square`, `Main Camera` | seus, intocados (a câmera ganhou um componente) |

Pare o Play: **tudo isso desaparece**. Nada foi salvo na cena. É essa a natureza do andaime.

---

## Passo 4 — A auto-instalação

O Bootstrap não precisa estar na cena. Ele se instala:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
private static void GarantirNaCena()
{
    if (FindAnyObjectByType<Bootstrap>(FindObjectsInactive.Include) != null)
        return;                                    // já tem um → não cria outro

    GameObject obj = new GameObject("Bootstrap (automatico)");
    obj.AddComponent<Bootstrap>();
}
```

Duas coisas para guardar:

1. **Apagar o componente da cena não desliga nada.** Ele volta no próximo Play, com os
   valores padrão. Essa é a causa do "eu desliguei e voltou".
2. **Se já existe um Bootstrap na cena, ele não cria outro** — inclusive um com a chave
   `Ativo` desmarcada. É assim que se desliga sem mexer em código: você deixa um desligado
   na cena, ocupando o lugar.

### As três formas de desligar

| Nível | Como | Quando usar |
|---|---|---|
| **Por parte** | desmarque só as caixinhas do que você já montou | migração aos poucos |
| **Chave mestra** | desmarque `Ativo` (e **não apague o objeto**) | a cena é sua, o projeto continua com o andaime |
| **No projeto** | símbolo `JOGO_SEM_BOOTSTRAP` em `Project Settings ▸ Player ▸ Scripting Define Symbols` | você terminou com o andaime de vez |

O caminho mais fácil é o menu: **`Tools ▸ Jogo ▸ Montar na cena ▸ Desligar o Bootstrap desta cena`**.
Ele deixa o objeto desligado no lugar certo e explica na janelinha.

---

## Passo 5 — Os campos, um por um

O que acontece se você **desmarcar** cada um:

| Campo | Padrão | Desmarcado / mudado |
|---|---|---|
| `Ativo` | ligado | ele não faz **absolutamente nada** |
| `Montar Jogador` | ligado | usa o boneco que já está na cena (e não desativa mais nada) |
| `Montar Inimigos` | ligado | os inimigos da cena ficam como estão |
| `Inimigos Novos` | 3 | quantidade **mínima**; a cena tendo menos, ele completa |
| `Completar Fase` | ligado | nenhuma peça é acrescentada — só o que você desenhou |
| `Montar Hud` | ligado | sem barra de vida, frascos nem lista de controles |
| `Chao De Emergencia` | ligado | cena sem chão = boneco cai para sempre |
| `Ajustar Camera` | ligado | sua câmera fica intocada (e sem seguir ninguém) |
| `Tamanho Da Camera` | 2.8 | meia altura em unidades; menor = mais perto |
| `Limitar Camera` | ligado | a câmera passa das bordas da fase |
| `Ajustar Gravidade` | ligado | vale a gravidade de `Project Settings ▸ Physics 2D` |
| `Gravidade` | (0, -9.81) | é o mesmo valor do projeto, então desmarcar não muda nada aqui |
| `Relatorio No Console` | ligado | ele monta calado |

> **Sobre `Ajustar Camera`:** ligado, ele força a câmera para ortográfica, muda o
> `Orthographic Size` e adiciona o componente `Cameramov`. Se você ajustou a câmera à mão e
> ela "volta" a cada Play, é esta caixinha.

---

## Passo 6 — Quatro modos de uso

### 1. Tudo automático — aprendendo e testando

Todas as caixinhas ligadas. É o padrão, e é o melhor jeito de experimentar mecânica: você
sempre tem um campo de treino completo, não importa o que fez na cena.

### 2. Minha fase, boneco dele — o modo mais útil no começo

| Campo | Valor |
|---|---|
| `Montar Jogador` | ligado |
| `Completar Fase` | **desmarcado** |
| `Montar Inimigos` | ligado, `Inimigos Novos` 0 |
| `Chao De Emergencia` | desmarcado |

Você desenha a fase e põe os inimigos onde quiser; ele só garante que o boneco, a câmera e a
HUD funcionam. Ponha um objeto vazio chamado **`PontoDeNascimento`** onde o boneco deve
nascer (veja o Passo 7).

### 3. Meu boneco, fase dele — afinando mecânica

| Campo | Valor |
|---|---|
| `Montar Jogador` | **desmarcado** |
| `Completar Fase` | ligado |

Você montou o boneco à mão (ou veio do prefab) e quer o campo de treino em volta dele para
testar cada mecânica.

### 4. Desligado — a cena é sua

`Ativo` desmarcado, objeto deixado na cena. Veja o Passo 9.

---

## Passo 7 — Como ele decide as coisas

### Onde o boneco nasce

Ele tenta nesta ordem, e para na primeira que der:

1. um objeto chamado **exatamente `PontoDeNascimento`**;
2. um objeto com componente `Player` (inclusive desativado);
3. um objeto com a **tag** `Player`;
4. a posição da `Main Camera`;
5. a origem (0, 0).

**O truque útil:** crie um objeto vazio (`GameObject ▸ Create Empty`), chame de
`PontoDeNascimento` e ponha onde quiser. É a forma limpa de mandar onde o boneco começa sem
precisar de um boneco na cena. Serve também como checkpoint inicial.

### Onde ele acha o chão

Um raio de **40 unidades para baixo**, saindo meio metro acima do nascimento, procurando as
camadas de chão **e de parede** (dá para ficar de pé em cima de um bloco de parede). O ponto
onde bate é a referência de altura de tudo: as peças de fase, os inimigos e a altura da morte
(`chão − 12`).

Se não achar nada e `Chao De Emergencia` estiver ligado, ele cria um chão de 30 × 0,8 logo
abaixo do nascimento.

### Onde ficam as peças de fase

Todas relativas ao nascimento (`x`) e ao chão (`y`), dentro de um objeto `Fase (Bootstrap)`:

| Peça | Posição | Tamanho | Serve para |
|---|---|---|---|
| `Tunel` | x+1,6 · y+0,62 | 1,6 × 0,4 | escorregar por baixo |
| `Plataforma 1` | x+3,6 · y+0,8 | 1,4 × 0,25 | pulo simples |
| `Plataforma 2` | x+5,4 · y+1,7 | 1,4 × 0,25 | pulo duplo |
| `Beirada` | x+7,4 · y+1,1 | 1,2 × 2,2 | pendurar (camada de parede) |
| `Parede A` | x+9,4 · y+1,6 | 0,3 × 3,2 | wall jump |
| `Parede B` | x+10,9 · y+1,6 | 0,3 × 3,2 | o outro lado do corredor |
| `Saida do corredor` | x+12,3 · y+3,0 | 2 × 0,25 | onde cair ao sair do corredor |
| `Escada` | x−3,2 | 0,4 × 2,4 | subir (trigger) |
| `Topo da escada` | x−2,0 | 2 × 0,25 | o piso **ao lado** da escada |

Se você quer o campo de treino num lugar diferente, mova o `PontoDeNascimento` — tudo
acompanha.

### Onde ficam os inimigos

1. Reaproveita a posição de **cada inimigo que já estava na cena** (e desativa o original).
2. Faz o mesmo com objetos de demonstração dos pacotes de arte (nome contendo `LightBandit`,
   `HeavyBandit`, `Bandit`).
3. Completa até `Inimigos Novos` usando pontos escolhidos a dedo — **x+2,7 · x−6,5 · x+6,3 ·
   x−9,2 · x+13,6** — que caem no chão livre, longe do túnel, das paredes e da beirada.

### Os limites da câmera

Ele encapsula todos os colliders **sólidos e estáticos** da cena (não-trigger, sem
Rigidbody2D), folga 1,5 na horizontal e 4 na vertical, e entrega isso para o `Cameramov`.
Efeito: a câmera não mostra o vazio fora da fase. Fase menor que a tela num eixo → ele
centraliza nesse eixo em vez de travar torto.

---

## Passo 8 — O Construtor: a receita compartilhada

O Bootstrap **não sabe** montar um boneco. Quem sabe é
[Construtor.cs](Assets/Scripts/Bootstrap/Construtor.cs) — uma classe estática, sem estado,
com três receitas: `MontarJogador`, `MontarInimigo` e `MontarBloco`.

Duas coisas usam essas receitas: o Bootstrap (no Play) e o menu `Montar na cena` (no editor).
Se cada um tivesse a sua cópia, um dia o boneco do Play e o boneco do prefab iam divergir num
detalhe e a caça ao bug levaria uma tarde. **Existe uma receita só.**

### A ordem de dependência

Dentro da receita, a ordem dos `AddComponent` é a parte que importa mais:

```
Rigidbody2D → BoxCollider2D → (filhos: Visual, sensores, Hitbox)
 → Entrada → Movimento → Vida → Ataque → Cura → EfeitoDeImpacto
 → AnimacaoDoJogador → Player
```

Fora do editor, `AddComponent` chama o `Awake` **na hora**. E vários componentes procuram os
vizinhos no próprio `Awake`, guardando a referência uma vez só:

| Componente | Procura no Awake | Se o vizinho não existir ainda |
|---|---|---|
| `Vida` | `IControladorDeMovimento` (o `Movimento`) | o empurrão do dano nunca funciona |
| `Ataque` | a `Espada` nos filhos | o golpe não machuca ninguém |
| `Movimento` | os filhos `GroundCheck`, `WallCheck`, `LedgeCheck` | ele cria os que faltarem |
| `Cura` | `Ataque`, `Movimento`, `Entrada` | a cura não cancela direito |

É por isso que os **filhos são criados antes dos componentes de lógica**, e por isso que a
hitbox do inimigo passou a ser criada pela receita em vez de pelo `Awake` do `Inimigo`: no
editor o `Awake` não roda, e o prefab sairia sem hitbox.

### Os números do "jogo pronto"

A receita também aplica valores que o padrão do arquivo não pode ter, porque o mesmo
componente serve para o boneco e para o inimigo:

| | Vida | Defesa | Destruir ao morrer | Pisca |
|---|---|---|---|---|
| Jogador | 120 | 0 | **não** (ele renasce) | sim |
| Inimigo | 55 | 1 | sim, após 1,1 s | não |

O `Destruir Ao Morrer` é o campo que **tem** que ser diferente entre os dois — e é o erro
mais fácil de cometer montando à mão.

---

## Passo 9 — Aposentar o Bootstrap

Quando a cena virar sua, rode:

```
Tools ▸ Jogo ▸ Montar na cena ▸ Kit completo
```

Ele monta Jogador, Inimigo, a paleta de blocos, a HUD e a câmera **na cena**, salva os
prefabs, e já desliga o Bootstrap. Detalhes no `TUTORIAL.md`, Passo 4.

**O que conferir depois de desligar**, porque deixam de acontecer sozinhos:

| O que o Bootstrap fazia | Depois de desligado |
|---|---|
| Achava o chão e definia a `Altura Da Morte` | ajuste no componente `Player` (padrão −20) |
| Calculava os limites da câmera | use `Usar Limites` + os cantos, no `Cameramov` |
| Garantia a tag `Player` e as camadas | o prefab já vem com elas — mas confira em objetos feitos à mão |
| Desativava bonecos duplicados | **você** vira o responsável: um boneco só na cena |
| Punha a gravidade em −9,81 | é o valor do projeto, então não muda nada |

E o aviso que vale repetir: **não apague o objeto `Bootstrap (desligado)`**.

### Chamar na mão

`Montar()` é público. Dá para deixar `Ativo` desmarcado e chamar de outro script quando você
quiser — por exemplo, num botão de "recarregar campo de treino", ou depois de um menu:

```csharp
FindAnyObjectByType<Bootstrap>().Montar();
```

---

## Passo 10 — O que ele não faz

Para você não procurar um campo que não existe:

| Não faz | Onde isso viveria |
|---|---|
| **Salvar** o que montou na cena | é o menu `Montar na cena` que faz isso |
| **Checkpoints** | o `Player` tem `DefinirPontoDeRenascimento(Transform)`; falta quem chame |
| **Trocar de sala / carregar cena** | precisa de um gerenciador de cenas |
| **Música e som** | nenhum componente de áudio é montado |
| **Menu, pause, game over** | o `Player` tem os eventos `Ao Morrer` / `Ao Renascer` para ligar |
| **Mais de um tipo de inimigo** | ele monta todos iguais; use variantes de prefab no modo manual |
| **Fase de verdade** | as peças são um campo de treino, não um nível projetado |

Esse penúltimo é o sinal mais claro de quando aposentar o andaime: no dia em que você quiser
dois tipos de inimigo na mesma fase, o modo manual passou a ser melhor.

---

## Quando algo dá errado

| Sintoma | Causa | Conserto |
|---|---|---|
| **Dois bonecos** na cena | Bootstrap ligado + boneco montado à mão | desmarque `Montar Jogador` (ou `Ativo`) |
| "Desliguei e voltou" | o objeto do componente foi apagado | deixe um na cena com `Ativo` desmarcado |
| Boneco nasce no lugar errado | ele achou um `Player` antigo primeiro | crie um `PontoDeNascimento`, ou apague o boneco velho |
| Peças de fase no lugar errado | estão relativas ao **nascimento** | mova o `PontoDeNascimento` |
| Inimigo nasce dentro de um bloco | você mudou a fase e um ponto fixo caiu num bloco | ponha os inimigos à mão e use `Inimigos Novos` 0 |
| Câmera "volta" a cada Play | `Ajustar Camera` ligado | desmarque |
| Gravidade muda a cada Play | `Ajustar Gravidade` ligado | desmarque |
| Console: biblioteca de animações não existe | ela nunca foi gerada | `Tools ▸ Jogo ▸ Preparar projeto` |
| Chão de emergência aparecendo sem querer | o raio não achou seu chão | confira a **camada** dos seus blocos (chão ou parede) |
| Boneco cai para sempre | sem chão, e `Chao De Emergencia` desmarcado | marque, ou ponha chão |
| Fase duplicada (dois `Fase (Bootstrap)`) | dois Bootstrap na cena | `[DisallowMultipleComponent]` impede no mesmo objeto, mas não em dois objetos — apague um |
| Nada acontece no Play | `Ativo` desmarcado, ou o símbolo `JOGO_SEM_BOOTSTRAP` está ligado | é isso mesmo |

---

## Colinha

| Campo | Padrão |
|---|---|
| `Ativo` | ligado |
| `Montar Jogador` | ligado |
| `Montar Inimigos` | ligado |
| `Inimigos Novos` | 3 (mínimo) |
| `Completar Fase` | ligado |
| `Montar Hud` | ligado |
| `Chao De Emergencia` | ligado |
| `Ajustar Camera` | ligado |
| `Tamanho Da Camera` | 2.8 |
| `Limitar Camera` | ligado |
| `Ajustar Gravidade` | ligado |
| `Gravidade` | (0, −9.81) |
| `Relatorio No Console` | ligado |

| Menu | O que faz |
|---|---|
| `Tools ▸ Jogo ▸ Preparar projeto` | camadas, tags, matriz de colisão e a biblioteca |
| `Tools ▸ Jogo ▸ Conferir a cena` | lista o que está faltando |
| `Tools ▸ Jogo ▸ Montar na cena ▸ Kit completo` | passa para o modo manual |
| `Tools ▸ Jogo ▸ Montar na cena ▸ Desligar o Bootstrap desta cena` | só a chave mestra |

O código está em [Bootstrap.cs](Assets/Scripts/Bootstrap/Bootstrap.cs) (o quê e quando) e
[Construtor.cs](Assets/Scripts/Bootstrap/Construtor.cs) (o como). O menu do editor está em
[MontadorDeCena.cs](Assets/Editor/MontadorDeCena.cs).

---

## Os quatro tutoriais

| Arquivo | Assunto |
|---|---|
| `TUTORIAL.md` | o caminho: do primeiro Play até a sua própria fase |
| `TUTORIAL-PLAYER.md` | o boneco por dentro, e receitas de sensação |
| `TUTORIAL-INIMIGO.md` | a IA por dentro, e receitas de inimigo |
| `TUTORIAL-BOOTSTRAP.md` | este: o andaime, e como tirá-lo |

Referência de "onde está o quê": `ARQUITETURA.md`. Caderno de estudo das peças da Unity:
`MANUAL.md`.
