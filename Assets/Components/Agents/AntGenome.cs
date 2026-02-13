using System;

namespace Antymology.Agents
{
    [Serializable]
    public class AntGenome
    {
        // Movement weights: how much the ant prefers each direction
        public float moveNorthWeight = 1f;
        public float moveSouthWeight = 1f;
        public float moveEastWeight = 1f;
        public float moveWestWeight = 1f;
        // Probability to dig down
        public float digWeight = 0.3f;
        // Probability to eat mulch
        public float consumeWeight = 0.8f;
        // Probability to share health with another ant
        public float shareWeight = 0.2f;
        // Probability for queen to build nest block
        public float buildWeight = 0.4f;

        // Creates a random genome for a new ant
        public static AntGenome CreateRandom(Random rng)
        {
            return new AntGenome
            {
                moveNorthWeight = RandomWeight(rng),
                moveSouthWeight = RandomWeight(rng),
                moveEastWeight = RandomWeight(rng),
                moveWestWeight = RandomWeight(rng),
                digWeight = RandomWeight(rng),
                consumeWeight = RandomWeight(rng),
                shareWeight = RandomWeight(rng),
                buildWeight = RandomWeight(rng)
            };
        }

        // Makes a shallow copy of this genome
        public AntGenome Clone()
        {
            return (AntGenome)MemberwiseClone();
        }

        // Combines two genomes to make a child genome (randomly picks each weight)
        public AntGenome Crossover(AntGenome other, Random rng)
        {
            AntGenome child = new AntGenome();
            child.moveNorthWeight = Pick(rng, moveNorthWeight, other.moveNorthWeight);
            child.moveSouthWeight = Pick(rng, moveSouthWeight, other.moveSouthWeight);
            child.moveEastWeight = Pick(rng, moveEastWeight, other.moveEastWeight);
            child.moveWestWeight = Pick(rng, moveWestWeight, other.moveWestWeight);
            child.digWeight = Pick(rng, digWeight, other.digWeight);
            child.consumeWeight = Pick(rng, consumeWeight, other.consumeWeight);
            child.shareWeight = Pick(rng, shareWeight, other.shareWeight);
            child.buildWeight = Pick(rng, buildWeight, other.buildWeight);
            return child;
        }

        // Randomly mutates each weight with given rate and magnitude
        public void Mutate(Random rng, float rate, float magnitude)
        {
            moveNorthWeight = MutateValue(rng, moveNorthWeight, rate, magnitude);
            moveSouthWeight = MutateValue(rng, moveSouthWeight, rate, magnitude);
            moveEastWeight = MutateValue(rng, moveEastWeight, rate, magnitude);
            moveWestWeight = MutateValue(rng, moveWestWeight, rate, magnitude);
            digWeight = MutateValue(rng, digWeight, rate, magnitude);
            consumeWeight = MutateValue(rng, consumeWeight, rate, magnitude);
            shareWeight = MutateValue(rng, shareWeight, rate, magnitude);
            buildWeight = MutateValue(rng, buildWeight, rate, magnitude);
        }

        // Returns a random weight between 0.1 and 1.6
        private static float RandomWeight(Random rng)
        {
            return (float)rng.NextDouble() * 1.5f + 0.1f;
        }

        // Randomly picks one of two values
        private static float Pick(Random rng, float a, float b)
        {
            return rng.NextDouble() < 0.5 ? a : b;
        }

        // Mutates a value if random chance passes, clamps to minimum
        private static float MutateValue(Random rng, float value, float rate, float magnitude)
        {
            if (rng.NextDouble() > rate)
            {
                return value;
            }

            float delta = ((float)rng.NextDouble() * 2f - 1f) * magnitude;
            float mutated = value + delta;
            return Math.Max(0.05f, mutated);
        }
    }
}
