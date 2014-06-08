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
using Windows.UI.ViewManagement;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Media.SpeechRecognition;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace LightCTRL
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class VoiceCommandPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public VoiceCommandPage()
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
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);

            SpeechRecognitionResult speechRecognitionResult = e.Parameter as SpeechRecognitionResult;

            string voiceCommandName = speechRecognitionResult.RulePath[0];
            string navigationTarget = speechRecognitionResult.SemanticInterpretation.Properties["NavigationTarget"][0];
            IReadOnlyDictionary<string, IReadOnlyList<string>> properties = speechRecognitionResult.SemanticInterpretation.Properties;
            UriBuilder uri = null;
            MessageDialog mbox = null;

            switch (voiceCommandName)
            {
                case "ChangeAllLightState":
                    {
                        StorageHelper.SelectedBulbs = StorageHelper.Bulbs;

                        foreach (LifxBulb bulb in StorageHelper.SelectedBulbs)
                        {
                            await bulb.SetPowerStateCommand(properties["LightState"][0]);
                        }
                    }
                    break;
                case "ChaneOneLightState":
                case "ChangeOneLightStateAlternate":
                    {
                        StorageHelper.SelectedBulb = StorageHelper.GetBulb(properties["BulbName"][0], true);
                        await StorageHelper.SelectedBulbs[0].SetPowerStateCommand(properties["LightState"][0]);
                    }
                    break;
                case "ChangeAllLightColour":
                    {
                        string colourname = properties["Colour"][0].Replace(" ", string.Empty);
                        
                        try
                        {
                            StorageHelper.SelectedBulbs = StorageHelper.Bulbs;
                            LifxColour colour = LifxColour.ColorToLifxColour(colourname);

                            foreach (LifxBulb bulb in StorageHelper.SelectedBulbs)
                            {
                                bulb.Colour = colour;
                                await bulb.SetColorCommand(colour, 0);
                            }
                        }
                        catch (ColorNotFoundException)
                        {
                            mbox = new MessageDialog(StorageHelper.ErrorMessages.GetString("ColourNotFound_Voice"), "Colour Not Found");
                        }
                    }
                    break;
                case "ChangeOneLightColour":
                    {
                        string colourname = properties["Colour"][0].Replace(" ", string.Empty);
                        string uriString = string.Empty;

                        try
                        {
                            LifxColour colour = LifxColour.ColorToLifxColour(colourname);
                            StorageHelper.SelectedBulb = StorageHelper.GetBulb(properties["BulbName"][0], true);

                            await StorageHelper.SelectedBulbs[0].SetColorCommand(LifxColour.ColorToLifxColour(colourname), 0);

                            //uriString = typeof(VoiceCommandPage).ToString() + "?BulbName=" + properties["BulbName"][0] + "&Colour=" + colourname;
                            //uri = new UriBuilder(uriString);
                        }
                        catch (BulbNotFoundException)
                        {
                            mbox = new MessageDialog(StorageHelper.ErrorMessages.GetString("BulbNotFound_Voice"), "Bulb Not Found");
                        }
                        catch (ColorNotFoundException)
                        {
                            mbox = new MessageDialog(StorageHelper.ErrorMessages.GetString("ColourNotFound_Voice"), "Colour Not Found");
                        }
                    }
                    break;
                default:
                    break;
            }

            if (mbox != null)
                await mbox.ShowAsync();

            if (StorageHelper.LocalSettings.HasFlag(Settings.CloseOnVoiceCommand))
                App.Current.Exit();
            else
            {
                Frame rootFrame = new Frame();
                SuspensionManager.RegisterFrame(rootFrame, "AppFrame");
                Window.Current.Content = rootFrame;
                rootFrame.Navigate(typeof(MainPage), uri);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion
    }
}
