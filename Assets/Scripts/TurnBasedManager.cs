using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurnBasedManager : MonoBehaviour
{
    public static TurnBasedManager Instance { get; private set; }

    [Header("Status do Jogador")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float maxFocus = 100f;
    public float currentFocus;
    public float baseAttack = 10f;

    [Header("Chances de Combate (%)")]
    [Range(0, 100)] public float playerCritChance = 10f;
    [Range(0, 100)] public float playerEvasionChance = 10f;
    [Range(0, 100)] public float bossEvasionChance = 20f;

    [Header("Limites Máximos/Mínimos")]
    public const float MAX_EVASION_LIMIT = 75f;
    public const float MIN_EVASION_LIMIT = 5f;

    [Header("Estados e Status Negativos")]
    public bool hasNegativeStatus = false;
    public string currentStatusName = "Nenhum";
    public bool isPlayerStunnedByDistraction = false; 

    [Header("Turno do Boss")]
    private int bossTurnCounter = 0; 

    [Header("Elementos de HUD (UI)")]
    public Slider healthSlider;
    public TextMeshProUGUI healthText;
    public Slider focusSlider;
    public TextMeshProUGUI focusText;
    public TextMeshProUGUI statusText;

    [Header("Painéis da HUD")]
    public GameObject mainActionsPanel;
    public GameObject skillsPanel;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (PlayerStatus.Instance != null)
        {
            float vitPct = PlayerStatus.Instance.vitalityPercentage / 100f;
            float strPct = PlayerStatus.Instance.strengthPercentage / 100f;
            float intPct = PlayerStatus.Instance.intelligencePercentage / 100f;

            maxHealth = Mathf.Lerp(10f, 100f, vitPct);
            baseAttack = Mathf.Lerp(1f, 10f, strPct);
            maxFocus = Mathf.Lerp(10f, 100f, intPct);
        }

        currentHealth = maxHealth;
        currentFocus = maxFocus;

        ShowMainPanel();
        UpdateHUD();
    }

    public void UpdateHUD()
    {
        if (healthSlider != null) { healthSlider.maxValue = maxHealth; healthSlider.value = currentHealth; }
        if (healthText != null) healthText.text = $"VIDA: {Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";

        if (focusSlider != null) { focusSlider.maxValue = maxFocus; focusSlider.value = currentFocus; }
        if (focusText != null) focusText.text = $"MENTE: {Mathf.CeilToInt(currentFocus)} / {Mathf.CeilToInt(maxFocus)}";

        if (statusText != null)
        {
            statusText.text = hasNegativeStatus ? $"Status: <color=red>{currentStatusName}</color>" : "Status: Normal";
        }
    }


    public void ActionAttack()
    {
        if (isPlayerStunnedByDistraction)
        {
            Debug.Log("Você está distraído e perdeu o turno!");
            isPlayerStunnedByDistraction = false;
            EndPlayerTurn();
            return;
        }

        Debug.Log("Jogador atacou!");
        if (Random.Range(0f, 100f) < bossEvasionChance)
        {
            Debug.Log("O Boss se esquivou do ataque!");
        }
        else
        {
            float damage = baseAttack;
            if (Random.Range(0f, 100f) < playerCritChance)
            {
                damage *= 2f;
                Debug.Log("ACERTO CRÍTICO!");
            }
            Debug.Log($"Dano causado no Boss: {damage}");
        }

        EndPlayerTurn();
    }

    public void ActionSkillFocus()
    {
        if (isPlayerStunnedByDistraction)
        {
            Debug.Log("Você está distraído e perdeu o turno!");
            isPlayerStunnedByDistraction = false;
            EndPlayerTurn();
            return;
        }

        Debug.Log("Jogador focou a mente!");
        
        currentFocus = Mathf.Min(currentFocus + 10f, maxFocus);

        playerCritChance = Mathf.Min(playerCritChance + 15f, 100f);
        playerEvasionChance = Mathf.Min(playerEvasionChance + 15f, MAX_EVASION_LIMIT);
        bossEvasionChance = Mathf.Max(bossEvasionChance - 10f, MIN_EVASION_LIMIT);

        UpdateHUD();
        EndPlayerTurn();
    }

    public void ActionSkillCleanMind()
    {
        if (isPlayerStunnedByDistraction)
        {
            Debug.Log("Você está distraído e perdeu o turno!");
            isPlayerStunnedByDistraction = false;
            EndPlayerTurn();
            return;
        }

        Debug.Log("Jogador limpou a mente!");
        if (hasNegativeStatus)
        {
            hasNegativeStatus = false;
            currentStatusName = "Nenhum";
        
            playerCritChance = 10f; 
            Debug.Log("Efeitos negativos removidos!");
        }

        UpdateHUD();
        EndPlayerTurn();
    }


    private void EndPlayerTurn()
    {
        ShowMainPanel();
        Invoke(nameof(ExecuteBossTurn), 1f);
    }

    private void ExecuteBossTurn()
    {
        Debug.Log("--- Turno do Chefe ---");
        bossTurnCounter++;

        float bossDamage = Random.Range(5f, 9.1f);
        
        if (Random.Range(0f, 100f) < 25f) 
        {
            bossDamage *= 1.5f;
            Debug.Log("CRÍTICO DO BOSS!");
        }

        if (Random.Range(0f, 100f) < playerEvasionChance)
        {
            Debug.Log("Você se esquivou do ataque físico do Chefe!");
        }
        else
        {
            currentHealth -= bossDamage;
            Debug.Log($"Chefe causou {bossDamage:F1} de dano físico!");
        }

        if (bossTurnCounter >= 2)
        {
            bossTurnCounter = 0; 
            ExecuteBossSpell();
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Debug.Log("Jogador foi derrotado!");
        }

        UpdateHUD();
    }

    private void ExecuteBossSpell()
    {
        int chosenSpell = Random.Range(0, 2);

        bool spellHits = Random.Range(0f, 100f) < 75f;

        if (!spellHits)
        {
            Debug.Log("O Chefe tentou lançar um feitiço, mas errou!");
            return;
        }

        if (chosenSpell == 0)
        {

            Debug.Log("Chefe lançou o feitiço: DÚVIDA!");

            if (currentFocus > 20f)
            {
                currentFocus -= 20f;
                Debug.Log("Foco suficiente! O feitiço Dúvida foi anulado, perdendo 20 de Foco.");
            }
            else
            {
                hasNegativeStatus = true;
                currentStatusName = "Dúvida";
                playerCritChance = 0f;       
                bossEvasionChance *= 2f;     
                Debug.Log("Foco baixo! Você foi afetado pelo status Dúvida (Crit 0%, Esquiva do Boss dobrada).");
            }
        }
        else
        {
            Debug.Log("Chefe lançou o feitiço: DISTRAÇÃO!");

            if (currentFocus < 30f)
            {
                isPlayerStunnedByDistraction = true;
                Debug.Log("Foco abaixo de 30! Você foi atingido por Distração e perderá o próximo turno.");
            }
            else
            {
                Debug.Log("Seu foco era alto o suficiente (>= 30), você resistiu à Distração!");
            }
        }
    }

    public void ShowSkillsPanel() { mainActionsPanel.SetActive(false); skillsPanel.SetActive(true); }
    public void ShowMainPanel() { mainActionsPanel.SetActive(true); skillsPanel.SetActive(false); }
}