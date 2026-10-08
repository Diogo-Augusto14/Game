# Lançador: escolher a versão, um save pro jogo antigo e outro pro novo

O `ThePrettie-Lancador.exe` mostra todas as versões publicadas no repositório público
`Diogo-Augusto14/ThePrettie-Releases`, com as novidades de cada uma. Você escolhe qual jogar (a de cima é
a mais nova) e aperta **Jogar** (ou dá dois cliques):

1. Se a versão ainda não está instalada, ele baixa o zip dela e instala em `versoes\<versão>\`, ao lado
   do lançador. Cada versão tem a sua pasta; nenhuma apaga a outra.
2. Põe no lugar o **save do jogo** dessa versão e abre o jogo. Enquanto o jogo roda, o lançador fica
   escondido.
3. Quando o jogo fecha, guarda o save (`saves\antigo.txt` ou `saves\novo.txt`) e volta a aparecer, pra
   jogar de novo ou escolher outra.

Sem internet, ele mostra só as versões já instaladas. Com o jogo já aberto, ele não deixa abrir outra.

## Como o save do jogo antigo e o do novo ficam separados

A Unity guarda o save do jogo (o PlayerPrefs) no registro do Windows, numa chave só
(`HKEY_CURRENT_USER\Software\DefaultCompany\The Prettie`), a mesma pra todas as versões. Por isso o
lançador troca o conteúdo dessa chave: antes de abrir uma versão, põe o save dela; quando o jogo fecha,
guarda de volta. O jogo não precisa saber de nada disso.

- São só dois saves: `saves\antigo.txt` pras versões do jogo antigo (antes da 1.2.0) e `saves\novo.txt`
  pras do jogo novo (1.2.0 em diante). Atualizar o jogo **não** perde heróis liberados, progresso nem
  configurações: a versão nova usa o mesmo save da anterior.
- O `antigo.txt` é o save que estava no registro quando o lançador novo abriu pela primeira vez.
- Antes, cada versão tinha o seu save (`saves\<versão>.txt`). Na primeira vez que precisa do
  `novo.txt`, o lançador começa ele com o maior desses saves do jogo novo (no empate, o mais recente).
  Os arquivos por versão ficam lá, sem uso.
- Se o lançador for fechado à força com o jogo aberto, na próxima vez ele guarda o save que ficou
  pendente (`saves\em-uso.txt` diz de qual versão era) antes de qualquer coisa.
- Pra começar do zero, é só apagar o `saves\novo.txt` (ou o `antigo.txt`) com o lançador fechado.

## A primeira vez do lançador novo

O lançador antigo instalava o jogo direto na pasta dele. Na primeira vez, o novo move essa instalação
pra `versoes\<versão>\` (a de `versao.txt`) e guarda o save que já existe pra ela e como `saves\antigo.txt`.

O lançador antigo não sabe trocar a si mesmo (pula o próprio `.exe` ao instalar). Por isso o zip traz o
lançador também como `ThePrettie-Lancador-novo.exe`, e o jogo, ao abrir, põe ele no lugar do velho
(`Assets/Scripts/Nucleo/TrocaDoLancador.cs`). Da segunda vez em diante já abre o lançador novo.

O lançador novo se atualiza sozinho: quando instala a versão mais nova e o lançador que veio nela é
diferente, troca de nome com ele (um `.exe` aberto não pode ser apagado, mas pode mudar de nome) e o novo
vale na próxima vez.

## Preparação (uma vez só)

Criar o repositório público `ThePrettie-Releases` no GitHub (vazio, sem código do jogo). Quem cria e publica é o diogo, escrevendo "pode criar e publicar" no tópico "Testes no PC".

## Gerar uma versão nova pela nuvem (sem o PC)

O workflow `.github/workflows/gerar-versao.yml` faz tudo no GitHub: build do Unity para Windows, o mesmo zip do `gerar-versao.ps1` (com o lançador e o `versao.txt`) e, se pedido, a publicação no `ThePrettie-Releases`. Cada execução gasta minutos do Actions; a conta grátis tem 2.000 minutos por mês em repositório privado, e a máquina Windows conta em dobro.

1. No GitHub, abra a aba **Actions** do repositório `Game`, escolha **Gerar versão do jogo** e clique em **Run workflow**.
2. Preencha a versão (`1.1.0`, sem o `v`). Marque **publicar** só se for para todo mundo receber; sem marcar, ele só gera o zip.
3. As novidades da release vêm de `Lancador/notas/<versão>.md` (por exemplo `Lancador/notas/1.1.0.md`); se esse arquivo não existir, vale o texto do campo **notas**.
4. No fim, o zip fica em **Artifacts**, no rodapé da página da execução, por 30 dias. Se a versão já existir no `ThePrettie-Releases`, ele para logo no começo, sem gastar o build.

### Segredos (uma vez só)

Em **Settings > Secrets and variables > Actions > New repository secret** do repositório `Game`:

- `UNITY_EMAIL`: e-mail da conta Unity.
- `UNITY_PASSWORD`: senha da conta Unity (quem entra pelo Google precisa criar uma senha na conta Unity).
- `UNITY_LICENSE`: o texto inteiro do arquivo `C:\ProgramData\Unity\Unity_lic.ulf`. Se o arquivo não existir, no Unity Hub vá em **Preferences > Licenses > Add > Get a free personal license** e ele aparece. Serve o arquivo de qualquer computador logado na mesma conta.
- `TOKEN_RELEASES` (só para publicar): token do GitHub que só enxerga o `ThePrettie-Releases`. Em **github.com > foto > Settings > Developer settings > Personal access tokens > Fine-grained tokens > Generate new token**: em **Repository access** escolha **Only select repositories** e o `ThePrettie-Releases`; em **Permissions**, **Contents: Read and write**.

## Gerar uma versão nova pelo PC

1. No Unity, faça o build do Windows em `Builds\Windows`, com o nome `ThePrettie.exe` (sem espaço). Se a pasta já tiver um build com outro nome, apague a pasta antes.
2. Rode, na raiz do projeto:
   `powershell -ExecutionPolicy Bypass -File Lancador\gerar-versao.ps1 -Versao 1.1.0`
   Isso compila o lançador, copia para o build, grava `versao.txt` e cria `Builds\ThePrettie-Windows-1.1.0.zip`.
3. Crie a release no repositório público, com a tag `v1.1.0` e o zip anexado:
   `gh release create v1.1.0 Builds\ThePrettie-Windows-1.1.0.zip --repo Diogo-Augusto14/ThePrettie-Releases --title "The Prettie 1.1.0" --notes "Novidades da versão 1.1.0"`
4. Quem já tem o jogo recebe a atualização na próxima vez que abrir o lançador.

## Primeira instalação de quem ainda não tem o lançador

Mandar o zip da versão mais recente uma única vez. Depois disso, as atualizações chegam sozinhas.

## Observações

- Rascunhos e pré-lançamentos não aparecem na lista.
- O número da tag segue o formato `v1.2.3`; o `v` é ignorado na comparação.

## Testar sem o repositório público

Defina a variável de ambiente `PRETTIE_URL_TESTE` com a URL de um JSON no mesmo formato da API do GitHub (uma lista de releases, ou uma só; cada uma com `tag_name` e um `browser_download_url` terminando em `.zip`), por exemplo servido por `python -m http.server`, e abra o lançador na mesma janela do terminal. Sem essa variável, ele sempre usa o repositório oficial.
