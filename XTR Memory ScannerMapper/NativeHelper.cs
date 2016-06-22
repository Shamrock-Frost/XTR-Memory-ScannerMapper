using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MemoryScanner
{
    /*
        This class imports all the functions from the NativeHelper namespace in the dll.
    */
    unsafe class NativeHelper
    {
        private const string path = @"MemSMAPI.dll";

		//I ended up just using on in here, makes the class kind of useless
        [DllImport(path,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "?GetProcessMainModulePath@NativeHelper@@YAPBDPAX@Z",
            ExactSpelling = false)]
        public extern static sbyte* GetProcessMainModulePath(IntPtr processHandle);
    }
}
