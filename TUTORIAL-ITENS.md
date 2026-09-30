# Itens e coletáveis

Tudo em `Assets/Scripts/Itens/`. Funciona sozinho na cena do jogo
(`Assets/Scenes/Jogo.unity`): é só dar Play.

## O que tem no andar

| Onde | O que aparece |
|---|---|
| Sala do item (chão dourado) | Pedestal com um item passivo. Encostou, pegou. |
| Sala do chefe | Pedestal com item quando a sala é limpa. |
| Sala comum | 50% de chance de um coletável no meio quando a sala é limpa. |
| Cada inimigo | 15% de chance de soltar um coletável ao morrer. |
| Andar 2 em diante | A porta da sala do item tem cadeado (gasta 1 chave). A sala inicial dá uma chave. |

As chances ficam no componente `Andar ▸ Itens e coletaveis`.

## Coletáveis

- **Coração** (vermelho): cura 20. Só é pego se você estiver machucado.
- **Moeda** (amarela): guardada pra loja, que ainda não existe.
- **Chave** (cinza, comprida): abre o cadeado da sala do item.
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
