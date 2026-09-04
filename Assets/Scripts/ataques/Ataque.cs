using UnityEngine;
public class Ataque : MonoBehaviour
{
    public float duracaoAtaque = 1.5f;
    public float couwdown = 0;
    public GameObject golpePrefab;


    void Update()

    {
        Movimento movimento = GetComponent<Movimento>();
        if (movimento != null)
        {
            float direcao = transform.localScale.x >= 0f ? 1f : -1f;
            Vector3 posicaoGolpe = new Vector3(transform.position.x + (1.5f * direcao), transform.position.y, transform.position.z);
            if (Input.GetButtonDown("Fire1") && Time.time > couwdown)

            {
                Debug.Log("golpe");
                GameObject golpeCriado = Instantiate(golpePrefab, posicaoGolpe, Quaternion.identity);
                couwdown = Time.time + duracaoAtaque;
                golpeCriado.transform.localScale = new Vector3(direcao, 1f, 1f);
                Destroy(golpeCriado, 0.5f);
            }
        ;
        }


    }
}
