// ESpeakNG.cs

using System;
using System.Runtime.InteropServices;

public static class ESpeakNG
{
    private const string LibName = "espeak-ng";

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int SynthCallback(IntPtr wav, int numSamples, IntPtr events);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int espeak_Initialize(int output, int buflength, string path, int options);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void espeak_SetSynthCallback(SynthCallback callback);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int espeak_SetVoiceByName(string name);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int espeak_Synth(byte[] text, int size, uint position, int positionType, uint endPosition, uint flags, IntPtr uniqueIdentifier, IntPtr userData);
    
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int espeak_GetSampleRate();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int espeak_Terminate();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr espeak_TextToPhonemes(ref IntPtr text, int textmode, int phonememode);
}