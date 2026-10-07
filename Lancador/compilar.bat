@echo off
rem Compila o lancador usando o compilador C# que ja vem no Windows (.NET Framework 4).
rem Funciona de qualquer pasta: entra na pasta do proprio .bat antes de compilar.
pushd "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
"%CSC%" /nologo /target:winexe /out:ThePrettie-Lancador.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:System.Web.Extensions.dll Lancador.cs
if errorlevel 1 (popd & echo Falhou ao compilar. & exit /b 1)
popd
echo Pronto: ThePrettie-Lancador.exe
