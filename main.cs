using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Diagnostics;

class Program
{
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("gdi32.dll")] static extern bool StretchBlt(IntPtr hdcD, int xD, int yD, int wD, int hD, IntPtr hdcS, int xS, int yS, int wS, int hS, uint op);
    [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr hdcD, int x, int y, int w, int h, IntPtr hdcS, int sx, int sy, uint op);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vKey);
    [DllImport("user32.dll")] static extern bool DrawIcon(IntPtr hdc, int x, int y, IntPtr hIcon);
    [DllImport("user32.dll")] static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("winmm.dll")] static extern uint waveOutOpen(out IntPtr hwo, uint uDeviceID, ref WAVEFORMATEX pwfx, IntPtr dwCallback, IntPtr dwInstance, uint fdwOpen);
    [DllImport("winmm.dll")] static extern uint waveOutWrite(IntPtr hwo, ref WAVEHDR pwh, uint cbwh);
    [DllImport("winmm.dll")] static extern uint waveOutPrepareHeader(IntPtr hwo, ref WAVEHDR pwh, uint cbwh);

    [StructLayout(LayoutKind.Sequential)]
    struct WAVEFORMATEX { public ushort wFormatTag, nChannels; public uint nSamplesPerSec, nAvgBytesPerSec; public ushort nBlockAlign, wBitsPerSample, cbSize; }
    [StructLayout(LayoutKind.Sequential)]
    struct WAVEHDR { public IntPtr lpData; public uint dwBufferLength, dwBytesRecorded, dwUser, dwFlags, dwLoops, dwReserved; public IntPtr lpNext, reserved; }

    static void Main()
    {
        IntPtr hdc = GetDC(IntPtr.Zero);
        IntPtr cursor = LoadIcon(IntPtr.Zero, (IntPtr)32512);
        Random r = new Random();
        DateTime start = DateTime.Now;
        string tempPath = Path.Combine(Path.GetTempPath(), "warning.txt");
        int t = 0, phase = 1;
        double wave = 0;
        bool msgShowed = false;

        WAVEFORMATEX wfx = new WAVEFORMATEX { wFormatTag = 1, nChannels = 1, nSamplesPerSec = 8000, nAvgBytesPerSec = 8000, nBlockAlign = 1, wBitsPerSample = 8, cbSize = 0 };
        waveOutOpen(out IntPtr hwo, 0, ref wfx, IntPtr.Zero, IntPtr.Zero, 0);

        // Поток звука
        Thread soundThread = new Thread(() => {
            while (true)
            {
                byte[] buf = new byte[8000];
                for (int i = 0; i < buf.Length; i++, t++)
                {
                    if (phase == 1) buf[i] = (byte)((t * (t >> 13 & t >> 8)) >> (t >> 16 & t >> 4) | (t >> 5 | t >> 8) * (t >> 10 & 32));
                    else if (phase == 2) buf[i] = (byte)((((t * (t >> 2 | t >> 10) & 4 & t >> 3) ^ (t & t >> 15 | t >> 10)) | (t * (t >> 32 | t >> 10) & 4 & t >> 7)) << 4);
                    else if (phase == 3) buf[i] = (byte)((t >> 6 | t | t >> (t >> 16)) * 10 + ((t >> 11) & 7) ^ (t >> 16 | t | t >> (t >> 10) & 2));
                    else buf[i] = (byte)(((t * (t >> 16 & t >> 9)) >> (t >> 10 & t >> 2) | (t >> 2 | t >> 7) * (t >> 12 & 13) | (t * (t >> 4 | t >> 17 | t >> 12) & (t >> 7 & 180))) << 2);
                }
                WAVEHDR hdr = new WAVEHDR { lpData = Marshal.AllocHGlobal(buf.Length), dwBufferLength = (uint)buf.Length };
                Marshal.Copy(buf, 0, hdr.lpData, buf.Length);
                waveOutPrepareHeader(hwo, ref hdr, (uint)Marshal.SizeOf(hdr));
                waveOutWrite(hwo, ref hdr, (uint)Marshal.SizeOf(hdr));
                Thread.Sleep(980);
            }
        });
        soundThread.IsBackground = true;
        soundThread.Start();

        // Цикл эффектов
        while ((GetAsyncKeyState(0x1B) & 0x8000) == 0)
        {
            double sec = (DateTime.Now - start).TotalSeconds;
            if (sec < 50) phase = 1; else if (sec < 70) phase = 2; else if (sec < 80) phase = 3; else if (sec < 115) phase = 4; else break;

            if (phase == 1)
            {
                int x = r.Next(1920), y = r.Next(1080);
                StretchBlt(hdc, x + r.Next(-20, 20), y + r.Next(-20, 20), 400 + r.Next(-50, 50), 400 + r.Next(-50, 50), hdc, x, y, 400, 400, 0x00CC0020);
                Thread.Sleep(10);
            }
            else if (phase == 2)
            {
                wave += 0.8;
                for (int y = 0; y < 1080; y += 40) BitBlt(hdc, (int)(Math.Sin((y / 120.0) + wave) * 35), y, 1920, 40, hdc, 0, y, 0x550009);
                Thread.Sleep(20);
            }
            else if (phase == 3)
            {
                BitBlt(hdc, -15, 0, 1920, 1080, hdc, 0, 0, 0x00CC0020);
                for (int i = 0; i < 4; i++) DrawIcon(hdc, r.Next(1920), r.Next(1080), cursor);
                Thread.Sleep(15);
            }
            else
            {
                if (!msgShowed)
                {
                    File.WriteAllText(tempPath, "Твой ПК в жопе мира");
                    Process.Start("notepad.exe", tempPath);
                    msgShowed = true;
                }
                BitBlt(hdc, r.Next(-60, 60), r.Next(-60, 60), 1920, 1080, hdc, 0, 0, 0x00CC0020);
                Thread.Sleep(10);
            }
        }

        if (File.Exists(tempPath)) File.Delete(tempPath);
        Environment.Exit(0); // Полный выход
    }
}
