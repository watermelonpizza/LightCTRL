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
using Windows.UI.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
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
        private const string APP_HEADING_TEXT = "LightCTRL - ";

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

            switch (StorageHelper.SelectedType)
            {
                case SelectionType.AllBulbs:
                    {
                        MainPivot.Title = APP_HEADING_TEXT + "all bulbs";
                    }
                    break;
                case SelectionType.BulbGroup:
                    {
                        MainPivot.Title = APP_HEADING_TEXT + StorageHelper.SelectedBulbs[0].Tags;
                    }
                    break;
                case SelectionType.IndividualBulb:
                    {
                        MainPivot.Title = APP_HEADING_TEXT + StorageHelper.SelectedBulbs[0].Label;
                        SetSliders(StorageHelper.SelectedBulbs[0].Colour);
                        TogglePowerSwitch(StorageHelper.SelectedBulbs[0].IsOn == LifxPowerState.On ? true : false);

                        EditBulbAppBarButton.IsEnabled = true;
                    }
                    break;
                default:
                    break;
            }

            //UriBuilder uri = e.Parameter as UriBuilder;

            //if (uri != null)
            //{
            //    if (uri.Host == typeof(VoiceCommandPage).ToString().ToLower())
            //    {
            //        string[] query = uri.Query.Trim('?').Split('&');
            //        Dictionary<string, string> queries = new Dictionary<string, string>();

            //        foreach (string s in query)
            //        {
            //            string[] split = s.Split('=');
            //            queries.Add(split[0], split[1]);
            //        }

            //        LifxColour colour = LifxColour.FromRgbColor(queries["Colour"]);
            //        PopulateBulbList(queries["BulbName"]);
            //    }
            //}
            //else
            //{
            //    PopulateBulbList();
            //}

            foreach (RGBColour colour in LifxColour.RGBColourList)
            {
                SolidColorBrush colourBrush = new SolidColorBrush(colour.Colour);

                TextBlock colourTextBlock = new TextBlock()
                {
                    VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center,
                    HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,
                    FontSize = 22,
                    Margin = new Thickness { Bottom = 5 },
                    Text = colour.FriendlyName,
                    Tag = colour.Colour
                };

                //Rectangle colourRectangle = new Rectangle()
                //{
                //    Fill = new SolidColorBrush(Colors.Black),
                //    HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,
                //    Width = 0,
                //};

                Polygon colourPolygon = new Polygon()
                {
                    Fill = colourBrush
                };

                colourPolygon.Points.Add(new Point(0, 0));
                colourPolygon.Points.Add(new Point(24, 15));
                colourPolygon.Points.Add(new Point(0, 30));

                Line colourLine = new Line()
                {
                    StrokeThickness = 0,
                    X2 = 400
                };

                Grid grid = new Grid();
                grid.Children.Add(colourPolygon);
                grid.Children.Add(colourLine);
                //grid.Children.Add(colourRectangle);
                grid.Children.Add(colourTextBlock);

                ListViewItem colourListViewItem = new ListViewItem()
                {
                    VerticalContentAlignment = Windows.UI.Xaml.VerticalAlignment.Center,
                    BorderBrush = colourBrush,
                    //BorderThickness = new Thickness { Left = 5, Right = 5 },
                    Content = grid
                };

                colourListViewItem.Tapped += ColourListViewItem_Tapped;

                ColourListView.Items.Add(colourListViewItem);
            }

            SetControlState(true, true);
            SyncBulbAppBarButton.IsEnabled = true;

            if (StorageHelper.LocalSettings.HasFlag(Settings.StartOnAdvanced))
                MainPivot.SelectedIndex = 1;
        }

        void ColourListViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            TextBlock tappedColour = e.OriginalSource as TextBlock;

            if (tappedColour != null)
            {
                SetColour(LifxColour.ColorToLifxColour((Color)tappedColour.Tag));

                if (StorageHelper.LocalSettings.HasFlag(Settings.PivotOnColourChoice))
                    MainPivot.SelectedIndex = 0;
            }
            else
            {
                ListViewItem tappedColourItem = sender as ListViewItem;
                if (tappedColourItem != null)
                {
                    tappedColour = (tappedColourItem.Content as Grid).Children[2] as TextBlock;
                    SetColour(LifxColour.ColorToLifxColour((Color)tappedColour.Tag));

                    if (StorageHelper.LocalSettings.HasFlag(Settings.PivotOnColourChoice))
                        MainPivot.SelectedIndex = 0;
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the selected bulb via the tag (MAC address) from the bulb combo box
        /// </summary>
        /// <returns>The selected bulb</returns>
        //private LifxBulb GetSelectedBulb()
        //{
        //    return StorageHelper.GetBulb((BulbListComboBox.SelectedItem as ComboBoxItem).Tag as Byte[]);
        //}

        //private void PopulateBulbList(string bulbname)
        //{
        //    BulbListComboBox.Items.Clear();
        //    int index = 0;
        //    int selectedIndex = 0;

        //    List<List<KeyValuePair<Byte[], string>>> bulbmap = StorageHelper.GetBulbMap();
        //    foreach (List<KeyValuePair<Byte[], string>> list in bulbmap)
        //    {
        //        foreach (KeyValuePair<Byte[], string> bulbData in list)
        //        {
        //            BulbListComboBox.Items.Add(new ComboBoxItem() { Content = bulbData.Value, Tag = bulbData.Key });

        //            if (bulbData.Value.ToUpper() == bulbname.ToUpper())
        //                selectedIndex = index;

        //            index++;
        //        }
        //    }

        //    BulbListComboBox.SelectedIndex = selectedIndex;
        //}

        //private void PopulateBulbList(int selectedIndex = -1)
        //{
        //    BulbListComboBox.Items.Clear();

        //    List<List<KeyValuePair<Byte[], string>>> bulbmap = StorageHelper.GetBulbMap();
        //    foreach (List<KeyValuePair<Byte[], string>> list in bulbmap)
        //    {
        //        foreach (KeyValuePair<Byte[], string> bulbData in list)
        //        {
        //            BulbListComboBox.Items.Add(new ComboBoxItem() { Content = bulbData.Value, Tag = bulbData.Key });
        //        }
        //    }

        //    BulbListComboBox.SelectedIndex = selectedIndex;
        //}

        private void SetControlState(bool enabled, bool enablepowerswitch = false)
        {
            if (enablepowerswitch)
                PowerStateAppBarToggleButton.IsEnabled = enabled;
            //PowerToggleSwitch.IsEnabled = enabled;

            FadeTimeSlider.IsEnabled = enabled;
            HueSlider.IsEnabled = enabled;
            SaturationSlider.IsEnabled = enabled;
            LuminositySlider.IsEnabled = enabled;
            KelvinSlider.IsEnabled = enabled;
            //ChooseColourButton.IsEnabled = enabled;
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
            KelvinSlider.Value = colour.Kelvin;
            BindValueChangedEventHandlers();
        }

        private void TogglePowerSwitch(bool isOn)
        {
            PowerStateAppBarToggleButton.Checked -= PowerStateAppBarToggleButton_Checked;

            PowerStateAppBarToggleButton.IsChecked = isOn;

            if (isOn)
                PowerStateAppBarToggleButtonIcon.UriSource = new Uri("ms-resource:/Files/Assets/AppbarIcons/appbar.lightbulb.hue.on.png");
            else
                PowerStateAppBarToggleButtonIcon.UriSource = new Uri("ms-resource:/Files/Assets/AppbarIcons/appbar.lightbulb.hue.png");

            PowerStateAppBarToggleButton.Checked += PowerStateAppBarToggleButton_Checked;

            BitmapImage image = new BitmapImage();
            image.DecodePixelWidth = 168;

            if (isOn)
            {
                image.UriSource = new Uri(LightPowerStateImage.BaseUri, "ms-resource:/Files/Assets/powerIconOn.png");
                LightPowerStateImage.Tag = LifxPowerState.On.ToString();
            }
            else
            {
                image.UriSource = new Uri(LightPowerStateImage.BaseUri, "ms-resource:/Files/Assets/powerIcon.png");
                LightPowerStateImage.Tag = LifxPowerState.Off.ToString();
            }

            LightPowerStateImage.Source = image;

            //PowerToggleSwitch.Toggled -= PowerToggleSwitch_Toggled;
            //PowerToggleSwitch.IsOn = isOn;
            //PowerToggleSwitch.Toggled += PowerToggleSwitch_Toggled;
        }

        private void HandleLightStatusMessage(LifxLightStatusMessage message, LifxBulb bulb)
        {
            if (message != null)
            {
                FadeTimeSlider.Value = message.Dim;

                bulb.Colour = new LifxColour() { Hue = message.Hue, Saturation = message.Saturation, Luminosity = message.Lumnosity, Kelvin = message.Kelvin };
                bulb.Label = message.Label;
                bulb.Tags = message.Tags;
                bulb.IsOn = message.PowerState;

                StorageHelper.SaveToStorage();

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

        private async Task HandlePowerStateMessage(LifxPowerStateMessage message, LifxBulb bulb)
        {
            if (message != null)
            {
                if (message.PowerState == LifxPowerState.On)
                {
                    TogglePowerSwitch(true);
                    SetControlState(true, true);

                    LifxLightStatusMessage lightstatus = await bulb.GetLightStatusCommand();
                    HandleLightStatusMessage(lightstatus, bulb);
                }
                else if (message.PowerState == LifxPowerState.Off)
                {
                    TogglePowerSwitch(false);
                    //PowerToggleSwitch.IsEnabled = true;
                    SetControlState(false);

                    bulb.IsOn = LifxPowerState.Off;
                }
            }
        }

        private async void SetColour()
        {
            LifxColour colour = new LifxColour()
            {
                Hue = (UInt16)HueSlider.Value,
                Saturation = (UInt16)SaturationSlider.Value,
                Luminosity = (UInt16)LuminositySlider.Value,
                Kelvin = (UInt16)(KelvinSlider.Value)
            };

            foreach (LifxBulb bulb in StorageHelper.SelectedBulbs)
            {
                if (FadeTimeSlider.Value > 0)
                    await bulb.SetWaveformCommand(0, 0, colour, Convert.ToUInt32(FadeTimeSlider.Value), 1f, 1, Waveform.Sine);
                else
                    await bulb.SetColorCommand(colour, 0);

                bulb.Colour = colour;
            }

            StorageHelper.SaveToStorage();
        }

        private async void SetColour(LifxColour colour)
        {
            Helper.ShowProgressIndicator("Setting colour...");

            SetSliders(colour);

            foreach (LifxBulb bulb in StorageHelper.SelectedBulbs)
            {
                if (FadeColourAppBarButton.IsChecked.Value == true)
                    await bulb.SetWaveformCommand(0, 0, colour, Convert.ToUInt32(FadeTimeSlider.Value), 1.0f, 1, Waveform.Sine);
                else
                    await bulb.SetColorCommand(colour, 0);

                bulb.Colour = colour;
            }

            StorageHelper.SaveToStorage();
            Helper.HideProgressIndicator();
        }

        #endregion

        // Event handlers for when the colour sliders have changed
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

        private async void PowerStateAppBarToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            Helper.ShowProgressIndicator("Turning on...");

            foreach (LifxBulb bulb in StorageHelper.SelectedBulbs)
            {
                LifxPowerStateMessage psm = await bulb.SetPowerStateCommand(LifxPowerState.On);
                await HandlePowerStateMessage(psm, bulb);
            }

            Helper.HideProgressIndicator();
        }

        private async void PowerStateAppBarToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            Helper.ShowProgressIndicator("Turning off...");

            SetControlState(false);

            foreach (LifxBulb bulb in StorageHelper.SelectedBulbs)
            {
                LifxPowerStateMessage psm = await bulb.SetPowerStateCommand(LifxPowerState.Off);
                await HandlePowerStateMessage(psm, bulb);
            }

            Helper.HideProgressIndicator();
        }

        //private async void PowerToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        //{
        //    if (((ToggleSwitch)sender).IsOn)
        //    {
        //        Helper.ShowProgressIndicator("Turning on...");

        //        foreach (LifxBulb bulb in StorageHelper.SelectedBulbs)
        //        {
        //            LifxPowerStateMessage psm = await bulb.SetPowerStateCommand(LifxPowerState.On);
        //            await HandlePowerStateMessage(psm, bulb);
        //        }

        //        Helper.HideProgressIndicator();
        //    }
        //    else
        //    {
        //        Helper.ShowProgressIndicator("Turning off...");

        //        SetControlState(false);

        //        foreach (LifxBulb bulb in StorageHelper.SelectedBulbs)
        //        {
        //            LifxPowerStateMessage psm = await bulb.SetPowerStateCommand(LifxPowerState.Off);
        //            await HandlePowerStateMessage(psm, bulb);
        //        }

        //        Helper.HideProgressIndicator();
        //    }
        //}

        private async void SyncBulbAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            Helper.ShowProgressIndicator("Syncing...");

            foreach (LifxBulb bulb in StorageHelper.SelectedBulbs)
            {
                LifxLightStatusMessage lsm = await bulb.GetLightStatusCommand();
                PowerStateAppBarToggleButton.IsChecked = lsm.PowerState == LifxPowerState.On ? true : false;
                //PowerToggleSwitch.IsOn = lsm.PowerState == LifxPowerState.On ? true : false;

                if (StorageHelper.SelectedType == SelectionType.IndividualBulb)
                    MainPivot.Title = APP_HEADING_TEXT + lsm.Label;

                bulb.Colour = new LifxColour() { Hue = lsm.Hue, Saturation = lsm.Saturation, Luminosity = lsm.Lumnosity, Kelvin = lsm.Kelvin };
                bulb.Label = lsm.Label;
                bulb.Tags = lsm.Tags;
                bulb.IsOn = lsm.PowerState;
            }

            StorageHelper.SaveToStorage();
            Helper.HideProgressIndicator();
        }

        private async void EditBulbAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (StorageHelper.SelectedType == SelectionType.IndividualBulb)
            {
                EditBulbDialog editBulbDialog = new EditBulbDialog();
                ((editBulbDialog.Content as StackPanel).Children[0] as TextBox).Text = StorageHelper.SelectedBulbs[0].MACAddress;
                ((editBulbDialog.Content as StackPanel).Children[1] as TextBox).Text = StorageHelper.SelectedBulbs[0].Label;
                editBulbDialog.Closed += EditBulbDialog_Closed;
                ContentDialogResult result = await editBulbDialog.ShowAsync();
            }
            else
            {
                MessageDialog mbox = new MessageDialog(StorageHelper.ErrorMessages.GetString("EditBulbLabelGroup"), "Cannot Edit Groups");
                await mbox.ShowAsync();
            }
        }

        async void EditBulbDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            LifxLabelMessage llm = await StorageHelper.SelectedBulbs[0].SetLabelCommand(((sender.Content as StackPanel).Children[1] as TextBox).Text);
            StorageHelper.SelectedBulbs[0].Label = llm.BulbLabel;
            StorageHelper.SaveToStorage();

            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (StorageHelper.SelectedType == SelectionType.IndividualBulb)
                        MainPivot.Title = APP_HEADING_TEXT + llm.BulbLabel;
                });
        }

        //private async void BulbListComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    Helper.ShowProgressIndicator("Getting bulb info...");

        //    SyncBulbAppBarButton.IsEnabled = true;
        //    EditBulbAppBarButton.IsEnabled = true;

        //    LifxPowerStateMessage psm = await StorageHelper.GetBulb((e.AddedItems[0] as ComboBoxItem).Tag as Byte[]).GetPowerStateCommand();
        //    await HandlePowerStateMessage(psm);

        //    Helper.HideProgressIndicator();
        //}

        private void SettingsAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage));
        }

        //private async void ChooseColourButton_Click(object sender, RoutedEventArgs e)
        //{
        //    ColourChooserDialog colourChooserDialog = new ColourChooserDialog();
        //    colourChooserDialog.Closed += ColourChooserDialog_Closed;
        //    await colourChooserDialog.ShowAsync();
        //}

        //async void ColourChooserDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        //{
        //    if (args.Result == ContentDialogResult.Primary)
        //    {
        //        Helper.ShowProgressIndicator("Setting colour...");

        //        CheckBox fadeCheckBox = (sender.Content as StackPanel).Children[0] as CheckBox;
        //        ComboBoxItem selectedItem = ((sender.Content as StackPanel).Children[1] as ComboBox).SelectedItem as ComboBoxItem;

        //        LifxColour colour = LifxColour.ColorToLifxColour((selectedItem.Tag as RGBColour).Colour);
        //        SetSliders(colour);

        //        foreach (LifxBulb bulb in StorageHelper.SelectedBulbs)
        //        {
        //            if (fadeCheckBox.IsChecked.Value == true)
        //                await bulb.SetWaveformCommand(0, 0, colour, Convert.ToUInt32(FadeTimeTextBox.Text) * 1000, 0f, 0, Waveform.Sine);
        //            else
        //                await bulb.SetColorCommand(colour, 0);

        //            bulb.Colour = colour;
        //        }

        //        StorageHelper.SaveToStorage();
        //        Helper.HideProgressIndicator();
        //    }
        //}

        private void BulbControllerAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(BulbSelector));
        }

        private void MainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainPivot.SelectedIndex == 0)
            {
                BottomAppBar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;

                BulbControllerAppBarButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                EditBulbAppBarButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                SyncBulbAppBarButton.Visibility = Windows.UI.Xaml.Visibility.Visible;

                PowerStateAppBarToggleButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                FadeColourAppBarButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else if (MainPivot.SelectedIndex == 1)
            {
                BottomAppBar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;

                BulbControllerAppBarButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                EditBulbAppBarButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                SyncBulbAppBarButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                PowerStateAppBarToggleButton.Visibility = Windows.UI.Xaml.Visibility.Visible;

                FadeColourAppBarButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else if (MainPivot.SelectedIndex == 2)
            {
                BottomAppBar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;

                FadeColourAppBarButton.Visibility = Windows.UI.Xaml.Visibility.Visible;

                BulbControllerAppBarButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                EditBulbAppBarButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                PowerStateAppBarToggleButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                SyncBulbAppBarButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private async void LightPowerStateImage_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (((Image)sender).Tag.ToString() == LifxPowerState.Off.ToString())
            {
                Helper.ShowProgressIndicator("Turning on...");

                foreach (LifxBulb bulb in StorageHelper.SelectedBulbs)
                {
                    LifxPowerStateMessage psm = await bulb.SetPowerStateCommand(LifxPowerState.On);
                    await HandlePowerStateMessage(psm, bulb);
                }

                Helper.HideProgressIndicator();
            }
            else
            {
                Helper.ShowProgressIndicator("Turning off...");

                SetControlState(false);

                foreach (LifxBulb bulb in StorageHelper.SelectedBulbs)
                {
                    LifxPowerStateMessage psm = await bulb.SetPowerStateCommand(LifxPowerState.Off);
                    await HandlePowerStateMessage(psm, bulb);
                }

                Helper.HideProgressIndicator();
            }
        }

        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            LifxWiFiFirmwareMessage wifi = await StorageHelper.SelectedBulbs[0].GetWifiFirmwareState();
        }
    }
}