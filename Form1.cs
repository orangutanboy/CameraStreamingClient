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

        byte[] _allBytes = new byte[300000];

        public Form1()
        {
            InitializeComponent();
        }

        private async void ConnectBluetooth()
        {
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
            if (!_bluetoothClient.Connected)
            {
                ConnectBluetooth();
            }

            if (_bluetoothStream == null)
            {
                return;
            }

            using (Stream stream = ReadToSize(_bluetoothStream, 230454))
            {
                if (stream.Length > 0)
                {
                    DrawFromStream(stream);
                    timer.Enabled = false;
                }
            }
        }

        private void DrawFromStream(Stream stream)
        {
            var bitmap = Bitmap.FromStream(stream);
            pictureBox1.Image = bitmap;
        }

        public Stream ReadToSize(NetworkStream stream, int size)
        {
            byte[] buffer = new byte[size];
            var ms = new MemoryStream();
            while (ms.Length < size)
            {
                int read = stream.Read(buffer, 0, buffer.Length);

                if (read > 0)
                {
                    ms.Write(buffer, 0, read);
                    progressBar1.Value = (int)ms.Length;                   
                }
            }
            return ms;
        }


        private void WriteIt(byte[] bytes, int length)
        {
            using (var fileStream = File.OpenWrite(@"c:\temp\gadgeteer.bmp"))
            {
                fileStream.Write(bytes, 0, length);
            }
        }
    }
}
