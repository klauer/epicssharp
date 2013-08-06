using System.Windows;

namespace GatewayDebugWindow
{
    /// <summary>
    /// Interaction logic for GatewayChooser.xaml
    /// </summary>
    public partial class GatewayChooser : Window
    {
        public string Gateway
        {
            get
            {
                return txtGateway.Text;
            }
            set
            {
                txtGateway.Text = value;
            }
        }

        public GatewayChooser()
        {
            InitializeComponent();
        }

        private void BtnOkClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancelClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
