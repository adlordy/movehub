using System;
using Windows.Storage.Streams;

namespace MoveHub
{
    internal abstract class MotorCommandBase
    {
        protected MotorCommandBase(Port port, sbyte power)
        {
            Port = port;
            Power = power;
        }
        public Port Port { get; }
        public sbyte Power { get; }
        public abstract byte Length { get; }
        public abstract byte Command { get; }
        public abstract void WriteBody(DataWriter writer);
    }

    internal class MotorTimeCommand : MotorCommandBase
    {
        public MotorTimeCommand(Port port, ushort time, sbyte power) : base(port, power)
        {
            Time = time;
        }

        public ushort Time { get; }

        public override byte Length => 0x0C;
        public override byte Command => 0x09;
        public override void WriteBody(DataWriter writer)
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
        public override byte Length => 0x0D;
        public override byte Command => 0x0A;
    }

    internal class MotorAngleCommand : MotorCommandBase
    {
        public MotorAngleCommand(Port port, uint angle, sbyte power) : base(port, power)
        {
            Angle = angle;
        }
        public uint Angle { get; }
        public override byte Length => 0x0E;
        public override byte Command => 0x0B;
        public override void WriteBody(DataWriter writer)
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
        public override byte Length => 0x0F;
        public override byte Command => 0x0C;
    }

    internal interface IPowerB
    {
        sbyte PowerB { get; }
    }
}