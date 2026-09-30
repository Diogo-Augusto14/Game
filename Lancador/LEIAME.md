# Lançador com atualização automática

O `ThePrettie-Lancador.exe` fica na mesma pasta do `ThePrettie.exe`. Toda vez que é aberto ele:

1. Lê a versão instalada em `versao.txt` (na pasta do jogo; se não existir, considera `0.0.0`).
2. Consulta a última release do repositório público `Diogo-Augusto14/ThePrettie-Releases`.
3. Se a versão publicada for maior, baixa o zip, extrai por cima da pasta do jogo e atualiza o `versao.txt`.
4. Abre o `ThePrettie.exe`. Sem internet ou com qualquer erro, abre o jogo mesmo assim.

O jogo deve ser aberto sempre pelo lançador. O repositório de releases guarda só os zips, o código do jogo continua privado.

## Preparação (uma vez só)

Criar o repositório público `ThePrettie-Releases` no GitHub (vazio, sem código do jogo). Quem cria e publica é o diogo, escrevendo "pode criar e publicar" no tópico "Testes no PC".

## Gerar uma versão nova

1. No Unity, faça o build do Windows em `Builds\Windows`.
2. Rode, na raiz do projeto:
   `powershell -ExecutionPolicy Bypass -File Lancador\gerar-versao.ps1 -Versao 1.1.0`
   Isso compila o lançador, copia para o build, grava `versao.txt` e cria `Builds\ThePrettie-Windows-1.1.0.zip`.
3. Crie a release no repositório público, com a tag `v1.1.0` e o zip anexado:
   `gh release create v1.1.0 Builds\ThePrettie-Windows-1.1.0.zip --repo Diogo-Augusto14/ThePrettie-Releases --title "The Prettie 1.1.0" --notes "Novidades da versão 1.1.0"`
4. Quem já tem o jogo recebe a atualização na próxima vez que abrir o lançador.

## Primeira instalação de quem ainda não tem o lançador

Mandar o zip da versão mais recente uma única vez. Depois disso, as atualizações chegam sozinhas.

## Observações

- O lançador não se sobrescreve durante a atualização; para trocar o próprio lançador, mande um zip novo.
- Só a última release marcada como "Latest" conta (rascunhos e pré-lançamentos são ignorados).
- O número da tag segue o formato `v1.2.3`; o `v` é ignorado na comparação.

## Testar sem o repositório público

Defina a variável de ambiente `PRETTIE_URL_TESTE` com a URL de um JSON no mesmo formato da API do GitHub (precisa de `tag_name` e `browser_download_url` terminando em `.zip`), por exemplo servido por `python -m http.server`, e abra o lançador na mesma janela do terminal. Sem essa variável, ele sempre usa o repositório oficial.
