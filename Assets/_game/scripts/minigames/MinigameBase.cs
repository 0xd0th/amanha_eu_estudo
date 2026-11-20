using System;
using UnityEngine;

[System.Serializable]
public struct MinigameResult
{
    public float NormalizedScore; // Nota de 0.0 a 1.0 (atende critério de aceite)
    public int Recompensa;
}

public abstract class MinigameBase : MonoBehaviour
{
    [Header("Configurações do Minigame")]
    public string NomeMinigame;
    public float TempoDuracao = 30f; // 30 a 60s
    public int Dificuldade = 1;

    // Ação acionada no fim para retornar ao fluxo principal UMA ÚNICA VEZ
    public Action<MinigameResult> OnMinigameCompleted;

    public abstract void IniciarMinigame();
    public abstract void FinalizarMinigame();
}