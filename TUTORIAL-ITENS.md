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
