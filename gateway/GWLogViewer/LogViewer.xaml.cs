using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GWLogViewer
{
    /// <summary>
    /// Interaction logic for LogViewer.xaml
    /// </summary>
    public partial class LogViewer : UserControl
    {
        public event EventHandler EventTypeClick;
        public event EventHandler ChainClick;

        public LogViewer()
        {
            InitializeComponent();
        }

        Log currentLog;
        public Log Log
        {
            get
            {
                return currentLog;
            }
            set
            {
                currentLog = value;
                Redraw();
            }
        }

        void Redraw()
        {
            mainGrid.Children.Clear();
            mainGrid.RowDefinitions.Clear();

            SolidColorBrush background = Brushes.White;
            SolidColorBrush foreground = Brushes.Black;
            FontWeight weight = FontWeights.Normal;

            TextBlock t;

            string[] titles = new string[] { "Date:", "Title:", "Type:", "Sender:", "ChainId:", "Message" };
            int col = 0;
            foreach (string title in titles)
            {
                t = new TextBlock { Text = title, Padding = new Thickness { Left = 2, Right = 2 }, Background = Brushes.Black, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
                t.SetValue(Grid.RowProperty, 0);
                t.SetValue(Grid.ColumnProperty, col);
                mainGrid.Children.Add(t);
                col++;
            }

            int rowNumber = 1;
            foreach (LogEntry entry in currentLog)
            {
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                if (rowNumber % 2 == 0)
                    background = Brushes.Wheat;
                else
                    background = Brushes.White;

                switch (entry.EventType)
                {
                    case System.Diagnostics.TraceEventType.Critical:
                    case System.Diagnostics.TraceEventType.Error:
                        foreground = Brushes.Red;
                        break;
                    case System.Diagnostics.TraceEventType.Information:
                    case System.Diagnostics.TraceEventType.Resume:
                    case System.Diagnostics.TraceEventType.Suspend:
                    case System.Diagnostics.TraceEventType.Transfer:
                    case System.Diagnostics.TraceEventType.Verbose:
                        foreground = Brushes.Black;
                        break;
                    case System.Diagnostics.TraceEventType.Start:
                        foreground = Brushes.Green;
                        break;
                    case System.Diagnostics.TraceEventType.Stop:
                        foreground = Brushes.LightSalmon;
                        break;
                    case System.Diagnostics.TraceEventType.Warning:
                        foreground = Brushes.Salmon;
                        break;
                    default:
                        break;
                }
                if (foreground == Brushes.Black)
                    weight = FontWeights.Normal;
                else
                    weight = FontWeights.Black;

                t = new TextBlock { Text = entry.Date.ToShortDateString(), Padding = new Thickness { Left = 2, Right = 2 }, Background = background, FontWeight = weight, Foreground = foreground };
                t.SetValue(Grid.RowProperty, rowNumber);
                t.SetValue(Grid.ColumnProperty, 0);
                mainGrid.Children.Add(t);

                t = new TextBlock { Text = entry.Date.ToShortTimeString(), Padding = new Thickness { Left = 2, Right = 2 }, Background = background, FontWeight = weight, Foreground = foreground };
                t.SetValue(Grid.RowProperty, rowNumber);
                t.SetValue(Grid.ColumnProperty, 1);
                mainGrid.Children.Add(t);

                t = new TextBlock { Text = entry.EventType.ToString(), Padding = new Thickness { Left = 2, Right = 2 }, Background = background, FontWeight = weight, Foreground = foreground };
                t.MouseEnter += new MouseEventHandler(t_MouseEnter);
                t.MouseLeave += new MouseEventHandler(t_MouseLeave);
                t.MouseUp += new MouseButtonEventHandler(EventType_MouseUp);
                t.SetValue(Grid.RowProperty, rowNumber);
                t.SetValue(Grid.ColumnProperty, 2);
                mainGrid.Children.Add(t);

                t = new TextBlock { Text = entry.Sender, Padding = new Thickness { Left = 2, Right = 2 }, Background = background, FontWeight = weight, Foreground = foreground };
                t.SetValue(Grid.RowProperty, rowNumber);
                t.SetValue(Grid.ColumnProperty, 3);
                mainGrid.Children.Add(t);

                t = new TextBlock { Text = entry.ChainId.ToString(), Padding = new Thickness { Left = 2, Right = 2 }, Background = background, FontWeight = weight, Foreground = foreground };
                t.MouseEnter += new MouseEventHandler(t_MouseEnter);
                t.MouseLeave += new MouseEventHandler(t_MouseLeave);
                t.MouseUp += new MouseButtonEventHandler(Chain_MouseUp);
                t.SetValue(Grid.RowProperty, rowNumber);
                t.SetValue(Grid.ColumnProperty, 4);
                mainGrid.Children.Add(t);

                t = new TextBlock { Text = entry.Message, Padding = new Thickness { Left = 2, Right = 2 }, Background = background, FontWeight = weight, Foreground = foreground };
                t.SetValue(Grid.RowProperty, rowNumber);
                t.SetValue(Grid.ColumnProperty, 5);
                mainGrid.Children.Add(t);
                rowNumber++;
            }
        }

        void EventType_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TextBlock t = (TextBlock)sender;
            if (EventTypeClick != null)
                EventTypeClick((System.Diagnostics.TraceEventType)Enum.Parse(typeof(System.Diagnostics.TraceEventType), t.Text), null);
        }

        void Chain_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TextBlock t = (TextBlock)sender;
            if (ChainClick != null)
                ChainClick(int.Parse(t.Text), null);
        }

        void t_MouseLeave(object sender, MouseEventArgs e)
        {
            TextBlock t = (TextBlock)sender;
            t.TextDecorations = null;
            if ((int)t.GetValue(Grid.RowProperty) % 2 == 0)
                t.Background = Brushes.Wheat;
            else
                t.Background = Brushes.White;
        }

        void t_MouseEnter(object sender, MouseEventArgs e)
        {
            TextBlock t = (TextBlock)sender;
            t.TextDecorations = TextDecorations.Underline;
            t.Background = Brushes.SkyBlue;
        }
    }
}
