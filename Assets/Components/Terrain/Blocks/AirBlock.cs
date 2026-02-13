using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Antymology.Terrain
{
    /// <summary>
    /// The air type of block. Contains the internal data representing phermones in the air.
    /// </summary>
    public class AirBlock : AbstractBlock
    {

        #region Fields

        /// <summary>
        /// Statically held is visible.
        /// </summary>
        private static bool _isVisible = false;

        /// <summary>
        /// A dictionary representing the phermone deposits in the air. Each type of phermone gets it's own byte key, and each phermone type has a concentration.
        /// THIS CURRENTLY ONLY EXISTS AS A WAY OF SHOWING YOU HOW YOU CAN MANIPULATE THE BLOCKS.
        /// </summary>
        private Dictionary<byte, double> phermoneDeposits;

        /// <summary>
        /// Lightweight pheromone values for movement bias.
        /// </summary>
        private float foodPheromone;
        private float nestPheromone;

        #endregion

        #region Methods

        /// <summary>
        /// Air blocks are going to be invisible.
        /// </summary>
        public override bool isVisible()
        {
            return _isVisible;
        }

        /// <summary>
        /// Air blocks are invisible so asking for their tile map coordinate doesn't make sense.
        /// </summary>
        public override Vector2 tileMapCoordinate()
        {
            throw new Exception("An invisible tile cannot have a tile map coordinate.");
        }

        public float GetFoodPheromone()
        {
            return foodPheromone;
        }

        public float GetNestPheromone()
        {
            return nestPheromone;
        }

        public void AddFoodPheromone(float amount, float maxValue)
        {
            foodPheromone = Mathf.Min(maxValue, foodPheromone + amount);
        }

        public void AddNestPheromone(float amount, float maxValue)
        {
            nestPheromone = Mathf.Min(maxValue, nestPheromone + amount);
        }

        public void Decay(float rate)
        {
            foodPheromone = Mathf.Max(0f, foodPheromone - rate);
            nestPheromone = Mathf.Max(0f, nestPheromone - rate);
        }

        /// <summary>
        /// Diffuses pheromone values to/from neighboring air blocks.
        /// Each tick, a portion of this block's pheromone is shared with neighbors, and vice versa.
        /// </summary>
        /// <param name="neighbours">Array of adjacent blocks (should be AirBlocks).</param>
        public void Diffuse(AbstractBlock[] neighbours)
        {
            // Simple diffusion: average with neighbors
            float totalFood = foodPheromone;
            float totalNest = nestPheromone;
            int count = 1;
            foreach (var block in neighbours)
            {
                if (block is AirBlock air)
                {
                    totalFood += air.foodPheromone;
                    totalNest += air.nestPheromone;
                    count++;
                }
            }
            float avgFood = totalFood / count;
            float avgNest = totalNest / count;
            // Blend towards average (diffusion rate = 0.2)
            float diffusionRate = 0.2f;
            foodPheromone = Mathf.Lerp(foodPheromone, avgFood, diffusionRate);
            nestPheromone = Mathf.Lerp(nestPheromone, avgNest, diffusionRate);
        }

        #endregion

    }
}
