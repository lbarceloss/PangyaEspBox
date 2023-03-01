using System;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace PangyaEspBox
{
    public class MemoryHandler
    {

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, ref uint lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, Boolean bInheritHandle, UInt32 dwProcessId);


        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr processHandle, IntPtr baseAddress, byte[] buffer, int size, out int numberOfBytesWritten);

        private string processName;

        private Process[] processList;
        private Process process;

        private IntPtr handle;

        private bool handleOpened;

        private byte[] bytes;
        private uint size;
        private uint rw;

        public bool HandleOpened
        {
            get { return this.handleOpened; }
        }

        public MemoryHandler(string processName)
        {
            this.processName = processName.Split('.')[0];

            this.handleOpened = false;

            this.bytes = new byte[24];
            this.size = sizeof(int);
            this.rw = 0;

            this.CheckForProcess();
        }

        public uint GetPointer(uint address, params uint[] offsets)
        {
            try
            {
                if (this.handleOpened)
                {
                    uint currentAddress = 0;

                    ReadProcessMemory(this.handle, (IntPtr)address, this.bytes, (UIntPtr)this.size, ref this.rw);

                    for (int i = 0; i < offsets.Length; i++)
                    {
                        uint newContent = BitConverter.ToUInt32(this.bytes, 0);
                        uint newAddress = newContent + offsets[i];

                        currentAddress = newAddress;

                        ReadProcessMemory(this.handle, (IntPtr)newAddress, this.bytes, (UIntPtr)this.size, ref this.rw);
                    }

                    return currentAddress;
                }
                else
                {
                    return 0;
                }
            }
            catch { return 0; }
        }

        public byte[] ReadBytes(uint address, int size)
        {
            try
            {
                if (this.handleOpened)
                {
                    byte[] buffer = new byte[size];

                    ReadProcessMemory(this.handle, (IntPtr)address, buffer, (UIntPtr)size, ref this.rw);

                    return buffer;
                }
                else
                {
                    return new byte[0];
                }
            }
            catch { return new byte[0]; }
        }

        public int ReadInt32(uint address)
        {
            try
            {
                if (this.handleOpened)
                {
                    ReadProcessMemory(this.handle, (IntPtr)address, this.bytes, (UIntPtr)this.size, ref this.rw);

                    return BitConverter.ToInt32(this.bytes, 0);
                }
                else
                {
                    return 0;
                }
            }
            catch { return 0; }
        }

        public void CheckForProcess()
        {
            try
            {
                if (this.handleOpened)
                {
                    if (!this.IsProcessValid())
                    {
                        this.handleOpened = false;
                    }
                }

                if (!this.handleOpened)
                {
                    if (this.IsProcessValid()) this.Open();
                }
            }
            catch { }
        }

        private bool IsProcessValid()
        {
            try
            {
                this.processList = Process.GetProcessesByName(this.processName);

                if (this.processList.Length != 0) return true;
                else return false;
            }
            catch { return false; }
        }

        private void Open()
        {
            try
            {
                this.process = this.processList[0];
                this.handle = OpenProcess(0x10, false, (uint)this.process.Id);

                this.handleOpened = true;
            }
            catch { }
        }

    }
}
