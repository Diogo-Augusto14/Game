using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A vida do jogador acabou: desliga os controles, o movimento e a arma, o corpo para e deixa de
/// bater nas coisas (a <see cref="AnimacaoDoJogador"/> toca a queda) e abre a
/// <see cref="TelaDeFimDeJogo"/>, que faz o resto: clarao vermelho, camera lenta, a tela
/// escurecendo, o resumo da partida e os botoes de tentar de novo.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Vida))]
public class MorteDoJogador : MonoBehaviour
{
    private Vida vida;
    private GameObject quemBateu;

    private void Awake()
    {
        vida = GetComponent<Vida>();
    }

    private void OnEnable()
    {
        vida.AoMorrer += Morreu;
        vida.AoTomarDano += Apanhou;
    }

    private void OnDisable()
    {
        vida.AoMorrer -= Morreu;
        vida.AoTomarDano -= Apanhou;
    }

    private void Apanhou(Dano dano) => quemBateu = dano.fonte;

    private void Morreu()
    {
        // Fica no bestiario quem matou; a partida salva se perde.
        Registro.Morreu(quemBateu);
        Salvamento.Apagar();

        // Nada de andar, atirar ou esquivar; o corpo para e deixa de bater nas coisas.
        DesligarSeTiver<ControlesDoJogador>();
        DesligarSeTiver<MovimentoDoJogador>();
        DesligarSeTiver<ArmaDoJogador>();

        if (TryGetComponent(out Rigidbody2D corpo))
        {
            corpo.linearVelocity = Vector2.zero;
            corpo.simulated = false;
        }

        GeradorDoAndar andar = FindAnyObjectByType<GeradorDoAndar>();

        if (andar != null)
            TelaDeFimDeJogo.Mostrar(andar);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void DesligarSeTiver<T>() where T : Behaviour
    {
        if (TryGetComponent(out T peca))
            peca.enabled = false;
    }
}
