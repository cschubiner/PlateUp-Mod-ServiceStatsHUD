using KitchenServiceStatsHUD.Helpers;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KitchenServiceStatsHUD.Visuals
{
    public class ServiceStatsHudManager : MonoBehaviour
    {
        private const float Margin = 24f;
        private const float BaseFontSize = 22f;
        private const float ReferenceHeight = 1080f;
        private const float PanelWidth = 920f;
        private const float PanelHeight = 720f;

        private static ServiceStatsHudManager _instance;

        private Canvas _canvas;
        private RectTransform _panel;
        private TextMeshProUGUI _text;
        private TMP_FontAsset _font;

        public static ServiceStatsHudManager GetOrCreate()
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ServiceStatsHudManager>();
            }

            if (_instance == null)
            {
                GameObject gameObject = new GameObject("Service Stats HUD");
                gameObject.hideFlags = HideFlags.HideAndDontSave;
                _instance = gameObject.AddComponent<ServiceStatsHudManager>();
                DontDestroyOnLoad(gameObject);
            }

            return _instance;
        }

        public void Sync(ServiceStatsHudState state)
        {
            EnsureUi();

            string debugText = state == null
                ? string.Empty
                : ServiceStatsHudLogic.BuildDebugText(state.Cards, state.ShowOrders, state.ShowWashed, state.ShowActions, state.ShowDistance, state.ShowIdle);

            if (string.IsNullOrEmpty(debugText))
            {
                _canvas.enabled = false;
                return;
            }

            _canvas.enabled = true;
            _panel.anchoredPosition = new Vector2(-Margin, ServiceStatsHudLogic.CalculateAnchoredYOffset(state.YOffsetScreenPercent, ReferenceHeight, Margin));
            _text.font = ResolveFont(state.Font);
            _text.text = debugText;
            _text.fontSize = BaseFontSize * ClampScale(state.ScaleMultiplier);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void EnsureUi()
        {
            if (_canvas != null)
            {
                return;
            }

            _font = ResolveFont(ServiceStatsFontOption.Default);

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 4800;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.75f;

            GraphicRaycaster raycaster = gameObject.AddComponent<GraphicRaycaster>();
            raycaster.enabled = false;

            GameObject panelObject = new GameObject("Service Stats Debug Text");
            panelObject.transform.SetParent(transform, false);
            _panel = panelObject.AddComponent<RectTransform>();
            _panel.anchorMin = new Vector2(1f, 1f);
            _panel.anchorMax = new Vector2(1f, 1f);
            _panel.pivot = new Vector2(1f, 1f);
            _panel.anchoredPosition = new Vector2(-Margin, -Margin);
            _panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            _text = panelObject.AddComponent<TextMeshProUGUI>();
            _text.font = _font;
            _text.fontSize = BaseFontSize;
            _text.fontStyle = FontStyles.Bold;
            _text.color = new Color(1f, 1f, 1f, 0.96f);
            _text.alignment = TextAlignmentOptions.TopRight;
            _text.enableWordWrapping = false;
            _text.overflowMode = TextOverflowModes.Overflow;
            _text.richText = false;
            _text.margin = Vector4.zero;
            _text.raycastTarget = false;

            Shadow shadow = panelObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.90f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }

        private TMP_FontAsset ResolveFont(ServiceStatsFontOption option)
        {
            TMP_FontAsset defaultFont = ResolveDefaultFont();
            if (option == ServiceStatsFontOption.Default)
            {
                return defaultFont;
            }

            List<TMP_FontAsset> alternateFonts = ResolveAlternateFonts(defaultFont);
            int alternateIndex = ((int) option) - 1;
            if (alternateIndex >= 0 && alternateIndex < alternateFonts.Count)
            {
                return alternateFonts[alternateIndex];
            }

            return defaultFont;
        }

        private TMP_FontAsset ResolveDefaultFont()
        {
            if (TMP_Settings.defaultFontAsset != null)
            {
                return TMP_Settings.defaultFontAsset;
            }

            TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (fonts != null && fonts.Length > 0)
            {
                return fonts[0];
            }

            return null;
        }

        private static List<TMP_FontAsset> ResolveAlternateFonts(TMP_FontAsset defaultFont)
        {
            TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            List<TMP_FontAsset> alternateFonts = new List<TMP_FontAsset>();
            if (fonts == null)
            {
                return alternateFonts;
            }

            for (int index = 0; index < fonts.Length; index++)
            {
                TMP_FontAsset font = fonts[index];
                if (font == null || font == defaultFont || alternateFonts.Contains(font))
                {
                    continue;
                }

                alternateFonts.Add(font);
            }

            alternateFonts.Sort(CompareFontNames);
            return alternateFonts;
        }

        private static int CompareFontNames(TMP_FontAsset left, TMP_FontAsset right)
        {
            string leftName = left == null ? string.Empty : left.name;
            string rightName = right == null ? string.Empty : right.name;
            return string.Compare(leftName, rightName, System.StringComparison.OrdinalIgnoreCase);
        }

        private static float ClampScale(float value)
        {
            if (value < 0.30f)
            {
                return 0.30f;
            }

            if (value > 1.30f)
            {
                return 1.30f;
            }

            return value;
        }
    }
}
