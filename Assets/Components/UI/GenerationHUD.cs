using UnityEngine;
using UnityEngine.UI;
using Antymology.Agents;
using Antymology.Terrain;

namespace Antymology.UI
{
    public class GenerationHUD : MonoBehaviour
    {
        [SerializeField] private Text hudText;

        private void Awake()
        {
            if (hudText == null)
            {
                hudText = GetComponent<Text>();
            }
        }

        private void Update()
        {
            if (hudText == null)
            {
                return;
            }

            AntSimulationController controller = Object.FindFirstObjectByType<AntSimulationController>();
            if (controller == null || WorldManager.Instance == null)
            {
                hudText.text = "Generating world...";
                return;
            }

            hudText.text =
                "Generation: " + controller.GenerationIndex +
                "\nTime Remaining: " + controller.TimeRemaining.ToString("0.0") +
                "\nAlive Ants: " + controller.AliveCount +
                "\nNest Blocks: " + WorldManager.Instance.NestBlockCount +
                "\nTotal Mulch Consumed: " + controller.CurrentTotalMulchConsumed;
        }
    }
}
