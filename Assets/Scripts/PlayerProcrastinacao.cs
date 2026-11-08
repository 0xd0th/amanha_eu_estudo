using UnityEngine;
using UnityEngine.InputSystem; // Usa o novo Input System

public class PlayerProcrastinacao : MonoBehaviour
{
    public float velocidade = 10f;
    public float limiteX = 7f;

    void Update()
    {
        float moverX = 0f;

        // Leitura através do novo Input System
        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
            {
                moverX = -1f;
            }
            else if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
            {
                moverX = 1f;
            }
        }

        // Aplica o movimento
        Vector3 posicao = transform.position;
        posicao.x += moverX * velocidade * Time.deltaTime;
        posicao.x = Mathf.Clamp(posicao.x, -limiteX, limiteX);

        transform.position = posicao;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Notificacao"))
        {
            MinigameProcrastinacao manager = FindAnyObjectByType<MinigameProcrastinacao>();
            if (manager != null)
            {
                manager.ReceberDanoDistracao(15f);
            }

            Destroy(col.gameObject);
        }
    }
}