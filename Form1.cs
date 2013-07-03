using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BluetoothWindowsClient
{
    public partial class Form1 : Form
    {
        BluetoothClient _bluetoothClient = new BluetoothClient();
        NetworkStream _bluetoothStream;

        public Form1()
        {
            InitializeComponent();
        }

        private async void ConnectBluetoothAsync()
        {
            //Attempt to connect to a known device, with a known pin.
            //Don't block the UI thread.
            await Task.Run(() =>
            {
                try
                {
                    var gadgeteerDevice = _bluetoothClient.DiscoverDevices()
                                                   .Where(d => d.DeviceName == "Gadgeteer")
                                                   .FirstOrDefault();

                    if (gadgeteerDevice != null)
                    {
                        _bluetoothClient.SetPin("0000");
                        _bluetoothClient.Connect(gadgeteerDevice.DeviceAddress, BluetoothService.SerialPort);
                        _bluetoothStream = _bluetoothClient.GetStream();
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.Message);
                }
            });
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            //Attempt to connect to the bluetooth device
            if (!_bluetoothClient.Connected)
            {
                ConnectBluetoothAsync();
            }

            //Only continue if the NetworkSteam is defined (this is a property of the bluetooth client)
            if (_bluetoothStream == null)
            {
                return;
            }

            //Read the next 230454 bytes received (230454 is the size of a 320x240 bitmap)
            using (Stream stream = ReadToSize(_bluetoothStream, 230454))
            {
                //If data was received
                if (stream.Length > 0)
                {
                    //Stop the timer, it's not needed any more
                    timer.Enabled = false;

                    //Convert the bytes into an image and display
                    DrawFromStream(stream);
                }
            }
        }

        private void DrawFromStream(Stream stream)
        {
            // Reconstitute the bytes in the stream into a bitmap and show it
            var bitmap = Bitmap.FromStream(stream);
            pictureBox1.Image = bitmap;
        }

        //Reads the next [size] bytes from the stream
        public Stream ReadToSize(NetworkStream stream, int size)
        {
            byte[] buffer = new byte[size];
            var memoryStream = new MemoryStream();
            
            //Keep going until all the expeected bytes are received
            while (memoryStream.Length < size)
            {
                //Read the next chunk
                int bytesRead = stream.Read(buffer, 0, buffer.Length);

                //If any were read, write them out to the stream
                if (bytesRead > 0)
                {
                    memoryStream.Write(buffer, 0, bytesRead);
                    //Feed back current progress
                    progressBar1.Value = (int)memoryStream.Length;
                }
            }
            return memoryStream;
        }
    }
}
