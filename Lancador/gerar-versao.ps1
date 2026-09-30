# Gera o zip de uma versão do The Prettie, com o lançador e o arquivo versao.txt dentro.
# Uso:  powershell -ExecutionPolicy Bypass -File Lancador\gerar-versao.ps1 -Versao 1.1.0
# Pré-requisito: já ter feito o build do Windows em Builds\Windows (File > Build no Unity).
param([Parameter(Mandatory=$true)][string]$Versao)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$build = Join-Path $raiz 'Builds\Windows'
if (-not (Test-Path (Join-Path $build 'ThePrettie.exe'))) { throw "Build não encontrado em $build. Faça o build do Unity primeiro." }
# Só pode haver um jogo na pasta: o lançador sempre abre o ThePrettie.exe.
$sobrando = Get-ChildItem $build -Filter *.exe | Where-Object { $_.Name -notin 'ThePrettie.exe','ThePrettie-Lancador.exe','UnityCrashHandler64.exe' }
if ($sobrando) { throw "Há outro executável em $build ($($sobrando.Name -join ', ')). Apague a pasta Builds\Windows e faça o build de novo com o nome ThePrettie.exe." }

& (Join-Path $PSScriptRoot 'compilar.bat') | Out-Host
Copy-Item (Join-Path $PSScriptRoot 'ThePrettie-Lancador.exe') $build -Force
Set-Content -Path (Join-Path $build 'versao.txt') -Value $Versao -NoNewline

$zip = Join-Path $raiz "Builds\ThePrettie-Windows-$Versao.zip"
if (Test-Path $zip) { Remove-Item $zip }
# As pastas *_DoNotShip são só para depuração e não vão para os jogadores.
$itens = Get-ChildItem $build | Where-Object { $_.Name -notlike '*_DoNotShip' }
Compress-Archive -Path $itens.FullName -DestinationPath $zip
Write-Host "Zip pronto: $zip"
