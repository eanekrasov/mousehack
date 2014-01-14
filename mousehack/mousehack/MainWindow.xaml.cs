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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace mousehack
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        UsbWrapper device = new UsbWrapper(0x047, 0x045e); // pid и vid нужной мыши
        //InterceptMouse interceptor = new InterceptMouse();

        public MainWindow()
        {
            InitializeComponent();

            if (device.Open())
            {
                // success
            }
            else
            {
                // fail
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            device.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            byte buttons = device.Read();
            if ((buttons | 1) == buttons) {
                MessageBox.Show("1");
            }
            if ((buttons | 2) == buttons)
            {
                MessageBox.Show("2");
            }
        }
    }
}
