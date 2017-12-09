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
                controller.Connected += async (s, e) =>
                {
                    await controller.SetColor(Color.Red);
                    await Task.Delay(1000);
                    await controller.StartMotor(Port.AB, 200, 20);
                    await Task.Delay(200);
                    await controller.SetColor(Color.Blue);
                    await Task.Delay(1000);
                    await controller.StartMotors(1000, 100, -100);
                    await Task.Delay(1000);
                    mre.Set();
                };
                System.Console.WriteLine("Press button on move hub");
                mre.WaitOne();
            }
        }
    }
}
