using System;
using Windows.Storage.Streams;

namespace MoveHub
{
    internal abstract class CommandBase
    {

        protected CommandBase(Port port)
        {
            Port = port;
        }
        protected Port Port { get; }
        protected abstract byte Length { get; }
        protected abstract byte Command { get; }
        protected abstract void WriteBody(DataWriter writer);

        internal void Write(DataWriter writer)
        {
            writer.WriteBytes(new byte[] { Length, 0x00, 0x81 });
            writer.WriteByte((byte)Port);
            writer.WriteBytes(new byte[] { 0x11, Command });
            writer.ByteOrder = ByteOrder.LittleEndian;
            WriteBody(writer);
        }
    }

    internal class ColorCommand : CommandBase
    {
        public ColorCommand(Color color) : base(Port.LED)
        {
            Color = color;
        }

        protected Color Color { get; }

        protected override byte Length => 0x08;
        protected override byte Command => 0x51;

        protected override void WriteBody(DataWriter writer)
        {
            writer.WriteByte(0x00);
            writer.WriteByte((byte)Color);
        }
    }
    internal abstract class MotorCommandBase : CommandBase
    {
        protected MotorCommandBase(Port port, sbyte power) : base(port)
        {
            Power = power;
        }
        protected sbyte Power { get; }

        protected abstract void WriteValue(DataWriter writer);

        protected override void WriteBody(DataWriter writer)
        {
            WriteValue(writer);
            writer.WriteByte(unchecked((byte)Power));
            if (this is IPowerB b)
                writer.WriteByte(unchecked((byte)b.PowerB));
            writer.WriteBytes(new byte[] { 0x64, 0x7F, 0x03 });
        }
    }

    internal class MotorTimeCommand : MotorCommandBase
    {
        public MotorTimeCommand(Port port, ushort time, sbyte power) : base(port, power)
        {
            Time = time;
        }

        protected ushort Time { get; }

        protected override byte Length => 0x0C;
        protected override byte Command => 0x09;
        protected override void WriteValue(DataWriter writer)
        {
            writer.WriteUInt16(Time);
        }
    }

    internal class MotorTimeMoveCommand : MotorTimeCommand, IPowerB
    {
        public MotorTimeMoveCommand(ushort time, sbyte powerA, sbyte powerB) : base(Port.AB, time, powerA)
        {
            PowerB = powerB;
        }
        public sbyte PowerB { get; }
        protected override byte Length => 0x0D;
        protected override byte Command => 0x0A;
    }

    internal class MotorAngleCommand : MotorCommandBase
    {
        public MotorAngleCommand(Port port, uint angle, sbyte power) : base(port, power)
        {
            Angle = angle;
        }
        protected uint Angle { get; }
        protected override byte Length => 0x0E;
        protected override byte Command => 0x0B;
        protected override void WriteValue(DataWriter writer)
        {
            writer.WriteUInt32(Angle);
        }
    }

    internal class MotorAngleMoveCommand : MotorAngleCommand, IPowerB
    {
        public MotorAngleMoveCommand(uint angle, sbyte powerA, sbyte powerB) : base(Port.AB, angle, powerA)
        {
            PowerB = powerB;
        }
        public sbyte PowerB { get; }
        protected override byte Length => 0x0F;
        protected override byte Command => 0x0C;
    }

    internal interface IPowerB
    {
        sbyte PowerB { get; }
    }
}