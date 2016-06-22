using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MemoryScanner
{
    /*
        This class imports all the functions from the MemSMAPI namespace in the DLL
        and defines the types that are needed.
    */
    unsafe public class MemSMAPI
    {
        private const string path = "MemSMAPI.dll";

		/* These are all the functions I am importing
			and the struct defintions. The names are mangled because otherwise
			overloading wouldn't be possible. To find out the mangled names I had to use dumpbin.exe.
		*/
		
        [StructLayout(LayoutKind.Sequential)]
        public struct Region
        {
            public void* baseAddress;

            public UInt32 regionSize;
            public UInt32 numberOfPages;

            public UInt32 state;

            public Int32 readable;
            public Int32 writable;
            public Int32 executable;

            public UInt32 type;

            public Region* next;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct MemoryMap
        {
            public UInt32 size;
            public UInt32 pageSize;
            public Region* regions;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ScanResult
        {
            public UInt32 resultSize;
            public void** result;
        }

        [DllImport(path,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "?CreateMemoryMap@MemSMAPI@@YA?AUMemoryMap@@PBX0@Z",
            ExactSpelling = false)]
        public extern static MemoryMap CreateMemoryMap(UIntPtr startAddress, UIntPtr endAddress);

        [DllImport(path,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "?DummyFunction@MemSMAPI@@YAXXZ",
            ExactSpelling = false)]
        public extern static void DummyFunction();

        [DllImport(path,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "?GetLastExceptionMessage@MemSMAPI@@YAPBDXZ",
            ExactSpelling = false)]
        public extern static sbyte* GetLastExceptionMessage();

        [DllImport(path,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "?InitializeMemSMAPI@MemSMAPI@@YAXPAX@Z",
            ExactSpelling = false)]
        public extern static void InitializeMemSMAPI(IntPtr processHandle);

        [DllImport(path,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "?UninitializeMemSMAPI@MemSMAPI@@YAXXZ",
            ExactSpelling = false)]
        public extern static void UninitializeMemSMAPI();

        [DllImport(path,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "?ScanForBytes@MemSMAPI@@YA?AUScanResult@1@PBX0PBEK@Z",
            ExactSpelling = false)]
        public extern static ScanResult ScanForBytes(void* startAddress, void* endAddress, byte* bytes, UInt32 bytesSize);

        [DllImport(path,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "?ScanForBytes@MemSMAPI@@YA?AUScanResult@1@PAPBXKPBEK@Z",
            ExactSpelling = false)]
        public extern static ScanResult ScanForBytes(void** addresses, UInt32 addressesSize, byte* bytes, UInt32 bytesSize);

        [DllImport(path,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "?DeleteMemoryMap@MemSMAPI@@YAXUMemoryMap@@@Z",
            ExactSpelling = false)]
        public extern static void DeleteMemorMap(MemoryMap memoryMap);

        [DllImport(path,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "?DeleteScanResult@MemSMAPI@@YAXUScanResult@1@@Z",
            ExactSpelling = false)]
        public extern static void DeleteScanResult(ScanResult scanResult);

        [DllImport(path,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "?WriteProcessMemory@MemSMAPI@@YAXPAXPBXK@Z",
            ExactSpelling = false)]
        public extern static void WriteProcessMemory(void* address, void* data, UInt32 dataSize);

        [DllImport(path,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "?ReadProcessMemory@MemSMAPI@@YAXPAX0K@Z",
            ExactSpelling = false)]
        public extern static void ReadProcessMemory(void* address, void* buffer, UInt32 bufferSize);

        [DllImport(path,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "?IsMemSMAPIInitialized@MemSMAPI@@YA_NXZ",
            ExactSpelling = false)]
        public extern static bool IsMemSMAPIInitialized();
    }
}
