using RavenTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RavenTweenSamples {
    /// <summary>
    /// Drop this component on an empty GameObject in a blank scene and press Play.
    /// Shows a typewriter reveal, then a score counter that punches on every update.
    /// </summary>
    public sealed class TextDemo : MonoBehaviour {
        TMP_Text _title;
        TMP_Text _score;
        int _scoreValue;

        void Start() {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            _title = CreateText(canvasGo.transform, new Vector2(0f, 80f), 48f);
            _score = CreateText(canvasGo.transform, new Vector2(0f, -40f), 72f);
            _title.text = "The raven takes flight.";
            _score.text = "0";

            // Reveal the title, then start awarding points.
            _title.TweenTypewriter(1.5f).OnComplete(AwardPoints);
        }

        void AwardPoints() {
            int from = _scoreValue;
            _scoreValue += 250;
            _score.TweenNumber(from, _scoreValue, 0.6f).Ease(Ease.OutCubic);
            Raven.PunchScale(_score.transform, Vector3.one * 0.25f, 0.4f);
            Raven.Delay(1.2f, AwardPoints);
        }

        static TMP_Text CreateText(Transform parent, Vector2 position, float size) {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = size;
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(900f, 120f);
            rect.anchoredPosition = position;
            return text;
        }
    }
}
