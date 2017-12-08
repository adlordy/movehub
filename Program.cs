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
            var serviceId = new Guid("00001623-1212-efde-1623-785feabcd123");
            var devices = new Dictionary<string, BluetoothDevice>();
            var filter = new BluetoothLEAdvertisementFilter();
            filter.Advertisement.ServiceUuids.Add(serviceId);
            var watcher = new BluetoothLEAdvertisementWatcher(filter);
            var mre = new ManualResetEvent(false);
            watcher.Received += async (s, e) =>
            {
                watcher.Stop();
                System.Console.WriteLine("Received {0}, {1}, {2}", e.Advertisement.LocalName, e.BluetoothAddress, String.Join(",", e.Advertisement.ServiceUuids.Select(u => u.ToString())));
                using (var device = await BluetoothLEDevice.FromBluetoothAddressAsync(e.BluetoothAddress))
                {
                    var gatt = await device.GetGattServicesForUuidAsync(serviceId);
                    if (gatt.Status == GattCommunicationStatus.Success)
                    {
                        var services = gatt.Services.Single();
                        var cs = await services.GetCharacteristicsAsync();
                        if (cs.Status == GattCommunicationStatus.Success)
                        {
                            var c = cs.Characteristics.Single();
                            await SetColor(c, "orange");
                            await StartMotor(c, 0x39, 2000, -50);
                            await Task.Delay(5000);
                            mre.Set();
                        }
                    }
                }
            };
            System.Console.WriteLine("Starting");
            Console.CancelKeyPress += (s, e) =>
            {
                mre.Set();
            };
            watcher.Start();
            mre.WaitOne();
        }

        private static async Task StartMotor(GattCharacteristic c, byte port, ushort time, sbyte dutyCycle)
        {
            var writer = new DataWriter();
            writer.WriteBytes(new byte[] { 0x0C, 0x00, 0x81 });
            writer.WriteByte(port);
            writer.WriteBytes(new byte[] { 0x11, 0x09 });
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteUInt16(time);
            writer.WriteByte(unchecked((byte)dutyCycle));
            writer.WriteBytes(new byte[] { 0x64, 0x7F, 0x03 });
            await c.WriteValueAsync(writer.DetachBuffer());
        }

        static async Task SetColor(GattCharacteristic c, string color)
        {
            var writer = new DataWriter();
            var colors = new[]{
                                "off",
                                "pink",
                                "purple",
                                "blue",
                                "lightblue",
                                "cyan",
                                "green",
                                "yellow",
                                "orange",
                                "red",
                                "white"
                            };
            var index = Array.IndexOf(colors, color);
            if (index == -1)
                throw new ArgumentOutOfRangeException(nameof(color));

            writer.WriteBytes(new byte[] { 0x08, 0x00, 0x81, 0x32, 0x11, 0x51, 0x00, (byte)index });
            var writeResult = await c.WriteValueAsync(writer.DetachBuffer());
        }
    }

}
