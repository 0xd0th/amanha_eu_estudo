using System.Collections.Generic;
using System.IO;
using AmanhaEuEstudo.Core;
using AmanhaEuEstudo.DebugTools;
using AmanhaEuEstudo.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AmanhaEuEstudo.EditorTools
{
    // Editor menus for the HUD card (B08). Nothing is overwritten: existing assets are logged and skipped.
    public static class B08Menu
    {
        private const string SpriteFolder = "Assets/_Project/Art/UI/HUD/Placeholder";
        private const string WhitePixelPath = SpriteFolder + "/white_pixel.png";
        private const string ConfigPath = "Assets/_Project/Data/HudConfig.asset";
        private const string GameConfigPath = "Assets/_Project/Data/GameConfig.asset";
        private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string StatBarPath = PrefabFolder + "/StatBar.prefab";
        private const string HudPath = PrefabFolder + "/HUD.prefab";
        private const string ScenePath = "Assets/_Project/Scenes/B08_HudTestScene.unity";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        private static readonly string[] RequiredSprites =
        {
            "icon_saude_coracao", "icon_psicologico_cerebro", "icon_social_pessoas",
            "calendario_vazio", "pilha_livros", "retrato_personagem",
        };

        // ---------------------------------------------------------------- sprites

        [MenuItem("Tools/Amanhã eu estudo/Configurar sprites do HUD")]
        public static void ConfigureHudSprites()
        {
            if (!AssetDatabase.IsValidFolder(SpriteFolder))
            {
                Debug.LogError($"[B08] Pasta não encontrada: {SpriteFolder}");
                return;
            }
            EnsureWhitePixel();
            int changed = 0, total = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                total++;
                if (ApplyPixelArtImport(importer)) { importer.SaveAndReimport(); changed++; }
            }
            Debug.Log($"[B08] Sprites do HUD: {changed} reimportado(s), {total - changed} já estavam corretos.");
        }

        private static bool ApplyPixelArtImport(TextureImporter i)
        {
            bool dirty = false;
            if (i.textureType != TextureImporterType.Sprite) { i.textureType = TextureImporterType.Sprite; dirty = true; }
            if (i.spriteImportMode != SpriteImportMode.Single) { i.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            if (i.filterMode != FilterMode.Point) { i.filterMode = FilterMode.Point; dirty = true; }
            if (i.mipmapEnabled) { i.mipmapEnabled = false; dirty = true; }
            if (i.textureCompression != TextureImporterCompression.Uncompressed)
            {
                i.textureCompression = TextureImporterCompression.Uncompressed;
                dirty = true;
            }
            if (!i.alphaIsTransparency) { i.alphaIsTransparency = true; dirty = true; }
            return dirty;
        }

        private static void EnsureWhitePixel()
        {
            if (File.Exists(WhitePixelPath)) return;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            File.WriteAllBytes(WhitePixelPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(WhitePixelPath);
            Debug.Log($"[B08] Criado: {WhitePixelPath}");
        }

        // ---------------------------------------------------------------- HUD assets

        [MenuItem("Tools/Amanhã eu estudo/Criar HUD")]
        public static void CreateHud()
        {
            // All checks first: abort before creating anything.
            if (!File.Exists(TmpSettingsPath) || TMP_Settings.defaultFontAsset == null)
            {
                Debug.LogError("[B08] TextMeshPro Essential Resources não encontrados. Importe em " +
                    "Window > TextMeshPro > Import TMP Essential Resources e rode 'Criar HUD' de novo. Nada foi criado.");
                return;
            }
            if (!AssetDatabase.IsValidFolder(SpriteFolder))
            {
                Debug.LogError($"[B08] Pasta de sprites não encontrada: {SpriteFolder}. Nada foi criado.");
                return;
            }
            var missing = new List<string>();
            foreach (string name in RequiredSprites)
                if (!File.Exists($"{SpriteFolder}/{name}.png")) missing.Add(name + ".png");
            if (missing.Count > 0)
            {
                Debug.LogError($"[B08] Sprites faltando em {SpriteFolder}: {string.Join(", ", missing)}. Nada foi criado.");
                return;
            }

            ConfigureHudSprites();
            EnsureFolder("Assets/_Project/Data");
            EnsureFolder(PrefabFolder);

            var font = TMP_Settings.defaultFontAsset;
            var config = LoadOrCreateConfig();
            var statBar = LoadOrBuildStatBar(font, config);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(HudPath) != null)
                Debug.Log($"[B08] Pulado (já existe): {HudPath}");
            else
                BuildHudPrefab(font, config, statBar);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static Sprite LoadSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/{name}.png");
        }

        private static HudConfig LoadOrCreateConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<HudConfig>(ConfigPath);
            if (existing != null)
            {
                Debug.Log($"[B08] Pulado (já existe): {ConfigPath}");
                return existing;
            }

            var config = ScriptableObject.CreateInstance<HudConfig>();
            // PLACEHOLDERS: colors, Intelligence icon (people sprite) and pixel steps are pending art/team (G03).
            var bars = new[]
            {
                new StatBarVisual(StatType.Health, LoadSprite("icon_saude_coracao"), new Color(0.86f, 0.25f, 0.30f), 48),
                new StatBarVisual(StatType.Intelligence, LoadSprite("icon_social_pessoas"), new Color(0.30f, 0.55f, 0.95f), 48),
                new StatBarVisual(StatType.Psychological, LoadSprite("icon_psicologico_cerebro"), new Color(0.70f, 0.45f, 0.90f), 48),
            };
            var portrait = LoadSprite("retrato_personagem");
            var states = new[]
            {
                new PortraitState(50, portrait, Color.white),
                new PortraitState(25, portrait, new Color(0.75f, 0.75f, 0.75f)),
                new PortraitState(0, portrait, new Color(0.45f, 0.45f, 0.45f)),
            };
            config.Setup(bars, states);
            AssetDatabase.CreateAsset(config, ConfigPath);
            Debug.Log($"[B08] Criado: {ConfigPath}");
            return config;
        }

        private static GameObject LoadOrBuildStatBar(TMP_FontAsset font, HudConfig config)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(StatBarPath);
            if (existing != null)
            {
                Debug.Log($"[B08] Pulado (já existe): {StatBarPath}");
                return existing;
            }

            var root = new GameObject("StatBar", typeof(RectTransform), typeof(StatBarView));
            var rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(460, 56);

            var icon = NewImage("Icon", root.transform, null, Color.white);
            Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(56, 56));
            icon.preserveAspect = true;

            var frame = NewImage("Frame", root.transform, null, Color.white);
            Place(frame.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(68, 0), new Vector2(300, 32));

            var background = NewImage("Background", frame.transform, null, Color.black);
            Stretch(background.rectTransform, 3);

            var fill = NewImage("Fill", frame.transform, LoadSprite("white_pixel"), Color.white);
            Stretch(fill.rectTransform, 3);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;

            var value = NewText("Value", root.transform, font, 36, Color.white, TextAlignmentOptions.MidlineLeft);
            Place(value.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(380, 0), new Vector2(80, 56));

            var so = new SerializedObject(root.GetComponent<StatBarView>());
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("frame").objectReferenceValue = frame;
            so.FindProperty("background").objectReferenceValue = background;
            so.FindProperty("fill").objectReferenceValue = fill;
            so.FindProperty("valueText").objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, StatBarPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[B08] Criado: {StatBarPath}");
            return prefab;
        }

        private static void BuildHudPrefab(TMP_FontAsset font, HudConfig config, GameObject statBarPrefab)
        {
            var root = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(HudView));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            // No GraphicRaycaster on purpose: the HUD never blocks clicks.
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); // placeholder until the art defines the resolution (G03)
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var topLeft = new Vector2(0, 1);
            var topRight = new Vector2(1, 1);

            // Portrait + bars (top left)
            var portraitImage = NewImage("Portrait", root.transform, LoadSprite("retrato_personagem"), Color.white);
            Place(portraitImage.rectTransform, topLeft, topLeft, new Vector2(24, -24), new Vector2(168, 200));
            portraitImage.rectTransform.pivot = topLeft;
            var portrait = portraitImage.gameObject.AddComponent<PortraitView>();
            var pso = new SerializedObject(portrait);
            pso.FindProperty("config").objectReferenceValue = config;
            pso.FindProperty("image").objectReferenceValue = portraitImage;
            pso.ApplyModifiedPropertiesWithoutUndo();

            var barViews = new List<StatBarView>();
            for (int i = 0; i < EnumExtensions.StatCount; i++)
            {
                var stat = (StatType)i;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(statBarPrefab, root.transform);
                go.name = $"StatBar_{stat}";
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = topLeft;
                rt.anchoredPosition = new Vector2(216, -24 - i * 64);
                var view = go.GetComponent<StatBarView>();
                var bso = new SerializedObject(view);
                bso.FindProperty("stat").enumValueIndex = (int)stat;
                bso.ApplyModifiedPropertiesWithoutUndo();
                barViews.Add(view);
            }

            // Debt pile (top right corner)
            var pileGo = new GameObject("DebtPile", typeof(RectTransform), typeof(DebtPileView));
            pileGo.transform.SetParent(root.transform, false);
            var pileRt = (RectTransform)pileGo.transform;
            pileRt.anchorMin = pileRt.anchorMax = pileRt.pivot = topRight;
            pileRt.anchoredPosition = new Vector2(-24, -24);
            pileRt.sizeDelta = new Vector2(165, 165);
            var pileSprite = LoadSprite("pilha_livros");
            var back = NewImage("Back", pileGo.transform, pileSprite, Color.white);
            Stretch(back.rectTransform, 0);
            var front = NewImage("Front", pileGo.transform, pileSprite, Color.white);
            Stretch(front.rectTransform, 0);
            front.type = Image.Type.Filled;
            front.fillMethod = Image.FillMethod.Vertical;
            front.fillOrigin = (int)Image.OriginVertical.Bottom;
            var dso = new SerializedObject(pileGo.GetComponent<DebtPileView>());
            dso.FindProperty("config").objectReferenceValue = config;
            dso.FindProperty("back").objectReferenceValue = back;
            dso.FindProperty("front").objectReferenceValue = front;
            dso.ApplyModifiedPropertiesWithoutUndo();

            // Calendar (left of the pile)
            var calImage = NewImage("Calendar", root.transform, LoadSprite("calendario_vazio"), Color.white);
            var calRt = calImage.rectTransform;
            calRt.anchorMin = calRt.anchorMax = calRt.pivot = topRight;
            calRt.anchoredPosition = new Vector2(-24 - 165 - 24, -24);
            calRt.sizeDelta = new Vector2(135, 132);
            var calendar = calImage.gameObject.AddComponent<CalendarView>();
            var number = NewText("DaysNumber", calImage.transform, font, 56, new Color(0.1f, 0.1f, 0.12f), TextAlignmentOptions.Center);
            Stretch(number.rectTransform, 0);
            var period = NewText("Period", calImage.transform, font, 28, Color.white, TextAlignmentOptions.Top);
            Place(period.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, -4), new Vector2(200, 32));
            period.rectTransform.pivot = new Vector2(0.5f, 1);
            var support = NewText("DaysSupport", calImage.transform, font, 24, Color.white, TextAlignmentOptions.Top);
            Place(support.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, -38), new Vector2(240, 30));
            support.rectTransform.pivot = new Vector2(0.5f, 1);
            var cso = new SerializedObject(calendar);
            cso.FindProperty("numberText").objectReferenceValue = number;
            cso.FindProperty("periodText").objectReferenceValue = period;
            cso.FindProperty("supportText").objectReferenceValue = support;
            cso.ApplyModifiedPropertiesWithoutUndo();

            // HudView wiring
            var hso = new SerializedObject(root.GetComponent<HudView>());
            hso.FindProperty("config").objectReferenceValue = config;
            var barsProp = hso.FindProperty("bars");
            barsProp.arraySize = barViews.Count;
            for (int i = 0; i < barViews.Count; i++) barsProp.GetArrayElementAtIndex(i).objectReferenceValue = barViews[i];
            hso.FindProperty("debtPile").objectReferenceValue = pileGo.GetComponent<DebtPileView>();
            hso.FindProperty("calendar").objectReferenceValue = calendar;
            hso.FindProperty("portrait").objectReferenceValue = portrait;
            hso.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, HudPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[B08] Criado: {HudPath}");
        }

        // ---------------------------------------------------------------- test scene

        [MenuItem("Tools/Amanhã eu estudo/Criar cena de teste do HUD")]
        public static void CreateTestScene()
        {
            var gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            if (gameConfig == null)
            {
                Debug.LogError("[B08] GameConfig não encontrado. Rode antes 'Criar dados de exemplo'.");
                return;
            }
            var hud = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
            if (hud == null)
            {
                Debug.LogError("[B08] HUD.prefab não encontrado. Rode antes 'Criar HUD'.");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Debug.Log($"[B08] Pulado (já existe): {ScenePath}");
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
            var gso = new SerializedObject(gm.GetComponent<GameManager>());
            gso.FindProperty("config").objectReferenceValue = gameConfig;
            gso.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.InstantiatePrefab(hud);

            // DebugHud at the bottom/center so it does not cover the HUD (top corners).
            var debugHud = new GameObject("DebugHud", typeof(DebugHud));
            var dso = new SerializedObject(debugHud.GetComponent<DebugHud>());
            dso.FindProperty("panelAnchorMin").vector2Value = new Vector2(0.3f, 0f);
            dso.FindProperty("panelAnchorMax").vector2Value = new Vector2(0.7f, 0.62f);
            dso.ApplyModifiedPropertiesWithoutUndo();

            new GameObject("HudDebugPanel", typeof(HudDebugPanel));

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[B08] Criada: {ScenePath}");
        }

        // ---------------------------------------------------------------- helpers

        private static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, TMP_FontAsset font, float size,
            Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = alignment;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.raycastTarget = false;
            return t;
        }

        private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(anchorMin.x, anchorMin.y);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
