using System;
using Windows.Storage.Streams;

namespace MoveHub
{
    public abstract class Notification
    {

        public static Notification Parse(DataReader reader)
        {
            var length = reader.ReadByte();
            var version = reader.ReadByte();
            var type = reader.ReadByte();
            switch (type)
            {
                case 0x01:
                    return DeviceInfoNotification.ParseInfo(reader);
                case 0x04:
                    return PortInfoNotification.ParseInfo(reader);
                default:
                    return new RawNotification(type, null, reader);
            }

        }
    }

    internal abstract class PortInfoNotification : Notification
    {
        public Port Port { get; }
        public DeviceType DeviceType { get; }

        protected PortInfoNotification(Port port, DeviceType deviceType)
        {
            Port = port;
            DeviceType = deviceType;
        }

        internal static Notification ParseInfo(DataReader reader)
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
    }

    internal class GroupAttached : PortInfoNotification
    {
        public byte Port1 { get; }
        public byte Port2 { get; }

        public GroupAttached(Port port, DeviceType deviceType, byte port1, byte port2) : base(port, deviceType)
        {
            Port1 = port1;
            Port2 = port2;
        }
    }

    internal class PortAttached : PortInfoNotification
    {
        internal PortAttached(Port port, DeviceType deviceType) : base(port, deviceType)
        {
        }
    }

    internal class PortDetached : PortInfoNotification
    {
        internal PortDetached(Port port, DeviceType deviceType) : base(port, deviceType)
        {

        }
    }

    internal class RawNotification : Notification
    {
        public RawNotification(byte type, byte? subType, DataReader reader)
        {
            Type = type;
            SubType = subType;
            Buffer = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(Buffer);
        }

        public byte Type { get; }
        public byte? SubType { get; }
        public byte[] Buffer { get; }

        public override string ToString()
        {
            return BitConverter.ToString(Buffer);
        }
    }

    internal abstract class DeviceInfoNotification : Notification
    {
        internal static Notification ParseInfo(DataReader reader)
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
    }

    internal class DeviceInfoStringNotification : DeviceInfoNotification
    {
        public DeviceInfoStringNotification(byte type, DataReader reader)
        {
            Type = type;
            Value = reader.ReadString(reader.UnconsumedBufferLength);
        }
        public byte Type { get; }
        public string Value { get; }
    }
}