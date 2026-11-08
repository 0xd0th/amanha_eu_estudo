using UnityEngine;

public class NotificacaoItem : MonoBehaviour
{
    public float velocidadeQueda = 5f;
    public float limiteInferiorY = -6f;

    void Update()
    {
        // Fazer o item cair
        transform.Translate(Vector3.down * velocidadeQueda * Time.deltaTime);

        // Se passar do fundo da tela, apaga o objeto
        if (transform.position.y < limiteInferiorY)
        {
            Destroy(gameObject);
        }
    }
}