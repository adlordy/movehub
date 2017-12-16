namespace MoveHub{
    public enum DeviceType : byte{
        Unknown = 0x00,
        PowerVoltage = 0x14,
        CircutPower = 0x15,
        Led = 0x17,
        DistanceColorSensor = 0x25,
        IMOTOR = 0x26, 
        MOTOR = 0x27,
        TiltSensor = 0x28
    }
}