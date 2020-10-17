#region using

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;

#endregion

namespace HackFramework
{
    /// <summary>
    ///     https://github.com/alessiocentorrino/hihutex-memory-class/blob/master/Memory.cs
    /// </summary>
    /// <summary>
    /// 
    ///     <example>
    ///         <![CDATA[
    /// string   process = "svencoop";                            
    /// IntPtr[] offsets = { (IntPtr) 0x570EE00, (IntPtr) 0x88 }; 
    /// Memory   m       = new Memory();                          
    /// m.AttatchLoop( process );                                 
    /// var basePtr = m.GetModuleAddress( "hw.dll" );             
    /// var posPtr = m.GetMultiLevelPointer( basePtr, offsets, false );       
    /// while ( true ) {                                          
    ///     Thread.Sleep( 300 );                                  
    ///     var pos = m.ReadMemory<Vec3>( posPtr );               
    ///     Console.WriteLine( pos );                             
    /// }                                                         
    /// ]]></example>
    /// </summary>
    public class Memory
    {
        private static int     mIBytesWritten;
        private static int     mIBytesRead;
        public         Process MiProcess;
        public         IntPtr  MiProcessHandle;

        public void AttachLoop(string processName, int trys = 100)
        {
            var found = false;
            Console.Write("Waiting for " + processName + "");
            int cl = Console.CursorLeft;

            for (var i = 0; i < trys; i++)
            {
                Thread.Sleep(500);
                Console.SetCursorPosition(cl, Console.CursorTop);
                Console.Write(new string('.', i % 8) + new string(' ', 8));

                if (Attatch(processName, ref this.MiProcess, ref this.MiProcessHandle)) break;
            }

            Thread.Sleep(500);
            Console.WriteLine(" Found!");
        }

        #region DynamicInvokers

        public void WriteMemory <T>(IntPtr address, object value) => WriteMemory<T>(address, value, ref this.MiProcessHandle);

        public void WriteMemory <T>(IntPtr address, char[] value) => WriteMemory<T>(address, value, ref this.MiProcessHandle);

        public T ReadMemory <T>(IntPtr address) where T : struct => ReadMemory<T>(address, ref this.MiProcessHandle);

        public byte[] ReadMemory(IntPtr offset, int size) => ReadMemory(offset, size, ref this.MiProcessHandle);

        public float[] ReadMatrix <T>(IntPtr address, int matrixSize) where T : struct => ReadMatrix<T>(address, matrixSize, ref this.MiProcessHandle);

        public IntPtr GetModuleAddress(string name) => GetModuleAddress(name, ref this.MiProcess);

        public IntPtr GetMultiLevelPointer(IntPtr @base, IntPtr[] offsets, bool x64, bool debug = default) =>
            x64 ?
                GetMultiLevelPointer64Bit(@base, offsets, ref this.MiProcessHandle, debug) :
                GetMultiLevelPointer32Bit(@base, offsets, ref this.MiProcessHandle, debug);

        #endregion

        #region Static

        public static bool Attatch(string procName, ref Process mIProcess, ref IntPtr mIProcessHandle)
        {
            if (Process.GetProcessesByName(procName).Length > 0)
            {
                mIProcess = Process.GetProcessesByName(procName)[0];
                mIProcessHandle = OpenProcess(ProcessFlags.PROCESS_VM_OPERATION | ProcessFlags.PROCESS_VM_READ | ProcessFlags.PROCESS_VM_WRITE,
                    false,
                    mIProcess.Id);

                return true;
            }

            return false;
        }

        public static void WriteMemory <T>(IntPtr address, object value, ref IntPtr miProcessHandle)
        {
            var buffer = StructureToByteArray(value);

            NtWriteVirtualMemory(miProcessHandle, address, buffer, buffer.Length, out mIBytesWritten);
        }

        public static void WriteMemory <T>(IntPtr address, char[] value, ref IntPtr miProcessHandle)
        {
            var buffer = Encoding.UTF8.GetBytes(value);

            NtWriteVirtualMemory(miProcessHandle, address, buffer, buffer.Length, out mIBytesWritten);
        }

        public static T ReadMemory <T>(IntPtr address, ref IntPtr miProcessHandle) where T : struct
        {
            int byteSize = Marshal.SizeOf<T>();

            var buffer = new byte[byteSize];

            NtReadVirtualMemory(miProcessHandle, address, buffer, buffer.Length, out mIBytesRead);

            return ByteArrayToStructure<T>(buffer.Take(mIBytesRead).ToArray());
        }

        public static byte[] ReadMemory(IntPtr offset, int size, ref IntPtr miProcessHandle)
        {
            var buffer = new byte[size];

            NtReadVirtualMemory(miProcessHandle, offset, buffer, size, out mIBytesRead);

            return buffer.Take(mIBytesRead).ToArray();
        }

        public static long ReadInt64(IntPtr address, ref IntPtr miProcessHandle) => BitConverter.ToInt64(ReadMemory(address, 8, ref miProcessHandle), 0);
        public static int  ReadInt32(IntPtr address, ref IntPtr miProcessHandle) => BitConverter.ToInt32(ReadMemory(address, 4, ref miProcessHandle), 0);

        public static float[] ReadMatrix <T>(IntPtr address, int matrixSize, ref IntPtr miProcessHandle) where T : struct
        {
            int byteSize = Marshal.SizeOf(typeof(T));
            var buffer   = new byte[byteSize * matrixSize];
            NtReadVirtualMemory(miProcessHandle, address, buffer, buffer.Length, out mIBytesRead);

            return ConvertToFloatArray(buffer.Take(mIBytesRead).ToArray());
        }

        public static IntPtr GetModuleAddress(string name, ref Process miProcess)
        {
            try
            {
                foreach (ProcessModule procMod in miProcess.Modules)
                    if (name == procMod.ModuleName)
                        return procMod.BaseAddress;
            }
            catch (Exception e) { Console.WriteLine(e); }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: Cannot find - " + name + " | Check file extension.");
            Console.ResetColor();

            return IntPtr.Zero;
        }

        public static IntPtr GetMultiLevelPointer64Bit(IntPtr @base, IntPtr[] offsets, ref IntPtr miProcessHandle, bool debug = default)
        {
            long tmpptr = 0;

            for (var i = 0; i < offsets.Length; i++)
            {
                if (debug)
                    Console.Write($"[OFFSET {i:X2}] [{(i == 0 ? @base.ToInt64() : tmpptr):X16} + {offsets[i].ToInt64():X16}]");

                if (i == 0)
                {
                    long ptr = @base.ToInt64() + offsets[i].ToInt64();
                    tmpptr = ReadInt64(new IntPtr(ptr), ref miProcessHandle);
                }
                else if (i == offsets.Length - 1)
                {
                    long ptr2 = tmpptr + offsets[i].ToInt64();
                    tmpptr = ptr2;
                }
                else
                {
                    long ptr2 = tmpptr + offsets[i].ToInt64();
                    tmpptr = ReadInt64(new IntPtr(ptr2), ref miProcessHandle);
                }

                if (debug)
                    Console.WriteLine($" -> {tmpptr:X16}");
            }

            return (IntPtr) tmpptr;
        }

        public static IntPtr GetMultiLevelPointer32Bit(IntPtr @base, IntPtr[] offsets, ref IntPtr miProcessHandle, bool debug = default)
        {
            int tmpptr = 0;

            for (var i = 0; i < offsets.Length; i++)
            {
                if (debug)
                    Console.Write($"[OFFSET {i:X2}] [{(i == 0 ? @base.ToInt64() : tmpptr):X8} + {offsets[i].ToInt64():X8}]");

                if (i == 0)
                {
                    int ptr = @base.ToInt32() + offsets[i].ToInt32();
                    tmpptr = ReadInt32(new IntPtr(ptr), ref miProcessHandle);
                }
                else if (i == offsets.Length - 1)
                {
                    int ptr2 = tmpptr + offsets[i].ToInt32();
                    tmpptr = ptr2;
                }
                else
                {
                    int ptr2 = tmpptr + offsets[i].ToInt32();
                    tmpptr = ReadInt32(new IntPtr(ptr2), ref miProcessHandle);
                }

                if (debug)
                    Console.WriteLine($" -> {tmpptr:X8}");
            }

            return (IntPtr) tmpptr;
        }

        #endregion

        #region Conversion

        public static float[] ConvertToFloatArray(byte[] bytes)
        {
            if (bytes.Length % 4 != 0)
                throw new ArgumentException();

            var floats = new float[bytes.Length / 4];

            for (var i = 0; i < floats.Length; i++)
                floats[i] = BitConverter.ToSingle(bytes, i * 4);

            return floats;
        }

        public static T ByteArrayToStructure <T>(byte[] bytes) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

            try
            {
                return (T) Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
        }

        public static byte[] StructureToByteArray(object obj)
        {
            int length = Marshal.SizeOf(obj);

            var array = new byte[length];

            var pointer = Marshal.AllocHGlobal(length);

            Marshal.StructureToPtr(obj, pointer, true);
            Marshal.Copy(pointer, array, 0, length);
            Marshal.FreeHGlobal(pointer);

            return array;
        }

        #endregion

        #region NativeMethodes

        [DllImport("kernel32.dll")] public static extern IntPtr OpenProcess(ProcessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("ntdll")] public static extern bool NtReadVirtualMemory(IntPtr  processHandle, IntPtr baseAddress, byte[] buffer, int numberOfBytesToRead,  out int numberOfBytesRead);
        [DllImport("ntdll")] public static extern bool NtWriteVirtualMemory(IntPtr processHandle, IntPtr baseAddress, byte[] buffer, int numberOfBytesToWrite, out int numberOfBytesWritten);

        #region Other

        public enum ProcessFlags
        {
            PROCESS_VM_OPERATION = 0x0008, PROCESS_VM_READ = 0x0010, PROCESS_VM_WRITE = 0x0020
        }

        #endregion

        #endregion
    }

    public class DirectUpdate <T> where T : struct
    {
        public readonly  Memory           Memory;
        private readonly PointerLevelData data;

        private IntPtr basePtr;
        private IntPtr upDatePtr;

        public DirectUpdate(Memory memory, PointerLevelData data)
        {
            this.Memory = memory;
            this.data   = data;
            RecalculatePtr();
        }

        public void RecalculatePtr()
        {
            Console.WriteLine($"{this.data.Process}: " + this.Memory.MiProcessHandle.ToInt64().ToString("X8"));
            this.basePtr = this.Memory.GetModuleAddress(this.data.BaseModule);
            Console.WriteLine($"{this.data.BaseModule}: {this.basePtr}");
            this.upDatePtr = this.Memory.GetMultiLevelPointer(this.basePtr, this.data.GetOffset(), this.data.X64, true);
            Console.WriteLine("FinalPointer: " + this.basePtr);
            Console.WriteLine($"Value: {GetValue()}");
        }

        public void SetValue(T value)
        {
            this.Memory.WriteMemory<T>(this.upDatePtr, value);
        }

        public T GetValue()
        {
            return this.Memory.ReadMemory<T>(this.upDatePtr);
        }

        public T Value { [DebuggerStepThrough] get => GetValue(); [DebuggerStepThrough] set => SetValue(value); }
    }

    public class PointerLevelData
    {
        public static PointerLevelData SampleSetup { get; } = new PointerLevelData {
            X64        = false,
            Offsets    = new string[] { "0x0", "0x0" },
            Process    = Guid.NewGuid().ToString("N").ToUpper() + " // you process name hear without extension",
            BaseModule = "process base module with extension",
            Scale      = 1,
        };

        public bool     X64        { get; set; }
        public string[] Offsets    { get; set; }
        public string   Process    { get; set; }
        public string   BaseModule { get; set; }
        public float    Scale      { get; set; }

        public IntPtr[] GetOffset()
        {
            IntPtr[] res = new IntPtr[Offsets.Length];
            for (var i = 0; i < this.Offsets.Length; i++)
            {
                res[i] = new IntPtr(Convert.ToInt64(this.Offsets[i], 16));
            }

            return res;
        }
    }
}
