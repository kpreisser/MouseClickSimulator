using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TTMouseclickSimulator.Core;
using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.ToontownRewritten.Actions;
using TTMouseclickSimulator.Core.ToontownRewritten.Actions.DoodleInteraction;
using TTMouseclickSimulator.Core.ToontownRewritten.Actions.Fishing;
using TTMouseclickSimulator.Core.ToontownRewritten.Actions.Keyboard;
using TTMouseclickSimulator.Core.ToontownRewritten.Actions.Speedchat;

namespace TTMouseclickSimulator.Project
{
    /// <summary>
    /// Deserializes a SimulatorProject from an XML file.
    /// </summary>
    public class XmlProjectDeserializer
    {

        /// <summary>
        /// The namespace to be used for the XML elements.
        /// </summary>
        public const string XmlNamespace = "https://github.com/TTExtensions/MouseClickSimulator";
        private static readonly XNamespace ns = XmlNamespace;

        /// <summary>
        /// A dictionary for getting an action constructor by a string name.
        /// Note: This Deserializer assumes that every action only has one constructor!
        /// </summary>
        private static readonly IDictionary<string, Type> actionTypes;
        
        static XmlProjectDeserializer()
        {
            actionTypes = new SortedDictionary<string, Type>();
            actionTypes.Add("Compound", typeof(CompoundAction));
            actionTypes.Add("Loop", typeof(LoopAction));

            actionTypes.Add("DoodlePanel", typeof(DoodlePanelAction));

            actionTypes.Add("AutomaticFishing", typeof(AutomaticFishingAction));
            actionTypes.Add("StraightFishing", typeof(StraightFishingAction));
            actionTypes.Add("SellFish", typeof(SellFishAction));
            actionTypes.Add("QuitFishing", typeof(QuitFishingAction));

            actionTypes.Add("PressKey", typeof(PressKeyAction));
            actionTypes.Add("WriteText", typeof(WriteTextAction));

            actionTypes.Add("Speedchat", typeof(SpeedchatAction));

            actionTypes.Add("Pause", typeof(PauseAction));
        }


        public SimulatorProject Deserialize(Stream s) => ParseDocument(XDocument.Load(s));

        private SimulatorProject ParseDocument(XDocument doc)
        {
            string title, description;
            var root = doc.Root;
            if (root.Name != ns + "SimulatorProject")
                throw new InvalidDataException("Root element <SimulatorProject> " 
                    + $"in namespace \"{XmlNamespace}\" not found.");

            // Find the <Title>
            title = root.Element(ns + "Title")?.Value.Trim() ?? string.Empty;

            // Find the <Desription>
            description = root.Element(ns + "Description")?.Value.Trim() ?? string.Empty;

            // Parse the configuration elements directly from the root node.
            var config = ParseConfiguration(root);

            return new SimulatorProject()
            {
                Title = title,
                Description = description,
                Configuration = config
            };
        }

        private SimulatorConfiguration ParseConfiguration(XElement configEl)
        {
            // Find the <MainAction>
            var mainActionEl = configEl.Element(ns + "MainAction");
            if (mainActionEl == null)
                throw new InvalidDataException("The <MainAction> element is missing.");

            IList<IAction> actionList = ParseActionList(mainActionEl);
            if (actionList.Count != 1)
            {
                throw new InvalidDataException("<MainAction> must contain exactly one Action element.");
            }

            return new SimulatorConfiguration()
            {
                MainAction = actionList[0]
            };
        }

        private IList<IAction> ParseActionList(XElement parent)
        {
            List<IAction> actionList = new List<IAction>();
            foreach (var child in parent.Elements())
            {
                if (child.Name.Namespace == ns)
                {
                    // Look if we find the action type.
                    Type t;
                    if (!actionTypes.TryGetValue(child.Name.LocalName, out t))
                        throw new InvalidDataException($"{child.Name} could not be recognized " 
                            + "as an Action type.");

                    // Get the constructor.
                    var constructors = t.GetConstructors();
                    if (constructors.Length != 1)
                        throw new NotSupportedException($"{t} has {constructors.Length} constructors! "
                            + "This implementation can only handle one constructor.");

                    var constr = constructors[0];
                    var parameters = constr.GetParameters();
                    var parameterValues = new object[parameters.Length];

                    int paramSubactionSingleIdx = -1;
                    int paramSubactionListIdx = -1;

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        // Check if the element specifies the parameter. If not, use the default
                        // value if available.
                        XAttribute attr = child.Attribute(param.Name);
                        if (typeof(IList<IAction>).IsAssignableFrom(param.ParameterType))
                        {
                            paramSubactionListIdx = i;
                        }
                        else if (typeof(IAction).IsAssignableFrom(param.ParameterType))
                        {
                            paramSubactionSingleIdx = i;
                        }
                        else if (attr == null)
                        {
                            if (param.IsOptional && param.HasDefaultValue)
                            {
                                parameterValues[i] = param.DefaultValue;
                            }
                            else
                            {
                                throw new InvalidDataException($"Parameter \"{param.Name}\" is " 
                                    + $"missing for element <{child.Name.LocalName}>.");
                            }
                        }
                        else
                        {
                            string attrval = attr.Value;
                            if (param.ParameterType.IsAssignableFrom(typeof(bool)))
                            {
                                bool b = false;
                                string s = attrval.Trim();
                                if (s == "1" || s.ToLowerInvariant() == "true")
                                    b = true;

                                parameterValues[i] = b;
                            }
                            else if (param.ParameterType.IsAssignableFrom(typeof(int)))
                            {
                                int number = int.Parse(attrval.Trim(), CultureInfo.InvariantCulture);
                                parameterValues[i] = number;
                            }
                            else if (param.ParameterType.IsAssignableFrom(typeof(double)))
                            {
                                double number = double.Parse(attrval.Trim(), 
                                    CultureInfo.InvariantCulture);
                                parameterValues[i] = number;
                            }
                            else if (param.ParameterType.IsAssignableFrom(typeof(int[]))
                                || param.ParameterType.IsAssignableFrom(typeof(byte[])))
                            {
                                string[] valueElements = attrval.Split(new string[] { "," }, 
                                    StringSplitOptions.RemoveEmptyEntries);
                                Array values = Array.CreateInstance(param.ParameterType.GetElementType(),
                                    valueElements.Length);

                                for (int j = 0; j < valueElements.Length; j++)
                                {
                                    object v;
                                    if (param.ParameterType.IsAssignableFrom(typeof(byte[])))
                                        v = byte.Parse(valueElements[j].Trim(), CultureInfo.InvariantCulture);
                                    else
                                        v = int.Parse(valueElements[j].Trim(), CultureInfo.InvariantCulture);
                                    values.SetValue(v, j);
                                }
                                parameterValues[i] = values;
                            }
                            else if (param.ParameterType.IsAssignableFrom(typeof(string)))
                            {
                                parameterValues[i] = attrval;
                            }
                            else if (typeof(Enum).IsAssignableFrom(param.ParameterType))
                            {
                                // Find the enum entry with the specified name.
                                Array values = Enum.GetValues(param.ParameterType);
                                bool found = false;
                                foreach (Enum val in values)
                                {
                                    if (val.ToString() == attrval)
                                    {
                                        found = true;
                                        parameterValues[i] = val;
                                        break;
                                    }
                                }
                                if (!found)
                                {
                                    throw new InvalidDataException($"Could not find enum entry " 
                                        + $"\"{attrval}\" for attribute {attr.Name.LocalName} " 
                                        + $"on element <{child.Name.LocalName}>.");
                                }

                            }
                            else
                            {
                                throw new NotSupportedException("Unsupported type: " 
                                    + $"{param.ParameterType}");
                            }

                        }
                    }

                    // Parse child actions.
                    IList<IAction> childActions = ParseActionList(child);
                    if (paramSubactionListIdx != -1)
                    {
                        parameterValues[paramSubactionListIdx] = childActions;
                    }
                    else if (paramSubactionSingleIdx != -1)
                    {
                        if (childActions.Count != 1)
                        {
                            throw new InvalidDataException($"Element <{child.Name.LocalName}> needs exactly "
                                + $"one child Action element, but found {childActions.Count} elements.");
                        }
                        else
                        {
                            parameterValues[paramSubactionSingleIdx] = childActions[0];
                        }
                    }


                    // Now instanciate the IAction.
                    IAction instance = (IAction)constr.Invoke(parameterValues);
                    actionList.Add(instance);
                }
            }

            return actionList;
        }
    }
}
