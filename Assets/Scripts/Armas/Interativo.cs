using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Uma coisa do mundo que o jogador usa chegando perto e apertando interagir (E, ou B no controle):
/// uma arma no chao, um bau. A <see cref="InteracaoDoJogador"/> escolhe a mais perto e mostra a dica.
/// </summary>
public abstract class Interativo : MonoBehaviour
{
    /// <summary>Todas as que existem agora (cada uma entra ao ligar e sai ao desligar).</summary>
    public static readonly List<Interativo> Todos = new List<Interativo>();

    /// <summary>Quao perto o jogador precisa chegar, em unidades.</summary>
    public virtual float Alcance => 1.1f;

    /// <summary>Ainda da pra usar (um bau aberto nao da mais).</summary>
    public virtual bool Disponivel => true;

    /// <summary>O texto da dica, ex.: "pegar Tomo das Brasas".</summary>
    public abstract string Dica { get; }

    public abstract void Usar(GameObject jogador);

    protected virtual void OnEnable() => Todos.Add(this);

    protected virtual void OnDisable() => Todos.Remove(this);
}
