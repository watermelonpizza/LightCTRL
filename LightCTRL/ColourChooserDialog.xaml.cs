using LIFX_Net;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers.Provider;
using Windows.UI.Xaml;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace LightCTRL_wp
{
    public sealed partial class ColourChooserDialog : ContentDialog
    {
        public ColourChooserDialog()
        {
            this.InitializeComponent();

            foreach (RGBColour colour in LifxColour.RGBColourList)
            {
                ColourComboBox.Items.Add(new ComboBoxItem() { Content = colour.Name, FontSize = 22, Margin = new Thickness { Left = 30, Top = 10 }, Tag = colour });
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (ColourComboBox.SelectedIndex < 0)
            {
                ColourComboBox.PlaceholderText = "please choose a colour :)";
                args.Cancel = true;
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
