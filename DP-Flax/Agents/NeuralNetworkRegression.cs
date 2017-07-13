/* 
 * Faculty of Information Technology of Brno University of Technology 
 * Master's thesis - Predictor of the Effect of Amino Acid Substitutions on Protein Stability
 * Author: Michal Flax
 * Year: 2017
 */

using System;
using System.Collections.Generic;
using Accord;
using Accord.Math;
using Accord.Neuro;
using Accord.Neuro.Learning;

namespace DP_Flax
{
    /// <summary>
    /// This class implement neural network for regression.
    /// </summary>
    class NeuralNetworkRegression : Agent
    {
        /// <summary>
        /// Neural network.
        /// </summary>
        private ActivationNetwork network;
        
        /// <summary>
        /// Vectors contains names of features.
        /// </summary>
        private List<string> features;

        private List<string> features1 = new List<string>() { "molecular_weight", "K0", "H_t", "H_p", "P", "pH_i", "pK", "B_l", "R_f", "mi",
                                    "H_nc", "E_sm", "E_l", "E_t", "P_alfa", "P_beta", "P_t", "P_c", "C_alfa", "F",
                                    "B_r", "R_a", "N_s", "alfa_n", "alfa_c", "alfa_m", "V0", "N_m", "N_l", "H_gm",
                                    "ASA_D", "ASA_N", "dASA", "dG_h", "G_hD", "G_hN", "dH_h", "TdS_h", "dC_ph", "dG_c",
                                    "dH_c", "TdS_c", "dG", "dH", "TdS", "v", "s", "f"};

        private List<string> features2 = new List<string>() { "ECI", "ISA", "hydropathy", "aromatic", "aliphatic", "sidechain_hydro", "vnwaals_volume" };

        private List<string> features3 = new List<string>() { "asa", "struc", "information_content", "3D_atom_numbers", "3D_freq" };

        private List<string> features4 = new List<string>() { "windowAA10" };

        /// <summary>
        /// Number of created instance of this class.
        /// </summary>
        internal static int numberGlobal = 0;

        private int number = 0;

        /// <summary>
        /// Determines if network compute regression for stabilization or destabilization mutations.
        /// </summary>
        private bool positive;

        /// <summary>
        /// Range of output values.
        /// </summary>
        private DoubleRange range;

        /// <summary>
        /// Constructor for this class.
        /// </summary>
        /// <param name="data">Input data.</param>
        public NeuralNetworkRegression(Data data, bool positive) : base(data)
        {
            number = numberGlobal;

            numberGlobal++;

            features = new List<string>();

            this.positive = positive;

            if (positive)
            {
                range = new DoubleRange(-11, 0.0);

                features.AddRange(features1);
                features.AddRange(features2);
                features.AddRange(features3);
                features.AddRange(features4);
            }
            else
            {
                range = new DoubleRange(0.0, 31);

                features.AddRange(features2);
                features.AddRange(features3);
            }
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public override int[] ClassificationOutputs
        {
            get
            {
                throw new NotSupportedException("This isn't agent for classification");
            }

            protected set
            {
            }
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public override double[] RegressionOutputs { get; protected set; }

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

            for (int i = 0; i < data.data.Count; i++)
            {
                double result = 0.0;

                if (positive && data.data[i]["class"] == 1.0)
                {
                    result = network.Compute(inputs[i])[0];
                    result = Vector.Scale(result, new DoubleRange(0, 1), range);
                }

                if (!positive && data.data[i]["class"] == 0.0)
                {
                    result = network.Compute(inputs[i])[0];
                    result = Vector.Scale(result, new DoubleRange(0, 1), range);
                }

                RegressionOutputs[i] = result;
            }
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public override void Train()
        {
            var inputsOriginal = data.GetSelectedInput(features);
            var outputsOriginal = data.GetExpectedRegressionOutput();

            var tempInputs = new List<double[]>();
            var tempOutputs = new List<double>();

            for (int i = 0; i < inputsOriginal.Length; i++)
            {
                if (positive && outputsOriginal[i] < 0.0)
                {
                    tempInputs.Add(inputsOriginal[i]);
                    tempOutputs.Add(outputsOriginal[i]);
                }

                if (!positive && outputsOriginal[i] > 0.0)
                {
                    tempInputs.Add(inputsOriginal[i]);
                    tempOutputs.Add(outputsOriginal[i]);
                }
            }

            var inputs = tempInputs.ToArray();
            var outputs = tempOutputs.ToArray();

            var function = new SigmoidFunction();

            network = new ActivationNetwork(function, inputs[0].Length, 5, 1);

            var teacher = new ResilientBackpropagationLearning(network);

            var initialization = new NguyenWidrow(network);

            initialization.Randomize();

            var scaledOutputs = Vector.Scale(outputs, range, new DoubleRange(0.0, 1.0));

            var outputsNetwork = new double[outputs.Length][];

            for (int i = 0; i < outputs.Length; i++)
                outputsNetwork[i] = new double[1] { scaledOutputs[i] };

            double error = Double.PositiveInfinity;

            double maxError = outputs.Length / 5e2;

            int epoch = 0;

            while (error > maxError && epoch < 5000)
            {
                error = teacher.RunEpoch(inputs, outputsNetwork);
            }

            Save();
        }

        public override string ToString()
        {
            return this.GetType().Name + number;
        }
    }
}
