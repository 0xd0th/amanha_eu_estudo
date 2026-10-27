using System.Collections.Generic;
using AmanhaEuEstudo.Core;
using AmanhaEuEstudo.DebugTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AmanhaEuEstudo.EditorTools
{
    public static class B02Menu
    {
        private const string DataFolder = "Assets/_Project/Data";
        private const string ChoicesFolder = DataFolder + "/Choices";
        private const string ConfigPath = DataFolder + "/GameConfig.asset";
        private const string ScenePath = "Assets/_Project/Scenes/B02_DebugScene.unity";

        private struct ChoiceSpec
        {
            public string id, name;
            public PeriodMask periods;
            public ChoiceAvailability availability;
            public bool exhausts;
            public StatDelta[] deltas;
        }

        private static StatDelta D(StatType s, int a) => new StatDelta(s, a);

        // PLACEHOLDER balance values for testing only; real numbers are pending team decision (G03).
        private static readonly ChoiceSpec[] Specs =
        {
            new ChoiceSpec { id = "go_to_college", name = "Ir para a faculdade", periods = PeriodMask.Morning,
                availability = ChoiceAvailability.RestedOnly,
                deltas = new[] { D(StatType.Intelligence, 4), D(StatType.Psychological, -2) } },
            new ChoiceSpec { id = "go_tired", name = "Ir mesmo cansado", periods = PeriodMask.Morning,
                availability = ChoiceAvailability.ExhaustedOnly,
                deltas = new[] { D(StatType.Intelligence, 2), D(StatType.Health, -5), D(StatType.Psychological, -5) } },
            new ChoiceSpec { id = "skip_and_sleep", name = "Faltar e dormir", periods = PeriodMask.Morning,
                availability = ChoiceAvailability.ExhaustedOnly,
                deltas = new[] { D(StatType.Health, 8), D(StatType.Psychological, 3), D(StatType.Intelligence, -5) } },
            new ChoiceSpec { id = "gym", name = "Academia", periods = PeriodMask.Afternoon | PeriodMask.Night,
                deltas = new[] { D(StatType.Health, 12), D(StatType.Psychological, -3) } },
            new ChoiceSpec { id = "study", name = "Estudar", periods = PeriodMask.Afternoon | PeriodMask.Night,
                deltas = new[] { D(StatType.Intelligence, 12), D(StatType.Psychological, -6), D(StatType.Health, -3) } },
            new ChoiceSpec { id = "memes", name = "Ver memes (amanhã eu estudo)", periods = PeriodMask.Afternoon | PeriodMask.Night,
                deltas = new[] { D(StatType.Psychological, 8), D(StatType.Intelligence, -2) } },
            new ChoiceSpec { id = "friends", name = "Sair com amigos", periods = PeriodMask.Afternoon | PeriodMask.Night,
                deltas = new[] { D(StatType.Psychological, 12), D(StatType.Health, -3), D(StatType.Intelligence, -3) } },
            new ChoiceSpec { id = "sleep_early", name = "Dormir cedo", periods = PeriodMask.Night,
                deltas = new[] { D(StatType.Health, 10), D(StatType.Psychological, 2) } },
            new ChoiceSpec { id = "all_nighter", name = "Virar a noite estudando", periods = PeriodMask.Night,
                exhausts = true,
                deltas = new[] { D(StatType.Intelligence, 15), D(StatType.Health, -10), D(StatType.Psychological, -5) } },
            new ChoiceSpec { id = "nightclub", name = "Ir para a boate", periods = PeriodMask.Night,
                exhausts = true,
                deltas = new[] { D(StatType.Psychological, 15), D(StatType.Health, -8) } },
        };

        [MenuItem("Tools/Amanhã eu estudo/Criar dados de exemplo")]
        public static void CreateSampleData()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(ChoicesFolder);

            var choices = new List<ChoiceData>();
            foreach (var spec in Specs)
            {
                string path = $"{ChoicesFolder}/Choice_{spec.id}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<ChoiceData>(path);
                if (existing != null)
                {
                    Debug.Log($"[B02] Pulado (já existe): {path}");
                    choices.Add(existing);
                    continue;
                }
                var choice = ScriptableObject.CreateInstance<ChoiceData>();
                choice.Setup(spec.id, spec.name, spec.periods, spec.availability, spec.deltas, spec.exhausts);
                AssetDatabase.CreateAsset(choice, path);
                choices.Add(choice);
                Debug.Log($"[B02] Criado: {path}");
            }

            if (AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath) != null)
            {
                Debug.Log($"[B02] Pulado (já existe): {ConfigPath}. Novas escolhas, se houver, não foram adicionadas à lista dele.");
            }
            else
            {
                var config = ScriptableObject.CreateInstance<GameConfig>();
                // Decay of -5 per stat is a placeholder, pending team decision (G03).
                config.Setup(60, 0, 100, 14, new[]
                {
                    D(StatType.Health, -5), D(StatType.Intelligence, -5), D(StatType.Psychological, -5),
                }, choices);
                AssetDatabase.CreateAsset(config, ConfigPath);
                Debug.Log($"[B02] Criado: {ConfigPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Amanhã eu estudo/Criar cena de debug")]
        public static void CreateDebugScene()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                EditorUtility.DisplayDialog("Amanhã eu estudo",
                    "GameConfig não encontrado. Rode antes 'Criar dados de exemplo'.", "OK");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Debug.Log($"[B02] Pulado (já existe): {ScenePath}");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder("Assets/_Project/Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera", typeof(Camera));
            cam.tag = "MainCamera";
            var camera = cam.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.12f);

            var gm = new GameObject("GameManager", typeof(GameManager));
            var so = new SerializedObject(gm.GetComponent<GameManager>());
            so.FindProperty("config").objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();

            new GameObject("DebugHud", typeof(DebugHud));

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[B02] Criada: {ScenePath}");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
