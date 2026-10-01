# Gerador do áudio do jogo

Scripts que fizeram as músicas (`Assets/Arte/Resources/Musica`) e os efeitos sonoros
(`Assets/Arte/Resources/Sons/*.ogg`). Não fazem parte do jogo: servem pra mexer numa música
ou num som e gerar de novo.

Precisa de Python 3 com `numpy`, `scipy` e `mido`, do `fluidsynth`, do `ffmpeg` e do
soundfont MuseScore General (no Ubuntu: `apt install fluidsynth musescore-general-soundfont-small
freedoom ffmpeg`).

| Arquivo | O que faz |
|---|---|
| `motor.py` | Escreve o MIDI, toca no fluidsynth e corta o loop sem emenda (pega a 2ª repetição e funde o começo com o que vem depois) |
| `musicas.py`, `musicas2.py` | As 10 músicas, nota por nota: acordes, melodias, instrumentos |
| `gerar.py` | `python3 gerar.py Porao Chefe` gera só essas (sem nome, gera todas) em `saida/*.wav` |
| `extrair_freedoom.py` | Tira os sons do `freedoom2.wad` pra `fd/sfx` |
| `sfx.py` | Os efeitos: síntese, trechos do Freedoom e vinhetas no soundfont, em `sfx_wav/*.wav` |

Depois de gerar, converter pra OGG e copiar com o mesmo nome por cima do arquivo do jogo
(o `.meta` fica):

```
ffmpeg -i saida/Porao.wav -c:a libvorbis -q:a 3 ../../Assets/Arte/Resources/Musica/Porao.ogg
ffmpeg -i sfx_wav/Moeda.wav -ac 1 -c:a libvorbis -q:a 4 ../../Assets/Arte/Resources/Sons/Moeda.ogg
```

Créditos e licenças do que entra no jogo: `Assets/StreamingAssets/CREDITOS-AUDIO.txt`.
