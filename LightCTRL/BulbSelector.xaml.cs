using LIFX_Net;
using LIFX_Net.Messages;

using LightCTRL.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace LightCTRL
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BulbSelector : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private bool powerStateTapped = false;

        public BulbSelector()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
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

            PopulateList();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void PopulateList()
        {
            BulbListView.Items.Clear();

            BulbListView.Items.Add(new ListViewItem()
            {
                Margin = new Thickness { Bottom = 12 },
                Height = 50,
                VerticalContentAlignment = Windows.UI.Xaml.VerticalAlignment.Center,
                BorderBrush = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness { Bottom = 2 },
                Content = new TextBlock()
                {
                    Margin = new Thickness { Left = 8 },
                    FontSize = 26,
                    Text = "all bulbs"
                }
            });

            foreach (ulong tag in StorageHelper.Tags)
            {
                ListViewItem tagsHeader = new ListViewItem()
                {
                    Margin = new Thickness { Bottom = 12, Top = 8 },
                    Height = 50,
                    VerticalContentAlignment = Windows.UI.Xaml.VerticalAlignment.Center,
                    BorderBrush = new SolidColorBrush(Colors.White),
                    BorderThickness = new Thickness { Bottom = 1 },
                    Content = new TextBlock()
                    {
                        Margin = new Thickness{ Left = 4 },
                        FontSize = 22,
                        Text = tag.ToString()
                    }
                };

                tagsHeader.Tapped += tagsHeader_Tapped;

                BulbListView.Items.Add(tagsHeader);

                foreach (LIFX_Net.LifxBulb bulb in StorageHelper.GetBulbs(tag))
                {
                    TextBlock bulbLabel = new TextBlock()
                    {
                        Margin = new Thickness { Left = 46 },
                        FontSize = 22,
                        VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center,
                        Text = bulb.Label
                    };

                    //bulbLabel.Inlines.Add(new Run() { Text = bulb.Label, Foreground = new SolidColorBrush(LifxColour.ToRgbColour(bulb.Colour)) });

                    TextBlock bulbPowerState = new TextBlock()
                    {
                        HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Right,
                        Margin = new Thickness { Right = 10 },
                        FontSize = 22,
                        VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center,
                        Tag = bulb.UID
                    };

                    //Rectangle colourRectangle = new Rectangle()
                    //{
                    //    Fill = new SolidColorBrush(LifxColour.LifxColourToColor(bulb.Colour)),
                    //    HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left,
                    //    Margin = new Thickness { Left = 12 },
                    //    Height = 22,
                    //    Width = 22
                    //};

                    bulbPowerState.Inlines.Add(new Run()
                    {
                        Text = bulb.IsOn.ToString(),
                        Foreground = bulb.IsOn == LifxPowerState.On ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Gray)
                    });
                    bulbPowerState.Tapped += bulbPowerState_Tapped;

                    Grid grid = new Grid() { Width = 300 };
                    grid.Children.Add(bulbLabel);
                    //grid.Children.Add(colourRectangle);
                    grid.Children.Add(bulbPowerState);

                    ListViewItem bulbItem = new ListViewItem()
                    {
                        Margin = new Thickness { Left = 22, Top = 8, Bottom = 8 },
                        Height = 30,
                        VerticalContentAlignment = Windows.UI.Xaml.VerticalAlignment.Center,
                        Content = grid,
                        BorderBrush = new SolidColorBrush(LifxColour.LifxColourToColor(bulb.Colour)),
                        BorderThickness = new Thickness { Left = 30 },
                    };

                    bulbItem.Tapped += bulbItem_Tapped;

                    BulbListView.Items.Add(bulbItem);
                }
            }
        }

        async void bulbPowerState_Tapped(object sender, TappedRoutedEventArgs e)
        {
            powerStateTapped = true;

            TextBlock powerTextBlock = sender as TextBlock;

            if (powerTextBlock != null)
            {
                LifxBulb bulb = StorageHelper.GetBulb(powerTextBlock.Tag as Byte[]);
                LIFX_Net.Messages.LifxPowerStateMessage psm = await bulb.SetPowerStateCommand(powerTextBlock.Text == LifxPowerState.On.ToString() ? LifxPowerState.Off : LifxPowerState.On);
 
                powerTextBlock.Inlines.Clear();
                powerTextBlock.Inlines.Add(new Run()
                {
                    Text = psm.PowerState.ToString(),
                    Foreground = psm.PowerState == LifxPowerState.On ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Gray)
                });

                bulb.IsOn = psm.PowerState;
                StorageHelper.SaveToStorage();
            }
        }

        void bulbItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!powerStateTapped)
            {
                TextBlock bulbItem = e.OriginalSource as TextBlock;

                if (bulbItem != null)
                {
                    StorageHelper.SelectedBulb = StorageHelper.GetBulb(bulbItem.Text, true);
                    Frame.Navigate(typeof(MainPage), typeof(BulbSelector).ToString());
                }
            }
            else
                powerStateTapped = false;
        }

        void tagsHeader_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!powerStateTapped)
            {
                TextBlock tagHeader = e.OriginalSource as TextBlock;

                if (tagHeader != null)
                {
                    StorageHelper.SelectedBulbs = StorageHelper.GetBulbs(Convert.ToUInt64(tagHeader.Text));

                    Frame.Navigate(typeof(MainPage), typeof(BulbSelector).ToString());
                }
            }
            else
                powerStateTapped = false;
        }

        private async void SyncBulbAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            Helper.ShowProgressIndicator("Syncing...");

            foreach (LIFX_Net.LifxBulb bulb in StorageHelper.Bulbs)
            {
                LIFX_Net.Messages.LifxLightStatusMessage lsm = await bulb.GetLightStatusCommand();

                bulb.Colour = new LIFX_Net.LifxColour() { Hue = lsm.Hue, Saturation = lsm.Saturation, Luminosity = lsm.Lumnosity, Kelvin = lsm.Kelvin };
                bulb.Label = lsm.Label;
                bulb.Tags = lsm.Tags;
                bulb.IsOn = lsm.PowerState;
            }

            PopulateList();

            Helper.HideProgressIndicator();
        }

        private void SettingsAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage));
        }
    }
}
