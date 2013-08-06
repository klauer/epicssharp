using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows;

namespace GatewayDebugWindow
{
    /// <summary>
    /// Interaction logic for LogEntry.xaml
    /// </summary>
    public partial class LogEntry : UserControl
    {
        public LogEntry()
        {
            InitializeComponent();
        }

        public string Date
        {
            get
            {
                return txtDate.Text;
            }
            set
            {
                txtDate.Text = value;
            }
        }

        public string Source
        {
            get
            {
                return txtSource.Text;
            }
            set
            {
                txtSource.Text = value;
            }
        }

        public string Message
        {
            get
            {
                return txtMessage.Text;
            }
            set
            {
                txtMessage.Text = value;
            }
        }

        TraceEventType eventType = TraceEventType.Information;
        public TraceEventType EventType
        {
            get
            {
                return eventType;
            }
            set
            {
                eventType = value;
                switch (eventType)
                {
                    case TraceEventType.Critical:
                        txtMessage.Foreground = Brushes.Red;
                        txtMessage.FontWeight = FontWeights.Bold;
                        break;
                    case TraceEventType.Error:
                        txtMessage.Foreground = Brushes.Crimson;
                        txtMessage.FontWeight = FontWeights.Bold;
                        break;
                    case TraceEventType.Start:
                        txtMessage.Foreground = Brushes.DarkGreen;
                        txtMessage.FontWeight = FontWeights.Bold;
                        break;
                    case TraceEventType.Stop:
                        txtMessage.Foreground = Brushes.DarkRed;
                        txtMessage.FontWeight = FontWeights.Bold;
                        break;
                    default:
                        txtMessage.Foreground = Brushes.Black;
                        txtMessage.FontWeight = FontWeights.Normal;
                        break;
                }
            }
        }
    }
}
