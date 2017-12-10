
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace MoveHub
{
    public class Controller : IDisposable
    {
        private static readonly Guid ServiceId = new Guid("00001623-1212-efde-1623-785feabcd123");
        private ulong _address;
        private BluetoothLEDevice _device;
        private GattCharacteristic _characteristic;

        public event EventHandler<EventArgs> Connected;

        public Controller()
        {
            InitWatcher();
            Connected += (s, e) => { };
        }

        private void InitWatcher()
        {
            var filter = new BluetoothLEAdvertisementFilter();
            filter.Advertisement.ServiceUuids.Add(ServiceId);
            var watcher = new BluetoothLEAdvertisementWatcher(filter);
            watcher.Received += async (s, e) => await Received(s, e);
            watcher.Start();
        }

        private async Task Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            sender.Stop();
            _address = args.BluetoothAddress;
            await OnConnected();
        }

        private async Task OnConnected()
        {
            _device = await BluetoothLEDevice.FromBluetoothAddressAsync(_address);
            var gatt = await _device.GetGattServicesForUuidAsync(ServiceId);
            if (gatt.Status != GattCommunicationStatus.Success)
                throw new Exception("Failed to create GATT service");

            var services = gatt.Services.Single();
            var cs = await services.GetCharacteristicsAsync();
            if (cs.Status != GattCommunicationStatus.Success)
                throw new Exception("Failed to get characteristic");

            _characteristic = cs.Characteristics.Single();
            Connected(this, new EventArgs());
        }

        public Task StartMotorTime(Port port, ushort time, sbyte power)
        {
            return WriteCommand(new MotorTimeCommand(port, time, power));
        }

        public Task StartMoveTime(ushort time, sbyte powerA, sbyte powerB)
        {
            return WriteCommand(new MotorTimeMoveCommand(time, powerA, powerB));
        }

        public Task StartMotorAngle(Port port, uint angle, sbyte power)
        {
            return WriteCommand(new MotorAngleCommand(port, angle, power));
        }

        public Task StartMoveAngle(uint angle, sbyte powerA, sbyte powerB)
        {
            return WriteCommand(new MotorAngleMoveCommand(angle, powerA, powerB));
        }

        public Task SetColor(Color color)
        {
            return WriteCommand(new ColorCommand(color));
        }

        private async Task WriteCommand(CommandBase command)
        {
            using (var writer = new DataWriter())
            {
                command.Write(writer);
                var result = await _characteristic.WriteValueAsync(writer.DetachBuffer());
                if (result != GattCommunicationStatus.Success)
                    throw new Exception($"Failed to write command {command}");
            }
        }

        public void Dispose()
        {
            _device?.Dispose();
        }
    }
}

