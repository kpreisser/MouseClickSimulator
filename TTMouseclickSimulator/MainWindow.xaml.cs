using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using TTMouseclickSimulator.Core;
using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;
using TTMouseclickSimulator.Core.ToontownRewritten.Environment;
using TTMouseclickSimulator.Project;
using TTMouseclickSimulator.Utils;

namespace TTMouseclickSimulator
{
    public partial class MainWindow : Window
    {
        private const string AppName = "TTR Mouse Click Simulator";

        private const string actionTitleMainAction = "Main Action";

        /// <summary>
        /// The file extension for Simulator Project files. Currently we use ".xml".
        /// </summary>
        private const string ProjectFileExtension = ".xml";
        private const string SampleProjectsFolderName = "SampleProjects";

        private SimulatorProject project;
        private SimulatorConfiguration.QuickActionDescriptor currentQuickAction;
        private Button[] quickActionButtons;

        private Simulator simulator;

        // Callbacks that we need to call when we start or stop the simulator.
        private Action simulatorStartAction, simulatorStopAction;

        /// <summary>
        /// If true, the window should be closed after the simulator stopped.s
        /// </summary>
        private bool closeWindowAfterStop;
        
        private readonly Microsoft.Win32.OpenFileDialog openFileDialog;
        
        public MainWindow()
        {
            this.InitializeComponent();

            this.lblAppName.Content = AppName;
            this.Title = AppName;

            this.openFileDialog = new Microsoft.Win32.OpenFileDialog();
            this.openFileDialog.DefaultExt = ProjectFileExtension;
            this.openFileDialog.Filter = "XML Simulator Project|*" + ProjectFileExtension;
            // Set the initial directory to the executable path or the "SampleProjects" folder if it exists.
            string exeDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string sampleProjectsPath = Path.Combine(exeDirectory, SampleProjectsFolderName);

            if (Directory.Exists(sampleProjectsPath))
                this.openFileDialog.InitialDirectory = sampleProjectsPath;
            else
                this.openFileDialog.InitialDirectory = exeDirectory;

            this.RefreshProjectControls();
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            await this.RunSimulatorAsync();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // If the simulator is currently running, don't close the window but stop the
            // simulator and wait until it is finished.
            if (this.simulator != null)
            {
                e.Cancel = true;
                this.Hide();

                this.closeWindowAfterStop = true;
                this.simulator.Cancel();
            }
        }

        private async Task RunSimulatorAsync()
        {
            this.btnStart.IsEnabled = false;
            this.btnStop.IsEnabled = true;
            this.btnLoad.IsEnabled = false;
            this.chkEnableBackgroundMode.IsEnabled = false;

            if (this.quickActionButtons != null)
            {
                foreach (var bt in this.quickActionButtons)
                    bt.IsEnabled = false;
            }

            bool backgroundMode = this.chkEnableBackgroundMode.IsChecked == true;

            // Run the simulator in another task so it is not executed in the GUI thread.
            // However, we then await that new task so we are notified when it is finished.
            this.simulatorStartAction?.Invoke();

            var runException = default(Exception);
            await Task.Run(async () =>
            {
                try
                {
                    var environment = TTRWindowsEnvironment.Instance;

                    var sim = this.simulator = new Simulator(
                        this.currentQuickAction != null ?
                            this.currentQuickAction.Action :
                            this.project.Configuration.MainAction,
                        environment,
                        backgroundMode);

                    sim.AsyncRetryHandler = async ex => !this.closeWindowAfterStop && await this.HandleSimulatorRetryAsync(sim, ex);

                    sim.SimulatorInitializing += multipleWindowsAvailable => this.Dispatcher.Invoke(new Action(() =>
                    {
                        if (multipleWindowsAvailable != null)
                        {
                            // Initialization has started, so show the overlay message.
                            this.overlayMessageBorder.Visibility = Visibility.Visible;

                            this.overlayMessageTextBlock.Text = multipleWindowsAvailable.Value ?
                                    "Multiple Toontown windows detected. Please activate the " +
                                    "window that the Simulator should use." :
                                    "Waiting for the window to be activated...";
                        }
                        else
                        {
                            // Initialization has finished.
                            this.overlayMessageBorder.Visibility = Visibility.Hidden;
                        }
                    }));

                    await sim.RunAsync();
                }
                catch (Exception ex)
                {
                    runException = ex;   
                }
            });

            this.simulatorStopAction?.Invoke();

            // Don't show a messagebox if we need to close the window.
            if (!this.closeWindowAfterStop && runException != null && !(runException is SimulatorCanceledException))
            {
                var dialog = new TaskDialog()
                {
                    Title = AppName,
                    MainInstruction = "Simulator stopped!",
                    Content = runException.Message,
                    ExpandedInformation = GetExceptionDetailsText(runException),
                    MainIcon = TaskDialog.TaskDialogIcon.Stop,
                    CommonButtons = TaskDialog.TaskDialogButtons.OK
                };
                dialog.Flags |= TaskDialog.TaskDialogFlags.ExpandFooterArea;
                dialog.Show(this);
            }

            this.HandleSimulatorCanceled();
        }

        private async Task<bool> HandleSimulatorRetryAsync(Simulator sim, Exception ex)
        {
            // Show a TaskDialog.
            bool result = false;
            await this.Dispatcher.InvokeAsync(new Action(() =>
            {
                if (!this.closeWindowAfterStop)
                {
                    var dialog = new TaskDialog()
                    {
                        Title = AppName,
                        MainInstruction = "Simulator interrupted!",
                        Content = ex.Message,
                        ExpandedInformation = GetExceptionDetailsText(ex),
                        MainIcon = TaskDialog.TaskDialogIcon.Warning,
                        CommonButtons = TaskDialog.TaskDialogButtons.Cancel
                    };
                    dialog.Flags |= TaskDialog.TaskDialogFlags.UseCommandLinks |
                            TaskDialog.TaskDialogFlags.ExpandFooterArea;

                    var buttonTryAgain = dialog.CreateCustomButton("Try again\n"  +
                            "The Simulator will try to run the current action again.");
                    var buttonStop = dialog.CreateCustomButton("Stop the Simulator");

                    dialog.CustomButtons = new TaskDialog.ICustomButton[] { buttonTryAgain, buttonStop };
                    dialog.DefaultCustomButton = buttonStop;

                    dialog.Show(this);

                    if (dialog.ResultCustomButton == buttonTryAgain)
                        result = true;
                }
            }));

            return result;
        }

        private static string GetExceptionDetailsText(Exception ex)
        {
            StringBuilder detailsSb = null;
            var innerEx = ex.InnerException;
            while (innerEx != null)
            {
                if (detailsSb == null)
                    detailsSb = new StringBuilder();
                else
                    detailsSb.Append("\n\n");

                detailsSb.Append(innerEx.Message);

                innerEx = innerEx.InnerException;
            }

            return detailsSb?.ToString();
        }

        private void HandleSimulatorCanceled()
        {
            this.simulator = null;
            this.btnStart.IsEnabled = true;
            this.btnStop.IsEnabled = false;
            this.btnLoad.IsEnabled = true;
            this.chkEnableBackgroundMode.IsEnabled = true;

            if (this.quickActionButtons != null)
            {
                foreach (var bt in this.quickActionButtons)
                    bt.IsEnabled = true;
            }

            if (this.currentQuickAction != null)
            {
                this.currentQuickAction = null;
                this.RefreshProjectControls();
            }

            if (this.closeWindowAfterStop)
                this.Close();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            this.simulator.Cancel();
            this.btnStop.IsEnabled = false;
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (this.openFileDialog.ShowDialog(this) == true)
            {
                // Try to load the given project.
                try
                {
                    using (var fs = new FileStream(
                        this.openFileDialog.FileName,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read))
                    {
                        this.project = XmlProjectDeserializer.Deserialize(fs);
                    }
                }
                catch (Exception ex)
                {
                    var dialog = new TaskDialog()
                    {
                        Title = AppName,
                        MainInstruction = "Could not load the selected project.",
                        Content = ex.Message,
                        ExpandedInformation = GetExceptionDetailsText(ex),
                        MainIcon = TaskDialog.TaskDialogIcon.Stop,
                        MainUpdateIcon = TaskDialog.TaskDialogIcon.Stop,
                        CommonButtons = TaskDialog.TaskDialogButtons.OK
                    };
                    dialog.Flags |=  TaskDialog.TaskDialogFlags.SizeToContent |
                        TaskDialog.TaskDialogFlags.ExpandFooterArea;

                    dialog.Show(this);
                    return;
                }


                if (this.quickActionButtons != null)
                {
                    foreach (var b in this.quickActionButtons)
                        this.gridProjectControls.Children.Remove(b);

                    this.quickActionButtons = null;
                }

                this.RefreshProjectControls();

                // For each quick action, create a button.
                this.quickActionButtons = new Button[this.project.Configuration.QuickActions.Count];
                for (int idx = 0; idx < this.project.Configuration.QuickActions.Count; idx++)
                {
                    int i = idx;
                    var quickAction = this.project.Configuration.QuickActions[i];

                    var b = this.quickActionButtons[i] = new Button();
                    b.Height = 21;
                    b.HorizontalAlignment = HorizontalAlignment.Left;
                    b.VerticalAlignment = VerticalAlignment.Top;
                    b.Margin = new Thickness(0, 2 + 23 * i, 0, 0);
                    b.Content = "  " + quickAction.Name + "  ";
                    this.gridProjectControls.Children.Add(b);
                    Grid.SetRow(b, 1);

                    b.Click += async (_s, _e) =>
                    {
                        this.currentQuickAction = quickAction;
                        this.RefreshProjectControls();

                        await this.RunSimulatorAsync();
                    };
                }
            }
        }

        private void RefreshProjectControls()
        {
            this.lblActionTitle.Content = this.currentQuickAction != null ? this.currentQuickAction.Name 
                : this.project?.Configuration.MainAction != null ? actionTitleMainAction : "";

            if (this.project == null)
            {
                this.lblCurrentProject.Content = "(none)";
                this.txtDescription.Text = string.Empty;
                this.btnStart.IsEnabled = false;
            }
            else 
            {
                this.lblCurrentProject.Content = this.project.Title;
                this.txtDescription.Text = this.project.Description;
                this.btnStart.IsEnabled = this.project.Configuration.MainAction != null;

                // Create labels for each action.
                this.actionListGrid.Children.Clear();
                var mainAct = this.currentQuickAction != null ? this.currentQuickAction.Action 
                    : this.project.Configuration.MainAction;
                if (mainAct != null)
                {
                    int posCounter = 0;
                    this.CreateActionLabels(mainAct, this.actionListGrid, 0, ref posCounter,
                        out this.simulatorStartAction, out this.simulatorStopAction);
                }
            }
        }

        private void HandleChkEnableBackgroundModeChecked(object sender, RoutedEventArgs e)
        {
            this.textBlockStopSimulatorNote.Visibility = Visibility.Collapsed;
        }

        private void HandleChkEnableBackgroundModeUnchecked(object sender, RoutedEventArgs e)
        {
            this.textBlockStopSimulatorNote.Visibility = Visibility.Visible;
        }

        private void CreateActionLabels(IAction action, Grid grid, int recursiveCount, 
            ref int posCounter, out Action handleStart, out Action handleStop)
        {
            var l = new Label();
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

            action.ActionInformationUpdated += s => this.Dispatcher.Invoke(new Action(() => l.Content = str + " – " + s));

            posCounter++;

            if (action is IActionContainer)
            {                
                var cont = (IActionContainer)action;
                var subActions = cont.SubActions;
                var handleStartActions = new Action[subActions.Count];
                var handleStopActions = new Action[subActions.Count];

                for (int i = 0; i < subActions.Count; i++)
                {
                    this.CreateActionLabels(subActions[i], grid, recursiveCount + 1, ref posCounter,
                        out handleStartActions[i], out handleStopActions[i]);
                }

                int? currentActiveAction = null;
                cont.SubActionStartedOrStopped += (idx) => this.Dispatcher.Invoke(new Action(() =>
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
    }
}
