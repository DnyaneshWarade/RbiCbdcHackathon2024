using Android.Bluetooth;
using Java.Util;
using System.Text;
using BharatEpaisaApp.Services;
using Java.IO;

namespace BharatEpaisaApp.Platforms.Android
{
    public class BluetoothService : IBluetoothService
    {
        private readonly BluetoothAdapter _bluetoothAdapter;
        private BluetoothSocket _socket;

        public BluetoothService()
        {
            _bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
        }

        public List<string> GetPairedDevicesAsync()
        {
            var devices = _bluetoothAdapter.BondedDevices;
            var deviceNames = devices.Select(d => d.Name).ToList();
            return deviceNames;
        }

        public async Task<Task> SendDataAsync(string deviceName, string jsonData)
        {
            try
            {
                var device = _bluetoothAdapter.BondedDevices.FirstOrDefault(d => d.Name == deviceName);
                if (device == null) throw new Exception("Device not found");

                var uuid = UUID.FromString("00001101-0000-1000-8000-00805f9b34fb"); // Standard SPP UUID
                _socket = device.CreateRfcommSocketToServiceRecord(uuid);
                _socket.Connect();

                var outputStream = _socket.OutputStream;
                var data = Encoding.UTF8.GetBytes(jsonData + "\n");
                //outputStream.Write(data, 0, data.Length);
                int chunkSize = 1024; 
                int totalChunks = (int)Math.Ceiling(data.Length / (double)chunkSize);
                for (int i = 0; i < totalChunks; i++)
                {
                    int offset = i * chunkSize;
                    int count = Math.Min(chunkSize, data.Length - offset);
                    outputStream.Write(data, offset, count);
                    outputStream.Flush();
                }
                await Task.Delay(2000);
                outputStream.Close();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
            }
            return Task.CompletedTask;
        }

        public void StartListening(Action<string> onMessageReceived)
        {
            Task.Run(() =>
            {
                var uuid = UUID.FromString("00001101-0000-1000-8000-00805f9b34fb");
                var serverSocket = _bluetoothAdapter.ListenUsingRfcommWithServiceRecord("BharatEpaisaApp", uuid);
                while (true)
                {
                    try
                    {
                        var clientSocket = serverSocket.Accept();
                        var inputStream = clientSocket.InputStream;
                        
                        var memoryStream = new MemoryStream();
                        var buffer = new byte[1024];
                        int bytesRead;

                        while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            memoryStream.Write(buffer, 0, bytesRead);
                            
                            // Check for the delimiter in the received data
                            var receivedData = Encoding.UTF8.GetString(memoryStream.ToArray());
                            if (receivedData.Contains("\n")) // End of transmission detected
                            {
                                var message = receivedData.TrimEnd('\n');
                                onMessageReceived?.Invoke(message);
                                break;
                            }
                        }
                        
                        inputStream.Close();
                        clientSocket.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine(ex.Message);
                    }
                    
                }
            });
        }
    }
}
