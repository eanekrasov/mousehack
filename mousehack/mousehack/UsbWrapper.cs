﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.Main;
using System.Collections.ObjectModel;
using System.Windows;
using System.Threading;

namespace mousehack
{
    class UsbWrapper
    {
        private const int READ_TIMEOUT = 1000; // 1 секунда таймаута на чтение данных из мыши.
        private const int DATA_LENGTH = 4;
        private UsbDevice usbDevice = null;
        private int pid;
        private int vid;
        private UsbEndpointReader reader = null;
        private Thread bg = null;

        public sealed class ButtonEventArgs: EventArgs {
            public byte button;

            public ButtonEventArgs(byte button)
            {
                this.button = button;
            }
        }

        public event EventHandler<ButtonEventArgs> ButtonPressed;

        public event EventHandler<ButtonEventArgs> ButtonReleased;
 
        protected virtual void OnButtonPressed(ButtonEventArgs e)
        {
            EventHandler<ButtonEventArgs> handler = ButtonPressed;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnButtonReleased(ButtonEventArgs e)
        {
            EventHandler<ButtonEventArgs> handler = ButtonReleased;
            if (handler != null)
            {
                handler(this, e);
            }
        }
            
        public UsbWrapper(int vid, int pid) {
            this.pid = pid;
            this.vid = vid;
        }

        ~UsbWrapper()
        {
            if (bg != null)
            {
                StopThread();
            }

            Close();
        }

        private Boolean isOpen()
        {
            return (usbDevice != null) && (usbDevice.IsOpen);
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

        public void StartThread()
        {
            if (bg == null)
            {
                bg = new Thread(Read);
                bg.IsBackground = true;
                bg.Start();
            }
        }

        public void StopThread()
        {
            if (bg != null)
            {
                bg.Abort();
                bg.Join();
                bg = null;
            }
        }

        private Boolean isBitStateRising(byte which, byte from, byte to)
        {
            byte mask = (byte)(1 << (which - 1));
            return ((from != to) && (from & mask) == 0) && ((to & mask) > 0);
        }

        private void Read()
        {
            ErrorCode ec = ErrorCode.None;
            byte[] readBuffer = new byte[DATA_LENGTH];
            int bytesRead = -1;
            byte oldValue = 0;

            try
            {
                // Немножко хак. Очиста буфера перед использованием.
                while (bytesRead != 0)
                {
                    ec = reader.Read(readBuffer, 0, DATA_LENGTH, READ_TIMEOUT, out bytesRead);
                }

                while (true)
                {
                    ec = reader.Read(readBuffer, 0, DATA_LENGTH, READ_TIMEOUT, out bytesRead);

                    // readBuffer[0] показывает нажатые кнопки. 1 - первая, 2 - вторая, 3 - первая и вторая и т.д.
                    if (bytesRead > 0) {
                        byte newValue = readBuffer[0];
                        if (newValue != oldValue)
                        {
                            for (byte i = 1; i < 3; i++)
                            {
                                if (isBitStateRising(i, oldValue, newValue)) OnButtonPressed(new ButtonEventArgs(i));
                                if (isBitStateRising(i, newValue, oldValue)) OnButtonReleased(new ButtonEventArgs(i));
                            }

                            oldValue = readBuffer[0];
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
        }
    }
}
