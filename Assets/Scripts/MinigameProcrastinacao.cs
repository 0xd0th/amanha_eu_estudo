using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MinigameProcrastinacao : MonoBehaviour
{
    [Header("Elementos de UI")]
    public TMP_Text textoInstrucao;
    public TMP_Text textoTempo;
    public TMP_Text textoScore;
    public Slider barraFoco;

    [Header("Configurações do Desafio")]
    public float tempoDuracao = 30f;
    public float vidaFoco = 100f;
    private float vidaAtual;
    private bool jogando = false;

    private void Start()
    {
        IniciarMinigame();
    }

    public void IniciarMinigame()
    {
        vidaAtual = vidaFoco;
        StartCoroutine(FluxoMinigame());
    }

    private IEnumerator FluxoMinigame()
    {
        if (textoTempo != null) textoTempo.gameObject.SetActive(false);
        if (textoScore != null) textoScore.gameObject.SetActive(false);

        // 1. Instruções
        if (textoInstrucao != null)
        {
            textoInstrucao.gameObject.SetActive(true);
            textoInstrucao.text = "Desvie das Notificações! Use as Setas (← →) ou A/D";
        }

        yield return new WaitForSeconds(3f);

        if (textoInstrucao != null) textoInstrucao.gameObject.SetActive(false);
        if (textoTempo != null) textoTempo.gameObject.SetActive(true);

        // 2. Loop do Jogo
        jogando = true;
        float tempoRestante = tempoDuracao;

        while (tempoRestante > 0 && jogando)
        {
            tempoRestante -= Time.deltaTime;

            if (textoTempo != null)
                textoTempo.text = $"Tempo: {Mathf.CeilToInt(tempoRestante)}s";

            // Atualiza a barra visual
            if (barraFoco != null)
                barraFoco.value = vidaAtual / vidaFoco;

            yield return null;
        }

        FinalizarMinigame();
    }

    public void ReceberDanoDistracao(float dano)
    {
        if (!jogando) return;

        vidaAtual = Mathf.Clamp(vidaAtual - dano, 0f, vidaFoco);
        Debug.Log($"Hit! Foco restante: {vidaAtual}");
    }

    public void FinalizarMinigame()
    {
        jogando = false;
        float notaNormalizada = Mathf.Clamp01(vidaAtual / vidaFoco);

        if (textoScore != null)
        {
            textoScore.gameObject.SetActive(true);
            textoScore.text = $"Fim do Estudo! Foco mantido: {(notaNormalizada * 100):F0}%";
        }
    }
}