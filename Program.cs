using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace MoveHub
{
    class Program
    {
        static void Main(string[] args)
        {
            var mre = new ManualResetEvent(false);
            Console.CancelKeyPress += (s, e) =>
            {
                mre.Set();
            };
            using (var controller = new Controller())
            {
                controller.OnConnected += (s, e) =>
                {
                    System.Console.WriteLine("Connected");
                };
                System.Console.WriteLine("Press button on move hub");
                mre.WaitOne();
            }
        }
    }
}
