# Old Prison

A caverna do jogo usa a arte do pacote **EPIC RPG World Pack - Old Prison** (V1.7.1) e as regras do
Tiled Map Editor que vêm com ele. O pacote não fica no repositório; o `importar.py` tira dele só o
que o jogo usa:

| Vai para | O que é |
|---|---|
| `Assets/Arte/OldPrison/Chao.png` | A plataforma de pedra (`Tilesets/wall-1- 3 tiles tall.png`) |
| `Assets/Arte/OldPrison/Paredes.png` | As paredes altas de tijolo (`Tilesets/wall-2- 3 tiles tall.png`) |
| `Assets/Arte/OldPrison/Abismo.png` | Os ladrilhos do fundo roxo, tirados do terreno (os que o mapa de exemplo usa na camada `pit`) |
| `Assets/Arte/OldPrison/Sangue.png` | As poças de sangue, estilo 2 sem espinhos |
| `Assets/Arte/OldPrison/Enfeites.png` | Ossos, pedrinhas e papéis de 32 × 32, lado a lado |
| `Assets/Scripts/Andar/DadosDoOldPrison.cs` | As tabelas de cantos (dos `.tsx`) e as regras (dos `Rules/*.tmx`), em C# |

Uso, da raiz do projeto (precisa do Python 3 com o Pillow: `pip install pillow`):

```
python Ferramentas/OldPrison/importar.py "C:\caminho\EPIC RPG World Pack - Old Prison V1.7.1"
```

As regras da parede 1 e da parede 2 são as mesmas (os dois desenhos têm os ladrilhos nos mesmos
lugares), então o jogo usa um conjunto só para as duas.
