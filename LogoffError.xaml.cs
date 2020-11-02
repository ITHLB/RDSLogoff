using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RDS_Abmelder
{
    /// <summary>
    /// Interaktionslogik für LogoffError.xaml
    /// </summary>
    public partial class LogoffError : Window
    {
        public LogoffError(logoffReturn LogoffErrorDetails)
        {
            InitializeComponent();
            this.DataContext = LogoffErrorDetails;
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
