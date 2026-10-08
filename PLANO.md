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
  continua nas outras branches só pra consulta: nada é copiado de lá sem combinar. Combinado depois
  da etapa 7: os menus do jogo antigo vieram pra cá (início, configurações, pausa, fim de jogo,
  novidades) e a música. Na etapa 8: os inimigos, chefes, heróis, progresso e salvamento.
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
| 4 | **Armas** (feita) | Achar arma no chão e no baú, carregar duas (a do personagem e uma achada) e trocar, munição por arma (um número só, sem recarga), bolsas de munição. Começa só com o arco, que também pode ser trocado. Armas de fantasia (varinha, tomo, besta, cajado, machado). Os inimigos contornam parede pra chegar em você |
| 5 | **Inimigos e padrões** (feita, falta jogar) | Esqueleto (investida), Gosma (estoura em gotas), Esqueleto Arqueiro (leque, recua), Necromante (anéis), Olho (espiral), além do Bruxo; os mais difíceis só nos andares 2 e 3. Padrões de bala na arma: leque, anel, rajada, espiral, tiro que freia ou acelera |
| 6 | **Chefes** (feita) | Cada chefe tem um andar só dele, um salão com pilares: Minotauro no andar 3 e Golem de Brasa no andar 6 (o final). Ataques que se alternam, fúria com metade da vida, barra de vida, baú de prêmio |
| 7 | **Interface** (feita, falta jogar; menus copiados do jogo antigo) | Vida em corações, a arma na moldura com a munição, a barra do chefe, menu inicial, pausa, tela de controles e tela de fim de jogo (morreu ou venceu, com andar, inimigos e tempo), com a arte de interface e fontes em pixel |
| 8 | **Personagens e o resto do jogo antigo** (feita, falta jogar) | Nove heróis, cada um com vida, arma do começo (trocável) e uma habilidade (F); liberados vencendo chefes e zerando. Do jogo antigo: os outros 24 inimigos, 5 chefes (12 andares em 4 mundos, chefe sorteado entre dois), virada de fase, números de dano e hitstop, contorno claro, placa do andar, estatísticas, conquistas, bestiário e continuar a partida |

## Decisões que ficam pra hora certa

- **Armas de fantasia.** Decidido depois da etapa 4: o mundo é medieval de fantasia, então as armas não
  precisam existir, precisam encaixar (nada de pistola e metralhadora).
- **Energia ou munição?** Decidido na etapa 4: **munição por arma**; depois de jogar, simplificada pra
  um número só por arma, sem pente nem recarga (o pente confundia). A arma do personagem é infinita.
  **Duas armas** de uma vez, e a do personagem também pode ser trocada (senão o arco fica inútil no
  fim do jogo). O jogo começa só com ela: nada de arma de graça no começo.
- **Vários personagens.** Decidido depois da etapa 5: vários, como no Soul Knight, cada um com uma
  habilidade e uma arma inicial diferente (etapa 8).
- **A sua arte.** Talvez não tenha etapa só pra ela; quando o seu boneco ficar pronto, entra no lugar de
  um personagem (é só trocar as folhas de animação).
- **Tamanho dos andares e quantos andares.** 12 andares em 4 mundos (duas cavernas e um chefe; o último é
  o Olho do Abismo). Depois da 1.10.0, achou grande e estreito: a primeira caverna tem umas 1100 células
  e 20 inimigos (mais 120 células e 3 inimigos a cada caverna), com corredores de 3 a 4 de largura.
  Depois da 1.11.x, achou chato (caçar os últimos inimigos num mapa grande, pouco prêmio, sem tensão):
  na 1.12.0 o andar virou salas que fecham ao entrar, com prêmio por sala, e ficou menor e mais cheio
  (4 salas de luta no primeiro andar, 4 a 6 inimigos em cada, às vezes uma segunda onda).
- **O que precisava adaptar do jogo antigo** (feito depois da etapa 8): moedas, chaves e bombas, loja,
  itens passivos e ativos com sinergias, as salas especiais virando pedaços da caverna (emboscada,
  desafio, altar, baús especiais, área escura), armadilhas e cenário vivo, temas dos mundos e as flechas
  especiais virando arcos achados.
- **Onde ficam os chefes.** Decidido na etapa 6: num andar só deles (uma arena), não no fim da caverna.
- **Personagens do Old Prison.** O pacote tem um esqueleto, um esqueleto mago que atira e um inimigo tipo
  assassino, com animações. Na etapa 5 os inimigos novos vieram do mesmo pacote do Arqueiro e do Bruxo
  (Tiny RPG), pra ficarem do mesmo tamanho; os do Old Prison ficam de reserva. Os pixels do Tiny RPG são
  maiores que os do cenário do Old Prison; resolver quando entrar o seu boneco.
- **Inimigo contornando parede.** Feito na etapa 4 (os Bruxos ficavam presos na parede): com o caminho
  livre vão reto, senão seguem um mapa de caminhos pela caverna.
