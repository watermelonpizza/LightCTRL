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

namespace LightCTRL
{
    public sealed partial class ColourChooserDialog : ContentDialog
    {
        public ColourChooserDialog()
        {
            this.InitializeComponent();

            foreach (RGBColour colour in LifxColour.RGBColourList)
            {
                SolidColorBrush colourBrush = new SolidColorBrush(colour.Colour);

                TextBlock colourTextBlock = new TextBlock()
                {
                    HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,
                    FontSize = 22,
                    Margin = new Thickness { Bottom = 1 },
                    Text = colour.Name,
                    Tag = colour.Colour
                };

                Rectangle colourRectangle = new Rectangle()
                {
                    Fill = new SolidColorBrush(((SolidColorBrush)this.Resources["ContentDialogBackgroundThemeBrush"]).Color),
                    HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,
                    Width = 200,
                };

                Line colourLine = new Line()
                {
                    Stroke = colourBrush,
                    StrokeThickness = 4,
                    X2 = 400,
                    Y1 = 15, 
                    Y2 = 15
                };

                Grid grid = new Grid();
                grid.Children.Add(colourLine);
                grid.Children.Add(colourRectangle);
                grid.Children.Add(colourTextBlock);

                ListViewItem colourListViewItem = new ListViewItem()
                {
                    VerticalContentAlignment = Windows.UI.Xaml.VerticalAlignment.Center,
                    BorderBrush = colourBrush,
                    BorderThickness = new Thickness { Left = 4, Right = 4 },
                    Content = grid,
                    
                };

                ColourListView.Items.Add(colourListViewItem);
            }

            //ColourComboBox.Items.Add(new ComboBoxItem()
            //{
            //    Content = colour.Name,
            //    FontSize = 22,
            //    Margin = new Thickness { Left = 30, Top = 10 },
            //    Tag = colour.Name,
            //    BorderThickness = new Thickness { Right = 10 },
            //    BorderBrush = new SolidColorBrush { Color = colour.Colour }
            //});
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            //if (ColourComboBox.SelectedIndex < 0)
            //{
            //    ColourComboBox.PlaceholderText = "please choose a colour :)";
            //    args.Cancel = true;
            //}
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
