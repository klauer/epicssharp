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
using System.Diagnostics;

namespace GWLogViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            logViewer1.Log = Log.ReadLog("C:\\temp\\gw.log");

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;
            logViewer1.Log = Log.ReadLog("C:\\temp\\gw.log");
            this.Cursor = null;
        }

        private void logViewer1_ChainClick(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Wait;
            int chainId=(int)sender;
            logViewer1.Log = Log.ReadLog("C:\\temp\\gw.log", row => row.ChainId == chainId);
            this.Cursor = null;
        }

        private void logViewer1_EventTypeClick(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Wait;
            TraceEventType eventType = (TraceEventType)sender;
            logViewer1.Log = Log.ReadLog("C:\\temp\\gw.log", row => row.EventType == eventType);
            this.Cursor = null;
        }
    }
}
