using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BharatEpaisaApp.Services
{
    public interface IBluetoothService
    {
        List<string> GetPairedDevicesAsync();
        Task<Task> SendDataAsync(string deviceName, string jsonData);
        void StartListening(Action<string> onMessageReceived);
    }
}
