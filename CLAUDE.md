# The Prettie

## Pacotes de arte
- Todo pacote de arte que o Diogo mandar (zip): descompactar em `Pacotes/<Nome> V<versão>/` (fica só na
  máquina; o container é apagado no fim da sessão, então guarde o zip do seu lado).
- Nunca subir os pacotes inteiros no repositório `Game`: ele é **público** e a licença proíbe
  redistribuir. A pasta `Pacotes/` está no `.gitignore`; o jogo só leva as peças que usa.

## PixelLab
- Animação de personagem: **só** `animate_character` no modo `skeleton-v3` com um `template_animation_id`
  (andar, parado, ataque, morte...). O Diogo viu que o v3 por texto e o template comum saem feios.
- Monstro: `create_character_pro_flash` com a referência (`style_image`) no desenho de um monstro dos pacotes
  da série EPIC RPG World (Old Prison, Crypt, The Depths of the Mountain), de preferência o original do
  próprio bicho (o histórico do git guarda os que já foram trocados), e uma descrição caprichada (silhueta,
  materiais, cores, arma, pose). Os monstros desses pacotes ficam como estão: só servem de referência.
- Um monstro por vez: faz, mostra pro Diogo e só passa pro próximo quando ele aprovar.
