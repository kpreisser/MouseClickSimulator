using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using TTMouseclickSimulator.Core;
using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;
using TTMouseclickSimulator.Core.ToontownRewritten.Actions;
using TTMouseclickSimulator.Core.ToontownRewritten.Actions.Fishing;
using TTMouseclickSimulator.Core.ToontownRewritten.Actions.Keyboard;
using TTMouseclickSimulator.Core.ToontownRewritten.Environment;

namespace TTMouseclickSimulator
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Simulator sim;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            StartSimulator();
        }

        private async void StartSimulator()
        {
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;

            SimulatorConfiguration c = new SimulatorConfiguration();
            c.MinimumWaitInterval = 1000;
            c.MaximumWaitInterval = 1000;
            c.RunInOrder = false;
            c.Actions = new List<IAction>()
            {
                new PressKeyAction(AbstractEnvironmentInterface.VirtualKeyShort.LEFT, 500),
                new PressKeyAction(AbstractEnvironmentInterface.VirtualKeyShort.RIGHT, 500),
                new PressKeyAction(AbstractEnvironmentInterface.VirtualKeyShort.CONTROL, 1500)
            };

            sim = new Simulator(c, new TTRWindowsEnvironment());
            // Add some events to the simulator.
            sim.ActionStarted += (act, idx) => lblCurrentAction.Content = act.GetType().Name + $" (Idx {idx})"; 
            try
            {
                await sim.RunAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Simulator stopped!", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            HandleSimulatorCanceled();
        }

        private void HandleSimulatorCanceled()
        {
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;

            lblCurrentAction.Content = "...";
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            sim.Cancel();
            btnStop.IsEnabled = false;
        }
    }
}
