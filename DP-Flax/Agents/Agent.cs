/* 
 * Faculty of Information Technology of Brno University of Technology 
 * Master's thesis - Predictor of the Effect of Amino Acid Substitutions on Protein Stability
 * Author: Michal Flax
 * Year: 2017
 */

using System;
using System.IO;

namespace DP_Flax
{
    /// <summary>
    /// Base class for all agents classes.
    /// </summary>
    [Serializable]
    abstract class Agent
    {
        /// <summary>
        /// Object for storing input data.
        /// </summary>
        public Data data { get; protected set; }

        /// <summary>
        /// This property contains classification outputs.
        /// </summary>
        public abstract int[] ClassificationOutputs { get; protected set; }

        /// <summary>
        /// This property contains regression outputs.
        /// </summary>
        public abstract double[] RegressionOutputs { get; protected set; }
        
        /// <summary>
        /// Constructor for this class.
        /// </summary>
        /// <param name="data">Input data.</param>
        protected Agent(Data data)
        {
            this.data = data;
            
            ClassificationOutputs = new int[this.data.data.Count];

            RegressionOutputs = new double[this.data.data.Count];
        }
        
        /// <summary>
        /// This method serialize and save chosen object.
        /// </summary>
        /// <typeparam name="T">Type of saved object</typeparam>
        /// <param name="model">Saved object</param>
        /// <param name="name">Name of created file.</param>
        protected void Save<T>(T model, string name)
        {
            string savePath = Path.Combine(System.Windows.Forms.Application.StartupPath, "Agents/");

            Directory.CreateDirectory(savePath);
    
            Accord.IO.Serializer.Save<T>(model, savePath + name);
        }
        
        public abstract void Save();

        /// <summary>
        /// This method load and deserialize chosen object from a saved file.
        /// </summary>
        /// <typeparam name="T">Type of loaded object.</typeparam>
        /// <param name="name">Name of loaded file.</param>
        /// <returns></returns>
        protected T Load<T>(string name)
        {
            string savePath = Path.Combine(System.Windows.Forms.Application.StartupPath, "Agents/");

            return Accord.IO.Serializer.Load<T>(savePath + name);
        }
        
        protected abstract void Load();

        /// <summary>
        /// Learning of implemented model.
        /// </summary>
        public abstract void Train();
        
        /// <summary>
        /// Compute output values.
        /// </summary>
        public abstract void Run();

        /// <summary>
        /// Initialize implemented model.
        /// </summary>
        /// <param name="train">Select training or running mode.</param>
        public abstract void Init(Boolean train = false);
    }
}
