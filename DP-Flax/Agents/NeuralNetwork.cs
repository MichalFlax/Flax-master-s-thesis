/* 
 * Faculty of Information Technology of Brno University of Technology 
 * Master's thesis - Predictor of the Effect of Amino Acid Substitutions on Protein Stability
 * Author: Michal Flax
 * Year: 2017
 */

using System;
using System.Collections.Generic;
using Accord.Neuro;
using Accord.Neuro.Learning;

namespace DP_Flax
{
    /// <summary>
    /// This class implement neural network for classification.
    /// </summary>
    class NeuralNetwork : Agent
    {
        /// <summary>
        /// Neural network.
        /// </summary>
        private ActivationNetwork network;

        /// <summary>
        /// Vector contains names of selected features.
        /// </summary>
        private List<string> features = new List<string>() { "asa", "struc", "information_content", "3D_atom_numbers", "3D_freq", "windowAA10" };

        /// <summary>
        /// Number of created instance of this class.
        /// </summary>
        internal static int numberGlobal = 0;
        
        private int number = 0;

        /// <summary>
        /// Constructor for this class.
        /// </summary>
        /// <param name="data">Input data.</param>
        public NeuralNetwork(Data data) : base(data)
        {
            number = numberGlobal;

            numberGlobal++;
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public override int[] ClassificationOutputs { get; protected set; }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public override double[] RegressionOutputs
        {
            get
            {
                throw new NotSupportedException();
            }

            protected set
            {
            }
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public override void Init(bool train = false)
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
            Save<ActivationNetwork>(network, this.ToString());
        }

        protected override void Load()
        {
            network = Load<ActivationNetwork>(this.ToString());
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public override void Run()
        {
            var inputs = data.GetSelectedInput(features);

            for (int i = 0; i < inputs.Length; i++)
            {
                var output = network.Compute(inputs[i]);

                if (output[0] > 0.5)
                    ClassificationOutputs[i] = 1;
                else
                    ClassificationOutputs[i] = 0;
            }
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public override void Train()
        {
            var inputs = data.GetSelectedInput(features);

            var outputs = data.GetExpectedClassificationOutput();

            network = new ActivationNetwork(new SigmoidFunction(), inputs[0].Length, 25, 1);

            var initialization = new NguyenWidrow(network);

            initialization.Randomize();

            var teacher = new ResilientBackpropagationLearning(network);

            var NetworkOutputs = new double[inputs.Length][];

            for (int i = 0; i < NetworkOutputs.Length; i++)
            {
                NetworkOutputs[i] = new double[1] { outputs[i] };
            }

            double error = double.PositiveInfinity;

            int epoch = 0;

            while (error > 2.5 && epoch < 5000)
            {
                error = teacher.RunEpoch(inputs, NetworkOutputs);

                epoch++;
            }

            Save();
        }

        public override string ToString()
        {
            return this.GetType().Name + number;
        }
    }
}
