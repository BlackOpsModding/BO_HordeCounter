using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BO_HordeCounter
{
    public static class Constants
    {
        #region Members
        public const int C_PROCESS_ALL_ACCESS = 0x1F0FFF;
        public const int C_MAPADDRESS = 0x02F67B6C;

        // Insert addresses here

        public const int C_ZOMBIES_OUTSIDE_BARRIER_ADDRESS = 0x01BFBC20;

        #endregion
    }

    public static class BlackOpsLibrary
    {
        #region Imports

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int _DwDesiredAccess, bool _InheritHandle, int _DwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow); //ShowWindow needs an IntPtr

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(int _HandleProcess, int _LpBaseAddress, byte[] _LpBuffer, int _DwSize, ref int _LpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(int _HandleProcess, int _LpBaseAddress, byte[] _LpBuffer, int _DwSize, ref int _LpNumberOfBytesRead);
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr _HandleWindow, int _Msg, int _WParam, int _LParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        #endregion

        #region Members
        public static Process m_BlackOpsProcess = null;
        public static IntPtr? m_ProcessHandle = IntPtr.Zero;
        #endregion

        #region Getters / Setters
        public static Process BlackOpsProcess
        {
            get
            {
                if (m_BlackOpsProcess != null)
                {
                    try
                    {
                        DateTime exitTime = m_BlackOpsProcess.ExitTime;
                        m_BlackOpsProcess = null;
                    }
                    catch (Exception)
                    {
                    }
                }

                if (m_BlackOpsProcess == null)
                {
                    Process[] processes = Process.GetProcessesByName("BlackOps");
                    int count = processes.Length;
                    if (count <= 0)
                    {
                        processes = Process.GetProcessesByName("BGamerT5");
                        count = processes.Length;
                    }

                    if (m_BlackOpsProcess != null && count <= 0)
                    {
                        m_BlackOpsProcess = null;
                    }

                    if (count > 0)
                    {
                        m_BlackOpsProcess = processes[0];
                    }
                }

                return m_BlackOpsProcess;
            }
        }

        public static IntPtr? BlackOpsHandle
        {
            get
            {
                if (BlackOpsProcess == null)
                {
                    m_ProcessHandle = IntPtr.Zero;
                }
                if ((m_ProcessHandle == IntPtr.Zero || m_ProcessHandle == null) && BlackOpsProcess != null)
                {
                    m_ProcessHandle = OpenProcess(Constants.C_PROCESS_ALL_ACCESS, false, BlackOpsProcess.Id);
                }

                return m_ProcessHandle;
            }
        }
        #endregion

        #region Methods
        public static int ReadInt(int _Address)
        {
            int bytesRead = 0;
            byte[] buffer = new byte[24];
            ReadProcessMemory((int)m_ProcessHandle, _Address, buffer, buffer.Length, ref bytesRead);
            return BitConverter.ToInt32(buffer, 0);
        }
        public static bool WriteInt(int _Address, int _Value)
        {
            int lpNumberOfBytesWritten = 0;
            byte[] bytes = BitConverter.GetBytes(_Value);
            WriteProcessMemory((int)m_ProcessHandle, _Address, bytes, 4, ref lpNumberOfBytesWritten);
            return (lpNumberOfBytesWritten != 0);
        }

        public static bool IsGameClosed()
        {
            return BlackOpsProcess == null || BlackOpsProcess.HasExited || BlackOpsProcess.MainWindowHandle == IntPtr.Zero || BlackOpsHandle == null;
        }
        #endregion

    }
}
