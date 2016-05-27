using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using WinFormsIWin32Window = System.Windows.Forms.IWin32Window;

namespace TTMouseclickSimulator.Utils
{
    /// <summary>
    /// A TaskDialog is the successor of the MessageBox and provides a lot more features.
    /// See https://msdn.microsoft.com/en-us/library/windows/desktop/bb760441%28v=vs.85%29.aspx
    /// 
    /// Note: To use a TaskDialog, the app needs to be compiled with a manifest that enables
    /// the Microsoft.Windows.Common-Controls (6.0.0.0).
    /// </summary>
    public class TaskDialog : IWin32Window, WinFormsIWin32Window
    {
        private const int CustomButtonStartID = 1000;

        /// <summary>
        /// Window handle of the task dialog.
        /// </summary>
        private IntPtr hwndDialog;

        /// <summary>
        /// The delegate for the callback function. We must ensure to prevent this delegate
        /// from being garbage-collected as long as the dialog is active.
        /// </summary>
        private readonly TaskDialogCallbackProcDelegate callbackProcDelegate;

        /// <summary>
        /// The pointer that referenecs the dialog callback delegate. It is declared as a field
        /// to ensure only one function pointer is created for a specific TaskDialog instance.
        /// The pointer will become invalid once the delegate is garbage-collected.
        /// </summary>
        private readonly IntPtr ptrCallbackProcDelegate;

        private IntPtr? currentOwnerHwnd;

        private IDictionary<int, CustomButton> currentCustomButtons;
        private IDictionary<int, RadioButton> currentRadioButtons;

        private TaskDialogResult resultCommonButtonID;
        private CustomButton resultCustomButton;
        private RadioButton resultRadioButton;
        private bool resultVerificationFlagChecked;


        /// <summary>
        /// The window handle of the dialog, or <see cref="IntPtr.Zero"/> if the dialog is not active.
        /// </summary>
        public IntPtr Handle => hwndDialog;

        public string Title { get; set; }

        public string MainInstruction { get; set; }

        public string Content { get; set; }

        public string Footer { get; set; }

        public string VerificationText { get; set; }

        public string ExpandedInformation { get; set; }

        public string ExpandedControlText { get; set; }

        public string CollapsedControlText { get; set; }

        /// <summary>
        /// Flags for this TaskDialog instance. By default,
        /// <see cref="TaskDialogFlags.PositionRelativeToWindow"/> is set.
        /// </summary>
        public TaskDialogFlags Flags { get; set; }

        public TaskDialogIcon MainIcon { get; set; }

        /// <summary>
        /// If specified, after the TaskDialog is opened or navigated, its main icon will be updated
        /// to the specified one.
        /// </summary>
        public TaskDialogIcon MainUpdateIcon { get; set; }

        public TaskDialogIcon FooterIcon { get; set; }

        public TaskDialogButtons CommonButtons { get; set; }

        /// <summary>
        /// An array of custom buttons that have been created with
        /// <see cref="CreateCustomButton(string)"/>.
        /// </summary>
        public ICustomButton[] CustomButtons { get; set; }

        /// <summary>
        /// An array of radio buttons that have been created with
        /// <see cref="CreateRadioButton(string)"/>.
        /// </summary>
        public IRadioButton[] RadioButtons { get; set; }

        /// <summary>
        /// The default custom button. If null, the <see cref="DefaultCommonButton"/> will be used.
        /// </summary>
        public ICustomButton DefaultCustomButton { get; set; }

        public TaskDialogResult DefaultCommonButton { get; set; }

        public IRadioButton DefaultRadioButton { get; set; }


        /// <summary>
        /// If <see cref="ResultCustomButton"/> is null, this field contains the
        /// <see cref="TaskDialogResult"/> of the common buttons that was pressed.
        /// </summary>
        public TaskDialogResult ResultCommonButtonID => resultCommonButtonID;

        /// <summary>
        /// If not null, contains the custom button that was pressed. Otherwise, 
        /// <see cref="ResultCommonButtonID"/> contains the common button that was pressed.
        /// </summary>
        public ICustomButton ResultCustomButton => resultCustomButton;

        public IRadioButton ResultRadioButton => resultRadioButton;

        public bool ResultVerificationFlagChecked => resultVerificationFlagChecked;


        /// <summary>
        /// Called after the TaskDialog has been created but before it is displayed.
        /// </summary>
        public event EventHandler Opened;
        public event EventHandler Closing;
        public event EventHandler Navigated;
        public event EventHandler Help;
        public event EventHandler<HyperlinkClickedEventArgs> HyperlinkClicked;
        public event EventHandler<BooleanStatusEventArgs> ExpandoButtonClicked;
        public event EventHandler<BooleanStatusEventArgs> VerificationClicked;
        public event EventHandler TimerTick;
        public CommonButtonClickedDelegate CommonButtonClicked { get; set; }


        public TaskDialog()
        {
            // Create a delegate for the callback.
            callbackProcDelegate = new TaskDialogCallbackProcDelegate(TaskDialogCallbackProc);
            // Get a function pointer for the delegate.
            ptrCallbackProcDelegate = Marshal.GetFunctionPointerForDelegate(callbackProcDelegate);

            // Set default values
            Reset();
        }


        public ICustomButton CreateCustomButton(string text) => new CustomButton(this, text);

        public IRadioButton CreateRadioButton(string text) => new RadioButton(this, text);

        /// <summary>
        /// Resets all properties to their default values, e.g. for calling <see cref="Navigate"/>
        /// with new values.
        /// </summary>
        public void Reset()
        {
            Flags = TaskDialogFlags.PositionRelativeToWindow;
            Title = MainInstruction = Content = Footer = VerificationText =
                ExpandedInformation = ExpandedControlText = CollapsedControlText = null;
            MainIcon = MainUpdateIcon = FooterIcon = default(TaskDialogIcon);
            CommonButtons = default(TaskDialogButtons);
            CustomButtons = null;
            RadioButtons = null;
            DefaultCommonButton = default(TaskDialogResult);
            DefaultCustomButton = null;
            DefaultRadioButton = null;
        }


        private void CheckButtonConfig()
        {
            // Before assigning button IDs etc., check if the button configs are OK.
            // This needs to be done before clearing the old button IDs and assigning the
            // new ones. This is needed because it is possible to use the same button instances after a dialog
            // has been created for Navigate(), which need to do the check, then releasing the old buttons, then
            // assigning the new buttons.

            if (DefaultCustomButton != null && ((DefaultCustomButton as CustomButton)?.Creator != this))
                throw new InvalidOperationException("Custom buttons must be created with this TaskDialog instance.");
            if (DefaultRadioButton != null && ((DefaultRadioButton as RadioButton)?.Creator != this))
                throw new InvalidOperationException("Radio buttons must be created with this TaskDialog instance.");
            if (DefaultCustomButton != null && !(CustomButtons?.Contains(DefaultCustomButton) == true))
                throw new InvalidOperationException($"The default custom button must be part of the {nameof(CustomButtons)} array.");
            if (DefaultRadioButton != null && !(RadioButtons?.Contains(DefaultRadioButton) == true))
                throw new InvalidOperationException($"The default radio button must be part of the {nameof(CustomButtons)} array.");
            if ((Flags & TaskDialogFlags.UseCommandLinks) == TaskDialogFlags.UseCommandLinks && CustomButtons == null)
                throw new InvalidOperationException($"When specifying the {nameof(TaskDialogFlags.UseCommandLinks)} flag, the {nameof(CustomButtons)} array must not be null.");


            for (int i = 0; i < CustomButtons?.Length; i++)
            {
                var bt = CustomButtons[i] as CustomButton;
                if (bt?.Creator != this)
                    throw new InvalidOperationException("Custom buttons must be created with this TaskDialog instance.");
                // Check for duplicates.
                for (int j = 0; j < i; j++)
                {
                    if (CustomButtons[j] == bt)
                        throw new InvalidOperationException("Duplicate custom button found.");
                }
            }
            for (int i = 0; i < RadioButtons?.Length; i++)
            {
                var rbt = RadioButtons[i] as RadioButton;
                if (rbt?.Creator != this)
                    throw new InvalidOperationException("Radio buttons must be created with this TaskDialog instance.");
                // Check for duplicates.
                for (int j = 0; j < i; j++)
                {
                    if (RadioButtons[j] == rbt)
                        throw new InvalidOperationException("Duplicate radio button found.");
                }
            }
        }

        private void PrepareButtonConfig(out IDictionary<int, CustomButton> currentCustomButtons, out IDictionary<int, RadioButton> currentRadioButtons)
        {
            // Assign IDs to the custom buttons and populate the dictionaries.
            // Note: This method assumes CheckButtonConfig() has already been called.
            currentCustomButtons = null;
            currentRadioButtons = null;

            int currentCustomButtonID = CustomButtonStartID;

            if (CustomButtons?.Length > 0)
            {
                currentCustomButtons = new SortedDictionary<int, CustomButton>();
                foreach (var button in CustomButtons)
                {
                    var bt = (CustomButton)button;
                    int buttonId = currentCustomButtonID++;
                    bt.ButtonID = buttonId;
                    currentCustomButtons.Add(buttonId, bt);
                }
            }
            if (RadioButtons?.Length > 0)
            {
                currentRadioButtons = new SortedDictionary<int, RadioButton>();
                foreach (var button in RadioButtons)
                {
                    var bt = (RadioButton)button;
                    int buttonId = currentCustomButtonID++;
                    bt.ButtonID = buttonId;
                    currentRadioButtons.Add(buttonId, bt);
                }
            }
        }

        private static void ClearButtonConfig(IDictionary<int, CustomButton> currentCustomButtons, IDictionary<int, RadioButton> currentRadioButtons)
        {
            if (currentCustomButtons != null)
            {
                foreach (var bt in currentCustomButtons.Values)
                    bt.ButtonID = null;
            }
            if (currentRadioButtons != null)
            {
                foreach (var rbt in currentRadioButtons.Values)
                    rbt.ButtonID = null;
            }
        }

        private void CreateConfig(out TaskDialogConfig config)
        {
            config = new TaskDialogConfig()
            {
                cbSize = Marshal.SizeOf<TaskDialogConfig>(),
                hwndParent = currentOwnerHwnd.Value,
                pszWindowTitle = Title,
                pszMainInstruction = MainInstruction,
                pszContent = Content,
                pszFooter = Footer,
                dwCommonButtons = CommonButtons,
                hMainIcon = (IntPtr)MainIcon,
                dwFlags = Flags,
                hFooterIcon = (IntPtr)FooterIcon,
                pszVerificationText = VerificationText,
                pszExpandedInformation = ExpandedInformation,
                pszExpandedControlText = ExpandedControlText,
                pszCollapsedControlText = CollapsedControlText,
                nDefaultButton = (DefaultCustomButton as CustomButton)?.ButtonID ?? (int)DefaultCommonButton,
                nDefaultRadioButton = (DefaultRadioButton as RadioButton)?.ButtonID ?? 0,
                pfCallback = ptrCallbackProcDelegate
            };

            if (currentCustomButtons?.Count > 0)
            {
                TaskDialogButtonStruct[] structs = currentCustomButtons.Values.Select(e =>
                    new TaskDialogButtonStruct(e.ButtonID.Value, e.Text)).ToArray();
                config.pButtons = AllocateAndMarshalButtons(structs);
                config.cButtons = structs.Length;
            }
            if (currentRadioButtons?.Count > 0)
            {
                TaskDialogButtonStruct[] structs = currentRadioButtons.Values.Select(e =>
                    new TaskDialogButtonStruct(e.ButtonID.Value, e.Text)).ToArray();
                config.pRadioButtons = AllocateAndMarshalButtons(structs);
                config.cRadioButtons = structs.Length;
            }
        }

        private static void DisposeConfig(ref TaskDialogConfig config)
        {
            if (config.pButtons != IntPtr.Zero)
            {
                FreeButtons(config.pButtons, config.cButtons);
                config.pButtons = IntPtr.Zero;
                config.cButtons = 0;
            }
            if (config.pRadioButtons != IntPtr.Zero)
            {
                FreeButtons(config.pRadioButtons, config.cRadioButtons);
                config.pRadioButtons = IntPtr.Zero;
                config.cRadioButtons = 0;
            }
        }

        private static IntPtr AllocateAndMarshalButtons(TaskDialogButtonStruct[] structs)
        {
            // Allocate memory for the array.
            IntPtr initialPtr = Marshal.AllocHGlobal(
                Marshal.SizeOf<TaskDialogButtonStruct>() * structs.Length);

            IntPtr currentPtr = initialPtr;
            foreach (var button in structs)
            {
                // Marshal the struct element. This will allocate memory for the strings.
                Marshal.StructureToPtr(button, currentPtr, false);
                currentPtr = IntPtr.Add(currentPtr, Marshal.SizeOf<TaskDialogButtonStruct>());
            }

            return initialPtr;
        }

        private static void FreeButtons(IntPtr pointer, int length)
        {
            IntPtr currentPtr = pointer;
            // We need to destroy each structure. Otherwise we will leak memory from the
            // allocated strings (TaskDialogButton.ButtonText) which have been allocated
            // using Marshal.StructureToPtr().
            for (int i = 0; i < length; i++)
            {
                Marshal.DestroyStructure<TaskDialogButtonStruct>(currentPtr);
                currentPtr = IntPtr.Add(currentPtr, Marshal.SizeOf<TaskDialogButtonStruct>());
            }

            Marshal.FreeHGlobal(pointer);
        }

        private void ApplyButtonInitialization()
        {
            // Apply current properties of buttons after the dialog has been created.
            if (currentCustomButtons != null)
            {
                foreach (var btn in currentCustomButtons.Values)
                {
                    if (!btn.Enabled)
                        btn.Enabled = false;
                    if (btn.ButtonElevationRequiredState)
                        btn.ButtonElevationRequiredState = true;
                }
            }
            if (currentRadioButtons != null)
            {
                foreach (var btn in currentRadioButtons.Values)
                {
                    if (!btn.Enabled)
                        btn.Enabled = false;
                }
            }

            // Check if we need to update the icon.
            if (MainUpdateIcon != default(TaskDialogIcon) && MainIcon != MainUpdateIcon)
            {
                CheckUpdateIcon(TaskDialogUpdateElements.MainIcon, TaskDialogUpdateElements.MainIcon, TaskDialogIconElement.Main, (IntPtr)MainUpdateIcon);
            }
        }

        private int TaskDialogCallbackProc(IntPtr hWnd, TaskDialogNotifications notification,
            IntPtr wparam, IntPtr lparam, IntPtr referencedata)
        {
            try
            {
                hwndDialog = hWnd;
                switch (notification)
                {
                    case TaskDialogNotifications.Created:
                        ApplyButtonInitialization();
                        OnOpened(EventArgs.Empty);
                        break;
                    case TaskDialogNotifications.Destroyed:
                        OnClosing(EventArgs.Empty);
                        // Clear the dialog handle.
                        hwndDialog = IntPtr.Zero;
                        break;
                    case TaskDialogNotifications.Navigated:
                        ApplyButtonInitialization();
                        OnNavigated(EventArgs.Empty);
                        break;
                    case TaskDialogNotifications.HyperlinkClicked:
                        string link = Marshal.PtrToStringUni(lparam);
                        OnHyperlinkClicked(new HyperlinkClickedEventArgs(link));
                        break;
                    case TaskDialogNotifications.ButtonClicked:
                        // Check if the button is part of the custom buttons.
                        int buttonID = wparam.ToInt32();
                        CustomButton bt;
                        if (currentCustomButtons != null && currentCustomButtons.TryGetValue(buttonID, out bt))
                            return bt.ButtonClicked?.Invoke(bt, EventArgs.Empty) ?? true ? HResultOk : HResultFalse;
                        else
                            return CommonButtonClicked?.Invoke(this, new CommonButtonClickedEventArgs((TaskDialogResult)buttonID)) ?? true ? HResultOk : HResultFalse;
                    case TaskDialogNotifications.RadioButtonClicked:
                        int rbuttonID = wparam.ToInt32();
                        RadioButton rbt;
                        // Note: It should not happen that we don't find the radio button id.
                        if (currentRadioButtons != null && currentRadioButtons.TryGetValue(rbuttonID, out rbt))
                            rbt.OnRadioButtonClicked(EventArgs.Empty);
                        break;
                    case TaskDialogNotifications.ExpandButtonClicked:
                        OnExpandoButtonClicked(new BooleanStatusEventArgs(wparam != IntPtr.Zero));
                        break;
                    case TaskDialogNotifications.VerificationClicked:
                        OnVerificationClicked(new BooleanStatusEventArgs(wparam != IntPtr.Zero));
                        break;
                    case TaskDialogNotifications.Help:
                        OnHelp(EventArgs.Empty);
                        break;
                    case TaskDialogNotifications.Timer:
                        OnTimerTick(EventArgs.Empty);
                        break;
                }

                return HResultOk;
            }
            catch (Exception e)
            {
                // We must catch all exceptions and translate them into a HResult, so that the TaskDialog function
                // is aware of the error and can return, where we rethrow the exception from the HResult.
                return Marshal.GetHRForException(e);
            }
        }

        private void SendTaskDialogMessage(TaskDialogMessages message, int wparam, IntPtr lparam)
        {
            if (hwndDialog == IntPtr.Zero)
                throw new InvalidOperationException("Can only update the state of a task dialog while it is active.");

            SendMessage(
                hwndDialog,
                (int)message,
                (IntPtr)wparam,
                lparam);
        }

        /// <summary>
        /// Shows the dialog. After the dialog is created, the <see cref="Opened"/>
        /// event occurs which allows to customize the dialog. When the dialog is closed, the
        /// <see cref="Closing"/> event occurs.
        /// 
        /// Starting with the <see cref="Opened"/>, you can call methods on the active task dialog
        /// to update its state until the <see cref="Closing"/> event occurs.
        /// </summary>
        public void Show() => Show(IntPtr.Zero);

        /// <summary>
        /// Shows the dialog. After the dialog is created, the <see cref="Opened"/>
        /// event occurs which allows to customize the dialog. When the dialog is closed, the
        /// <see cref="Closing"/> event occurs.
        /// 
        /// Starting with the <see cref="Opened"/>, you can call methods on the active task dialog
        /// to update its state until the <see cref="Closing"/> event occurs.
        /// </summary>
        /// <param name="owner">The window handle of the owner</param>
        public void Show(Window owner) => Show(GetWindowHandle(owner));

        /// <summary>
        /// Shows the dialog. After the dialog is created, the <see cref="Opened"/>
        /// event occurs which allows to customize the dialog. When the dialog is closed, the
        /// <see cref="Closing"/> event occurs.
        /// 
        /// Starting with the <see cref="Opened"/>, you can call methods on the active task dialog
        /// to update its state until the <see cref="Closing"/> event occurs.
        /// </summary>
        /// <param name="owner">The window handle of the owner</param>
        public void Show(IWin32Window owner) => Show(GetWindowHandle(owner));

        /// <summary>
        /// Shows the dialog. After the dialog is created, the <see cref="Opened"/>
        /// event occurs which allows to customize the dialog. When the dialog is closed, the
        /// <see cref="Closing"/> event occurs.
        /// 
        /// Starting with the <see cref="Opened"/>, you can call methods on the active task dialog
        /// to update its state until the <see cref="Closing"/> event occurs.
        /// </summary>
        /// <param name="owner">The window handle of the owner</param>
        public void Show(WinFormsIWin32Window owner) => Show(GetWindowHandle(owner));

        /// <summary>
        /// Shows the dialog. After the dialog is created, the <see cref="Opened"/>
        /// event occurs which allows to customize the dialog. When the dialog is closed, the
        /// <see cref="Closing"/> event occurs.
        /// 
        /// Starting with the <see cref="Opened"/>, you can call methods on the active task dialog
        /// to update its state until the <see cref="Closing"/> event occurs.
        /// </summary>
        /// <param name="owner">The window handle of the owner</param>
        public void Show(TaskDialog owner) => Show(GetWindowHandle((IWin32Window)owner));

        /// <summary>
        /// Shows the dialog. After the dialog is created, the <see cref="Opened"/>
        /// event occurs which allows to customize the dialog. When the dialog is closed, the
        /// <see cref="Closing"/> event occurs.
        /// 
        /// Starting with the <see cref="Opened"/>, you can call methods on the active task dialog
        /// to update its state until the <see cref="Closing"/> event occurs.
        /// </summary>
        /// <param name="owner">The window handle of the owner</param>
        public void Show(IntPtr hwndOwner)
        {
            // Recursive Show() is not possible because we would use the same callback delegate..
            if (currentOwnerHwnd.HasValue)
                throw new InvalidOperationException("Cannot recursively show the same task dialog instance.");

            CheckButtonConfig();
            PrepareButtonConfig(out currentCustomButtons, out currentRadioButtons);

            currentOwnerHwnd = hwndOwner;
            TaskDialogConfig config;
            CreateConfig(out config);
            try
            {
                int ret = 0;
                int resultButtonID, resultRadioButtonID;
                try
                {
                    ret = TaskDialogIndirect(ref config, out resultButtonID, out resultRadioButtonID,
                        out resultVerificationFlagChecked);
                }
                // Only catch exceptions if the hWnd of the task dialog is not set, otherwise the exception
                // must have occured in the callback.
                // Note: If a exception occurs here when hwndDialog is not 0, it means the TaskDialogIndirect
                // run the event loop and called a WndProc e.g. from a window, whose event handler threw an 
                // exception. In that case we cannot catch and marshal it to a HResult, so the CLR will 
                // manipulate the managed stack so that it doesn't contain the transition to and from native
                // code. However, the TaskDialog still calls our TaskDialogCallbackProc (by dispatching
                // messages to the WndProc) when the current event handler from WndProc returns, but the GC might
                // already have collected the delegate to it which will cause a NRE/AccessViolation.

                // This is OK because the same issue occurs when using a Messagebox with WPF or WinForms:
                // If do MessageBox.Show() wrapped in a try/catch on a button click, and before calling .Show()
                // create and start a timer which stops and throws an exception on its Tick event,
                // the application will crash with an AccessViolationException as soon as you close the MessageBox.
                catch (Exception ex) when (hwndDialog == IntPtr.Zero &&
                    (ex is DllNotFoundException || ex is EntryPointNotFoundException))
                {
                    // Show a regular messagebox instead. This should only happen if we debug and for some
                    // reason the VS host process doesn't use our manifest.
                    StringBuilder msgContent = new StringBuilder();
                    if (MainInstruction != null)
                        msgContent.Append(MainInstruction + "\n\n");
                    if (Content != null)
                        msgContent.Append(Content + "\n\n");
                    if (ExpandedInformation != null)
                        msgContent.Append(ExpandedInformation + "\n\n");
                    MessageBox.Show(msgContent.ToString(), Title, MessageBoxButton.OK);

                    resultButtonID = (int)TaskDialogResult.Ok;
                    resultRadioButtonID = 0;
                }
                // Marshal.ThrowExceptionForHR will use the IErrorInfo on the current thread if it exists, ignoring
                // the error code. Therefore we only call it if the HResult is not OK to avoid incorrect
                // exceptions being thrown.
                // However, if the HResult indicates an error we need to use the IErrorInfo because the exception might
                // be a managed exception thorwn in the callback and translated to a HResult by
                // Marshal.GetHRForException(Exception).
                if (ret != HResultOk)
                    Marshal.ThrowExceptionForHR(ret);

                // Set the result fields
                CustomButton myResultCustomButton = null;
                if (currentCustomButtons?.TryGetValue(resultButtonID, out myResultCustomButton) == true)
                {
                    resultCustomButton = myResultCustomButton;
                    resultCommonButtonID = 0;
                }
                else
                {
                    resultCommonButtonID = (TaskDialogResult)resultButtonID;
                    resultCustomButton = null;
                }

                // Note that even if we have radio buttons, it could be that the user didn't select one.
                if (!(currentRadioButtons?.TryGetValue(resultRadioButtonID, out resultRadioButton) == true))
                    resultRadioButton = null;

            }
            finally
            {
                // Clear the handles and free the memory.
                currentOwnerHwnd = null;
                DisposeConfig(ref config);

                ClearButtonConfig(currentCustomButtons, currentRadioButtons);
                currentCustomButtons = null;
                currentRadioButtons = null;

                // We need to ensure the callback delegate is not garbage-collected as long as TaskDialogIndirect
                // doesn't return, by calling GC.KeepAlive().
                // 
                // This is not an exaggeration, as the comment for GC.KeepAlive() says the following:
                // The JIT is very aggressive about keeping an 
                // object's lifetime to as small a window as possible, to the point
                // where a 'this' pointer isn't considered live in an instance method
                // unless you read a value from the instance.
                GC.KeepAlive(callbackProcDelegate);
            }
        }


        // Messages that can be sent to the dialog while it is active.

        /// <summary>
        /// Closes the dialog with a <see cref="TaskDialogResult.Cancel"/> result.
        /// </summary>
        public void Close()
        {
            // Send a click button message with the cancel result.
            ClickCommonButton(TaskDialogResult.Cancel);
        }

        /// <summary>
        /// Recreates an active task dialog with the current properties. After the dialog is recreated,
        /// the <see cref="Navigated"/> event occurs which allows to customize the dialog.
        /// Note that you should not call this method in the <see cref="Opened"/> event because the TaskDialog
        /// is not yet displayed in that state.
        /// </summary>
        public void Navigate()
        {
            // Need to check the button config before releasing the old one and preparing the new one.
            CheckButtonConfig();

            // OK, create the new button config.
            ClearButtonConfig(currentCustomButtons, currentRadioButtons);
            PrepareButtonConfig(out currentCustomButtons, out currentRadioButtons);

            // Create a new config and marshal it.
            TaskDialogConfig config;
            CreateConfig(out config);

            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<TaskDialogConfig>());
            Marshal.StructureToPtr(config, ptr, false);
            try
            {
                // Note: If the task dialog cannot be recreated with the new contents, the dialog
                // will close and TaskDialogIndirect() returns with an error code.
                SendTaskDialogMessage(TaskDialogMessages.NavigatePage, 0, ptr);
            }
            finally
            {
                // We can now destroy the structure because SendMessage does not return until the
                // message has been processed.
                Marshal.DestroyStructure<TaskDialogConfig>(ptr);
                Marshal.FreeHGlobal(ptr);

                DisposeConfig(ref config);
            }
        }

        private void SetButtonElevationRequiredState(int buttonID, bool requiresElevation) =>
            SendTaskDialogMessage(TaskDialogMessages.SetButtonElevationRequiredState, buttonID, (IntPtr)(requiresElevation ? 1 : 0));

        /// <summary>
        /// Specifies whether the command button icon of an active task dialog should be changed
        /// to the UAC shield symbol.
        /// </summary>
        /// <param name="buttonID"></param>
        /// <param name="requiresElevation"></param>
        public void SetButtonElevationRequiredState(TaskDialogResult buttonID, bool requiresElevation) =>
            SetButtonElevationRequiredState((int)buttonID, requiresElevation);

        public void SetProgressBarRange(int min, int max)
        {
            if (min < 0 || min > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(min));
            if (max < 0 || max > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(max));

            int param = (min | (max << 0x10));
            SendTaskDialogMessage(TaskDialogMessages.SetProgressBarRange, 0, (IntPtr)param);
        }

        public void SetProgressBarPos(int pos)
        {
            if (pos < 0 || pos > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(pos));

            SendTaskDialogMessage(TaskDialogMessages.SetProgressBarPosition, pos, IntPtr.Zero);
        }

        public void SetProgressBarState(ProgressBarState state) =>
            SendTaskDialogMessage(TaskDialogMessages.SetProgressBarState, (int)state, IntPtr.Zero);

        public void SetProgressBarMarquee(bool enableMarquee) =>
            SendTaskDialogMessage(TaskDialogMessages.SetProgressBarMarquee, enableMarquee ? 1 : 0, IntPtr.Zero);

        private void SetButtonEnabled(int buttonID, bool enable) =>
            SendTaskDialogMessage(TaskDialogMessages.EnableButton, buttonID, (IntPtr)(enable ? 1 : 0));

        /// <summary>
        /// Enables or disables a button of an active task dialog.
        /// </summary>
        /// <param name="buttonID"></param>
        /// <param name="enable"></param>
        public void SetCommonButtonEnabled(TaskDialogResult buttonID, bool enable) => SetButtonEnabled((int)buttonID, enable);

        /// <summary>
        /// Enables or disables a radio button of an active task dialog.
        /// </summary>
        /// <param name="buttonID"></param>
        /// <param name="enable"></param>
        private void SetRadioButtonEnabled(int buttonID, bool enable) =>
            SendTaskDialogMessage(TaskDialogMessages.EnableRadioButton, buttonID, (IntPtr)(enable ? 1 : 0));

        public void ClickVerification(bool isChecked, bool focus = false) =>
            SendTaskDialogMessage(TaskDialogMessages.ClickVerification, isChecked ? 1 : 0, (IntPtr)(focus ? 1 : 0));

        public void ClickCommonButton(TaskDialogResult buttonID) => ClickButton((int)buttonID);

        private void ClickButton(int buttonID) => SendTaskDialogMessage(TaskDialogMessages.ClickButton, buttonID, IntPtr.Zero);

        private void ClickRadioButton(int radioButtonID) =>
            SendTaskDialogMessage(TaskDialogMessages.ClickRadioButton, radioButtonID, IntPtr.Zero);

        /// <summary>
        /// Updates the specified dialog elements with the values from the current properties.
        /// Note that when updating the main icon, the bar color will not change.
        /// </summary>
        /// <param name="updateFlags"></param>
        public void UpdateElements(TaskDialogUpdateElements updateFlags)
        {
            CheckUpdateElementText(updateFlags, TaskDialogUpdateElements.Content, TaskDialogElements.Content, Content);
            CheckUpdateElementText(updateFlags, TaskDialogUpdateElements.ExpandedInformation, TaskDialogElements.ExpandedInformation, ExpandedInformation);
            CheckUpdateElementText(updateFlags, TaskDialogUpdateElements.Footer, TaskDialogElements.Footer, Footer);
            CheckUpdateElementText(updateFlags, TaskDialogUpdateElements.MainInstruction, TaskDialogElements.MainInstruction, MainInstruction);
            CheckUpdateIcon(updateFlags, TaskDialogUpdateElements.MainIcon, TaskDialogIconElement.Main, (IntPtr)MainIcon);
            CheckUpdateIcon(updateFlags, TaskDialogUpdateElements.FooterIcon, TaskDialogIconElement.Footer, (IntPtr)FooterIcon);
        }

        private void CheckUpdateElementText(TaskDialogUpdateElements updateFlags, TaskDialogUpdateElements flagToCheck,
            TaskDialogElements element, string text)
        {
            if ((updateFlags & flagToCheck) == flagToCheck)
            {
                IntPtr strPtr = Marshal.StringToHGlobalUni(text);
                try
                {
                    // Note: SetElementText will resize the dialog while UpdateElementText will not (which would
                    // lead to clipped controls), so we use the former.
                    SendTaskDialogMessage(TaskDialogMessages.SetElementText, (int)element, strPtr);
                }
                finally
                {
                    // We can now free the memory because SendMessage does not return until the
                    // message has been processed.
                    Marshal.FreeHGlobal(strPtr);
                }
            }
        }

        private void CheckUpdateIcon(TaskDialogUpdateElements updateFlags, TaskDialogUpdateElements flagToCheck,
            TaskDialogIconElement element, IntPtr icon)
        {
            if ((updateFlags & flagToCheck) == flagToCheck)
            {
                SendTaskDialogMessage(TaskDialogMessages.UpdateIcon, (int)element, icon);
            }
        }



        public static TaskDialogResult Show(string content, string instruction = null, string caption = null,
            TaskDialogButtons buttons = TaskDialogButtons.OK, TaskDialogIcon icon = 0)
             => Show(IntPtr.Zero, content, instruction, caption, buttons, icon);


        public static TaskDialogResult Show(Window owner, string content, string instruction = null, string caption = null,
            TaskDialogButtons buttons = TaskDialogButtons.OK, TaskDialogIcon icon = 0)
            => Show(GetWindowHandle(owner), content, instruction, caption, buttons, icon);

        public static TaskDialogResult Show(IWin32Window owner, string content, string instruction = null, string caption = null,
            TaskDialogButtons buttons = TaskDialogButtons.OK, TaskDialogIcon icon = 0)
            => Show(GetWindowHandle(owner), content, instruction, caption, buttons, icon);

        public static TaskDialogResult Show(WinFormsIWin32Window owner, string content, string instruction = null, string caption = null,
                    TaskDialogButtons buttons = TaskDialogButtons.OK, TaskDialogIcon icon = 0)
                    => Show(GetWindowHandle(owner), content, instruction, caption, buttons, icon);

        public static TaskDialogResult Show(TaskDialog owner, string content, string instruction = null, string caption = null,
            TaskDialogButtons buttons = TaskDialogButtons.OK, TaskDialogIcon icon = 0)
            => Show(GetWindowHandle((IWin32Window)owner), content, instruction, caption, buttons, icon);

        public static TaskDialogResult Show(IntPtr hwndOwner, string content, string instruction = null, string caption = null,
            TaskDialogButtons buttons = TaskDialogButtons.OK, TaskDialogIcon icon = 0)
        {
            TaskDialog dialog = new TaskDialog()
            {
                Content = content,
                MainInstruction = instruction,
                Title = caption,
                CommonButtons = buttons,
                MainIcon = icon
            };
            dialog.Show(hwndOwner);

            return dialog.ResultCommonButtonID;
        }


        protected void OnOpened(EventArgs e) => Opened?.Invoke(this, e);
        protected void OnClosing(EventArgs e) => Closing?.Invoke(this, e);
        protected void OnNavigated(EventArgs e) => Navigated?.Invoke(this, e);
        protected void OnHelp(EventArgs e) => Help?.Invoke(this, e);
        protected void OnHyperlinkClicked(HyperlinkClickedEventArgs e) => HyperlinkClicked?.Invoke(this, e);
        protected void OnExpandoButtonClicked(BooleanStatusEventArgs e) => ExpandoButtonClicked?.Invoke(this, e);
        protected void OnVerificationClicked(BooleanStatusEventArgs e) => VerificationClicked?.Invoke(this, e);
        protected void OnTimerTick(EventArgs e) => TimerTick?.Invoke(this, e);


        private static IntPtr GetWindowHandle(Window window) => new WindowInteropHelper(window).Handle;
        private static IntPtr GetWindowHandle(IWin32Window window) => window.Handle;
        private static IntPtr GetWindowHandle(WinFormsIWin32Window window) => window.Handle;



        [DllImport("comctl32.dll", CharSet = CharSet.Unicode, EntryPoint = "TaskDialogIndirect", ExactSpelling = true, SetLastError = true)]
        private static extern int TaskDialogIndirect(
            [In] ref TaskDialogConfig pTaskConfig,
            [Out] out int pnButton,
            [Out] out int pnRadioButton,
            [MarshalAs(UnmanagedType.Bool), Out] out bool pfVerificationFlagChecked
        );

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SendMessageW", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr SendMessage(
            IntPtr windowHandle,
            int message,
            IntPtr wparam,
            IntPtr lparam
        );

        private delegate int TaskDialogCallbackProcDelegate(IntPtr hWnd, TaskDialogNotifications notification,
            IntPtr wparam, IntPtr lparam, IntPtr referenceData);

        // Offset for user message types
        private const int UserMessage = 0x0400;

        private const int HResultOk = 0x0; // S_OK
        private const int HResultFalse = 0x0001; // S_FALSE

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns>True if the dialog should close; false otherwise</returns>
        public delegate bool CommonButtonClickedDelegate(object sender, CommonButtonClickedEventArgs e);

        public delegate bool CustomButtonClickedDelegate(object sender, EventArgs e);


        // Note: Packing must be set to 4 to make this work on 64-bit platforms.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
        private struct TaskDialogConfig
        {
            public int cbSize;
            public IntPtr hwndParent;
            public IntPtr hInstance;
            public TaskDialogFlags dwFlags;
            public TaskDialogButtons dwCommonButtons;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszWindowTitle;
            public IntPtr hMainIcon;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszMainInstruction;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszContent;
            public int cButtons;
            public IntPtr pButtons;
            public int nDefaultButton;
            public int cRadioButtons;
            public IntPtr pRadioButtons;
            public int nDefaultRadioButton;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszVerificationText;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszExpandedInformation;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszExpandedControlText;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszCollapsedControlText;
            public IntPtr hFooterIcon;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszFooter;
            public IntPtr pfCallback;
            public IntPtr lpCallbackData;
            public int cxWidth;
        }

        // Note: Packing must be set to 4 to make this work on 64-bit platforms.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
        private struct TaskDialogButtonStruct
        {
            public int ButtonID;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string ButtonText;

            public TaskDialogButtonStruct(int buttonID, string buttonText)
            {
                ButtonID = buttonID;
                ButtonText = buttonText;
            }
        }

        [Flags]
        public enum TaskDialogButtons : int
        {
            OK = 0x0001,
            Yes = 0x0002,
            No = 0x0004,
            Cancel = 0x0008,
            Retry = 0x0010,
            Close = 0x0020
        }

        // Identify button *return values* - note that, unfortunately, these are different
        // from the inbound button values.
        public enum TaskDialogResult : int
        {
            Ok = 1,
            Cancel = 2,
            Abort = 3,
            Retry = 4,
            Ignore = 5,
            Yes = 6,
            No = 7,
            Close = 8
        }

        [Flags]
        public enum TaskDialogFlags : int
        {
            None = 0,
            EnableHyperlinks = 0x0001,
            // We currently don't support using Icon handles.
            //UseMainIconHandle = 0x0002,
            //UseFooterIconHandle = 0x0004,
            AllowCancel = 0x0008,
            UseCommandLinks = 0x0010,
            UseNoIconCommandLinks = 0x0020,
            ExpandFooterArea = 0x0040,
            ExpandedByDefault = 0x0080,
            CheckVerificationFlag = 0x0100,
            ShowProgressBar = 0x0200,
            ShowMarqueeProgressBar = 0x0400,
            /// <summary>
            /// If set, the <see cref="TimerTick"/> event will be raised approximately
            /// every 200 milliseconds while the dialog is active.
            /// </summary>
            UseTimer = 0x0800,
            PositionRelativeToWindow = 0x1000,
            RightToLeftLayout = 0x2000,
            NoDefaultRadioButton = 0x4000,
            SizeToContent = 0x1000000
        }

        public enum TaskDialogIcon : int
        {
            Question = 99,
            Information = ushort.MaxValue - 2,
            Warning = ushort.MaxValue,
            Stop = ushort.MaxValue - 1,
            SecurityShield = ushort.MaxValue - 3,
            SecurityShieldBlueBar = ushort.MaxValue - 4,
            SecurityShieldGrayBar = ushort.MaxValue - 8,
            SecurityWarningBar = ushort.MaxValue - 5,
            SecurityErrorBar = ushort.MaxValue - 6,
            SecuritySuccessBar = ushort.MaxValue - 7,

            SecurityQuestion = 104,
            SecurityWarning = 107,
            SecurityError = 105,
            SecuritySuccess = 106,

            //CommandButtons = ushort.MaxValue - 99
        }


        [Flags]
        public enum TaskDialogUpdateElements
        {
            Content = 0x1,
            ExpandedInformation = 0x2,
            Footer = 0x4,
            MainInstruction = 0x8,
            MainIcon = 0x10,
            FooterIcon = 0x20
        }

        private enum TaskDialogElements
        {
            Content = 0,
            ExpandedInformation = 1,
            Footer = 2,
            MainInstruction = 3
        }

        private enum TaskDialogIconElement
        {
            Main = 0,
            Footer = 1
        }

        public enum ProgressBarState : int
        {
            Normal = 0x1,
            Error = 0x2,
            Paused = 0x3
        }

        private enum TaskDialogMessages : int
        {
            NavigatePage = UserMessage + 101,
            ClickButton = UserMessage + 102,
            SetMarqueeProgressBar = UserMessage + 103,
            SetProgressBarState = UserMessage + 104,
            SetProgressBarRange = UserMessage + 105,
            SetProgressBarPosition = UserMessage + 106,
            SetProgressBarMarquee = UserMessage + 107,
            SetElementText = UserMessage + 108,
            ClickRadioButton = UserMessage + 110,
            EnableButton = UserMessage + 111,
            EnableRadioButton = UserMessage + 112,
            ClickVerification = UserMessage + 113,
            UpdateElementText = UserMessage + 114,
            SetButtonElevationRequiredState = UserMessage + 115,
            UpdateIcon = UserMessage + 116
        }

        private enum TaskDialogNotifications : int
        {
            Created = 0,
            Navigated = 1,
            ButtonClicked = 2,
            HyperlinkClicked = 3,
            Timer = 4,
            Destroyed = 5,
            RadioButtonClicked = 6,
            Constructed = 7,
            VerificationClicked = 8,
            Help = 9,
            ExpandButtonClicked = 10
        }


        public class HyperlinkClickedEventArgs : EventArgs
        {
            public string Hyperlink { get; }

            public HyperlinkClickedEventArgs(string hyperlink)
            {
                Hyperlink = hyperlink;
            }
        }

        public class CommonButtonClickedEventArgs : EventArgs
        {
            /// <summary>
            /// The <see cref="TaskDialogResult"/> that was clicked.
            /// </summary>
            public TaskDialogResult ButtonID { get; }

            public CommonButtonClickedEventArgs(TaskDialogResult buttonID)
            {
                ButtonID = buttonID;
            }
        }

        public class BooleanStatusEventArgs : EventArgs
        {
            public bool Status { get; }

            public BooleanStatusEventArgs(bool status)
            {
                Status = status;
            }
        }

        private abstract class ButtonBase : IButtonBase
        {
            public int? ButtonID { get; set; }
            public TaskDialog Creator { get; }
            public string Text { get; }


            public ButtonBase(TaskDialog creator, string text)
            {
                Creator = creator;
                Text = text;
            }

            protected void VerifyState()
            {
                if (!TryVerifyState())
                {
                    throw new InvalidOperationException("This button is not part of an active task dialog.");
                }
            }

            protected bool TryVerifyState()
            {
                return ButtonID.HasValue;
            }

            public abstract void Click();

            protected abstract void SetEnabled(bool enabled);

            private bool enabled = true;
            public bool Enabled
            {
                get
                {
                    return enabled;
                }
                set
                {
                    SetEnabled(value);
                    enabled = value;
                }
            }
        }

        private class CustomButton : ButtonBase, ICustomButton
        {
            private bool buttonElevationRequiredState = false;

            public CustomButton(TaskDialog creator, string text)
                : base(creator, text)
            {
            }

            public CustomButtonClickedDelegate ButtonClicked { get; set; }

            public bool ButtonElevationRequiredState
            {
                get
                {
                    return buttonElevationRequiredState;
                }

                set
                {
                    // The Task dialog will set this property on th Created/Navigated event.
                    if (TryVerifyState())
                        Creator.SetButtonElevationRequiredState(ButtonID.Value, value);
                    buttonElevationRequiredState = value;
                }
            }

            public override void Click()
            {
                VerifyState();
                Creator.ClickButton(ButtonID.Value);
            }

            protected override void SetEnabled(bool enabled)
            {
                // The Task dialog will set this property on th Created/Navigated event.
                if (TryVerifyState())
                    Creator.SetButtonEnabled(ButtonID.Value, enabled);
            }
        }

        private class RadioButton : ButtonBase, IRadioButton
        {
            public event EventHandler RadioButtonClicked;

            public RadioButton(TaskDialog creator, string text)
                : base(creator, text)
            {
            }

            public override void Click()
            {
                VerifyState();
                Creator.ClickRadioButton(ButtonID.Value);
            }

            protected override void SetEnabled(bool enabled)
            {
                // The Task dialog will set this property on th Created/Navigated event.
                if (TryVerifyState())
                    Creator.SetRadioButtonEnabled(ButtonID.Value, enabled);
            }

            public void OnRadioButtonClicked(EventArgs e) => RadioButtonClicked?.Invoke(this, e);
        }


        public interface IButtonBase
        {
            string Text { get; }
            void Click();
            bool Enabled { get; set; }
        }

        public interface ICustomButton : IButtonBase
        {
            CustomButtonClickedDelegate ButtonClicked { get; set; }

            bool ButtonElevationRequiredState { get; set; }
        }

        public interface IRadioButton : IButtonBase
        {
            event EventHandler RadioButtonClicked;
        }
    }
}
