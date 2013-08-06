using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GatewayDebugData;
using PSI.EpicsClient2;
using System.Collections.Generic;

namespace GatewayDebugWindow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DebugContext context;
        EpicsClient epicsClient;

        readonly string[] channelNames = new string[] {
                ":VERSION",
                ":BUILD",
                ":CPU",
                ":MEM-FREE",
                ":NBCLIENTS",
                ":NBSERVERS",
                ":PVTOTAL",
                ":MONITORS",
                ":SEARCH-SEC",
                ":MESSAGES-SEC"
            };

        readonly Dictionary<string, TextBlock> channelDisplay = new Dictionary<string, TextBlock>();
        readonly List<EpicsChannel<string>> channels = new List<EpicsChannel<string>>();

        public MainWindow()
        {
            InitializeComponent();
            channelDisplay.Add(":VERSION", txtVersion);
            channelDisplay.Add(":BUILD", txtBuild);
            channelDisplay.Add(":CPU", txtCPU);
            channelDisplay.Add(":MEM-FREE", txtMEM);
            channelDisplay.Add(":NBCLIENTS", txtTotClients);
            channelDisplay.Add(":NBSERVERS", txtTotIocs);
            channelDisplay.Add(":PVTOTAL", txtPVs);
            channelDisplay.Add(":MONITORS", txtMonitors);
            channelDisplay.Add(":SEARCH-SEC", txtSearchPerSec);
            channelDisplay.Add(":MESSAGES-SEC", txtMessagesPerSec);
        }


        void Start()
        {
            GatewayChooser dlg = new GatewayChooser();
            dlg.Owner = this;
            dlg.Gateway = (string)Properties.Settings.Default["StoredGateway"];
            if (dlg.ShowDialog() == false)
            {
                Application.Current.Shutdown();
            }
            Properties.Settings.Default["StoredGateway"] = dlg.Gateway;
            Properties.Settings.Default.Save();

            context = new DebugContext(dlg.Gateway);
            context.RefreshAllIocs += new RefreshAllDelegate(CtxRefreshAllIocs);
            context.NewIocChannel += new NewConnectionChannelDelegate(CtxNewIocChannel);
            context.DropIoc += new DropConnectionDelegate(CtxDropIoc);
            context.RefreshAllClients += new RefreshAllDelegate(ContextRefreshAllClients);
            context.NewClientChannel += new NewConnectionChannelDelegate(ContextNewClientChannel);
            context.DropClient += new DropConnectionDelegate(ContextDropClient);
            context.ConnectionState += new ContextConnectionDelegate(ContextConnectionState);
            context.DebugLog += new DebugLogDelegate(ContextDebugLog);
            context.NewName += new NewGatewayNameDelegate(ContextNewName);
            context.DebugLevel += new DebugLevelDelegate(ContextDebugLevel);
        }

        void ContextDebugLevel(DebugContext ctx, bool fullLogs)
        {
            this.Dispatcher.BeginInvoke((Action)delegate
            {
                chkFullLogs.IsChecked = fullLogs;
            });
        }

        void ContextNewName(DebugContext ctx, string name)
        {
            this.Dispatcher.BeginInvoke((Action)delegate
            {
                if (epicsClient != null)
                {
                    foreach (var i in channels)
                    {
                        i.Dispose();
                    }
                    channels.Clear();
                    epicsClient.Dispose();
                }
                txtGatewayName.Text = name;

                epicsClient = new EpicsClient();
                epicsClient.Configuration.SearchAddress = (string)Properties.Settings.Default["StoredGateway"];

                foreach (var i in channelNames)
                {
                    EpicsChannel<string> channel = epicsClient.CreateChannel<string>(name + i);
                    channels.Add(channel);
                    channel.MonitorChanged += new EpicsDelegate<string>(ChannelMonitorChanged);
                }
            });
        }

        void ChannelMonitorChanged(EpicsChannel<string> sender, string newValue)
        {
            this.Dispatcher.BeginInvoke((Action)delegate
            {
                channelDisplay.First(row => sender.ChannelName.EndsWith(row.Key)).Value.Text = newValue;
            });
        }

        void ContextDebugLog(string source, System.Diagnostics.TraceEventType eventType, int chainId, string message)
        {
            // ReSharper disable ConvertToLambdaExpression
            this.Dispatcher.BeginInvoke((Action)delegate
            // ReSharper restore ConvertToLambdaExpression
            {
                lstLog.Children.Add(new LogEntry { Date = DateTime.Now.ToString(CultureInfo.InvariantCulture), Source = source, Message = message, EventType = eventType });
                while(lstLog.Children.Count > 300)
                    lstLog.Children.RemoveAt(0);
                scrLog.ScrollToBottom();
            });
        }

        void ContextDropClient(DebugContext ctx, string host)
        {
            ContextRefreshAllClients(ctx);
        }

        void ContextNewClientChannel(DebugContext ctx, string client, string channel)
        {
            this.Dispatcher.BeginInvoke((Action)delegate
            {
                bool cIsKnown = lstClients.Items.Cast<TextBlock>().Any(i => i.Text == client);

                if (!cIsKnown)
                    lstClients.Items.Add(new TextBlock { Text = client });

                if (lstClients.SelectedIndex != -1 && ((TextBlock)lstClients.SelectedItem).Text == client)
                {
                    lstClientsChannels.Items.Add(new TextBlock { Text = channel });
                }
            });
        }

        void ContextRefreshAllClients(DebugContext ctx)
        {
            this.Dispatcher.BeginInvoke((Action)delegate
            {
                lstClientsChannels.Items.Clear();
                lstClients.Items.Clear();

                foreach (var i in ctx.Clients)
                {
                    lstClients.Items.Add(new TextBlock { Text = i.Name });
                }
                lstClients.SelectedIndex = -1;
            }, new object[] { });
        }

        void ContextConnectionState(DebugContext ctx, System.Data.ConnectionState state)
        {
            this.Dispatcher.BeginInvoke((Action)delegate
            {
                txtStatus.Text = state.ToString();
            });
        }

        void CtxDropIoc(DebugContext ctx, string ioc)
        {
            CtxRefreshAllIocs(ctx);
        }

        void CtxNewIocChannel(DebugContext ctx, string ioc, string channel)
        {
            this.Dispatcher.BeginInvoke((Action)delegate
            {
                bool iocIsKnown = lstIocs.Items.Cast<TextBlock>().Any(i => i.Text == ioc);
                if (!iocIsKnown)
                {
                    lstIocs.Items.Add(new TextBlock { Text = ioc });
                }

                if (lstIocs.SelectedIndex != -1 && ((TextBlock)lstIocs.SelectedItem).Text == ioc)
                {
                    lstChannels.Items.Add(new TextBlock { Text = channel });
                }
            });
        }

        void CtxRefreshAllIocs(DebugContext ctx)
        {
            this.Dispatcher.BeginInvoke((Action)delegate
            {
                lstChannels.Items.Clear();
                lstIocs.Items.Clear();

                foreach (var i in ctx.Iocs)
                {
                    lstIocs.Items.Add(new TextBlock { Text = i.Name });
                }
                lstIocs.SelectedIndex = -1;
            }, new object[] { });
        }

        private void LstIocsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstIocs.SelectedIndex != -1)
            {
                string ioc = ((TextBlock)lstIocs.SelectedItem).Text;
                lstChannels.Items.Clear();
                foreach (var i in context.Iocs.GetByName(ioc).GetChannels())
                {
                    lstChannels.Items.Add(new TextBlock { Text = i });
                }
            }
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
            Start();
        }

        private void LstClientsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstClients.SelectedIndex != -1)
            {
                string client = ((TextBlock)lstClients.SelectedItem).Text;
                lstClientsChannels.Items.Clear();
                foreach (var i in context.Clients.GetByName(client).GetChannels())
                {
                    lstClientsChannels.Items.Add(new TextBlock { Text = i });
                }
            }
        }

        private void ChkFullLogsChecked(object sender, RoutedEventArgs e)
        {
            context.FullLogs = chkFullLogs.IsChecked.Value;
        }
    }
}
