/* 
 * Faculty of Information Technology of Brno University of Technology 
 * Master's thesis - Predictor of the Effect of Amino Acid Substitutions on Protein Stability
 * Author: Michal Flax
 * Year: 2017
 */

using System;
using System.Collections.Generic;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;

namespace DP_Flax
{
    /// <summary>
    /// This class implements support vector machines method.
    /// </summary>
    class SVM : Agent
    {
        /// <summary>
        /// Object contains support vector machines.
        /// </summary>
        private SupportVectorMachine<Gaussian, double[]> svm;
        
        /// <summary>
        /// Vector contains names of selected features.
        /// </summary>
        private List<string> features = new List<string>() { "asa", "struc", "information_content", "3D_atom_numbers", "3D_freq", "windowAA10" };
        
        /// <summary>
        /// Constructor for this class.
        /// </summary>
        /// <param name="data">Input data.</param>
        public SVM(Data data) : base(data) { }

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
                throw new NotSupportedException("This isn't agent for regression.");
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
            Save<SupportVectorMachine<Gaussian, double[]>>(svm, this.ToString());
        }

        protected override void Load()
        {
            svm = Load<SupportVectorMachine<Gaussian, double[]>>(this.ToString());
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public override void Run()
        {
            var inputs = data.GetSelectedInput(features);

            var result = svm.Decide(inputs);

            for (int i = 0; i < result.Length; i++)
            {
                ClassificationOutputs[i] = Convert.ToInt32(result[i]);
            }
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public override void Train()
        {
            var inputs = data.GetSelectedInput(features);
            var outputs = data.GetExpectedClassificationOutput();

            var teacher = new LeastSquaresLearning<Gaussian, double[]>()
            {
                Kernel = new Gaussian(),
                UseComplexityHeuristic = true,
                WeightRatio = 2.0,
                UseKernelEstimation = true,
            };

            svm = teacher.Learn(inputs, outputs);

            Save();
        }

        public override string ToString()
        {
            return this.GetType().Name;
        }

    }
}
