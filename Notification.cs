using System;
using Windows.Storage.Streams;

namespace MoveHub
{
    public abstract class Notification
    {
    }

    public abstract class PortInfoNotification : Notification
    {
        public Port Port { get; }
        public DeviceType DeviceType { get; }

        protected PortInfoNotification(Port port, DeviceType deviceType)
        {
            Port = port;
            DeviceType = deviceType;
        }
    }

    public class GroupAttached : PortInfoNotification
    {
        public byte Port1 { get; }
        public byte Port2 { get; }

        public GroupAttached(Port port, DeviceType deviceType, byte port1, byte port2) : base(port, deviceType)
        {
            Port1 = port1;
            Port2 = port2;
        }
    }

    public class PortAttached : PortInfoNotification
    {
        internal PortAttached(Port port, DeviceType deviceType) : base(port, deviceType)
        {
        }
    }

    public class PortDetached : PortInfoNotification
    {
        internal PortDetached(Port port, DeviceType deviceType) : base(port, deviceType)
        {

        }
    }

    public class RawNotification : Notification
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

    public class DeviceInfoStringNotification : Notification
    {
        public DeviceInfoStringNotification(byte type, DataReader reader)
        {
            Type = type;
            Value = reader.ReadString(reader.UnconsumedBufferLength);
        }
        public byte Type { get; }
        public string Value { get; }
    }

    public class MotorSensorNotification : Notification
    {
        public Port Port;
        public int Angle;

        public MotorSensorNotification(Port port, int angle)
        {
            Port = port;
            Angle = angle;
        }
    }

    public class TiltSensorNotification : Notification
    {
        public sbyte Roll { get; }
        public sbyte Pitch { get; }

        public TiltSensorNotification(sbyte roll, sbyte pitch)
        {
            Roll = roll;
            Pitch = pitch;
        }
    }

    public class ColorDistanceSensorNotification : Notification
    {
        public ColorDistanceSensorNotification(SensorColor color, byte value, byte partial)
        {
            Color = color;
            Distance = value + (partial != 0 ? 1f / partial : 0f);
        }

        public SensorColor Color { get; }
        public float Distance { get; }
    }
}