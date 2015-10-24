using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;
using TTMouseclickSimulator.Core;
using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;
using TTMouseclickSimulator.Core.ToontownRewritten.Actions;
using TTMouseclickSimulator.Core.ToontownRewritten.Actions.DoodleInteraction;
using TTMouseclickSimulator.Core.ToontownRewritten.Actions.Fishing;
using TTMouseclickSimulator.Core.ToontownRewritten.Actions.Keyboard;
using TTMouseclickSimulator.Core.ToontownRewritten.Actions.Speedchat;
using TTMouseclickSimulator.Core.ToontownRewritten.Environment;

namespace TTMouseclickSimulator
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private SimulatorProject project;
        private Simulator simulator;

        private const string ProjectFileExtension = ".mcsimproject";

        private readonly Microsoft.Win32.OpenFileDialog openFileDialog;

        public MainWindow()
        {
            InitializeComponent();

            openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.DefaultExt = ProjectFileExtension;
            openFileDialog.Filter = "Mouse Click Simulator Project|*" + ProjectFileExtension;


            RefreshProjectControls();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            StartSimulator();
        }

        private async void StartSimulator()
        {
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;
            btnLoad.IsEnabled = btnSave.IsEnabled = false;

            
            // TODO: If the window is closed, stop the simulator and wait for the task it!

            // Run the simulator in another task so it is not executed in the GUI thread.
            // However, we then await that new task so we are notified when it is finished.
            Exception runException = null;
            await Task.Run(async () =>
            {
                simulator = new Simulator(project.Configuration, TTRWindowsEnvironment.Instance);
                // Add some events to the simulator.
                //sim.ActionStarted += (act, idx) => Dispatcher.Invoke(() => lblCurrentAction.Content = act.GetType().Name + $" (Idx {idx})");

                try
                {
                    await simulator.RunAsync();
                }
                catch (Exception ex)
                {
                    runException = ex;   
                }
            });

            
            if (runException != null && !(runException is SimulatorCanceledException))
                MessageBox.Show(this, runException.Message, "Simulator stopped!", MessageBoxButton.OK, MessageBoxImage.Warning);

            HandleSimulatorCanceled();
        }

        private void HandleSimulatorCanceled()
        {
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            btnLoad.IsEnabled = btnSave.IsEnabled = true;
        }



        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            simulator.Cancel();
            btnStop.IsEnabled = false;
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (openFileDialog.ShowDialog(this) == true)
            {
                // Try to load the given project.
                try
                {
                    using (FileStream fs = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        project = (SimulatorProject)bf.Deserialize(fs);
                    }
                   
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Could not load the given project.\r\n\r\n"+  ex.GetType().ToString() 
                        + ": " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                RefreshProjectControls();
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RefreshProjectControls()
        {
            if (project == null)
            {
                lblCurrentProject.Content = "(none)";
                btnSave.IsEnabled = false;
                btnStart.IsEnabled = false;
            }
            else 
            {
                btnSave.IsEnabled = true;
                lblCurrentProject.Content = project.Name;
                txtDescription.Text = project.Description;
                btnStart.IsEnabled = true;
            }
        }



        

        private void btnLoadPredefined_Click(object sender, RoutedEventArgs e)
        {
            PredefinedProjectDialog dialog = new PredefinedProjectDialog();
            dialog.Owner = this;
            if (dialog.ShowDialog() == true && dialog.SelectedProject != null)
            {
                // Load the project.
                project = dialog.SelectedProject;
                RefreshProjectControls();
            }
        }
    }
}
