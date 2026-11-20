using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // 1. Adicione esta linha no topo

public class MinigameAula : MinigameBase
{
    [Header("Elementos de UI")]
    public TMP_Text textoInstrucao; // 2. Mude de Text para TMP_Text
    public TMP_Text textoTempo;     // 2. Mude de Text para TMP_Text
    public TMP_Text textoScore;     // 2. Mude de Text para TMP_Text
    public Slider barraFoco;

    private float pontuacaoAtual = 0f;
    private float pontuacaoMaxima = 100f;
    private bool jogando = false;

    private void Start()
    {
        IniciarMinigame();
    }

    public override void IniciarMinigame()
    {
        StartCoroutine(FluxoMinigame());
    }

    private IEnumerator FluxoMinigame()
{
    // Esconde o tempo e resultado de início
    if (textoTempo != null) textoTempo.gameObject.SetActive(false);
    if (textoScore != null) textoScore.gameObject.SetActive(false);

    // 1. Mostrar instrução por 3 segundos
    if (textoInstrucao != null)
    {
        textoInstrucao.gameObject.SetActive(true);
        textoInstrucao.text = "Aperte ESPAÇO para focar na aula!";
    }
    
    yield return new WaitForSeconds(3f);

    if (textoInstrucao != null)
        textoInstrucao.gameObject.SetActive(false);

    // Ativa a contagem de tempo
    if (textoTempo != null) textoTempo.gameObject.SetActive(true);

    // 2. Loop do minigame (30 segundos)
    jogando = true;
    float tempoRestante = TempoDuracao;

    while (tempoRestante > 0 && jogando)
    {
        tempoRestante -= Time.deltaTime;

        if (textoTempo != null)
            textoTempo.text = $"Tempo: {Mathf.CeilToInt(tempoRestante)}s";

        if (Input.GetKeyDown(KeyCode.Space))
        {
            pontuacaoAtual = Mathf.Clamp(pontuacaoAtual + 10f, 0f, pontuacaoMaxima);
        }
        else
        {
            pontuacaoAtual = Mathf.Clamp(pontuacaoAtual - (5f * Time.deltaTime), 0f, pontuacaoMaxima);
        }

        if (barraFoco != null)
            barraFoco.value = pontuacaoAtual / pontuacaoMaxima;

        yield return null;
    }

    FinalizarMinigame();
}

public override void FinalizarMinigame()
{
    jogando = false;
    float notaNormalizada = Mathf.Clamp01(pontuacaoAtual / pontuacaoMaxima);

    MinigameResult resultado = new MinigameResult
    {
        NormalizedScore = notaNormalizada,
        Recompensa = Mathf.RoundToInt(notaNormalizada * 10)
    };

    if (textoScore != null)
    {
        textoScore.gameObject.SetActive(true);
        textoScore.text = $"Fim! Nota: {(notaNormalizada * 100):F0}%";
    }

    OnMinigameCompleted?.Invoke(resultado);
    Debug.Log($"Minigame concluído! Nota normalizada: {notaNormalizada}");
}
}