# Itens e coletáveis

Tudo em `Assets/Scripts/Itens/`. Funciona sozinho na cena do andar
(`Tools ▸ Jogo ▸ Andar ▸ Criar cena de teste do andar`): é só dar Play.

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
