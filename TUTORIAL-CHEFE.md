# Primeiro chefe

A sala do chefe agora tem um chefe de verdade: o **Monstrão** (`ChefeDoAndar`, em
`Assets/Scripts/Chefe/`). Ele nasce dormindo no meio da sala; quando você entra, as portas
trancam, ele acorda e a barra de vida aparece embaixo da tela com o nome dele.

## Os ataques

Entre um ataque e outro ele anda devagar tentando ficar a uns 3 passos de você. Nunca
repete o mesmo ataque duas vezes seguidas, e **todo ataque tem um aviso antes** — os
olhos mudam de cor e dá pra aprender a ler o que vem:

| Ataque | Aviso | O que faz |
| --- | --- | --- |
| Anel | olhos amarelos, ele incha | 12 tiros em todas as direções |
| Rajada | olhos laranja, ele treme | 3 leques de 5 tiros mirados em você, alternando o vão |
| Investida | olhos vermelhos, linha de mira no chão | dispara reto; se bater na parede fica tonto (hora de bater) |
| Invocar | olhos roxos, ele pulsa | chama 2 perseguidores (só na segunda fase, no máximo 3 vivos) |

**Segunda fase** (metade da vida): olhos maiores e mais vermelhos, barra laranja, avisos
e esperas mais curtos, tiros mais rápidos, anel com 16 tiros e a investida solta um anel
de tiros ao bater na parede.

Tiro nele não atordoa nem empurra (senão cada lágrima cortaria o ataque no meio). Quando
ele morre, os lacaios morrem junto, as portas abrem e aparece o pedestal com o item.

## Ajustar

Os números ficam no próprio componente `ChefeDoAndar` (vida 120, dano do tiro 10, dano
da investida 20, tempos de aviso etc.). A vida e o tamanho ficam em
`FabricaDeInimigos.Criar`, no `case TipoDeInimigo.Chefe`. Para pôr o chefe em qualquer
sala: `sala.CriarInimigo(TipoDeInimigo.Chefe, Vector2.zero)`.
