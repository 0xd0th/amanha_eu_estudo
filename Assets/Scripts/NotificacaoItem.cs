using UnityEngine;

public class NotificacaoItem : MonoBehaviour
{
    public float velocidadeQueda = 5f;
    public float limiteInferiorY = -6f;

    [Header("Acessibilidade - Sprites do Raio")]
    public Sprite spriteRaioNormal;
    public Sprite spriteRaioDaltonico;

    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        // Verifica o estado do modo daltônico no PauseManager ao nascer
        AtualizarSprite();
    }

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

    public void AtualizarSprite()
    {
        if (sr != null)
        {
            // Aplica o sprite daltônico ou normal baseado na variável do PauseManager
            if (PauseManager.modoDaltonicoAtivo && spriteRaioDaltonico != null)
            {
                sr.sprite = spriteRaioDaltonico;
            }
            else if (spriteRaioNormal != null)
            {
                sr.sprite = spriteRaioNormal;
            }
        }
    }
}