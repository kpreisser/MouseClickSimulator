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
using TTMouseclickSimulator.Project;

namespace TTMouseclickSimulator
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private SimulatorProject project;
        private Simulator simulator;
        // Callbacks that we need to call when we start or stop the simulator.
        private Action simulatorStartAction, simulatorStopAction;
        /// <summary>
        /// If true, the window should be closed after the simulator stopped.s
        /// </summary>
        private bool closeWindowAfterStop;

        /// <summary>
        /// The file extension for Simulator Project files. Currently we use ".xml".
        /// </summary>
        private const string ProjectFileExtension = ".xml";

        private readonly Microsoft.Win32.OpenFileDialog openFileDialog;

        public MainWindow()
        {
            InitializeComponent();

            openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.DefaultExt = ProjectFileExtension;
            openFileDialog.Filter = "XML Simulator Project|*" + ProjectFileExtension;

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
            btnLoad.IsEnabled = false;


            // Run the simulator in another task so it is not executed in the GUI thread.
            // However, we then await that new task so we are notified when it is finished.
            Simulator sim = simulator = new Simulator(project.Configuration, TTRWindowsEnvironment.Instance);

            Exception runException = null;
            if (simulatorStartAction != null)
                simulatorStartAction();
            await Task.Run(async () =>
            {
                try
                {
                    await sim.RunAsync();
                }
                catch (Exception ex)
                {
                    runException = ex;   
                }
            });
            if (simulatorStopAction != null)
                simulatorStopAction();

            // Don't show a messagebox if we need to close the window.
            if (!closeWindowAfterStop && runException != null && !(runException is SimulatorCanceledException))
                MessageBox.Show(this, runException.Message, "Simulator stopped!", MessageBoxButton.OK, MessageBoxImage.Warning);

            HandleSimulatorCanceled();
        }

        private void HandleSimulatorCanceled()
        {
            simulator = null;
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            btnLoad.IsEnabled = true;

            if (closeWindowAfterStop)
                Close();
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
                        XmlProjectDeserializer deser = new XmlProjectDeserializer();
                        project = deser.Deserialize(fs);
                    }
                   
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Could not load the given project.\r\n\r\n"
                       + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                RefreshProjectControls();
            }
        }

        private void RefreshProjectControls()
        {
            if (project == null)
            {
                lblCurrentProject.Content = "(none)";
                txtDescription.Text = string.Empty;
                btnStart.IsEnabled = false;
            }
            else 
            {
                lblCurrentProject.Content = project.Title;
                txtDescription.Text = project.Description;
                btnStart.IsEnabled = true;

                // Create labels for each action.
                actionListGrid.Children.Clear();
                IAction mainAct = project.Configuration.MainAction;
                int posCounter = 0;
                CreateActionLabels(mainAct, actionListGrid, 0, ref posCounter, 
                    out simulatorStartAction, out simulatorStopAction);
                
            }
        }

        private void CreateActionLabels(IAction action, Grid grid, int recursiveCount, 
            ref int posCounter, out Action handleStart, out Action handleStop)
        {
            Label l = new Label();
            l.Margin = new Thickness(recursiveCount * 10, 18 * posCounter, 0, 0);
            grid.Children.Add(l);

            string str = action.ToString();
            l.Content = str;

            handleStart = () => {
                l.Foreground = new SolidColorBrush(Color.FromArgb(255, 29, 134, 184));
            };

            handleStop = () =>
            {
                l.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                l.Content = str;
            };

            action.ActionInformationUpdated += s => Dispatcher.Invoke(new Action(() => l.Content = str + " – " + s));

            posCounter++;

            if (action is IActionContainer)
            {
                
                IActionContainer cont = (IActionContainer)action;
                IList<IAction> subActions = cont.SubActions;
                Action[] handleStartActions = new Action[subActions.Count];
                Action[] handleStopActions = new Action[subActions.Count];
                for (int i = 0; i < subActions.Count; i++)
                {
                    CreateActionLabels(subActions[i], grid, recursiveCount + 1, ref posCounter,
                        out handleStartActions[i], out handleStopActions[i]);
                }

                int? currentActiveAction = null;
                cont.SubActionStartedOrStopped += (idx) => Dispatcher.Invoke(new Action(() =>
                {
                    if (idx.HasValue)
                    {
                        currentActiveAction = idx;
                        handleStartActions[idx.Value]();
                    }
                    else if (currentActiveAction.HasValue)
                    {
                        handleStopActions[currentActiveAction.Value]();
                    }
                }));
            }
        }

        

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // If the simulator is currently running, don't close the window but stop the
            // simulator and wait until it is finished.
            if (simulator != null)
            {
                e.Cancel = true;
                Hide();

                closeWindowAfterStop = true;
                simulator.Cancel();
            }
        }
    }
}
