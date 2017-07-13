/* 
 * Faculty of Information Technology of Brno University of Technology 
 * Master's thesis - Predictor of the Effect of Amino Acid Substitutions on Protein Stability
 * Author: Michal Flax
 * Year: 2017
 */
 
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Forms;


namespace DP_Flax
{
    /// <summary>
    /// Interaction logic for MyWindow.xaml
    /// </summary>
    public partial class MyWindow : Window
    {
        private List<MyDataRow> dataRows;

        private readonly BackgroundWorker runBackground = new BackgroundWorker();

        public MyWindow()
        {
            InitializeComponent();

            textBoxFile.MaxLines = 1;
            textBoxFile.TextWrapping = TextWrapping.NoWrap;
            
            dataGrid.CanUserAddRows = false;
            dataGrid.CanUserDeleteRows = false;

            dataRows = new List<MyDataRow>();

            runBackground.DoWork += runBackground_DoWork;
            runBackground.RunWorkerCompleted += runBackground_RunWorkerCompleted;
        }
        
        private void openFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();

            dialog.Filter = "CSV files(*.csv) | *.csv|All files (*.*)|*.*";

            if (dialog.ShowDialog() == true)
            {
                textBoxFile.Text = dialog.FileName;
            }
        }

        private void saveFile_Click(object sender, RoutedEventArgs e)
        {
            if (runBackground.IsBusy == true)
            {
                return;
            }

            var dialog = new FolderBrowserDialog();

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var path = dialog.SelectedPath;

                using (var file = new StreamWriter(System.IO.Path.Combine(path, "DP-Flax-output.csv")))
                {
                    file.WriteLine("protein,chain,mutation,stabilization,ddg");

                    if (Program.data != null)
                    {
                        for (int i = 0; i < Program.data.data.Count; i++)
                        {
                            file.WriteLine(Program.data.dataOriginal[i]["protein"] + "," + Program.data.dataOriginal[i]["chain"] + "," +
                                Program.data.dataOriginal[i]["mutation"] + "," + Program.resultClassification[i] + "," + Program.resultRegression[i]);
                        }
                    }
                }
            }
        }

        private void run_Click(object sender, RoutedEventArgs e)
        {
            Program.dataPath = textBoxFile.Text;

            if (informationContent.IsChecked == true)
            {
                Program.enableBLAST = false;
            }
            else
            {
                Program.enableBLAST = true;
            }

            dataRows.Clear();

            try
            {
                if (runBackground.IsBusy != true)
                {
                    textBlockStatus.Text = "Status: Running prediction";
                    runBackground.RunWorkerAsync();
                }
            }
            catch (IOException ex)
            {
                textBoxFile.Text = ex.Message;

                dataGrid.Items.Refresh();

                return;
            }
        }
        
        private class MyDataRow
        {
            public string protein { get; set; }
            public string mutation { get; set; }
            public string chain { get; set; }
            public string stabilization { get; set; }
            public string ddg { get; set; }
        }

        private void runBackground_DoWork(object sender, DoWorkEventArgs e)
        {
            Program.Run();
        }

        private void runBackground_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                textBoxFile.Text = e.Error.Message;
                textBlockStatus.Text = "Status:";
                return;
            }

            textBlockStatus.Text = "Status: Prediction completed ";

            for (int i = 0; i < Program.data.data.Count; i++)
            {
                var row = new MyDataRow();

                row.protein = Program.data.dataOriginal[i]["protein"];
                row.chain = Program.data.dataOriginal[i]["chain"];
                row.mutation = Program.data.dataOriginal[i]["mutation"];
                row.stabilization = Program.resultClassification[i].ToString();
                row.ddg = Program.resultRegression[i].ToString();

                dataRows.Add(row);
            }

            dataGrid.ItemsSource = dataRows;

            dataGrid.Items.Refresh();
        }
    }

}
