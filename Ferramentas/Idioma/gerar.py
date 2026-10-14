"""Escreve Assets/Scripts/Nucleo/Traducoes.cs a partir do ingles.py e confere as chaves.

Rodar da raiz do repositorio: python3 Ferramentas/Idioma/gerar.py
A conferencia avisa de chave que nao aparece em nenhum lugar do jogo (erro de digitacao: o texto ficaria
sem traduzir) e de chave repetida.
"""
import glob
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from ingles import T  # noqa: E402

SAIDA = os.path.join('Assets', 'Scripts', 'Nucleo', 'Traducoes.cs')


def cs(s):
    return '"' + s.replace('\\', '\\\\').replace('"', '\\"').replace('\n', '\\n') + '"'


def conferir():
    textos = []
    for f in glob.glob('Assets/Scripts/**/*.cs', recursive=True):
        if not f.endswith('Traducoes.cs'):
            textos.append(open(f, encoding='utf-8').read().replace('\\"', '"'))
    for f in glob.glob('Assets/Prefabs/**/*.prefab', recursive=True) + glob.glob('Assets/Dados/**/*.asset', recursive=True):
        textos.append(open(f, encoding='utf-8', errors='ignore').read())
    tudo = '\n'.join(textos)
    vistos, ruins = set(), 0
    for pt, en in T:
        if pt in vistos:
            print('repetida:', pt)
            ruins += 1
        vistos.add(pt)
        # As chaves montadas com "a" + "b" no codigo: confere o comeco.
        if pt not in tudo and pt[:40] not in tudo:
            print('nao achei no jogo:', pt)
            ruins += 1
        if pt.count('{') != en.count('{'):
            print('lacunas diferentes:', pt, '->', en)
            ruins += 1
    return ruins


def main():
    linhas = ['using System.Collections.Generic;', '',
              '/// <summary>',
              '/// O dicionario do ingles: o texto em portugues (como esta no jogo) e a traducao. Ver <see cref="Idioma"/>.',
              '/// Gerado pelo Ferramentas/Idioma/gerar.py a partir do ingles.py: mude la e rode de novo.',
              '/// </summary>',
              'public static class Traducoes', '{',
              '    public static readonly Dictionary<string, string> Ingles = new Dictionary<string, string>', '    {']
    feitas = set()
    for pt, en in T:
        if pt in feitas:
            continue
        feitas.add(pt)
        linhas.append(f'        {{ {cs(pt)}, {cs(en)} }},')
    linhas += ['    };', '}', '']
    open(SAIDA, 'w', encoding='utf-8').write('\n'.join(linhas))
    print(len(feitas), 'traducoes')
    print(conferir(), 'problemas')


if __name__ == '__main__':
    main()
