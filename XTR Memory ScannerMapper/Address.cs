using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryScanner
{	
	/*
        Holds information about an address
    */
    unsafe public class Address
    {
        public int typeIndex;
        public bool signed;
        public void* address;
        public int size; //-1 for default

		//returns hex representation of address
        override public string ToString()
        {
            return "0x" + ((UInt32) address).ToString("X8");
        }
    }
}
