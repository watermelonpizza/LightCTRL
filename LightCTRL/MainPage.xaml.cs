using LIFX_Net;
using LIFX_Net.Messages;

using LightCTRL.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.UI.ViewManagement;
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

namespace LightCTRL
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool firstLightStatusFlag = true;
        private NavigationHelper navigationHelper;

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
            this.navigationHelper = new NavigationHelper(this);
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
            LifxCommunicator.Instance.PanControllerFound += Instance_PanControllerFound;

            UriBuilder uri = e.Parameter as UriBuilder;

            if (uri != null)
            {
                if (uri.Host == typeof(VoiceCommandPage).ToString().ToLower())
                {
                    string[] query = uri.Query.Trim('?').Split('&');
                    Dictionary<string, string> queries = new Dictionary<string, string>();

                    foreach (string s in query)
                    {
                        string[] split = s.Split('=');
                        queries.Add(split[0], split[1]);
                    }

                    LifxColour colour = LifxColour.FromRgbColor(queries["Colour"]);
                    PopulateBulbList(queries["BulbName"]);
                }
            }
            else
            {
                PopulateBulbList();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        #region Private Methods

        private LifxBulb GetSelectedBulb()
        {
            return StorageHelper.GetBulb((BulbListComboBox.SelectedItem as ComboBoxItem).Tag as Byte[]);
        }

        private void PopulateBulbList(string bulbname)
        {
            BulbListComboBox.Items.Clear();
            int index = 0;
            int selectedIndex = 0;

            List<List<KeyValuePair<Byte[], string>>> bulbmap = StorageHelper.GetBulbMap();
            foreach (List<KeyValuePair<Byte[], string>> list in bulbmap)
            {
                foreach (KeyValuePair<Byte[], string> bulbData in list)
                {
                    BulbListComboBox.Items.Add(new ComboBoxItem() { Content = bulbData.Value, Tag = bulbData.Key });

                    if (bulbData.Value.ToUpper() == bulbname.ToUpper())
                        selectedIndex = index;

                    index++;
                }
            }

            BulbListComboBox.SelectedIndex = selectedIndex;
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

        private void SetControlState(bool enabled, bool setpowerswitch = false)
        {
            if (setpowerswitch)
                PowerToggleSwitch.IsEnabled = enabled;
    
            FadeTimeTextBox.IsEnabled = enabled;
            HueSlider.IsEnabled = enabled;
            SaturationSlider.IsEnabled = enabled;
            LuminositySlider.IsEnabled = enabled;
            KelvinSlider.IsEnabled = enabled;
            ChooseColourButton.IsEnabled = enabled;
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

        private void SetSliders(LifxColour colour)
        {
            UnBindValueChangedEventHandlers();
            HueSlider.Value = colour.Hue;
            SaturationSlider.Value = colour.Saturation;
            LuminositySlider.Value = colour.Luminosity;
            KelvinSlider.Value = 0;
            BindValueChangedEventHandlers();
        }

        private void TogglePowerSwitch(bool isOn)
        {
            PowerToggleSwitch.Toggled -= PowerToggleSwitch_Toggled;
            PowerToggleSwitch.IsOn = isOn;
            PowerToggleSwitch.Toggled += PowerToggleSwitch_Toggled;
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

        private async Task HandlePowerStateMessage(LifxPowerStateMessage message)
        {
            if (message != null)
            {
                if (message.PowerState == LifxPowerState.On)
                {
                    TogglePowerSwitch(true);
                    SetControlState(true, true);
                    
                    LifxLightStatusMessage lightstatus = await GetSelectedBulb().GetLightStatusCommand();
                    HandleLightStatusMessage(lightstatus);
                }
                else if (message.PowerState == LifxPowerState.Off)
                {
                    TogglePowerSwitch(false);
                    PowerToggleSwitch.IsEnabled = true;
                    SetControlState(false);
                }
            }
        }

        private async void SetColour()
        {
            await GetSelectedBulb().SetColorCommand(new LifxColour()
            {
                Hue = (UInt16)HueSlider.Value,
                Saturation = (UInt16)SaturationSlider.Value,
                Luminosity = (UInt16)LuminositySlider.Value,
                Kelvin = (UInt16)(KelvinSlider.Value)
            },
            Convert.ToUInt16(FadeTimeTextBox.Text));
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

        void Instance_PanControllerFound(object sender, LifxPanController e)
        {
            throw new NotImplementedException();
        }

        private async void PowerToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (((ToggleSwitch)sender).IsOn)
            {
                Helper.ShowProgressIndicator("Turning on...");

                LifxPowerStateMessage psm = await GetSelectedBulb().SetPowerStateCommand(LifxPowerState.On);
                await HandlePowerStateMessage(psm);

                Helper.HideProgressIndicator();
            }
            else
            {
                Helper.ShowProgressIndicator("Turning off...");

                SetControlState(false);
                LifxPowerStateMessage psm = await GetSelectedBulb().SetPowerStateCommand(LifxPowerState.Off);
                await HandlePowerStateMessage(psm);

                Helper.HideProgressIndicator();
            }
        }

        private async void SyncBulbAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            Helper.ShowProgressIndicator("Syncing...");

            if ((BulbListComboBox.SelectedItem as ComboBoxItem).Content as string == "Unknown")
            {
                LifxLabelMessage labelmessage = await GetSelectedBulb().GetLabelCommand();
                GetSelectedBulb().Label = labelmessage.BulbLabel;
            }

            LifxPowerStateMessage psm = await GetSelectedBulb().GetPowerStateCommand();
            PowerToggleSwitch.IsOn = psm.PowerState == LifxPowerState.On ? true : false;

            (BulbListComboBox.SelectedItem as ComboBoxItem).Content = GetSelectedBulb().Label;

            StorageHelper.SaveToStorage();

            Helper.HideProgressIndicator();
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
            StorageHelper.SaveToStorage();

            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    (BulbListComboBox.SelectedItem as ComboBoxItem).Content = llm.BulbLabel;
                });
        }

        private async void BulbListComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Helper.ShowProgressIndicator("Getting bulb info...");

            SyncBulbAppBarButton.IsEnabled = true;
            EditBulbAppBarButton.IsEnabled = true;

            LifxPowerStateMessage psm = await StorageHelper.GetBulb((e.AddedItems[0] as ComboBoxItem).Tag as Byte[]).GetPowerStateCommand();
            await HandlePowerStateMessage(psm);

            Helper.HideProgressIndicator();
        }

        private void SettingsAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage));
        }

        private async void ChooseColourButton_Click(object sender, RoutedEventArgs e)
        {
            ColourChooserDialog colourdialog = new ColourChooserDialog();
            colourdialog.Closed += colourdialog_Closed;
            await colourdialog.ShowAsync();
        }

        async void colourdialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            if (args.Result == ContentDialogResult.Primary)
            {
                Helper.ShowProgressIndicator("Setting colour...");

                ComboBoxItem selectedItem = ((sender.Content as StackPanel).Children[0] as ComboBox).SelectedItem as ComboBoxItem;
                LifxColour colour = LifxColour.FromRgbColor((selectedItem.Tag as RGBColour).Colour);
                SetSliders(colour);

                await GetSelectedBulb().SetColorCommand(new LifxColour()
                {
                    Hue = colour.Hue,
                    Saturation = colour.Saturation,
                    Luminosity = colour.Luminosity,
                    Kelvin = colour.Kelvin
                },
                Convert.ToUInt16(FadeTimeTextBox.Text));

                Helper.HideProgressIndicator();
            }
        }
    }
}
