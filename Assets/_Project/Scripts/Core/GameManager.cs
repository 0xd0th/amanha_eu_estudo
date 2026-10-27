using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmanhaEuEstudo.Core
{
    // Orchestrator. Knows nothing about HUD, minigames or combat: they subscribe to the events.
    // Instance is set in Awake (execution order -100) so other scripts can use it in OnEnable/Start.
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameConfig config;

        private GameRules rules;

        public event Action<StatType, int, int> OnStatChanged;
        public event Action<TimeOfDay> OnPeriodChanged;
        public event Action<int> OnDayStarted;
        public event Action<int> OnDayEnded;
        public event Action<StatType> OnBurnout;
        public event Action OnFinalDayReached;
        public event Action OnGameRestarted;

        public GameConfig Config => config;
        // Exposed for reading (HUD, save system). Mutate only through the methods below.
        public GameState State => rules?.State;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("More than one GameManager in the scene; destroying the extra one.", this);
                Destroy(this);
                return;
            }
            if (config == null)
            {
                Debug.LogError("GameManager has no GameConfig assigned.", this);
                return;
            }

            Instance = this;
            rules = new GameRules(config);
            rules.OnStatChanged += (s, o, n) => OnStatChanged?.Invoke(s, o, n);
            rules.OnPeriodChanged += p => OnPeriodChanged?.Invoke(p);
            rules.OnDayStarted += d => OnDayStarted?.Invoke(d);
            rules.OnDayEnded += d => OnDayEnded?.Invoke(d);
            rules.OnBurnout += s => OnBurnout?.Invoke(s);
            rules.OnFinalDayReached += () => OnFinalDayReached?.Invoke();
            rules.OnGameRestarted += () => OnGameRestarted?.Invoke();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public List<ChoiceData> GetAvailableChoices()
        {
            return rules != null ? rules.GetAvailableChoices() : new List<ChoiceData>();
        }

        // performanceMultiplier scales positive deltas only (see GameRules.ApplyChoice).
        public bool ApplyChoice(ChoiceData choice, float performanceMultiplier = 1f)
        {
            return rules != null && rules.ApplyChoice(choice, performanceMultiplier);
        }

        public void RestartGame()
        {
            rules?.Restart();
        }
    }
}
