/* 
 * Faculty of Information Technology of Brno University of Technology 
 * Master's thesis - Predictor of the Effect of Amino Acid Substitutions on Protein Stability
 * Author: Michal Flax
 * Year: 2017
 */

using System;
using System.Collections.Generic;
using Accord.MachineLearning.DecisionTrees;
using Accord;

namespace DP_Flax
{
    /// <summary>
    /// This class contains algorithm random forest for classification.
    /// </summary>
    class RandomForest : Agent
    {
        /// <summary>
        /// Constructor for this class.
        /// </summary>
        /// <param name="data">Input data.</param>
        public RandomForest(Data data) : base(data) { }

        /// <summary>
        /// Instance of random forest.
        /// </summary>
        private Accord.MachineLearning.DecisionTrees.RandomForest forest;
        
        /// <summary>
        /// Vector contains names of selected features.
        /// </summary>
        private List<string> features = new List<string>() { "ECI", "ISA", "hydropathy", "aromatic", "aliphatic", "sidechain_hydro", "vnwaals_volume",
                                                             "asa", "struc", "information_content", "3D_atom_numbers", "3D_freq" };
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public override int[] ClassificationOutputs { get; protected set; }

        public override double[] RegressionOutputs
        {
            get
            {
                throw new NotSupportedException("This isn't agent for regression.");
            }

            protected set
            {
            }
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public override void Init(bool train = true)
        {
            if (train)
            {

            }
            else
            {
                Load();
            }
        }
        
        public override void Save()
        {
            Save<Accord.MachineLearning.DecisionTrees.RandomForest>(forest, this.ToString());
        }

        protected override void Load()
        {
            forest = Load<Accord.MachineLearning.DecisionTrees.RandomForest>(this.ToString());
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public override void Train()
        {
            var inputs = data.GetSelectedInput(features);
            var outputs = data.GetExpectedClassificationOutput();

            var DecisionVariables = new List<DecisionVariable>();

            for (int i = 0; i < inputs[0].Length; i++)
            {
                DecisionVariables.Add(DecisionVariable.Continuous(i.ToString(), new DoubleRange(0.0, 1.0)));
            }

            var teacher = new RandomForestLearning(DecisionVariables.ToArray())
            {
                NumberOfTrees = 20
            };

            forest = teacher.Learn(inputs, outputs);

            Save();         
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public override void Run()
        {
            var inputs = data.GetSelectedInput(features);

            ClassificationOutputs = forest.Decide(inputs);
        }

        public override string ToString()
        {
            return this.GetType().Name;
        }

    }
}
