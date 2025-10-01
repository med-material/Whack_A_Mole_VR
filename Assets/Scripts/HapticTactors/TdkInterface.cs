#if !TDK_PRESENT
using System;
internal static class TdkInterface
{
    public static int Connect(string portName, int deviceType, IntPtr zero) => -1;
    public static int InitializeTI() => 0;
    public static int ChangeGain(int boardId, int tactorID, int gain, int delay) => 0;
    public static int Pulse(int boardId, int tactorID, int duration, int delay) => 0;
    public static int RampGain(int boardId, int tactorID, int startGain, int endGain, int duration, int func, int delay) => 0;
    public static int RampFreq(int boardId, int tactorID, int startFreq, int endFreq, int duration, int func, int delay) => 0;
    public static int Close(int boardId) => 0;
    public static int ShutdownTI() => 0;
}

internal static class TdkDefines
{
    public enum DeviceTypes
    {
        Serial = 0
    }

    public static string GetLastEAIErrorString() => "Tdk SDK not present.";
}
#endif
