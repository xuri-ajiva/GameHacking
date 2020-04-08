using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Framework {
    public class ImportsY {
        private static readonly int PROCESS_WM_READ = 0x0010;


        const  string  procName = "hl";
        static Process process  = null;
        static IntPtr  processHandle;

        public static byte[] Read(int handle, int address, int size, ref int bytes) {
            byte[] buffer = new byte[size];
            Imports.ReadProcessMemory( handle, address, buffer, size, ref bytes );
            return buffer;
        }

        static void Main(string[] args) {
            int    bytesRead = 0;
            byte[] value     = new byte[20];
            int    address   = 0x019EF0D7;

            Console.WriteLine( "Starting" );
            process       = Process.GetProcessesByName( procName )[0];
            processHandle = Imports.OpenProcess( PROCESS_WM_READ, false, process.Id );

            while ( true ) {
                //ReadProcessMemory((int)processHandle, jumpAddresses, jumpValues, jumpValues.Length, ref bytesRead);
                value = Read( (int) processHandle, address, 20, ref bytesRead );
                Console.WriteLine( int.Parse( bytesRead.ToString() ) + " " + bytesRead );
                Thread.Sleep( 100 );
            }
        }

    }


    /// <summary>
    /// https://github.com/alessiocentorrino/hihutex-memory-class/blob/master/Memory.cs
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    public class Imports {

        [DllImport( "kernel32.dll" )]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport( "kernel32.dll" )]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);


        [DllImport( "ntdll" )] public static extern bool NtReadVirtualMemory(IntPtr  ProcessHandle, IntPtr BaseAddress, byte[] Buffer, int NumberOfBytesToRead,  out int NumberOfBytesRead);
        [DllImport( "ntdll" )] public static extern bool NtWriteVirtualMemory(IntPtr ProcessHandle, IntPtr BaseAddress, byte[] Buffer, int NumberOfBytesToWrite, out int NumberOfBytesWritten);

        private readonly IntPtr processHandle;

        public Imports(IntPtr processHandle)
            => this.processHandle = processHandle;

        /*
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public unsafe T Read <T>(IntPtr address) where T : struct {
            byte[] buffer = new byte[Unsafe.SizeOf<T>()];

            NtReadVirtualMemory( processHandle, address, buffer, buffer.Length, 0 );

            fixed (byte* b = buffer)
                return Unsafe.Read<T>( b );
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public unsafe bool Write <T>(IntPtr address, T value) where T : struct {
            byte[] buffer = new byte[Unsafe.SizeOf<T>()];

            fixed (byte* b = buffer)
                Unsafe.Write<T>( b, value );

            return NtWriteVirtualMemory( processHandle, address, buffer, buffer.Length, 0 );
        }   */

    }
    /// <summary>
    /// <exception>
    /// <![CDATA[
    /// var Found = false;
    /// 
    /// while (!Found) {
    ///     Thread.Sleep(250);
    ///     Found = Memory.Attatch("Processname");
    /// }
    /// int MyDll = Memory.GetModuleAddress("MyDll.dll");
    /// Memory.WriteMemory<type>(offset, value);
    /// var Value = Memory.ReadMemory<type>(offset);
    /// ]]></exception>
    /// </summary>
    public class Memory {
        private static Process m_iProcess;
        private static IntPtr  m_iProcessHandle;

        private static int m_iBytesWritten;
        private static int m_iBytesRead;

        public static bool Attatch(string ProcName) {
            if ( Process.GetProcessesByName( ProcName ).Length > 0 ) {
                m_iProcess = Process.GetProcessesByName( ProcName )[0];
                m_iProcessHandle =
                    Imports.OpenProcess( Flags.PROCESS_VM_OPERATION | Flags.PROCESS_VM_READ | Flags.PROCESS_VM_WRITE,
                        false, m_iProcess.Id );
                return true;
            }

            return false;
        }

        public static void WriteMemory <T>(IntPtr Address, object Value) {
            var buffer = StructureToByteArray( Value );

            Imports.NtWriteVirtualMemory( m_iProcessHandle, Address, buffer, buffer.Length, out m_iBytesWritten );
        }

        public static void WriteMemory <T>(IntPtr Adress, char[] Value) {
            var buffer = Encoding.UTF8.GetBytes( Value );

            Imports.NtWriteVirtualMemory( m_iProcessHandle, Adress, buffer, buffer.Length, out m_iBytesWritten );
        }

        public static T ReadMemory <T>(IntPtr address) where T : struct {
            var ByteSize = Marshal.SizeOf<T>();

            var buffer = new byte[ByteSize];

            Imports.NtReadVirtualMemory( m_iProcessHandle, address, buffer, buffer.Length, out m_iBytesRead );

            return ByteArrayToStructure<T>( buffer.Take( m_iBytesRead ).ToArray() );
        }

        public static byte[] ReadMemory(IntPtr offset, int size) {
            var buffer = new byte[size];

            Imports.NtReadVirtualMemory( m_iProcessHandle, offset, buffer, size, out m_iBytesRead );

            return buffer.Take( m_iBytesRead ).ToArray();
        }

        public static float[] ReadMatrix <T>(IntPtr Adress, int MatrixSize) where T : struct {
            var ByteSize = Marshal.SizeOf( typeof(T) );
            var buffer   = new byte[ByteSize * MatrixSize];
            Imports.NtReadVirtualMemory( m_iProcessHandle, Adress, buffer, buffer.Length, out m_iBytesRead );

            return ConvertToFloatArray( buffer.Take( m_iBytesRead ).ToArray() );
        }

        public static int GetModuleAddress(string Name) {
            try {
                foreach ( ProcessModule ProcMod in m_iProcess.Modules )
                    if ( Name == ProcMod.ModuleName )
                        return (int) ProcMod.BaseAddress;
            } catch { }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine( "ERROR: Cannot find - " + Name + " | Check file extension." );
            Console.ResetColor();

            return -1;
        }

        #region Other

        internal struct Flags {
            public const int PROCESS_VM_OPERATION = 0x0008;
            public const int PROCESS_VM_READ      = 0x0010;
            public const int PROCESS_VM_WRITE     = 0x0020;
        }

        #endregion

        #region Conversion

        public static float[] ConvertToFloatArray(byte[] bytes) {
            if ( bytes.Length % 4 != 0 )
                throw new ArgumentException();

            var floats = new float[bytes.Length / 4];

            for ( var i = 0; i < floats.Length; i++ )
                floats[i] = BitConverter.ToSingle( bytes, i * 4 );

            return floats;
        }

        private static T ByteArrayToStructure <T>(byte[] bytes) where T : struct {
            var handle = GCHandle.Alloc( bytes, GCHandleType.Pinned );

            try {
                return (T) Marshal.PtrToStructure( handle.AddrOfPinnedObject(), typeof(T) );
            } finally {
                handle.Free();
            }
        }

        private static byte[] StructureToByteArray(object obj) {
            var length = Marshal.SizeOf( obj );

            var array = new byte[length];

            var pointer = Marshal.AllocHGlobal( length );

            Marshal.StructureToPtr( obj, pointer, true );
            Marshal.Copy( pointer, array, 0, length );
            Marshal.FreeHGlobal( pointer );

            return array;
        }

        #endregion

    }
}
