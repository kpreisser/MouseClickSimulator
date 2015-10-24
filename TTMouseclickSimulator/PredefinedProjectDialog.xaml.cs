using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TTMouseclickSimulator.Core;
using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.Environment;
using TTMouseclickSimulator.Core.ToontownRewritten.Actions;
using TTMouseclickSimulator.Core.ToontownRewritten.Actions.Fishing;
using TTMouseclickSimulator.Core.ToontownRewritten.Actions.Keyboard;
using TTMouseclickSimulator.Core.ToontownRewritten.Actions.Speedchat;

namespace TTMouseclickSimulator
{
    /// <summary>
    /// Interaktionslogik für PredefinedProjectDialog.xaml
    /// </summary>
    public partial class PredefinedProjectDialog : Window
    {

        private SimulatorProject selectedProject;
        public SimulatorProject SelectedProject {
            get { return selectedProject; }
        }


        private static readonly List<SimulatorProject> projects;

        static PredefinedProjectDialog()
        {
            projects = new List<SimulatorProject>();
            projects.Add(CreateSampleProject());
            projects.Add(CreatePunchlinePlaceFishingProj());
        }



        public PredefinedProjectDialog()
        {
            InitializeComponent();

            // Create the listbox items.

            for (int i = 0; i < projects.Count; i++)
            {
                var proj = projects[i];
                ListBoxItem item = new ListBoxItem()
                {
                    Content = proj.Name,
                    Tag = proj
                };

                listBox.Items.Add(item);
            }
        }


        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            ListBoxItem selectedListboxItem = (ListBoxItem)listBox.SelectedItem;
            if (selectedListboxItem == null)
            {
                MessageBox.Show(this, "No project is selected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            selectedProject = (SimulatorProject)selectedListboxItem.Tag;
            Close();
        }


        private static SimulatorProject CreateSampleProject()
        {
            SimulatorConfiguration c = new SimulatorConfiguration();
            // Create the action list for the main compound action.
            List<IAction> mainActions = new List<IAction>()
            {
                new PressKeyAction(AbstractWindowsEnvironment.VirtualKeyShort.LEFT, 500),
                new PressKeyAction(AbstractWindowsEnvironment.VirtualKeyShort.RIGHT, 700),
                new PressKeyAction(AbstractWindowsEnvironment.VirtualKeyShort.CONTROL, 500),
                new LoopAction(new CompoundAction(new List<IAction>()
                {
                    new PressKeyAction(AbstractWindowsEnvironment.VirtualKeyShort.UP, 300),
                    new PressKeyAction(AbstractWindowsEnvironment.VirtualKeyShort.DOWN, 300)
                }, CompoundAction.CompoundActionType.Sequential, 50, 50, false), 3),
                new SpeedchatAction(3, 0, 2),
                new WriteTextAction("Chacun est l'artisan de sa fortune.", 60),


            };
            // Create the main compound action.
            c.Action = new CompoundAction(mainActions,
                CompoundAction.CompoundActionType.RandomOrder, 100, 200);

            SimulatorProject proj = new SimulatorProject()
            {
                Name = "Sample Actions",
                Description = "This project contains some keyboard actions, a SpeedChat and a WriteText action.",
                Configuration = c
            };
            return proj;
        }


        private static SimulatorProject CreatePunchlinePlaceFishingProj()
        {
            SimulatorConfiguration c = new SimulatorConfiguration();
            // Create the action list for the main compound action.
            IAction mainAction = new CompoundAction(new List<IAction>()
            {
                new CompoundAction(new List<IAction>() {
                    new LoopAction(new AutomaticFishingAction(FishingSpotFlavor.PunchlinePlace), 19),
                    new StraightFishingAction(),
                    new QuitFishingAction()
                }, CompoundAction.CompoundActionType.Sequential, 200, 200, false),

                new PauseAction(2500),

                new CompoundAction(new List<IAction>() {
                    new PressKeyAction(AbstractWindowsEnvironment.VirtualKeyShort.LEFT, 220),
                    new PressKeyAction(AbstractWindowsEnvironment.VirtualKeyShort.DOWN, 3500),
                    new PauseAction(1300),
                    new SellFishAction(),
                    new PauseAction(1300),
                    new PressKeyAction(AbstractWindowsEnvironment.VirtualKeyShort.UP, 2200),
                    new PauseAction(2000),
                }, CompoundAction.CompoundActionType.Sequential, 400, 700, false)

            }, CompoundAction.CompoundActionType.Sequential, 0, 0);
            c.Action = mainAction;

            SimulatorProject proj = new SimulatorProject()
            {
                Name = "Automatic Fishing in Punchline Place",
                Description = "The Toon will automatically fish in Punchline Place, Toontown Central.\r\n\r\n"
                + "Before you click on start, make sure that\r\n"
                + "• you are in a quiet district,\r\n"
                + "• your fish bucket is empty,\r\n"
                + "• you have enough JellyBeans for 20 casts,\r\n"
                + "• your toon is standing on the front fishing board.",
                Configuration = c
            };
            return proj;
        }
    }
}
