using UnityEngine;
using UnityEngine.UI;
using Antymology.Terrain;

namespace Antymology.UI
{
    public class NestCountUI : MonoBehaviour
    {
        // Reference to the UI Text component that displays the nest block count
        [SerializeField] private Text nestCountText;

        // Try to grab the Text component if not set in the inspector
        private void Awake()
        {
            if (nestCountText == null)
            {
                nestCountText = GetComponent<Text>();
            }
        }

        // Update the UI every frame with the current nest block count
        private void Update()
        {
            if (nestCountText == null || WorldManager.Instance == null)
            {
                return;
            }

            nestCountText.text = "Nest Blocks: " + WorldManager.Instance.NestBlockCount;
        }
    }
}
