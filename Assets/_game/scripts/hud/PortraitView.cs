using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AmanhaEuEstudo.UI
{
    public class PortraitView : MonoBehaviour
    {
        [SerializeField] private HudConfig config;
        [SerializeField] private Image image;

        private readonly List<int> minValues = new List<int>();

        public void Configure(HudConfig newConfig)
        {
            config = newConfig;
        }

        public void Refresh(IReadOnlyList<int> stats)
        {
            if (config == null || image == null) return;
            var states = config.PortraitStates;
            minValues.Clear();
            for (int i = 0; i < states.Length; i++) minValues.Add(states[i].minValue);

            int index = PortraitSelector.Select(stats, config.PortraitSource, config.PortraitStat, minValues);
            if (index < 0) return;
            image.sprite = states[index].sprite;
            image.color = states[index].tint;
        }
    }
}
