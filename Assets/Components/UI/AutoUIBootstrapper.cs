using UnityEngine;
using UnityEngine.UI;

namespace Antymology.UI
{
    public class AutoUIBootstrapper : MonoBehaviour
    {
        private void Start()
        {
            if (Object.FindFirstObjectByType<GenerationHUD>() != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject("AntHUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            CreateHudText(canvas.transform, "HUDText", new Vector2(10f, -10f), new Vector2(420f, 140f))
                .gameObject.AddComponent<GenerationHUD>();
        }

        private Text CreateHudText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            text.text = "Loading...";
            return text;
        }

    }
}
