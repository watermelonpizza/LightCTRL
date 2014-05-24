using LIFX_Net;
using LIFX_Net.Messages;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace LightCTRL_wp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool firstLightStatusFlag = true;

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        private void PopulateBulbList(int selectedIndex = -1)
        {
            BulbListComboBox.Items.Clear();

            List<List<KeyValuePair<Byte[], string>>> bulbmap = StorageHelper.GetBulbMap();
            foreach (List<KeyValuePair<Byte[], string>> list in bulbmap)
            {
                foreach (KeyValuePair<Byte[], string> bulbData in list)
                {
                    BulbListComboBox.Items.Add(new ComboBoxItem() { Content = bulbData.Value, Tag = bulbData.Key });
                }
            }

            BulbListComboBox.SelectedIndex = selectedIndex;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.
            LifxCommunicator.Instance.PanControllerFound += Instance_PanControllerFound;

            PopulateBulbList();

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }

        void Instance_PanControllerFound(object sender, LifxPanController e)
        {
            throw new NotImplementedException();
        }

        #region Private Methods

        private LifxBulb GetSelectedBulb()
        {
            return StorageHelper.GetBulb((BulbListComboBox.SelectedItem as ComboBoxItem).Tag as Byte[]);
        }

        private void SetControlState(bool enabled = true)
        {
            PowerToggleSwitch.IsEnabled = enabled;
            FadeTimeTextBox.IsEnabled = enabled;
            HueSlider.IsEnabled = enabled;
            SaturationSlider.IsEnabled = enabled;
            LuminositySlider.IsEnabled = enabled;
            KelvinSlider.IsEnabled = enabled;

            ManualInputToggleButton.IsEnabled = enabled;
        }

        private void BindValueChangedEventHandlers()
        {
            HueSlider.ValueChanged += HueSlider_ValueChanged;
            SaturationSlider.ValueChanged += SaturationSlider_ValueChanged;
            LuminositySlider.ValueChanged += LuminositySlider_ValueChanged;
            KelvinSlider.ValueChanged += KelvinSlider_ValueChanged;
        }

        private void UnBindValueChangedEventHandlers()
        {
            HueSlider.ValueChanged -= HueSlider_ValueChanged;
            SaturationSlider.ValueChanged -= SaturationSlider_ValueChanged;
            LuminositySlider.ValueChanged -= LuminositySlider_ValueChanged;
            KelvinSlider.ValueChanged -= KelvinSlider_ValueChanged;
        }

        private void HandleLightStatusMessage(LifxLightStatusMessage message)
        {
            if (message != null)
            {
                FadeTimeTextBox.Text = message.Dim.ToString();

                if (firstLightStatusFlag)
                {
                    UnBindValueChangedEventHandlers();
                    HueSlider.Value = message.Hue;
                    SaturationSlider.Value = message.Saturation;
                    LuminositySlider.Value = message.Lumnosity;
                    KelvinSlider.Value = message.Kelvin;
                    BindValueChangedEventHandlers();
                    firstLightStatusFlag = false;
                }
            }
        }

        private async void HandlePowerStateMessage(LifxPowerStateMessage message)
        {
            if (message != null)
            {
                if (message.PowerState == LifxPowerState.On)
                {
                    Byte[] comboLabel = null;

                    PowerToggleSwitch.IsOn = true;
                    SetControlState(true);
                    comboLabel = (BulbListComboBox.SelectedItem as ComboBoxItem).Tag as Byte[];

                    LifxLightStatusMessage lightstatus = await StorageHelper.GetBulb(comboLabel).GetLightStatusCommand();
                    HandleLightStatusMessage(lightstatus);
                }
                else if (message.PowerState == LifxPowerState.Off)
                {
                    PowerToggleSwitch.IsOn = false;
                    SetControlState(false);
                }
            }
        }

        private async void SetColour()
        {
            await StorageHelper.GetBulb((BulbListComboBox.SelectedItem as ComboBoxItem).Tag as Byte[]).SetColorCommand(new LifxColor()
            {
                Hue = (UInt16)HueSlider.Value,
                Saturation = (UInt16)SaturationSlider.Value,
                Luminosity = (UInt16)LuminositySlider.Value,
                Kelvin = (UInt16)(KelvinSlider.Value)
            },
            Convert.ToUInt16(FadeTimeTextBox.Text));

            //await StorageHelper.GetBulb((BulbListComboBox.SelectedItem as ComboBoxItem).Tag as Byte[]).GetLightStatusCommand();
        }

        #endregion

        #region Slider Value Changed Methods

        private void HueSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (KelvinSlider.Value != 0)
            {
                UnBindValueChangedEventHandlers();
                KelvinSlider.Value = 0;
                BindValueChangedEventHandlers();
            }
            SetColour();
        }

        private void SaturationSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (KelvinSlider.Value != 0)
            {
                UnBindValueChangedEventHandlers();
                KelvinSlider.Value = 0;
                BindValueChangedEventHandlers();
            }
            SetColour();
        }

        private void LuminositySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            SetColour();
        }

        private void KelvinSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (HueSlider.Value != 0 || SaturationSlider.Value != 0)
            {
                UnBindValueChangedEventHandlers();
                HueSlider.Value = 0;
                SaturationSlider.Value = 0;
                BindValueChangedEventHandlers();
            }
            SetColour();
        }

        #endregion

        private async void PowerToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (((ToggleSwitch)sender).IsOn)
            {
                LifxPowerStateMessage psm = await GetSelectedBulb().SetPowerStateCommand(LifxPowerState.On);
                HandlePowerStateMessage(psm);
            }
            else
            {
                SetControlState(false);
                LifxPowerStateMessage psm = await GetSelectedBulb().SetPowerStateCommand(LifxPowerState.Off);
                HandlePowerStateMessage(psm);
            }
        }

        private async void SyncBulbAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            if ((BulbListComboBox.SelectedItem as ComboBoxItem).Content as string == "Unknown")
            {
                LifxLabelMessage labelmessage = await GetSelectedBulb().GetLabelCommand();
                StorageHelper.GetBulb((BulbListComboBox.SelectedItem as ComboBoxItem).Tag as Byte[]).Label = labelmessage.BulbLabel;
            }

            LifxPowerStateMessage psm = await GetSelectedBulb().GetPowerStateCommand();
            PowerToggleSwitch.IsOn = psm.PowerState == LifxPowerState.On ? true : false;

            (BulbListComboBox.SelectedItem as ComboBoxItem).Content = GetSelectedBulb().Label;
        }

        private async void EditBulbAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            EditBulbDialog dialog = new EditBulbDialog();
            ((dialog.Content as StackPanel).Children[0] as TextBox).Text = GetSelectedBulb().MACAddress;
            ((dialog.Content as StackPanel).Children[1] as TextBox).Text = GetSelectedBulb().Label;
            dialog.Closed += dialog_Closed;
            ContentDialogResult result = await dialog.ShowAsync();
        }

        async void dialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            LifxLabelMessage llm = await GetSelectedBulb().SetLabelCommand(((sender.Content as StackPanel).Children[1] as TextBox).Text);
            GetSelectedBulb().Label = llm.BulbLabel;

            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    (BulbListComboBox.SelectedItem as ComboBoxItem).Content = llm.BulbLabel;
                });
        }

        private async void BulbListComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LifxPowerStateMessage psm = await StorageHelper.GetBulb((e.AddedItems[0] as ComboBoxItem).Tag as Byte[]).GetPowerStateCommand();
            HandlePowerStateMessage(psm);
        }
    }
}
