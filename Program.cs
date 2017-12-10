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
                    await controller.StartMoveAngle(230, 20, -20);
                    await Task.Delay(2000);
                    await controller.StartMoveAngle(230, -20, 20);
                    await Task.Delay(2000);
                    await controller.StartMotorAngle(Port.D, 30, 50);
                    await Task.Delay(2000);
                    await controller.StartMotorAngle(Port.D, 30, -50);
                    mre.Set();
                };
                System.Console.WriteLine("Press button on move hub");
                mre.WaitOne();
            }
        }
    }
}
