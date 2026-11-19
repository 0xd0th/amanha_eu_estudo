using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
[AddComponentMenu("Game/Opening Development Options")]
public class OpeningDevelopmentOptions : MonoBehaviour
{
    [Header("Desenvolvimento (somente no editor)")]
    [Tooltip("Abre direto no menu ao entrar em Play. Ignorado na versão compilada do jogo.")]
    [SerializeField] private bool skipOpening;

    public bool SkipOpening
    {
        get
        {
#if UNITY_EDITOR
            return skipOpening;
#else
            return false;
#endif
        }
    }
}
