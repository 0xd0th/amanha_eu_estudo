using System.Collections.Generic;
using System.Globalization;
using AmanhaEuEstudo.Core;
using UnityEditor;
using UnityEngine;

namespace AmanhaEuEstudo.EditorTools
{
    // Updates the existing Choice assets with the B12 fields (study debt, low score, gain factors).
    // Only the fields listed below are touched; every change is logged; running it twice changes nothing.
    public static class B12Menu
    {
        private const string ChoicesFolder = "Assets/_Project/Data/Choices";

        // Target values. B02Menu uses the same ones so a clean project is born with them.
        // PLACEHOLDER balance values, pending team decision (G03).
        internal const int SkipAndSleepHealth = 15; // GDD
        internal const int SkipAndSleepDebt = 1;
        internal const int MemesDebt = 1;
        internal const float MemesLowScoreThreshold = 0.5f;
        internal const int MemesLowScorePsychological = 6;
        internal const int MemesLowScoreIntelligence = -6;
        internal const float GoTiredIntelligenceFactor = 0.5f;

        private static string PathOf(string id) => $"{ChoicesFolder}/Choice_{id}.asset";

        [MenuItem("Tools/Amanhã eu estudo/Atualizar dados (B12)")]
        public static void UpdateData()
        {
            int changes = 0;

            changes += Update("skip_and_sleep", (so, name) =>
                SetStatDelta(so, name, "deltas", StatType.Health, SkipAndSleepHealth)
                + SetInt(so, name, "debtDelta", SkipAndSleepDebt));

            changes += Update("memes", (so, name) =>
                SetInt(so, name, "debtDelta", MemesDebt)
                + SetFloat(so, name, "lowScoreThreshold", MemesLowScoreThreshold)
                + SetStatDeltas(so, name, "lowScoreDeltas", new[]
                {
                    new StatDelta(StatType.Psychological, MemesLowScorePsychological),
                    new StatDelta(StatType.Intelligence, MemesLowScoreIntelligence),
                })
                + SetBool(so, name, "lowScoreCausesExhaustion", true));

            changes += Update("go_tired", (so, name) =>
            {
                int n = 0;
                if (TryReadIntelligence("go_to_college", out int intelligence))
                    n += SetStatDelta(so, name, "deltas", StatType.Intelligence, intelligence);
                n += SetStatFactors(so, name, "gainFactors", new[]
                {
                    new StatFactor(StatType.Intelligence, GoTiredIntelligenceFactor),
                });
                n += SetBool(so, name, "forceMaxDifficulty", true);
                return n;
            });

            AssetDatabase.SaveAssets();
            Debug.Log(changes == 0
                ? "[B12] Dados já estavam atualizados: nada foi alterado."
                : $"[B12] {changes} campo(s) atualizado(s).");
        }

        // Loads the asset, runs the edit and saves only if something changed. Returns the change count.
        private static int Update(string id, System.Func<SerializedObject, string, int> edit)
        {
            string path = PathOf(id);
            var asset = AssetDatabase.LoadAssetAtPath<ChoiceData>(path);
            if (asset == null)
            {
                Debug.LogWarning($"[B12] Pulado (não existe): {path}");
                return 0;
            }
            var so = new SerializedObject(asset);
            int changes = edit(so, $"Choice_{id}");
            if (changes > 0)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }
            return changes;
        }

        // Sum of the Intelligence deltas of a choice (the rules add duplicates up too).
        private static bool TryReadIntelligence(string id, out int value)
        {
            value = 0;
            string path = PathOf(id);
            var asset = AssetDatabase.LoadAssetAtPath<ChoiceData>(path);
            if (asset == null)
            {
                Debug.LogWarning($"[B12] Não foi possível ler a Inteligência de {path} (não existe). Choice_go_tired.deltas não foi alterado.");
                return false;
            }
            bool found = false;
            foreach (var d in asset.Deltas)
            {
                if (d.stat != StatType.Intelligence) continue;
                value += d.amount;
                found = true;
            }
            if (!found)
                Debug.LogWarning($"[B12] {path} não tem delta de Inteligência. Choice_go_tired.deltas não foi alterado.");
            return found;
        }

        // ---------------------------------------------------------------- field helpers (return 1 if changed)

        private static SerializedProperty Find(SerializedObject so, string asset, string field)
        {
            var p = so.FindProperty(field);
            if (p == null) Debug.LogWarning($"[B12] {asset}: campo '{field}' não encontrado.");
            return p;
        }

        private static int SetInt(SerializedObject so, string asset, string field, int value)
        {
            var p = Find(so, asset, field);
            if (p == null || p.intValue == value) return 0;
            Debug.Log($"[B12] {asset}.{field}: {p.intValue} → {value}");
            p.intValue = value;
            return 1;
        }

        private static int SetFloat(SerializedObject so, string asset, string field, float value)
        {
            var p = Find(so, asset, field);
            if (p == null || Mathf.Approximately(p.floatValue, value)) return 0;
            Debug.Log($"[B12] {asset}.{field}: {Fmt(p.floatValue)} → {Fmt(value)}");
            p.floatValue = value;
            return 1;
        }

        private static int SetBool(SerializedObject so, string asset, string field, bool value)
        {
            var p = Find(so, asset, field);
            if (p == null || p.boolValue == value) return 0;
            Debug.Log($"[B12] {asset}.{field}: {p.boolValue} → {value}");
            p.boolValue = value;
            return 1;
        }

        // Sets the amount of the first delta of the stat; appends one if the stat is not listed.
        private static int SetStatDelta(SerializedObject so, string asset, string field, StatType stat, int amount)
        {
            var array = Find(so, asset, field);
            if (array == null) return 0;
            for (int i = 0; i < array.arraySize; i++)
            {
                var element = array.GetArrayElementAtIndex(i);
                if (element.FindPropertyRelative("stat").enumValueIndex != (int)stat) continue;
                var amountProp = element.FindPropertyRelative("amount");
                if (amountProp.intValue == amount) return 0;
                Debug.Log($"[B12] {asset}.{field}[{DisplayNames.Of(stat)}]: {amountProp.intValue} → {amount}");
                amountProp.intValue = amount;
                return 1;
            }
            int index = array.arraySize;
            array.arraySize = index + 1;
            var added = array.GetArrayElementAtIndex(index);
            added.FindPropertyRelative("stat").enumValueIndex = (int)stat;
            added.FindPropertyRelative("amount").intValue = amount;
            Debug.Log($"[B12] {asset}.{field}[{DisplayNames.Of(stat)}]: (ausente) → {amount}");
            return 1;
        }

        private static int SetStatDeltas(SerializedObject so, string asset, string field, StatDelta[] target)
        {
            var array = Find(so, asset, field);
            if (array == null) return 0;

            var current = new List<string>();
            for (int i = 0; i < array.arraySize; i++)
            {
                var e = array.GetArrayElementAtIndex(i);
                current.Add(Describe((StatType)e.FindPropertyRelative("stat").enumValueIndex,
                    e.FindPropertyRelative("amount").intValue.ToString("+0;-0;0")));
            }
            var wanted = new List<string>();
            foreach (var d in target) wanted.Add(Describe(d.stat, d.amount.ToString("+0;-0;0")));
            if (current.SequenceEqualTo(wanted)) return 0;

            Debug.Log($"[B12] {asset}.{field}: [{string.Join(", ", current)}] → [{string.Join(", ", wanted)}]");
            array.arraySize = target.Length;
            for (int i = 0; i < target.Length; i++)
            {
                var e = array.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("stat").enumValueIndex = (int)target[i].stat;
                e.FindPropertyRelative("amount").intValue = target[i].amount;
            }
            return 1;
        }

        private static int SetStatFactors(SerializedObject so, string asset, string field, StatFactor[] target)
        {
            var array = Find(so, asset, field);
            if (array == null) return 0;

            var current = new List<string>();
            for (int i = 0; i < array.arraySize; i++)
            {
                var e = array.GetArrayElementAtIndex(i);
                current.Add(Describe((StatType)e.FindPropertyRelative("stat").enumValueIndex,
                    "×" + Fmt(e.FindPropertyRelative("factor").floatValue)));
            }
            var wanted = new List<string>();
            foreach (var f in target) wanted.Add(Describe(f.stat, "×" + Fmt(f.factor)));
            if (current.SequenceEqualTo(wanted)) return 0;

            Debug.Log($"[B12] {asset}.{field}: [{string.Join(", ", current)}] → [{string.Join(", ", wanted)}]");
            array.arraySize = target.Length;
            for (int i = 0; i < target.Length; i++)
            {
                var e = array.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("stat").enumValueIndex = (int)target[i].stat;
                e.FindPropertyRelative("factor").floatValue = target[i].factor;
            }
            return 1;
        }

        private static string Describe(StatType stat, string value) => $"{DisplayNames.Of(stat)} {value}";

        private static string Fmt(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static bool SequenceEqualTo(this List<string> a, List<string> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (a[i] != b[i]) return false;
            return true;
        }
    }
}
