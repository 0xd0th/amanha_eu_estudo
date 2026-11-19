using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    public static PlayerStatus Instance { get; private set; }

    [Header("Atributos em Porcentagem (0 a 100)")]
    [Range(0f, 100f)] public float vitalityPercentage = 50f;
    [Range(0f, 100f)] public float strengthPercentage = 50f;
    [Range(0f, 100f)] public float intelligencePercentage = 50f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ModifyStats(float vitalityDelta, float strengthDelta, float intelligenceDelta)
    {
        vitalityPercentage = Mathf.Clamp(vitalityPercentage + vitalityDelta, 0f, 100f);
        strengthPercentage = Mathf.Clamp(strengthPercentage + strengthDelta, 0f, 100f);
        intelligencePercentage = Mathf.Clamp(intelligencePercentage + intelligenceDelta, 0f, 100f);
    }
}