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
using System.Net;
using System.IO;

namespace mousehack
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        UsbWrapper device = new UsbWrapper(0x047, 0x045e); // pid и vid нужной мыши

        public MainWindow()
        {
            InitializeComponent();

            if (device.Open())
            {
                device.ButtonPressed += device_ButtonPressed;
                device.ButtonReleased += device_ButtonReleased;
                device.StartThread();
            }
            else
            {
                // fail
            }
        }

        void device_ButtonPressed(object sender, UsbWrapper.ButtonEventArgs e)
        {
            MessageBox.Show(string.Format("Button {0} pressed", e.button));

            WebRequest request = WebRequest.Create("http://example.com/?button=" + e.button);
            request.GetResponse();
        }

        void device_ButtonReleased(object sender, UsbWrapper.ButtonEventArgs e)
        {
            MessageBox.Show(string.Format("Button {0} released", e.button));
        }
    }
}
