
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

        public Task StartMotor(Port port, ushort time, sbyte power)
        {
            return StartMotor(port, time, power, null);
        }

        public Task StartMotors(ushort time, sbyte aPower, sbyte bPower)
        {
            return StartMotor(Port.AB, time, aPower, bPower);
        }

        private async Task StartMotor(Port port, ushort time, sbyte power1, sbyte? power2)
        {
            var c = _characteristic;
            var writer = new DataWriter();
            writer.WriteBytes(new byte[] { power2.HasValue ? (byte)0x0D : (byte)0x0C, 0x00, 0x81 });
            writer.WriteByte((byte)port);
            writer.WriteBytes(new byte[] { 0x11, power2.HasValue ? (byte) 0x0A : (byte) 0x09 });
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteUInt16(time);
            writer.WriteByte(unchecked((byte)power1));
            if (power2.HasValue)
                writer.WriteByte(unchecked((byte)power2.Value));
            writer.WriteBytes(new byte[] { 0x64, 0x7F, 0x03 });
            await c.WriteValueAsync(writer.DetachBuffer());
        }

        public async Task SetColor(Color color)
        {
            var c = _characteristic;
            var writer = new DataWriter();
            writer.WriteBytes(new byte[] { 0x08, 0x00, 0x81, 0x32, 0x11, 0x51, 0x00, (byte)color });
            await c.WriteValueAsync(writer.DetachBuffer());
        }

        public void Dispose()
        {
            _device?.Dispose();
        }
    }
}

