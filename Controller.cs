
using System;
using System.Collections.Generic;
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

        public event EventHandler<EventArgs> OnConnected;
        public event EventHandler<Notification> OnNotification;

        private Dictionary<Port,DeviceType> _portMapping = new Dictionary<Port, DeviceType>();

        public Controller()
        {
            InitWatcher();
            OnConnected += (s, e) => { };
            OnNotification += (s, e) => {
                if (e is PortInfoNotification notification)
                    UpdatePortMapping(notification);
            };
        }

        private void UpdatePortMapping(PortInfoNotification notification)
        {
            switch(notification){
                case PortAttached a:
                    _portMapping[a.Port] = a.DeviceType;
                    break;
                case PortDetached d:
                    _portMapping.Remove(d.Port);
                    break;
                case GroupAttached g:
                    _portMapping[g.Port] = g.DeviceType;
                    break;
                default:
                    throw new ArgumentException();
            }
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
            await Connected();
        }

        private async Task Connected()
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
            await Subscribe();
            OnConnected(this, new EventArgs());
        }

        private async Task Subscribe()
        {
            var value = GattClientCharacteristicConfigurationDescriptorValue.None;
            if (_characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate))
            {
                value = GattClientCharacteristicConfigurationDescriptorValue.Indicate;
            }
            else if (_characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
            {
                value = GattClientCharacteristicConfigurationDescriptorValue.Notify;
            }
            var status = await _characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(value);
            _characteristic.ValueChanged += ValueChanged;
        }

        private void ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var buffer = args.CharacteristicValue;
            using (var reader = DataReader.FromBuffer(buffer))
            {
                var notification = ParseNotification(reader);
                OnNotification(this, notification);
            }
        }

        private Notification ParseNotification(DataReader reader)
        {
            var length = reader.ReadByte();
            var version = reader.ReadByte();
            var type = reader.ReadByte();
            switch (type)
            {
                case 0x01:
                    return ParseDeviceInfo(reader);
                case 0x04:
                    return ParsePortInfo(reader);
                case 0x45:
                    return ParseSensor(reader);
                default:
                    return new RawNotification(type, null, reader);
            }
        }

        private Notification ParseDeviceInfo(DataReader reader)
        {
            var type = reader.ReadByte();
            var spacer = reader.ReadByte();
            switch (type)
            {
                case 0x01:
                case 0x08:
                    return new DeviceInfoStringNotification(type, reader);
                default:
                    return new RawNotification(0x01, type, reader);
            }
        }

        private PortInfoNotification ParsePortInfo(DataReader reader)
        {
            var port = (Port)reader.ReadByte();
            var singleOrGroup = reader.ReadByte();
            var deviceType = singleOrGroup == 0x00 ? 0 :
                (DeviceType)reader.ReadByte();
            switch (singleOrGroup)
            {
                case 0x00:
                    return new PortDetached(port, deviceType);
                case 0x01:
                    return new PortAttached(port, deviceType);
                case 0x02:
                    var spacer = reader.ReadByte();
                    var port1 = reader.ReadByte();
                    var port2 = reader.ReadByte();
                    return new GroupAttached(port, deviceType, port1, port2);
                default:
                    throw new FormatException("Unknown value");
            }
        }

        private Notification ParseSensor(DataReader reader)
        {
            var port = (Port)reader.ReadByte();
            var something = reader.ReadByte();
            if (_portMapping.TryGetValue(port,out DeviceType deviceType)){
                switch(deviceType){
                    case DeviceType.IMOTOR:
                    case DeviceType.MOTOR:
                        reader.ByteOrder = ByteOrder.LittleEndian;
                        var angle = reader.ReadInt32();
                        return new MotorSensorNotification(port, angle);
                    case DeviceType.TiltSensor:
                        var roll = unchecked((sbyte) reader.ReadByte());
                        var pitch = unchecked((sbyte)reader.ReadByte());
                        return new TiltSensorNotification(roll,pitch);
                    case DeviceType.DistanceColorSensor:
                        var color = (SensorColor)reader.ReadByte();
                        var value = reader.ReadByte();
                        var spacer = reader.ReadByte();
                        var partial = reader.ReadByte();
                        return new ColorDistanceSensorNotification(color,value,partial);
                    default:
                        return new RawNotification(0x45, (byte) port, reader);
                }
            } else{
                throw new ArgumentException("Unmapped port");
            }
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
            if (_characteristic != null)
            {
                _characteristic.ValueChanged -= ValueChanged;
            }
            _device?.Dispose();
        }
    }
}

