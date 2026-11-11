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

    private float savedPlayerCritChance;
    private float savedPlayerEvasionChance;
    private float savedBossEvasionChance;

    public float bossMaxHealth = 100f;
    public float bossCurrentHealth;

    [Header("Limites Máximos/Mínimos")]
    public const float MAX_EVASION_LIMIT = 30f;
    public const float MIN_EVASION_LIMIT = 5f;

    [Header("Estados e Status Negativos")]
    public bool hasNegativeStatus = false;
    public string currentStatusName = "Nenhum";
    public bool isPlayerStunnedByDistraction = false; 

    [Header("Turno do Boss")]
    private int bossTurnCounter = 0; 
    private bool isAwaitingBossTurn  = false; 

    [Header("Elementos de HUD (UI)")]
    public Slider healthSlider;
    public TextMeshProUGUI healthText;
    public Slider focusSlider;
    public TextMeshProUGUI focusText;
    public TextMeshProUGUI statusText;

    [Header("Painéis da HUD")]
    public GameObject mainActionsPanel;
    public GameObject skillsPanel;

    [Header("Registro de Batalha (Log)")]
    public TextMeshProUGUI battleLogText; 
    [TextArea] public string currentLogMessage = "Derrote a prova e passe de semestre";
    private System.Collections.Generic.Queue<string> logQueue = new System.Collections.Generic.Queue<string>();
    private bool isDisplayingLog = false;
    public float messageDisplayDelay = 2.0f;


    [Header("Imagens de Efeitos (Status Visuais)")]
    public GameObject dudaStatusImage;      
    public GameObject distracaoStatusImage;   

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

        savedPlayerCritChance = playerCritChance;
        savedPlayerEvasionChance = playerEvasionChance;
        savedBossEvasionChance = bossEvasionChance;

        if (dudaStatusImage != null) dudaStatusImage.SetActive(false);
        if (distracaoStatusImage != null) distracaoStatusImage.SetActive(false);

        bossCurrentHealth = bossMaxHealth;
        ShowMainPanel();
        UpdateHUD();
        UpdateLog("A batalha começou!");
    }

private void UpdateLog(string message)
    {
        logQueue.Enqueue(message);

        if (!isDisplayingLog)
        {
            StartCoroutine(ProcessLogQueueRoutine());
        }
    }

    private System.Collections.IEnumerator ProcessLogQueueRoutine()
    {
        isDisplayingLog = true;

        while (logQueue.Count > 0)
        {
            string nextMessage = logQueue.Dequeue();
            currentLogMessage = nextMessage;

            if (battleLogText != null)
            {
                battleLogText.text = currentLogMessage;
            }

            yield return new WaitForSeconds(messageDisplayDelay);
        }

        isDisplayingLog = false;
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

        if (dudaStatusImage != null)
            dudaStatusImage.SetActive(hasNegativeStatus && currentStatusName == "Dúvida");

        if (distracaoStatusImage != null)
            distracaoStatusImage.SetActive(isPlayerStunnedByDistraction);
    }

    public void ActionAttack()
    {
        if (isAwaitingBossTurn) return; 

        if (isPlayerStunnedByDistraction)
        {
            UpdateLog("Você está distraído e perdeu o turno!");
            isPlayerStunnedByDistraction = false;
            UpdateHUD();
            EndPlayerTurn();
            return;
        }

        if (Random.Range(0f, 100f) < bossEvasionChance)
        {
            UpdateLog("O Boss se esquivou do seu ataque!");
        }
        else
        {
            float damage = baseAttack;
            if (Random.Range(0f, 100f) < playerCritChance)
            {
                damage *= 2f;
                UpdateLog("ACERTO CRÍTICO!");
            }

            bossCurrentHealth -= damage;
            
            UpdateLog($"Você causou: {damage:F1} de dano no chefe!");
            UpdateHUD();

            if (bossCurrentHealth <= 0)
            {
                bossCurrentHealth = 0;
                UpdateLog("Você derrotou a prova e passou de semestre!");
            }

        }

        EndPlayerTurn();
    }

    public void ActionSkillFocus()
    {
        if (isAwaitingBossTurn) return;

        if (isPlayerStunnedByDistraction)
        {
            UpdateLog("Você está distraído e perdeu o turno!");
            isPlayerStunnedByDistraction = false;
            UpdateHUD();
            EndPlayerTurn();
            return;
        }

        UpdateLog("Jogador focou a mente . . . sua chance de critico e de esquiva aumentaram!");
        
        currentFocus = Mathf.Min(currentFocus + 10f, maxFocus);

        if (!hasNegativeStatus)
        {
            savedPlayerCritChance = Mathf.Min(savedPlayerCritChance + 10f, 100f);
            savedPlayerEvasionChance = Mathf.Min(savedPlayerEvasionChance + 3f, MAX_EVASION_LIMIT);
            
            playerCritChance = savedPlayerCritChance;
            playerEvasionChance = savedPlayerEvasionChance;
            bossEvasionChance = savedBossEvasionChance;
        }

        UpdateHUD();
        EndPlayerTurn();
    }

    public void ActionSkillCleanMind()
    {
        if (isAwaitingBossTurn) return;

        if (isPlayerStunnedByDistraction)
        {
            UpdateLog("Você está distraído e perdeu o turno!");
            isPlayerStunnedByDistraction = false;
            UpdateHUD();
            EndPlayerTurn();
            return;
        }

        UpdateLog("Você limpou a sua mente e se purificou!");
        
        if (hasNegativeStatus)
        {
            hasNegativeStatus = false;
            currentStatusName = "Nenhum";
            
            playerCritChance = savedPlayerCritChance;
            playerEvasionChance = savedPlayerEvasionChance;
            bossEvasionChance = savedBossEvasionChance;

            UpdateLog("Efeitos negativos removidos!");
        }

        UpdateHUD();
        EndPlayerTurn();
    }

    private void EndPlayerTestTurn() {} 

    private void EndPlayerTurn()
    {
        isAwaitingBossTurn = true;
        ShowMainPanel();
        Invoke(nameof(ExecuteBossTurn), 1f);
    }

    private void ExecuteBossTurn()
    {
        isAwaitingBossTurn = false;
        bossTurnCounter++;

        float bossDamage = Random.Range(4f, 8.1f);
        
        if (Random.Range(0f, 100f) < playerEvasionChance)
        {
            UpdateLog("Você se esquivou do ataque físico do Chefe!");
        }
        else
        {
            currentHealth -= bossDamage;
            UpdateLog($"Ele causou {bossDamage:F1} de dano físico em você!");
            UpdateHUD();
        }

        if (bossTurnCounter >= 2)
        {
            bossTurnCounter = 0; 
            ExecuteBossSpell();
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            UpdateLog("Você reprovou");


        UpdateHUD();
    }
    }

    private void ExecuteBossSpell()
    {
        int chosenSpell = Random.Range(0, 2);
        bool spellHits = Random.Range(0f, 100f) < 75f;

        if (!spellHits)
        {
            UpdateLog("A prova tentou lançar um feitiço, mas errou!");
            return;
        }

        if (chosenSpell == 0)
        {
            UpdateLog("A prova lançou o feitiço: DÚVIDA!");

            if (currentFocus > 20f)
            {
                currentFocus -= 20f;
                UpdateLog("Seu foco foi o bastante para anular o feitiço.");
                UpdateHUD();
            }
            else
            {
                hasNegativeStatus = true;
                currentStatusName = "Dúvida"; 

                playerCritChance = 0f;
                playerEvasionChance = 0f;
                bossEvasionChance = Mathf.Min(savedBossEvasionChance * 2f, 100f);

                UpdateLog("Seu foco não foi o bastante e agora você está com Dúvida.");
                UpdateHUD();
            }
        }
        else
        {
            UpdateLog("Chefe lançou o feitiço: DISTRAÇÃO!");

            if (currentFocus < 30f)
            {
                isPlayerStunnedByDistraction = true;
                UpdateLog("Seu foco não foi o bastante para resistir ao feitiço de Distração!");
                UpdateHUD();
            }
            else
            {
                currentFocus -= 30f;
                UpdateLog("Seu foco foi o bastante para anular o feitiço de Distração.");
                UpdateHUD();
            }
        }

        UpdateHUD();
    }

    public void ShowSkillsPanel() { if (!isAwaitingBossTurn) { mainActionsPanel.SetActive(false); skillsPanel.SetActive(true); } }
    public void ShowMainPanel() { mainActionsPanel.SetActive(true); skillsPanel.SetActive(false); }
}