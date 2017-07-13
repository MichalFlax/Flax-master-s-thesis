/* 
 * Faculty of Information Technology of Brno University of Technology 
 * Master's thesis - Predictor of the Effect of Amino Acid Substitutions on Protein Stability
 * Author: Michal Flax
 * Year: 2017
 */

using System;
using System.Collections.Generic;
using Accord.Statistics.Analysis;
using Accord.Math.Optimization.Losses;
using System.Windows;

namespace DP_Flax
{
    /// <summary>
    /// Entry class of this application.
    /// </summary>
    class Program
    {
        //Select mode for predictor
        private const bool train = false;
        private const bool test = false;
        private const bool release = true;

        //Lists of agents
        private static List<Agent> classificationAgents; 
        private static List<Agent> regressionAgents;

        //Results of prediction.
        internal static int[] resultClassification;
        internal static double[] resultRegression;

        //Input data
        internal static Data data;

        internal static string dataPath;

        //Enable or disable computing information content
        internal static bool enableBLAST = true;

        //Enable or disable console mode.
        private static bool consoleMode = false;

        /// <summary>
        /// Entry point of this application.
        /// </summary>
        /// <param name="args">Program input parameters.</param>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length != 0 && args.Length != 2 && args.Length != 3)
            {
                Console.Error.WriteLine("Wrong arguments");

                return;
            }

            //Console mode
            if (args.Length >= 2 && args[0] == "-c")
            {
                dataPath = args[1];

                consoleMode = true;
                
                Run();
            }
            //Window mode
            else
            {
                Application app = new Application();

                app.Run(new MyWindow());
            }
        }

        /// <summary>
        /// Run prediction process.
        /// </summary>
        public static void Run()
        {                
            data = new Data(dataPath, enableBLAST); 
            
            data.DownloadProteinData();
            
            data.RunPrepareDataScript();
            
            data.ReadData();

            data.PrepareData();

            classificationAgents = new List<Agent>();
            regressionAgents = new List<Agent>();
            
            NeuralNetwork.numberGlobal = 0;
            NeuralNetworkRegression.numberGlobal = 0;

            classificationAgents.Add(new SVM(data));
            classificationAgents.Add(new RandomForest(data));
            classificationAgents.Add(new NeuralNetwork(data));
            classificationAgents.Add(new NeuralNetwork(data));
            classificationAgents.Add(new NeuralNetwork(data));

            regressionAgents.Add(new NeuralNetworkRegression(data, false));
            regressionAgents.Add(new NeuralNetworkRegression(data, false));

            regressionAgents.Add(new NeuralNetworkRegression(data, true));
            regressionAgents.Add(new NeuralNetworkRegression(data, true));

            //Initializing agents
            foreach (var agent in classificationAgents)
                agent.Init(train);

            foreach (var agent in regressionAgents)
                agent.Init(train);

            //Training mode
            if (train)
            {
                foreach (var agent in classificationAgents)
                    agent.Train();

                foreach (var agent in regressionAgents)
                    agent.Train();
            }

            //Testing mode
            if (test)
            {
                foreach (var agent in classificationAgents)
                    agent.Run();

                ComputeResultClassification();

                foreach (var agent in regressionAgents)
                    agent.Run();

                ComputeResultRegression();

                PrintTestResult();

            }

            //Running mode
            if (release)
            {
                foreach (var agent in classificationAgents)
                    agent.Run();

                ComputeResultClassification();

                foreach (var agent in regressionAgents)
                    agent.Run();

                ComputeResultRegression();

                //console
                if (consoleMode)
                {
                    PrintResult();
                }
            }
        }

        /// <summary>
        /// Compute majority consensus from classification outputs for each mutation.
        /// </summary>
        private static void ComputeResultClassification()
        {
            resultClassification = new int[data.data.Count];
            
            for (int i = 0; i < data.data.Count; i++)
            {
                int count = 0;

                foreach (var agent in classificationAgents)
                    count += agent.ClassificationOutputs[i];

                if (count > 2)
                {
                    resultClassification[i] = 1;
                }
                else
                {
                    resultClassification[i] = 0;
                }

                data.data[i].Add("class", resultClassification[i]);
            }
        }

        /// <summary>
        /// Compute average value from regression outputs for each mutation.
        /// </summary>
        private static void ComputeResultRegression()
        {
            resultRegression = new double[data.data.Count];
            
            for (int i = 0; i < data.data.Count; i++)
            {
                double ddg = 0.0;

                foreach (var agent in regressionAgents)
                    ddg += agent.RegressionOutputs[i];

                resultRegression[i] = ddg / 2.0;
            }
        }

        /// <summary>
        /// Print test results to console.
        /// </summary>
        private static void PrintTestResult()
        {
            var originalOutputs = data.GetExpectedClassificationOutput();
            var originalOuputsRegression = data.GetExpectedRegressionOutput();

            var matrix = new ConfusionMatrix(originalOutputs, resultClassification);

            Console.WriteLine("Test results:");

            Console.WriteLine("True positive\t" + matrix.TruePositives);
            Console.WriteLine("True negative\t" + matrix.TrueNegatives);
            Console.WriteLine("False positive\t" + matrix.FalsePositives);
            Console.WriteLine("False negative\t" + matrix.FalseNegatives);

            Console.WriteLine();

            Console.WriteLine("Sensitivity\t" + matrix.Sensitivity);
            Console.WriteLine("Specificity\t" + matrix.Specificity);
            Console.WriteLine("F-Score\t\t" + matrix.FScore);
            Console.WriteLine("MCC\t\t" + matrix.MatthewsCorrelationCoefficient);
            Console.WriteLine("Precision\t" + matrix.Precision);
            Console.WriteLine("Accuracy\t" + matrix.Accuracy);

            Console.WriteLine();

            var mse = new SquareLoss(originalOuputsRegression).Loss(resultRegression);

            Console.WriteLine("MSE: " + mse);
        }

        /// <summary>
        /// Print prediction results in CSV format to console.
        /// </summary>
        private static void PrintResult()
        {
            Console.WriteLine("protein,chain,mutation,stabilization,ddg");

            for (int i = 0; i < data.data.Count; i++)
            {
                Console.WriteLine(data.dataOriginal[i]["protein"] + "," + data.dataOriginal[i]["chain"] + "," + data.dataOriginal[i]["mutation"] + "," +
                    resultClassification[i] + "," + resultRegression[i]);
            }
        }
    }
}
