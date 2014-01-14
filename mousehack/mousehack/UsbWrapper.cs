using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.Main;
using System.Collections.ObjectModel;
using System.Windows;

namespace mousehack
{
    class UsbWrapper
    {
        private UsbDevice usbDevice = null;
        private int pid;
        private int vid;
        private UsbEndpointReader reader = null;

        private Boolean isOpen()
        {
            return (usbDevice != null) && (usbDevice.IsOpen);
        }

        public UsbWrapper(int vid, int pid) {
            this.pid = pid;
            this.vid = vid;
        }
            
        public Boolean Open()
        {
            if (!isOpen()) {
                usbDevice = UsbDevice.OpenUsbDevice(new UsbDeviceFinder(pid, vid));
                if (usbDevice != null)
                {
                    // If this is a "whole" usb device (libusb-win32, linux libusb-1.0)
                    // it exposes an IUsbDevice interface. If not (WinUSB) the 
                    // 'wholeUsbDevice' variable will be null indicating this is 
                    // an interface of a device; it does not require or support 
                    // configuration and interface selection.
                    IUsbDevice wholeUsbDevice = usbDevice as IUsbDevice;

                    if (!ReferenceEquals(wholeUsbDevice, null))
                    {
                        // This is a "whole" USB device. Before it can be used, 
                        // the desired configuration and interface must be selected.

                        // Select config #1
                        wholeUsbDevice.SetConfiguration(1);

                        // Claim interface #0.
                        wholeUsbDevice.ClaimInterface(0);
                    }

                    // open read endpoint 1.
                    reader = usbDevice.OpenEndpointReader(ReadEndpointID.Ep01);
                }
            }

            return isOpen();
        }

        public void Close()
        {
            if (usbDevice != null)
            {
                if (usbDevice.IsOpen)
                {
                    // If this is a "whole" usb device (libusb-win32, linux libusb-1.0)
                    // it exposes an IUsbDevice interface. If not (WinUSB) the 
                    // 'wholeUsbDevice' variable will be null indicating this is 
                    // an interface of a device; it does not require or support 
                    // configuration and interface selection.
                    IUsbDevice wholeUsbDevice = usbDevice as IUsbDevice;
                    if (!ReferenceEquals(wholeUsbDevice, null))
                    {
                        // Release interface #0.
                        wholeUsbDevice.ReleaseInterface(0);
                    }

                    usbDevice.Close();
                }
                usbDevice = null;

                // Free usb resources
                UsbDevice.Exit();
            }
        }

        public byte Read()
        {
            ErrorCode ec = ErrorCode.None;
            byte[] readBuffer = new byte[4];

            int bytesRead;
            // If the device hasn't sent data in the last 1 second,
            // a timeout error (ec = IoTimedOut) will occur. 
            ec = reader.Read(readBuffer, 0, 4, 1000, out bytesRead);

            // readBuffer[0] показывает нажатые кнопки. 1 - первая, 2 - вторая, 3 - первая и вторая и т.д.
            return (bytesRead > 0) ? readBuffer[0] : (byte)0;
        }
    }
}
