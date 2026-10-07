# Plano do jogo

## A ideia

Um roguelite de tiro visto de cima, com **Soul Knight** e **Enter the Gungeon** como referência (não
como cópia): andar, esquivar, mirar e atirar, atravessar andares feitos de salas, inimigos que enchem a
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
| 2 | **Algo pra acertar** (feita, falta jogar) | Boneco de treino e um inimigo simples que anda e atira; vida, dano, morrer, piscar ao tomar dano |
| 3 | Sala e andar | Salas ligadas por corredores; a porta fecha na luta e abre quando a sala fica limpa |
| 4 | Armas | Achar arma no chão e no baú, trocar de arma, e o recurso do tiro (energia ou munição) |
| 5 | Inimigos e padrões | Mais inimigos, cada um com o seu jeito, e padrões de bala de verdade |
| 6 | Chefe | O chefe do fim do andar |
| 7 | Interface | Vida, energia ou munição na tela, menu, pausa, fim de jogo |
| 8 | A sua arte | O seu boneco no lugar do Arqueiro, e o que mais você desenhar |

## Decisões que ficam pra hora certa

- **Energia ou munição?** Soul Knight usa energia que recarrega; Gungeon usa pente e munição. Decidir na
  etapa 4.
- **Vários personagens com habilidade própria** (como no Soul Knight) ou um só? Decidir depois da etapa 3.
- **Quantos andares e quantas salas por andar.** Decidir na etapa 3, jogando.
