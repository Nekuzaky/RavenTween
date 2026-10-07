using RavenTween;
using UnityEngine;
using UnityEngine.UI;

namespace RavenTweenSamples {
    /// <summary>
    /// Drop this component on an empty GameObject in a blank scene and press Play.
    /// It builds a small UI and animates it: panel slide-in, fade, and a pulsing button.
    /// </summary>
    public sealed class UIBasicsDemo : MonoBehaviour {
        CanvasGroup _group;
        RectTransform _panel;
        RectTransform _button;

        void Start() {
            BuildUI();
            // Slide the panel in from the left, then fade the whole group in as it arrives.
            _panel.anchoredPosition = new Vector2(-600f, 0f);
            _group.alpha = 0f;
            Raven.Sequence()
                .Chain(Raven.AnchoredPosition(_panel, Vector2.zero, 0.6f).Ease(Ease.OutBack))
                .Group(Raven.Alpha(_group, 1f, 0.4f))
                .OnComplete(PulseButton);
        }

        void PulseButton() {
            // An infinite yoyo scale: the classic "press me" pulse.
            Raven.Scale(_button, 1.1f, 0.5f).Ease(Ease.InOutSine).Infinite();
        }

        void BuildUI() {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var panelGo = new GameObject("Panel", typeof(Image), typeof(CanvasGroup));
            panelGo.transform.SetParent(canvasGo.transform, false);
            _panel = panelGo.GetComponent<RectTransform>();
            _panel.sizeDelta = new Vector2(400f, 240f);
            panelGo.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
            _group = panelGo.GetComponent<CanvasGroup>();

            var buttonGo = new GameObject("Button", typeof(Image));
            buttonGo.transform.SetParent(panelGo.transform, false);
            _button = buttonGo.GetComponent<RectTransform>();
            _button.sizeDelta = new Vector2(160f, 48f);
            _button.anchoredPosition = new Vector2(0f, -60f);
            buttonGo.GetComponent<Image>().color = new Color(0.9f, 0.45f, 0.2f);
        }
    }
}
