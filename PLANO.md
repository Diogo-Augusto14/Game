# Plano do jogo

## A ideia

Um roguelite de tiro visto de cima, com **Soul Knight** e **Enter the Gungeon** como referência (não
como cópia): andar, esquivar, mirar e atirar, atravessar andares que são cavernas gigantes, inimigos que enchem a
tela de tiro, armas achadas no caminho e um chefe no fim de cada andar. Partidas curtas; morreu, começa
de novo.

## Como a gente trabalha

- **Uma etapa por vez.** Cada etapa termina jogável e testada; a próxima só começa depois que você jogar
  e aprovar.
- **Começo limpo.** Tudo nasce nesta branch (`jogo-definitivo`). O jogo antigo, no estilo do Isaac,
  continua nas outras branches só pra consulta: nada é copiado de lá sem combinar.
- **Arte provisória.** Por enquanto, os desenhos dos pacotes (o Arqueiro do Tiny RPG, o chão do Old
  Prison). Quando o seu boneco ficar pronto, é só trocar as folhas de animação.
- **O personagem usa a arma dele.** O Arqueiro atira flechas do próprio arco, rápido. Personagem de arma
  de fogo segura a arma.

## Etapas

| # | Etapa | O que dá pra fazer no fim |
|---|---|---|
| 1 | **Movimento** (feita) | Andar, esquivar, mirar com o mouse e atirar flechas sem parar, num chão aberto que não acaba |
| 2 | **Algo pra acertar** (feita) | Boneco de treino e um inimigo simples que anda e atira; vida, dano, morrer, piscar ao tomar dano |
| 3 | **Andares gigantes** (feita) | Cada andar é uma caverna enorme e aberta, sem salas nem portas (como no Nuclear Throne); matar todos os inimigos abre o portal pro próximo andar. Com a arte do Old Prison (paredes, buracos de abismo, poças, enfeites), montada pelas regras do Tiled do pacote |
| 4 | **Armas** (feita, falta jogar) | Achar arma no chão e no baú, carregar duas (a do personagem e uma achada) e trocar, munição por arma com pente e recarga, caixas de munição. Os inimigos contornam parede pra chegar em você |
| 5 | Inimigos e padrões | Mais inimigos, cada um com o seu jeito, e padrões de bala de verdade |
| 6 | Chefe | O chefe do fim do andar |
| 7 | Interface | Vida, energia ou munição na tela, menu, pausa, fim de jogo |
| 8 | A sua arte | O seu boneco no lugar do Arqueiro, e o que mais você desenhar |

## Decisões que ficam pra hora certa

- **Energia ou munição?** Decidido na etapa 4: **munição por arma**, como no Gungeon (pente, reserva,
  recarga, caixas de munição); a arma do personagem é infinita. E **duas armas** de uma vez: a do
  personagem, fixa, e uma achada.
- **Vários personagens com habilidade própria** (como no Soul Knight) ou um só? Decidir depois da etapa 3.
- **Tamanho dos andares e quantos andares.** Por enquanto 3 andares, o primeiro com umas 1500 células de
  chão e 24 inimigos, crescendo a cada andar. Ajustar jogando.
- **Personagens do Old Prison.** O pacote tem um esqueleto, um esqueleto mago que atira e um inimigo tipo
  assassino, com animações. Bons candidatos pra etapa 5. Os pixels do Arqueiro e do Bruxo (Tiny RPG) são
  maiores que os do cenário do Old Prison; resolver quando entrar o seu boneco (etapa 8).
- **Inimigo contornando parede.** Feito na etapa 4 (os Bruxos ficavam presos na parede): com o caminho
  livre vão reto, senão seguem um mapa de caminhos pela caverna.
