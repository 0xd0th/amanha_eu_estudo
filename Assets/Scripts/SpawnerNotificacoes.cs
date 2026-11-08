using System.Collections;
using UnityEngine;

public class SpawnerNotificacoes : MonoBehaviour
{
    public GameObject prefabNotificacao;
    public float tempoEntreSpawns = 1f;
    public float limiteX = 7f;
    public float alturaSpawnY = 6f;

    private bool gerando = true;

    void Start()
    {
        StartCoroutine(GerarNotificacoes());
    }

    private IEnumerator GerarNotificacoes()
    {
        while (gerando)
        {
            yield return new WaitForSeconds(tempoEntreSpawns);

            if (prefabNotificacao != null)
            {
                float posXAleatoria = Random.Range(-limiteX, limiteX);
                Vector3 posicaoSpawn = new Vector3(posXAleatoria, alturaSpawnY, 0f);

                Instantiate(prefabNotificacao, posicaoSpawn, Quaternion.identity);
            }
        }
    }

    public void PararSpawner()
    {
        gerando = false;
    }
}