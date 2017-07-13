/* 
 * Faculty of Information Technology of Brno University of Technology 
 * Master's thesis - Predictor of the Effect of Amino Acid Substitutions on Protein Stability
 * Author: Michal Flax
 * Year: 2017
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Diagnostics;
using Accord.Math;
using Accord;
using System.Globalization;

namespace DP_Flax
{
    /// <summary>
    /// Class for storing input data
    /// </summary>
    /// 
    /// <remarks>
    /// Class storing input data and contains method for reading data, downloading PDB files from Protein Data Bank Database and other methods. 
    /// </remarks>
    public class Data
    {
        //Paths to data files and other stuff
        private string pathPdbData;
        private string pathData;
        private string pathPythonScript;
        private string pathPreparedData;

        private bool infoContentEnable;

        /// <summary>
        /// List for storing original data 
        /// </summary>
        public List<Dictionary<string, string>> dataOriginal { get; set; }

        /// <summary>
        /// List for storing prepared data
        /// </summary>
        public List<Dictionary<string, double>> data { get; private set; }

        /// <summary>
        /// Aminoacid codes
        /// </summary>
        public enum AACode
        {
            A = 1, R, N, D, C, E, Q, G, H, I, L, K, F, M, P, S, T, W, Y, V    
        }

        /// <summary>
        /// Secondary structure codes
        /// </summary>
        enum DSSPCode
        {
            H = 1, B, E, G, I, T, S
        }

        /// <summary>
        /// Constructor for this class.
        /// </summary>
        /// <param name="datafile">Path to the input file</param>
        public Data(string datafile, bool infoContentEnable = true)
        {
            dataOriginal = new List<Dictionary<string, string>>();
            data = new List<Dictionary<string, double>>();

            pathData = datafile;

            this.infoContentEnable = infoContentEnable;

            pathPdbData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DP-Flax-predictor/PDBData/");
            pathPythonScript = Path.Combine(System.Windows.Forms.Application.StartupPath, "Script/prepareData.py");
            pathPreparedData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DP-Flax-predictor/preparedData.csv");
        }

        /// <summary>
        /// Downloads data from PDB database for proteins in input file.
        /// </summary>
        public void DownloadProteinData()
        {
            List<string> proteinIDs = new List<string>();

            try
            {
                using (var datafile = new StreamReader(pathData))
                {
                    datafile.ReadLine();

                    while (!datafile.EndOfStream)
                    {
                        string pdbID = datafile.ReadLine().Split(',')[0];
                        if (!proteinIDs.Contains(pdbID))
                        {
                            proteinIDs.Add(pdbID);
                        }
                    }
                }

                //if directory for saving data not exist then create this directory
                Directory.CreateDirectory(pathPdbData);

                foreach (var pdbID in proteinIDs)
                {
                    //url for downloading pdb file
                    string pdbURL = "https://files.rcsb.org/download/" + pdbID + ".pdb";

                    //name of downloaded file
                    string pdbFile = pdbID + ".pdb";

                    //downloads data if file for selected protein don't exist
                    using (var client = new WebClient())
                    {
                        string[] pdbFiles = Directory.GetFiles(pathPdbData);

                        if (!(Enumerable.Any(pdbFiles, x => Path.GetFileName(x) == pdbFile)))
                        {
                            client.DownloadFile(pdbURL, pathPdbData + "/" + pdbFile);
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                throw ex;
                //Console.WriteLine(ex.Message);
            }
            catch (WebException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Read data from input file.
        /// </summary>
        public void ReadData()
        {
            try
            {
                using (var dataset = new StreamReader(pathPreparedData))
                {
                    string line = dataset.ReadLine();

                    string[] keys = line.Split(',');

                    while (!dataset.EndOfStream)
                    {
                        dataOriginal.Add(new Dictionary<string, string>());

                        line = dataset.ReadLine();

                        string[] values = line.Split(',');

                        for (int i = 0; i < keys.Length; i++)
                        {
                            dataOriginal.Last()[keys[i]] = values[i];
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Start python script for add values to input data data.
        /// </summary>
        public void RunPrepareDataScript()
        {
            ProcessStartInfo scriptSettings = new ProcessStartInfo();

            //set parameters for process executing python script
            scriptSettings.FileName = "python";
            scriptSettings.Arguments = "\"" + pathPythonScript + "\" "
                                     + "\"" + pathData + "\" "
                                     + "\"" + pathPreparedData + "\" "
                                     + "\"" + pathPdbData + "\" "
                                     + "\"" + infoContentEnable + "\" ";
            
            scriptSettings.UseShellExecute = false;
            scriptSettings.RedirectStandardOutput = true;
            scriptSettings.RedirectStandardError = true;

            try
            {
                using (Process script = Process.Start(scriptSettings))
                {
                    using (var scriptOutput = script.StandardOutput)
                    {
                        string msg = scriptOutput.ReadToEnd();
                        if (msg != "")
                        {
                            Console.WriteLine("Python script output:");
                            Console.WriteLine(msg);
                        }
                    }

                    using (var scriptOutput = script.StandardError)
                    {
                        string msg = scriptOutput.ReadToEnd();
                        if (msg != "")
                        {
                            Console.WriteLine("Python script exception:");
                            Console.WriteLine(msg);
                        }
                    }


                    script.WaitForExit();
                }
            }
            catch (SystemException ex)
            {
                Console.WriteLine("Python not found.");
            }
        }

        /// <summary>
        /// Return values of selected features.
        /// </summary>
        /// <param name="keys">Features identificators</param>
        /// <returns>Selected features</returns>
        public double[][] GetSelectedInput(List<string> keys)
        {
            double[][] inputs = new double[data.Count][];

            List<double> record;
            double[] mutation;
            double[] windowAA;
            double[] AAFreq;
  
            if (keys.Count == 0)
            {
                keys = data[0].Keys.ToList();
                keys.RemoveAll(x => x == "realddg" || x.Contains("prevAA") || x.Contains("nextAA") || x == "newAA" || x == "oldAA" || x.Contains("3D_freq"));
                keys.Add("windowAA10");
                keys.Add("3D_freq");
            }
            
            for (int i = 0; i < data.Count; i++)
            {
                record = new List<double>();
                mutation = new double[40];

                mutation[(int)data[i]["oldAA"] - 1] = 1.0;
                mutation[(int)data[i]["newAA"] - 1 + 20] = 1.0;

                record.AddRange(mutation.ToList());
                
                foreach (var key in keys)
                {
                    if (key.Contains("windowAA"))
                    {
                        int windowSize = Int32.Parse(key.Substring(8));

                        windowAA = new double[windowSize * 40];

                        for (int j = 0; j < windowSize; j++)
                        {
                            if (data[i]["prevAA" + (j + 1)] > 0)
                                windowAA[(int)data[i]["prevAA" + (j + 1)] - 1 + (j * 40)] = 1;
                            if (data[i]["nextAA" + (j + 1)] > 0)
                                windowAA[(int)data[i]["nextAA" + (j + 1)] - 1 + ((j * 40) + 20)] = 1;
                        }
                        
                        record.AddRange(windowAA.ToList()); 
                    }

                    else if (key == "3D_freq")
                    {
                        AAFreq = new double[20];
                        var names = Enum.GetNames(typeof(AACode));

                        for (int j = 0; j < names.Length; j++)
                        {
                            AAFreq[j] = data[i]["3D_freq" + "_" + names[j]];
                        }

                        record.AddRange(AAFreq.ToList());
                    }

                    else if (key == "struc")
                    {
                        double[] pop = new double[7];
                        if (data[i][key] > 0)
                            pop[(int)data[i]["struc"] - 1] = 1;
                        record.AddRange(pop.ToList());
                    }

                    else
                    {
                        record.Add(data[i][key]);
                    }
                }
                
                inputs[i] = record.ToArray();
            }

            return inputs;
        }

        /// <summary>
        /// Return expected classification outputs.
        /// </summary>
        /// <returns>Expected output</returns>
        public int[] GetExpectedClassificationOutput()
        {
            int[] outputs = new int[data.Count];

            for (int i = 0; i < data.Count; i++)
            {
                if (data[i]["realddg"] < 0.0)
                {
                    outputs[i] = 1;
                }
                else
                {
                    outputs[i] = 0;
                }
            }

            return outputs;
        }

        /// <summary>
        /// Return expected regression outputs
        /// </summary>
        /// <returns>Expected output</returns>
        public double[] GetExpectedRegressionOutput()
        {
            double[] outputs = new double[data.Count];

            for (int i = 0; i < data.Count; i++)
            {
                outputs[i] = data[i]["realddg"];
            }

            return outputs;
        }

        /// <summary>
        /// Prepare input data - rescaling, coding etc.
        /// </summary>        
        public void PrepareData()
        {
            for (int i = 0; i < dataOriginal.Count; i++)
            {
                data.Add(new Dictionary<string, double>());
            }
            
            foreach (var key in dataOriginal.First().Keys)
            {
                foreach (var record in dataOriginal.Zip(data, (valuesOriginal, values) => new { valuesOriginal, values }))
                {
                    if (key == "protein" || key == "chain") { }

                    else if (key == "mutation")
                    {
                        record.values["oldAA"] = (int)Enum.Parse(typeof(AACode), record.valuesOriginal[key].First().ToString());
                        record.values["newAA"] = (int)Enum.Parse(typeof(AACode), record.valuesOriginal[key].Last().ToString());
                    }

                    else if (key.Contains("prevAA") || key.Contains("nextAA"))
                    {
                        if (record.valuesOriginal[key] != "-")
                        {
                            record.values[key] = (int)Enum.Parse(typeof(AACode), record.valuesOriginal[key]);
                        }
                        else
                        {
                            record.values[key] = 0;
                        }
                    }

                    else if (key == "struc")
                    {
                        if (record.valuesOriginal[key] != "-")
                        {
                            record.values[key] = (int)Enum.Parse(typeof(DSSPCode), record.valuesOriginal[key]);
                        } 
                        else
                        {
                            record.values[key] = 0;
                        }
                    }

                    else
                    {
                        record.values[key] = Double.Parse(record.valuesOriginal[key], CultureInfo.InvariantCulture);
                    }                         
                }
            }

            RescaleData();   
        }

        /// <summary>
        /// Rescaling selected data to interval (0,1)
        /// </summary>
        private void RescaleData()
        {
            Dictionary<string, DoubleRange> intervalsOld = new Dictionary<string, DoubleRange>();
            DoubleRange intervalNew = new DoubleRange(0.0, 1.0);
            
            intervalsOld["molecular_weight"] = new DoubleRange(0.0, 130);
            intervalsOld["K0"] = new DoubleRange(0.0, 13.0);
            intervalsOld["H_t"] = new DoubleRange(0.0, 4.0);
            intervalsOld["H_p"] = new DoubleRange(0.0, 4.5);
            intervalsOld["P"] = new DoubleRange(0.0, 52.0);
            intervalsOld["pH_i"] = new DoubleRange(0.0, 8.0);
            intervalsOld["pK"] = new DoubleRange(0.0, 1.1);
            intervalsOld["B_l"] = new DoubleRange(0.0, 18.5);
            intervalsOld["R_f"] = new DoubleRange(0.0, 16.0);
            intervalsOld["mi"] = new DoubleRange(0.0, 43.0);
            intervalsOld["H_nc"] = new DoubleRange(0.0, 4.0);
            intervalsOld["E_sm"] = new DoubleRange(0.0, 10.5);
            intervalsOld["E_l"] = new DoubleRange(0.0, 4.2);
            intervalsOld["P_beta"] = new DoubleRange(0.0, 1.4);
            intervalsOld["P_t"] = new DoubleRange(0.0, 1.1);
            intervalsOld["P_c"] = new DoubleRange(0.0, 1.1);
            intervalsOld["C_alfa"] = new DoubleRange(0.0, 48);
            intervalsOld["R_a"] = new DoubleRange(0.0, 6.0);
            intervalsOld["N_s"] = new DoubleRange(0.0, 3.0);
            intervalsOld["alfa_n"] = new DoubleRange(0.0, 2.0);
            intervalsOld["alfa_c"] = new DoubleRange(0.0, 2.0);
            intervalsOld["alfa_m"] = new DoubleRange(0.0, 2.3);
            intervalsOld["V0"] = new DoubleRange(0.0, 101.0);
            intervalsOld["N_l"] = new DoubleRange(0.0, 3.0);
            intervalsOld["H_gm"] = new DoubleRange(0.0, 4.1);
            intervalsOld["ASA_D"] = new DoubleRange(0.0, 137.0);
            intervalsOld["ASA_N"] = new DoubleRange(0.0, 90.0);
            intervalsOld["dASA"] = new DoubleRange(0.0, 124.0);
            intervalsOld["dG_h"] = new DoubleRange(0.0, 6.3);
            intervalsOld["G_hD"] = new DoubleRange(0.0, 13.4);
            intervalsOld["G_hN"] = new DoubleRange(0.0, 7.1);
            intervalsOld["dH_h"] = new DoubleRange(0.0, 9.3);
            intervalsOld["TdS_h"] = new DoubleRange(0.0, 4.4);
            intervalsOld["dC_ph"] = new DoubleRange(0.0, 39.3);
            intervalsOld["dG_c"] = new DoubleRange(0.0, 7.0);
            intervalsOld["dH_c"] = new DoubleRange(0.0, 13.2);
            intervalsOld["TdS_c"] = new DoubleRange(0.0, 8.4);
            intervalsOld["dG"] = new DoubleRange(0.0, 2.9);
            intervalsOld["dH"] = new DoubleRange(0.0, 11.3);
            intervalsOld["TdS"] = new DoubleRange(0.0, 8.4);
            intervalsOld["v"] = new DoubleRange(0.0, 10.0);
            intervalsOld["s"] = new DoubleRange(0.0, 5.0);
            intervalsOld["f"] = new DoubleRange(0.0, 5.0);

            intervalsOld["ECI"] = new DoubleRange(0.0, 1.7);
            intervalsOld["ISA"] = new DoubleRange(0.0, 172);
            intervalsOld["hydropathy"] = new DoubleRange(0.0, 2.5);
            intervalsOld["sidechain_hydro"] = new DoubleRange(0.0, 155);
            intervalsOld["information_content"] = new DoubleRange(0.0, 4.5);
            intervalsOld["vnwaals_volume"] = new DoubleRange(0.0, 115);
            
            intervalsOld["3D_atom_numbers"] = new DoubleRange(0.0, 4000);

            foreach (var record in data)
            {
                foreach (var key in intervalsOld.Keys)
                {
                    record[key] = Vector.Scale(record[key], intervalsOld[key], intervalNew);                   
                }
            }
        }
    }
}