# Itens e coletáveis

Tudo em `Assets/Scripts/Itens/`. Funciona sozinho na cena do jogo
(`Assets/Scenes/Jogo.unity`): é só dar Play.

## O que tem no andar

| Onde | O que aparece |
|---|---|
| Sala do item (chão dourado) | Pedestal com um item passivo. Encostou, pegou. |
| Sala do chefe | Pedestal com item e a **chave dourada** quando a sala é limpa (menos no chefe final). |
| Sala comum | Quando é limpa: 10% de chance de um baú de madeira com 2 ou 3 coletáveis, senão 50% de chance de um coletável no meio. |
| Salas comuns longe do início | Um baú de ferro trancado por andar (dois do andar 3 em diante). |
| Cada inimigo | 15% de chance de soltar um coletável ao morrer (chave só em 5% disso). |
| Andar 2 em diante | A porta da sala do item tem cadeado (gasta 1 chave). |

As chances ficam no componente `Andar ▸ Itens e coletaveis` e `Andar ▸ Chaves e baus`.

## Chaves e baús

Chave é rara de propósito. Ela vem do chefe (a chave dourada girando, `ChaveDoChefe.cs`),
da loja, do baú da sala amaldiçoada e, bem de vez em quando, de um inimigo. A chave do
chefe do andar 1 é a que abre a sala do item do andar 2, e assim por diante; ou dá pra
gastar ela antes num baú de ferro, se o jogador preferir.

| O quê | Como abre | O que tem |
|---|---|---|
| Porta com cadeado (`Tranca.cs`) | Encostar com chave: o cadeado estala, o portão dourado sobe e só então passa | A sala do item |
| Baú de madeira (`Bau.Criar`) | Encostar | Os coletáveis que a sala sorteou |
| Baú de ferro (`Bau.CriarTrancado`) | Encostar com chave. Sem chave ele treme e faz o som de negado | 35%: um item passivo num pedestal. Senão, um de quatro tesouros (`Bau.SortearTesouro`): bolsa de moedas, arsenal de bombas, kit de cura ou um pouco de tudo, com mais moedas nos andares fundos |

Os baús abrem com a animação dos pacotes de masmorra (a tampa sobe e o tesouro brilha), com
som de rangido e brilho (`Som.BauAbre`) e de cadeado (`Som.Destranca`).

## Coletáveis

- **Coração** (vermelho): cura 20. Só é pego se você estiver machucado.
- **Moeda** (amarela): guardada pra loja, que ainda não existe.
- **Chave**: abre o cadeado da sala do item e os baús de ferro. A do chefe é dourada e gira.
- **Bomba** (preta com pavio): `E` solta uma bomba. Ela explode depois de 1,5 s, tira 60 dos
  inimigos e 20 de você se estiver perto. Você começa com 1.

Os contadores ficam embaixo da barra de vida.

## Itens passivos

Estão em `CatalogoDeItens` (`ItemPassivo.cs`). Cada item é uma lista de ajustes: dano,
cadência, alcance, velocidade do tiro, tamanho da lágrima, lágrimas extras (leque),
velocidade de andar, vida máxima e brindes (moedas, chaves, bombas).

Pra criar um item novo, acrescente uma linha em `CatalogoDeItens.Montar()`:

```csharp
new ItemPassivo("Nome", "Frase que aparece na tela", corDoItem)
    { somaDano = 1f, lagrimasExtras = 1 },
```

O `EstatisticasDoJogador` recalcula tudo a partir dos números de base a cada item: soma
primeiro, multiplica depois. Um item não sai duas vezes na mesma partida enquanto houver
item novo no catálogo.

Cada item tem `nome`, `descricao` (a frase curta que aparece ao pegar) e `Descricao` (o
efeito explicado por inteiro, que aparece na etiqueta da loja e ao passar o mouse no item).

## Efeitos especiais

Os itens que não são só número moram em `EfeitosDosItens.cs`, que o
`EstatisticasDoJogador` põe sozinho no jogador. Cada campo do `ItemPassivo` liga um efeito:

| Item | Campo | O que faz |
|---|---|---|
| Pena da Fenix | `renasce` | Uma vez por partida, em vez de morrer volta com metade da vida (`Vida.AntesDeMorrer`) |
| Escudo Sagrado | `escudoPorSala` | Bloqueia o primeiro golpe de cada sala; o escudinho em cima da cabeça mostra que está carregado (`Vida.Bloquear`) |
| Sangue de Vampiro | `inimigosParaCurar` | A cada 5 inimigos derrotados, cura meio coração (`InimigoDeSala.AlgumMorreu`) |
| Prego Enferrujado | `danoDeEspinhos` | Ao levar dano, espinhos saem em volta e ferem os inimigos perto (`EspinhosQueSaem.cs`) |
| Pedra-Ima | `raioDoIma` | Moedas, chaves, bombas e corações perto vêm sozinhos |
| Amuleto da Sorte | `multiplicaSorte` | Multiplica a chance de premio (`TabelaDeDrops.Sorte`) |
| Bolsa do Mercador | `descontoNaLoja` | Loja 35% mais barata (`Loja.Desconto`) |
| Brasa da Furia | `furia` | +60% de dano com um coração ou menos |
| Elixir de Nevoa | `somaInvencibilidade` | Mais tempo invencível depois de levar dano |
| Orbe Guardiao | `orbes` | Um orbe gira em volta, apaga tiros inimigos e fere quem encosta (`OrbeGuardiao.cs`) |
| Barril de Polvora | `multiplicaBomba` | Bombas com raio e dano 50% maiores (`Bomba.Criar`) |
| Carne Assada | `curaAoLimparSala` | Cura meio coração ao limpar uma sala |
| Pacto de Sangue | `multiplicaDano`, `somaVidaMaxima` | +60% de dano, mas perde um coração de vida máxima (nunca fica com menos de um) |

Quando um efeito acontece, um texto curto sobe do jogador ("Bloqueou!", "+ vida",
"Furia!", "Renasceu!"): `TextoFlutuante.cs`.

Os ícones vêm dos pacotes: folha de objetos da masmorra, `Masmorra/ObjetosV2` (tileset do
2D Pixel Dungeon Asset Pack v2.0: escudo, caveira, frascos), `TinySwords/Ouro` e
`TinySwords/Carne` (Tiny Swords) e as gemas e o brasão alado do Pixel UI pack 3
(`ArteImportada.IconeDoItem`).

## Loja

A sala da loja (`Loja.Montar`) vende dois itens passivos (15 moedas), um coração (3), uma
bomba (5) e uma chave (5). Chegando perto de um produto, uma etiqueta mostra o nome e o
efeito; o preço fica amarelo quando dá pra pagar e vermelho quando não dá. Encostou com
moeda suficiente, comprou.

Atrás do balcão fica o comerciante (`Comerciante.cs`): o homem de chapéu de aba larga do
2D Pixel Dungeon Asset Pack v2.0 (`Masmorra/Moradores`), com saco de ouro, moedas, barril,
caixa e a placa de LOJA. Ele respira, vira pro lado do jogador e dá pulinhos; num balão ele
cumprimenta quando o jogador chega, agradece a compra e avisa quando falta moeda. Não tem
vida nem colisor: não é inimigo.
