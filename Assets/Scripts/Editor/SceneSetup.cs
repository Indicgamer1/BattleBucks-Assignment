using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using BattleBucks.Match;
using BattleBucks.Systems;
using BattleBucks.UI;

namespace BattleBucks.Editor
{
    /// <summary>
    /// Menu: BattleBucks → Setup Scene
    /// Builds the entire scene wired for 1920×1080 landscape (Android 16:9).
    /// Delete all generated objects, then run again to rebuild cleanly.
    /// </summary>
    public static class SceneSetup
    {
        // ── Brand palette ────────────────────────────────────────────────
        private static readonly Color ColBg         = new Color(0.04f, 0.04f, 0.07f, 1f);   // dark navy
        private static readonly Color ColPanel      = new Color(0.08f, 0.07f, 0.11f, 0.97f); // dark purple-tinted
        private static readonly Color ColHeader     = new Color(0.91f, 0.22f, 0.16f, 1f);    // #E8372A brand red
        private static readonly Color ColText       = new Color(1.00f, 1.00f, 1.00f, 1f);
        private static readonly Color ColSubText    = new Color(1.00f, 1.00f, 1.00f, 0.50f);
        private static readonly Color ColRowDefault = new Color(1f,    1f,    1f,    0.04f);
        private static readonly Color ColRowLeader  = new Color(1.00f, 0.75f, 0.10f, 0.22f); // gold tint for #1
        private static readonly Color ColWarning    = new Color(1.00f, 0.60f, 0.00f, 1f);    // amber
        private static readonly Color ColFeedBg     = new Color(0.00f, 0.00f, 0.00f, 0.45f); // kill feed line bg

        // ── Landscape reference resolution ───────────────────────────────
        private const int   PLAYER_COUNT = 10;
        private const int   FEED_LINES   = 6;
        private const float REF_W        = 1920f;
        private const float REF_H        = 1080f;

        // ── Font sizes ───────────────────────────────────────────────────
        private const float BODY_FS      = 21f;   // general body text
        private const float RANK_FS      = 15f;   // small rank number (#1, #2...)
        private const float SCORE_FS     = 30f;   // big prominent kill count
        private const float FEED_FS      = 19f;   // kill feed entries
        private const float TIMER_VAL_FS = 64f;   // large timer digits

        // ── Layout constants (all in reference-space pixels) ─────────────
        //  Leaderboard
        private const float LB_W          = 400f;
        private const float LB_H          = REF_H - 32f;   // 1048
        private const float LB_ROW_H      = 80f;
        private const float LB_ROW_GAP    = 6f;
        private const float LB_ROW_W      = LB_W - 28f;    // 372
        private const float LB_HEADER_H   = 54f;
        private const float LB_DIVIDER_Y  = -(LB_HEADER_H + 20f);
        private const float LB_ROW0_Y     = LB_DIVIDER_Y - 10f;

        //  Timer
        private const float TIMER_W       = 280f;
        private const float TIMER_H       = 112f;

        //  Kill feed
        private const float FEED_W        = 460f;
        private const float FEED_LINE_H   = 46f;
        private const float FEED_LINE_GAP = 6f;

        // ── Entry point ──────────────────────────────────────────────────

        /// <summary>Destroys all generated root objects so Setup Scene can run fresh.</summary>
        [MenuItem("BattleBucks/Clear Scene _F4")]
        public static void ClearScene()
        {
            string[] names = { "GameSession", "KillSimulator", "UIManager", "Canvas", "EventSystem", "ScorePopup" };
            foreach (string n in names)
            {
                var go = GameObject.Find(n);
                if (go != null)
                {
                    Undo.DestroyObjectImmediate(go);
                    Debug.Log($"[BattleBucks] Removed {n}");
                }
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        [MenuItem("BattleBucks/Setup Scene _F5")]
        public static void SetupScene()
        {
            MatchConfig config = EnsureMatchConfig();

            // ── Logic GameObjects ─────────────────────────────────────────
            GameObject sessionGO   = CreateGO("GameSession");
            GameObject simulatorGO = CreateGO("KillSimulator");
            GameObject uiMgrGO     = CreateGO("UIManager");

            KillSimulator     killSim = simulatorGO.AddComponent<KillSimulator>();
            UIManager         uiMgr   = uiMgrGO.AddComponent<UIManager>();
            MatchBootstrapper boot    = sessionGO.AddComponent<MatchBootstrapper>();

            // ── Camera ────────────────────────────────────────────────────
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                cam = camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
            }
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = ColBg;
            cam.orthographic    = true;

            // ── Canvas ─────────────────────────────────────────────────────
            GameObject canvasGO = CreateGO("Canvas");
            Canvas     canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler        = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(REF_W, REF_H);
            // 0.5 = balanced width+height match — prevents TMP blur on non-1920 displays.
            // Pure 0 (width-only) makes text render below atlas density on smaller screens.
            scaler.matchWidthOrHeight  = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Full-screen background
            StretchFull(CreateUIImage("Background", canvasGO.transform, ColBg)
                            .GetComponent<RectTransform>());

            // ── Event System ──────────────────────────────────────────────
            EventSystem existingES = Object.FindFirstObjectByType<EventSystem>();
            if (existingES == null)
            {
                GameObject esGO = CreateGO("EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
            else
            {
                var legacy = existingES.GetComponent<StandaloneInputModule>();
                if (legacy != null)
                {
                    Object.DestroyImmediate(legacy);
                    existingES.gameObject
                              .AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
            }

            // ── LEADERBOARD PANEL (left side) ─────────────────────────────
            //   Anchored to left-middle edge; stretches full height minus margin.
            GameObject lbPanel = CreatePanel("LeaderboardPanel", canvasGO.transform,
                anchorMin: new Vector2(0f, 0.5f),
                anchorMax: new Vector2(0f, 0.5f),
                pivot:     new Vector2(0f, 0.5f),
                pos:       new Vector2(16f, 0f),
                size:      new Vector2(LB_W, LB_H));

            // "LEADERBOARD" header
            CreateLabel("LB_Header", lbPanel.transform, "LEADERBOARD",
                anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                pivot:     new Vector2(0.5f, 1f),
                pos:       new Vector2(0f, -14f),
                size:      new Vector2(LB_W - 20f, LB_HEADER_H),
                fontSize: BODY_FS, color: ColHeader, style: FontStyles.Bold | FontStyles.UpperCase,
                align: TextAlignmentOptions.Center, charSpacing: 4f);

            // Divider line
            CreateImage("LB_Divider", lbPanel.transform,
                ColHeader * new Color(1, 1, 1, 0.35f),
                anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                pos:  new Vector2(0f, LB_DIVIDER_Y),
                size: new Vector2(LB_W - 28f, 2f));

            // ── Row container (VerticalLayoutGroup auto-stacks runtime rows) ─
            GameObject rowContainerGO = new GameObject("RowContainer", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(rowContainerGO, "Create RowContainer");
            rowContainerGO.transform.SetParent(lbPanel.transform, false);

            RectTransform rowContainerRT = rowContainerGO.GetComponent<RectTransform>();
            rowContainerRT.anchorMin        = new Vector2(0.5f, 1f);
            rowContainerRT.anchorMax        = new Vector2(0.5f, 1f);
            rowContainerRT.pivot            = new Vector2(0.5f, 1f);
            rowContainerRT.anchoredPosition = new Vector2(0f, LB_ROW0_Y);
            rowContainerRT.sizeDelta        = new Vector2(LB_ROW_W, 0f); // height driven by content

            var vlg = rowContainerGO.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment         = TextAnchor.UpperCenter;
            vlg.spacing                = LB_ROW_GAP;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = true;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.padding                = new RectOffset(0, 0, 0, 0);

            var csf = rowContainerGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ── Single template row (inactive — parented to lbPanel, NOT rowContainer) ─
            // Keeping it outside RowContainer ensures VerticalLayoutGroup never
            // includes it in layout, even on the first frame before SpawnRows runs.
            LeaderboardRowUI rowTemplate = BuildLeaderboardRowTemplate(lbPanel.transform);

            // ── Wire LeaderboardUI ────────────────────────────────────────
            LeaderboardUI lbUI = canvasGO.AddComponent<LeaderboardUI>();
            SetPrivateField(lbUI, "rowTemplate",  rowTemplate);
            SetPrivateField(lbUI, "rowContainer", rowContainerRT);
            SetPrivateField(uiMgr, "leaderboard", lbUI);

            // ── TIMER PANEL (top-centre) ───────────────────────────────────
            GameObject timerPanel = CreatePanel("TimerPanel", canvasGO.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                pivot:     new Vector2(0.5f, 1f),
                pos:       new Vector2(0f, -16f),
                size:      new Vector2(TIMER_W, TIMER_H));

            CreateLabel("Timer_Label", timerPanel.transform, "TIME REMAINING",
                anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                pivot:     new Vector2(0.5f, 1f),
                pos:       new Vector2(0f, -8f),
                size:      new Vector2(TIMER_W - 10f, 32f),
                fontSize: BODY_FS, color: ColSubText, style: FontStyles.Normal,
                align: TextAlignmentOptions.Center);

            TextMeshProUGUI timerTMP = CreateLabel("Timer_Value", timerPanel.transform, "03:00",
                anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                pivot:     new Vector2(0.5f, 1f),
                pos:       new Vector2(0f, -46f),
                size:      new Vector2(TIMER_W - 10f, 66f),
                fontSize: TIMER_VAL_FS, color: ColHeader, style: FontStyles.Bold,
                align: TextAlignmentOptions.Center);

            TimerUI timerUI = canvasGO.AddComponent<TimerUI>();
            SetPrivateField(timerUI, "timerText",        timerTMP);
            SetPrivateField(timerUI, "warningColor",     ColWarning);
            SetPrivateField(timerUI, "normalColor",      ColHeader);
            SetPrivateField(timerUI, "warningThreshold", 30f);

            // ── KILL FEED PANEL (top-right) ────────────────────────────────
            float feedTotalH = FEED_LINES * (FEED_LINE_H + FEED_LINE_GAP) + 8f;
            GameObject feedPanel = CreatePanel("KillFeedPanel", canvasGO.transform,
                anchorMin: new Vector2(1f, 1f),
                anchorMax: new Vector2(1f, 1f),
                pivot:     new Vector2(1f, 1f),
                pos:       new Vector2(-16f, -TIMER_H - 32f),
                size:      new Vector2(FEED_W, feedTotalH));
            feedPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0); // panel itself transparent

            TextMeshProUGUI[] feedLines = new TextMeshProUGUI[FEED_LINES];
            for (int i = 0; i < FEED_LINES; i++)
            {
                float lineY = -8f - i * (FEED_LINE_H + FEED_LINE_GAP);

                // Dark pill background behind each entry
                CreateImage($"FeedBg_{i}", feedPanel.transform, ColFeedBg,
                    anchorMin: new Vector2(1f, 1f), anchorMax: new Vector2(1f, 1f),
                    pos:  new Vector2(-4f, lineY - FEED_LINE_H * 0.5f),
                    size: new Vector2(FEED_W - 8f, FEED_LINE_H));

                feedLines[i] = CreateLabel($"FeedLine_{i}", feedPanel.transform, "",
                    anchorMin: new Vector2(1f, 1f), anchorMax: new Vector2(1f, 1f),
                    pivot:     new Vector2(1f, 1f),
                    pos:       new Vector2(-12f, lineY),
                    size:      new Vector2(FEED_W - 24f, FEED_LINE_H),
                    fontSize: FEED_FS, color: ColText, style: FontStyles.Normal,
                    align: TextAlignmentOptions.Right);
                UIVisibility.Hide(feedLines[i].gameObject);
            }

            KillFeedUI feedUI = canvasGO.AddComponent<KillFeedUI>();
            SetArrayField(feedUI, "feedLines", feedLines);
            SetPrivateField(feedUI, "lineDuration", 4f);

            // ── WINNER PANEL (full-screen overlay, starts hidden) ──────────
            GameObject winPanel = CreateUIImage("WinnerPanel", canvasGO.transform,
                new Color(0.04f, 0.02f, 0.02f, 0.96f));
            StretchFull(winPanel.GetComponent<RectTransform>());
            UIVisibility.Hide(winPanel);   // hidden via CanvasGroup — keeps GameObject active

            CreateImage("Win_AccentTop", winPanel.transform, ColHeader,
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                pos: new Vector2(0f, 130f), size: new Vector2(480f, 3f));

            TextMeshProUGUI subTitleTMP = CreateLabel("Win_SubTitle", winPanel.transform, "WINNER",
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                pivot:     new Vector2(0.5f, 0.5f),
                pos:       new Vector2(0f, 90f),
                size:      new Vector2(560f, 50f),
                fontSize: BODY_FS, color: ColHeader,
                style: FontStyles.Bold | FontStyles.UpperCase,
                align: TextAlignmentOptions.Center, charSpacing: 10f);

            TextMeshProUGUI winNameTMP = CreateLabel("Win_PlayerName", winPanel.transform, "Player X",
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                pivot:     new Vector2(0.5f, 0.5f),
                pos:       new Vector2(0f, 10f),
                size:      new Vector2(700f, 100f),
                fontSize: 72f, color: ColText, style: FontStyles.Bold,
                align: TextAlignmentOptions.Center);

            TextMeshProUGUI winScoreTMP = CreateLabel("Win_Score", winPanel.transform, "10 kills",
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                pivot:     new Vector2(0.5f, 0.5f),
                pos:       new Vector2(0f, -70f),
                size:      new Vector2(400f, 50f),
                fontSize: BODY_FS, color: ColSubText, style: FontStyles.Normal,
                align: TextAlignmentOptions.Center);

            CreateImage("Win_AccentBot", winPanel.transform, ColHeader,
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                pos: new Vector2(0f, -110f), size: new Vector2(480f, 3f));

            // WinnerUI must be on an ACTIVE object (canvasGO) — Awake is never called
            // for components on inactive GameObjects, so the OnMatchEnded subscription
            // would never happen if WinnerUI were placed on the inactive winPanel.
            WinnerUI winnerUI = canvasGO.AddComponent<WinnerUI>();
            SetPrivateField(winnerUI, "panel",           winPanel);
            SetPrivateField(winnerUI, "winnerNameText",  winNameTMP);
            SetPrivateField(winnerUI, "winnerScoreText", winScoreTMP);
            SetPrivateField(winnerUI, "subTitleText",    subTitleTMP);

            // ── SCORE POPUP (floating +1 labels, parented to lbPanel) ────────
            // Template: a small bold TMP label inside lbPanel (sibling of RowContainer)
            TextMeshProUGUI popupTmp = CreateLabel("ScorePopup_Template", lbPanel.transform, "+1",
                anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                pivot:     new Vector2(0.5f, 1f),
                pos:       new Vector2(0f, 0f),
                size:      new Vector2(60f, 36f),
                fontSize: 22f, color: new Color(1f, 0.85f, 0.1f, 1f),
                style: FontStyles.Bold, align: TextAlignmentOptions.Center);
            UIVisibility.Hide(popupTmp.gameObject);

            ScorePopupUI scorePopup = canvasGO.AddComponent<ScorePopupUI>();
            SetPrivateField(scorePopup, "popupTemplate", popupTmp);
            SetPrivateField(scorePopup, "leaderboard",   lbUI);
            // xOffset: popup anchor is at lbPanel center-top (LB_W/2 from left).
            // LB_W/2 + LB_W/2 + 20 = LB_W + 20 → 20px right of the panel's right edge.
            SetPrivateField(scorePopup, "xOffset", LB_W * 0.5f + 30f);

            // ── Wire MatchBootstrapper ─────────────────────────────────────
            var bootSO = new SerializedObject(boot);
            bootSO.FindProperty("config").objectReferenceValue        = config;
            bootSO.FindProperty("killSimulator").objectReferenceValue = killSim;
            bootSO.FindProperty("uiManager").objectReferenceValue     = uiMgr;
            bootSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[BattleBucks] Landscape scene setup complete (1920×1080). Press Play.");
            Selection.activeGameObject = sessionGO;
        }

        // ── Row template builder (ONE instance, inactive — cloned at runtime) ──

        private static LeaderboardRowUI BuildLeaderboardRowTemplate(Transform parent)
        {
            // Use a plain RectTransform-only root; VerticalLayoutGroup controls sizing.
            var rowGO = new GameObject("Row_Template", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(rowGO, "Create Row_Template");
            rowGO.transform.SetParent(parent, false);

            // Explicitly set sizeDelta so LeaderboardUI.SpawnRows reads the correct
            // row height (default RectTransform sizeDelta is 100×100).
            rowGO.GetComponent<RectTransform>().sizeDelta = new Vector2(LB_ROW_W, LB_ROW_H);

            // Image background — full size driven by LayoutElement
            var img   = rowGO.AddComponent<Image>();
            img.color = ColRowDefault;
            img.raycastTarget = false;

            // LayoutElement tells VerticalLayoutGroup the row height
            var le = rowGO.AddComponent<LayoutElement>();
            le.preferredHeight = LB_ROW_H;
            le.flexibleWidth   = 1f;

            // Rank  (#1) — small, dimmed, secondary info
            TextMeshProUGUI rankTMP = CreateLabel("Rank", rowGO.transform, "#1",
                anchorMin: new Vector2(0f, 0.5f), anchorMax: new Vector2(0f, 0.5f),
                pivot:     new Vector2(0f, 0.5f),
                pos:       new Vector2(12f, 0f),
                size:      new Vector2(42f, LB_ROW_H - 6f),
                fontSize: RANK_FS, color: ColSubText, style: FontStyles.Bold,
                align: TextAlignmentOptions.Left);

            // Name — large, primary, white
            TextMeshProUGUI nameTMP = CreateLabel("Name", rowGO.transform, "Player",
                anchorMin: new Vector2(0f, 0.5f), anchorMax: new Vector2(0f, 0.5f),
                pivot:     new Vector2(0f, 0.5f),
                pos:       new Vector2(58f, 0f),
                size:      new Vector2(LB_ROW_W - 58f - 80f, LB_ROW_H - 6f),
                fontSize: BODY_FS, color: ColText, style: FontStyles.Bold,
                align: TextAlignmentOptions.Left);

            // Score — largest element, brand red, high visual weight
            TextMeshProUGUI scoreTMP = CreateLabel("Score", rowGO.transform, "0",
                anchorMin: new Vector2(1f, 0.5f), anchorMax: new Vector2(1f, 0.5f),
                pivot:     new Vector2(1f, 0.5f),
                pos:       new Vector2(-14f, 0f),
                size:      new Vector2(76f, LB_ROW_H - 4f),
                fontSize: SCORE_FS, color: ColHeader, style: FontStyles.Bold,
                align: TextAlignmentOptions.Right);

            LeaderboardRowUI rowUI = rowGO.AddComponent<LeaderboardRowUI>();
            SetPrivateField(rowUI, "rankText",      rankTMP);
            SetPrivateField(rowUI, "nameText",      nameTMP);
            SetPrivateField(rowUI, "scoreText",     scoreTMP);
            SetPrivateField(rowUI, "rowBackground", img);
            SetPrivateField(rowUI, "leaderColor",   ColRowLeader);
            SetPrivateField(rowUI, "defaultColor",  ColRowDefault);

            // Hidden via CanvasGroup — stays active so Awake/lifecycle methods work
            UIVisibility.Hide(rowGO);
            return rowUI;
        }

        // ── MatchConfig asset ─────────────────────────────────────────────

        private static MatchConfig EnsureMatchConfig()
        {
            const string path = "Assets/Settings/MatchConfig.asset";
            MatchConfig cfg = AssetDatabase.LoadAssetAtPath<MatchConfig>(path);
            if (cfg != null) return cfg;

            Directory.CreateDirectory(Application.dataPath + "/../Assets/Settings");
            cfg = ScriptableObject.CreateInstance<MatchConfig>();
            AssetDatabase.CreateAsset(cfg, path);
            AssetDatabase.SaveAssets();
            return cfg;
        }

        // ── UI helpers (overloaded for brevity) ──────────────────────────

        private static GameObject CreateGO(string name)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            return go;
        }

        private static GameObject CreatePanel(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            GameObject go = CreateUIImage(name, parent, ColPanel);
            var rt        = go.GetComponent<RectTransform>();
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            go.AddComponent<CanvasGroup>();
            return go;
        }

        private static GameObject CreateUIImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return go;
        }

        private static GameObject CreateImage(string name, Transform parent, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var go = CreateUIImage(name, parent, color);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            return go;
        }

        private static TextMeshProUGUI CreateLabel(string name, Transform parent, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size,
            float fontSize, Color color, FontStyles style,
            TextAlignmentOptions align    = TextAlignmentOptions.Left,
            Vector2? pivot               = null,
            float charSpacing            = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = pivot ?? new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text             = text;
            tmp.fontSize         = fontSize;
            tmp.color            = color;
            tmp.fontStyle        = style;
            tmp.alignment        = align;
            tmp.characterSpacing = charSpacing;
            tmp.raycastTarget    = false;
            tmp.overflowMode     = TextOverflowModes.Ellipsis;
            return tmp;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ── Reflection helpers ────────────────────────────────────────────

        private static void SetArrayField<T>(Object target, string fieldName, T[] values)
            where T : Object
        {
            var so   = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null) { Debug.LogWarning($"[BB] Array '{fieldName}' not found on {target.GetType().Name}"); return; }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var so   = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null) { Debug.LogWarning($"[BB] Field '{fieldName}' not found on {target.GetType().Name}"); return; }
            switch (value)
            {
                case Object o:   prop.objectReferenceValue = o;   break;
                case float  f:   prop.floatValue  = f;            break;
                case int    i:   prop.intValue    = i;            break;
                case bool   b:   prop.boolValue   = b;            break;
                case Color  c:   prop.colorValue  = c;            break;
                default: Debug.LogWarning($"[BB] Unsupported type for '{fieldName}'"); break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
