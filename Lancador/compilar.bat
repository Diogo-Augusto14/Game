@echo off
rem Compila o lançador usando o compilador C# que já vem no Windows (.NET Framework 4).
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
"%CSC%" /nologo /target:winexe /out:ThePrettie-Lancador.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll Lancador.cs
if errorlevel 1 (echo Falhou ao compilar. & exit /b 1)
echo Pronto: ThePrettie-Lancador.exe
