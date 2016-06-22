using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MemoryScanner
{
    /*
        This is the main form of the program
    */ 
    public partial class Form1 : Form
    {
        private volatile Process currentProcess;
        private MemSMAPI.ScanResult lastScanResult;
        private Thread thread;
        private volatile byte[] currentScanValue;
        private volatile int currentScanTypeIndex;
        private volatile bool currentScanSigned;
        private List<Address> addresses;
        private bool failedToConstruct;

        //Form1 constructor
        public Form1()
        {
            InitializeComponent();

            failedToConstruct = false;

            try {
                MemSMAPI.DummyFunction();
            }
            catch(Exception ex) {
				//I can't end the application in the ctor, so I set the flag
				//and end it in form1.Load()
                MessageBox.Show(ex.Message);
                failedToConstruct = true;
                return;
            }

            addresses = new List<Address>();

			//add event handlers manually
            textBox1.TextChanged += new EventHandler(EnableOrDisabledButtons);
            comboBox1.SelectedIndexChanged += new EventHandler(EnableOrDisabledButtons);

            currentProcess = null;

            unsafe {
                lastScanResult.result = null;
            }
            lastScanResult.resultSize = 0;

            //copied from stackoverflow, really nice code
            var SetStyle = typeof(ListView).GetMethod("SetStyle", BindingFlags.Instance | BindingFlags.NonPublic);
            SetStyle.Invoke(listView1, new object[] { ControlStyles.OptimizedDoubleBuffer, true });
            SetStyle.Invoke(listView2, new object[] { ControlStyles.OptimizedDoubleBuffer, true });

            thread = new Thread(new ThreadStart(BackgroundThread));

            thread.Start();

            //weird windows 10 bug
            //this.Height = 530;
        }

        //Deletes the currentScanResult and checks if it's safe to do so before
        void SafeDeleteScanResult()
        {
            unsafe {
				//safely delete a scan result
                if (lastScanResult.result != null && lastScanResult.resultSize > 0) {
                    MemSMAPI.DeleteScanResult(lastScanResult);

                    lastScanResult.result = null;
                    lastScanResult.resultSize = 0;
                }
            }
        }

		//Sets the item color to red and puts in questionmarks
        void SetItemUnreadable(ListView listView, int index)
        {
            listView.Items[index].SubItems[listView.Items[index].SubItems.Count - 1].BackColor = Color.Red;
            listView.Items[index].SubItems[listView.Items[index].SubItems.Count - 1].Text = "???";
        }

		//Thread that runs in background and updates the values in the list box and checks if the process
		//has exited
        private void BackgroundThread()
        {
            while (true) {
                if (IsHandleCreated && !IsDisposed) {
                    if (currentProcess == null || currentProcess.HasExited) {
                        if (currentProcess != null) {
                            currentProcess.Dispose();
                            currentProcess = null;
                        }

                        MemSMAPI.UninitializeMemSMAPI();

						//I need to call invoke because this is another thread which can't access the GUI
                        Invoke(new MethodInvoker(delegate
                        {
                            pictureBox1.Image = null;
                            label1.Left = 12;
                            label1.Text = "No Process Selected";
                            button1.Enabled = false;
                            button2.Enabled = false;
                            button3.Enabled = false;
                            listView1.Columns[0].Text = "Addresses";
                        }));

                        SafeDeleteScanResult();
                    }


                    Invoke(new MethodInvoker(delegate
                    {
                        if (currentProcess != null) {
							//read all values
                            var values = ReadAddresses(addresses);

                                for (int i = 0; i < addresses.Count; ++i) {
									//If unreadable, set it to unreadable
                                    if (values[i] == null) {
                                        listView2.Items[i].SubItems[1].Text = TypeToString(addresses[i].typeIndex, addresses[i].signed);
                                        SetItemUnreadable(listView2, i);
                                    }
                                    else {
                                        listView2.Items[i].Text = addresses[i].ToString();
                                        listView2.Items[i].SubItems[1].Text = TypeToString(addresses[i].typeIndex, addresses[i].signed);
                                        listView2.Items[i].SubItems[2].Text = BytesToString(values[i], addresses[i].typeIndex, addresses[i].signed);
                                    }
                                }
                        

							//same thing as above, just for listview1
							//and check if the values are different from the scan value
                            List<Address> addresses2 = new List<Address>(listView1.Items.Count);

                            for (int i = 0; i < listView1.Items.Count; ++i) {
                                addresses2.Add(new Address());
                                unsafe
                                {
                                    addresses2[i].address = (void*)Convert.ToUInt32(listView1.Items[i].Text, 0x10);
                                }
                                addresses2[i].typeIndex = currentScanTypeIndex;
                                addresses2[i].signed = currentScanSigned;
                                addresses2[i].size = -1;
                            }


                            //read addresses
                            values = ReadAddresses(addresses2);
                            for (int i = 0; i < values.Count; ++i) {
                                if (values[i] == null) {
                                    SetItemUnreadable(listView1, i);
                                }
                                else {
                                    listView1.Items[i].SubItems[1].Text = BytesToString(values[i], currentScanTypeIndex, currentScanSigned);
									//red == changed, green == same
                                    listView1.Items[i].SubItems[1].BackColor = values[i].SequenceEqual(currentScanValue) ? Color.Green : Color.Red;
                                }
                            }
                        }
                        else {
                            //Set all to unreadable in both listViews
                            (new List<ListView> { listView1, listView2 }).ForEach(l =>
                                {
                                    for (int i = 0; i < l.Items.Count; ++i) {
                                        SetItemUnreadable(l, i);
                                    }
                                });
                        }
                    }));
                }

                Thread.Sleep(100);
            }
        }

		//Loop through the address and use the API to read from them
        private List<byte[]> ReadAddresses(List<Address> addresses)
        {
            var result = new List<byte[]>();

            foreach(var address in addresses) {
                int typeSize = address.size == -1 ? GetTypeSize(address.typeIndex) : address.size;
                byte[] buffer = new byte[typeSize];

                unsafe {
                    fixed (byte* bufferPtr = buffer) {
                        try {
                            MemSMAPI.ReadProcessMemory(address.address, bufferPtr, (UInt32) typeSize);
                        }
                        catch {
                            result.Add(null);
                            continue;
                        }
                        
                    }
                }

                result.Add(buffer);
            }

            return result;
        }

        //Shows form2, in which the user can select a process. When one is selected,
        //the form gets updated so that a scan can be started
        private void selectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Form2 form2 = new Form2()) {
                form2.ShowDialog();

                if (form2.resultProcess == null) {
                    MessageBox.Show("Please select a process");
                    return;
                }

				//clear all of the previous results
                listView1.Items.Clear();
                listView2.Items.Clear();
                addresses.Clear();
                SafeDeleteScanResult();

				//new current process
                currentProcess = form2.resultProcess;

                string mainModulePath;

                unsafe {
                    mainModulePath = new string(NativeHelper.GetProcessMainModulePath(currentProcess.Handle));
                }

				//move text next to the icon
                label1.Left = 50;
                pictureBox1.Image = Icon.ExtractAssociatedIcon(mainModulePath).ToBitmap();
                label1.Text = currentProcess.ProcessName + " [" + currentProcess.Id.ToString() + "]";

				//allow selecting a type and scannning
                AllowTypeChange(true);
                button2.Enabled = false;
                button3.Enabled = true;

                try {
                    MemSMAPI.InitializeMemSMAPI(currentProcess.Handle);
                }
                catch (Exception ex) {
                    HandleException(ex);
                }
                

                EnableOrDisabledButtons(null, null);
            }
        }

		//Checks if process is selected, a type is selected and a scan value is entered
        private void EnableOrDisabledButtons(object sender, EventArgs e)
        {
            button1.Enabled = currentProcess != null && comboBox1.SelectedIndex != -1 && textBox1.Text != "";
        }

		//Returns length in bytes of a scan type
        private int GetTypeSize(int typeIndex)
        {
            int currentScanValueLength = -1;

            if (currentScanValue != null) {
                currentScanValueLength = currentScanValue.Length;
            }
            
            int[] typeSizes = new int[] { -1, 1, 2, 4, 8, 4, 8, currentScanValueLength, currentScanValueLength, currentScanValueLength };

            return typeSizes[typeIndex + 1];
        }

		//Converts a type to a string and includes signed/unsigned
        private string TypeToString(int typeIndex, bool signed)
        {
            string type = comboBox1.Items[typeIndex].ToString();

            if (typeIndex >= 0 && typeIndex < 4 || typeIndex == 8) {
                type = (signed ? "signed " : "unsigned ") + type;
            }

            return type;
        }

		//Converts a byte array to a string representation of the type specified by typeindex and signed
        private string BytesToString(byte[] bytes, int typeIndex, bool signed)
        {
            if (bytes.Length == 0) {
                return "";
            }

            switch (typeIndex) {
                case 0: return ((int)bytes[0]).ToString();
                case 1: return signed ? BitConverter.ToInt16(bytes, 0).ToString(): BitConverter.ToUInt16(bytes, 0).ToString();
                case 2: return signed ? BitConverter.ToInt32(bytes, 0).ToString() : BitConverter.ToUInt32(bytes, 0).ToString();
                case 3: return signed ? BitConverter.ToInt64(bytes, 0).ToString() : BitConverter.ToUInt64(bytes, 0).ToString();
                case 4: return BitConverter.ToSingle(bytes, 0).ToString();
                case 5: return BitConverter.ToDouble(bytes, 0).ToString();
                case 6: return Encoding.ASCII.GetString(bytes);
                case 7: return Encoding.Unicode.GetString(bytes);
                case 8: {
                    string result = "";

                    for (int i = 0; i < bytes.Length; ++i) {
                        result += signed ? ((sbyte)bytes[i]).ToString() : bytes[i].ToString() + " ";
                    }

                    return result.Remove(result.Length - 1, 1);
                }
                default: return "";
            }
        }
		
		//Does the opposite of BytesToString. Converts the string to bytes according to the typeIndex & signed
        private byte[] StringToBytes(string str, int typeIndex, bool signed)
        {
            int fromBase = 10;
            if (str.StartsWith("0x")) {
                fromBase = 0x10;

            }

            switch (typeIndex) {
                case 0: return new byte[] { signed ? (byte) Convert.ToSByte(str, fromBase) : Convert.ToByte(str, fromBase) };
                case 1: return BitConverter.GetBytes(signed ? (UInt16) Convert.ToInt16(str, fromBase) : Convert.ToUInt16(str, fromBase));
                case 2: return BitConverter.GetBytes(signed ? (UInt32) Convert.ToInt32(str, fromBase) : Convert.ToUInt32(str, fromBase));
                case 3: return BitConverter.GetBytes(signed ? (UInt64) Convert.ToInt64(str, fromBase) : Convert.ToUInt64(str, fromBase));
                case 4: return BitConverter.GetBytes(Convert.ToSingle(str));
                case 5: return BitConverter.GetBytes(Convert.ToDouble(str));
                case 6: return Encoding.ASCII.GetBytes(str);
                case 7: return Encoding.Unicode.GetBytes(str);
                case 8: {
                        string[] splitted = str.Split(' ');
                        List<byte> result = new List<byte>();

                        return splitted.Select(s => signed ? (byte)Convert.ToSByte(s, fromBase) : Convert.ToByte(s, fromBase)).ToArray();
                }
                default: return new byte[] { };
            }
        }

		//Allows or disallows type changing in the gui
        void AllowTypeChange(bool allow)
        {
            comboBox1.Enabled = allow;
            checkBox1.Enabled = allow;
        }

        //When button1 is clicked, a new scan will be started
        private void button1_Click(object sender, EventArgs e)
        {
            try {
                if (lastScanResult.resultSize > 0) {
                    var dialogResult = MessageBox.Show("There is still a scan active. Starting a new scan will delete the previous results. Proceed?", "Warning", MessageBoxButtons.YesNo);

                    if (dialogResult == DialogResult.Yes) {
						//clear everything from the previous scan
                        listView1.Items.Clear();

                        SafeDeleteScanResult();

                        AllowTypeChange(true);

                        button2.Enabled = false;

                        listView1.Columns[0].Text = "Addresses";
                    }

                    return;
                }
                else {
                    AllowTypeChange(false);
                }

				//read input
                currentScanValue = StringToBytes(textBox1.Text, comboBox1.SelectedIndex, checkBox1.Checked);
                currentScanTypeIndex = comboBox1.SelectedIndex;
                currentScanSigned = checkBox1.Checked;

                unsafe {
                    fixed(byte* bytePtr = currentScanValue)
                    {
						//Scan whole memory
                        lastScanResult = MemSMAPI.ScanForBytes((void*) 0x00000000, (void*) 0xFFFFFFFF, bytePtr, (UInt32) currentScanValue.Length);
                    }
                }

                DisplayScanResult(lastScanResult);

                if (lastScanResult.resultSize == 0) {
                    MessageBox.Show("No Results");
                    AllowTypeChange(true);
                }  
                else {
                    button2.Enabled = true;
                }    
            }
            catch(Exception ex) {
                HandleException(ex);
                AllowTypeChange(true);
            }
        }

        //When the index changes, the checkBox1 gets enabled/disabled depending on the selected type
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Some types don't allow being signed or unsigned
            if (comboBox1.SelectedIndex >= 4 && comboBox1.SelectedIndex != 8) {
                checkBox1.Enabled = false;
                checkBox1.Checked = false;
            }
            else {
                checkBox1.Enabled = true;
            }
        }

        //Displays a scan results in listView1
        private void DisplayScanResult(MemSMAPI.ScanResult scanResult)
        {
            listView1.Items.Clear();

			//showing too many addresses will make the program lag. 1000 is the max
            bool greaterThan1000 = scanResult.resultSize >= 1000;

            if (greaterThan1000) {
                listView1.Columns[0].Text = "Addresses (only showing 1000 of " + scanResult.resultSize.ToString() + ")";
            }
            else {
                listView1.Columns[0].Text = "Addresses";
            }

                for (int i = 0; i < (greaterThan1000 ? 1000 : scanResult.resultSize); ++i) {
                    unsafe {
                        listView1.Items.Add("0x" + ((UInt32)scanResult.result[i]).ToString("X8"));
                    }
                    listView1.Items[i].UseItemStyleForSubItems = false;
                    listView1.Items[i].SubItems.Add(BytesToString(currentScanValue, currentScanTypeIndex, currentScanSigned));
                    listView1.Items[i].SubItems[1].BackColor = Color.Green;
                }
        }

        //Handles an exception. Depending on the exception type the program continues to run or exits
        private void HandleException(Exception ex)
        {
            string message;

			//This is how I handle C++ exceptions. GetLastExceptionMessage returns the message because SEHException doesnt contain the real
			//message
            if (ex is SEHException) {
                unsafe
                {
                    message = new string(MemSMAPI.GetLastExceptionMessage());
                }
            }
            else if (ex is FormatException || ex is ArithmeticException) {
                MessageBox.Show(ex.Message);
                return; //don't close the program when these happen
            }
            else {
                message = ex.Message;
            }

            MessageBox.Show(message);

            Application.Exit();
        }
		
        //When button3 is clicked, a memory map of the selected process is created
        unsafe private void button3_Click(object sender, EventArgs e)
        {
            MemSMAPI.MemoryMap memoryMap = MemSMAPI.CreateMemoryMap(UIntPtr.Zero, (UIntPtr) UInt32.MaxValue);
            MemSMAPI.Region* region = memoryMap.regions;

            Bitmap bmp = new Bitmap(5120, 5120);

            using (Graphics graphics = Graphics.FromImage(bmp)) {
                graphics.FillRectangle(Brushes.Red, new Rectangle(new Point(0, 0), new Size(5120, 5120)));

                float x = 0.0f;
                float y = 0.0f;

                while (region != null) {
                    Brush brush;
					//committed memory will be colored, not committed memory wont
                    if (region->state == 0x1000) {
                        if (region->executable == 1) {
                            brush = Brushes.Green; //green for executable
                        }
                        else if (region->writable == 1) {
                            brush = Brushes.Yellow; //yellow for writable
                        }
                        else if (region->readable == 1) {
                            brush = Brushes.Blue; //blue for only readable
                        }
                        else {
                            brush = Brushes.Red; //not accessible
                        }
                    }
                    else {
                        brush = Brushes.Black;
                    }

					//for each page draw a sqaure
                    for (int page = 0; page < region->numberOfPages; ++page) {
                        if (x == 5120.0f) {
                            x = 0.0f;
                            y += 5.0f;
                        }

                        graphics.FillRectangle(brush, new RectangleF(new PointF(x, y), new SizeF(5.0f, 5.0f)));

                        x += 5.0f;
                    }

                    region = region->next;
                }

                pictureBox2.Image = bmp;

				//allow clearing and saving
                button6.Enabled = true;
                button7.Enabled = true;
            }

            MemSMAPI.DeleteMemorMap(memoryMap);
        }

        //Clicking button2 will start a next scan (Go through the results of the first scan and check their (possibly) changed values)
        private void button2_Click(object sender, EventArgs e)
        {
            try {
                MemSMAPI.ScanResult scanResult;

				//read input
                currentScanValue = StringToBytes(textBox1.Text, currentScanTypeIndex, currentScanSigned);
                currentScanTypeIndex = comboBox1.SelectedIndex;
                currentScanSigned = checkBox1.Checked;

                unsafe {
                    fixed (byte* bytePtr = currentScanValue) {
						//Scan for the next value, this time using that lastScanResult
                        scanResult = MemSMAPI.ScanForBytes(lastScanResult.result, lastScanResult.resultSize, bytePtr, (UInt32) currentScanValue.Length);
                    }  
                }

				//delete the last scan result and make the last one the current one
                MemSMAPI.DeleteScanResult(lastScanResult);
                lastScanResult = scanResult;
				
				//display it
                DisplayScanResult(lastScanResult);

                if (lastScanResult.resultSize == 0) {
                    MessageBox.Show("No Results");
                    AllowTypeChange(true);
                    button2.Enabled = false;
                }
            }
            catch (Exception ex) {
                HandleException(ex);
            }
        }

        //Adds an address to the second list view and to the List<T>.
        public void AddAddressToListView2(Address address)
        {
            addresses.Add(address);

            listView2.Items.Add(address.ToString());
            listView2.Items[listView2.Items.Count - 1].UseItemStyleForSubItems = false;
            listView2.Items[listView2.Items.Count - 1].SubItems.Add(""); //add empty items so that I can simply access them later
            listView2.Items[listView2.Items.Count - 1].SubItems.Add("");
        }

        //Constructs a new Address and adds it to listView2
        unsafe void AddAddressToListView2(int typeIndex, bool signed, string addressString)
        {
            Address address = new Address();

            address.typeIndex = typeIndex;
            address.signed = signed;
            address.address = (void*)Convert.ToUInt32(addressString, addressString.StartsWith("0x") ? 0x10 : 10);
            address.size = -1;

            AddAddressToListView2(address);
        }

        //When an item in listView1 is double clicked, the address will get added to listView2
        unsafe private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            AddAddressToListView2(currentScanTypeIndex, currentScanSigned, listView1.Items[listView1.SelectedIndices[0]].Text);
        }

		//Abort the thread when closing so that the program doesnt crash
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (thread != null) {
                thread.Abort();
            }
        }

        //Shows form4, in which the user can select an address. It will then be added to listView2
        private void button4_Click(object sender, EventArgs e)
        {
            using (Form4 form4 = new Form4()) {
                try
                {
                    form4.ShowDialog();

                    if (form4.resultAddress == null)
                    {
                        MessageBox.Show("Please type in an address and select a type");
                        return;
                    }

                    AddAddressToListView2(form4.resultAddress);
                }
                catch (Exception ex)
                {
                    HandleException(ex);
                }
            }
        }

        //When an address in listView2 is clicked, form3 will be created in which the user can changed the value at that address
        private void listView2_DoubleClick(object sender, EventArgs e)
        {
            if (currentProcess == null) {
                MessageBox.Show("Process terminated, can't modify value");
                return;
            }

            try {
                int index = listView2.SelectedIndices[0];

                using (Form3 form3 = new Form3()) {
                    form3.ShowDialog();

                    if (form3.resultString == "") {
                        MessageBox.Show("Please enter a new value");
                    }

                    byte[] bytes = StringToBytes(form3.resultString, addresses[index].typeIndex, addresses[index].signed);

                    unsafe {
                        fixed (void* ptr = bytes) {
                            //Write the new value to the selected address
                            MemSMAPI.WriteProcessMemory(addresses[index].address, ptr, (UInt32)bytes.Length);
                        }
                    }
                }
            }
            catch(Exception ex) {
                HandleException(ex);
            }
        }

        //When button5 is clicked, the currently selected item from listView2 is removed
        private void button5_Click(object sender, EventArgs e)
        {
			//remove the item from the array AND the list. the array and the list should always have the same size
            if (listView2.SelectedIndices.Count > 0) {
                addresses.RemoveAt(listView2.SelectedIndices[0]);
                listView2.Items.RemoveAt(listView2.SelectedIndices[0]);
            }
        }

		//Clear the picturebox and disable the buttons
        private void button6_Click(object sender, EventArgs e)
        {
            pictureBox2.Image.Dispose();
            pictureBox2.Image = null;
            button6.Enabled = false;
            button7.Enabled = false;
        }

		//Save the bitmap from the pictureBox to a file
        private void button7_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.Title  = "Save Memory Map";
            saveFileDialog.Filter = "Image Files (*.bmp, *.png, *.jpg)|*.bmp;*.png;*.jpg";

			//if a file is selected
            if(saveFileDialog.ShowDialog() == DialogResult.OK) {
                pictureBox2.Image.Save(saveFileDialog.FileName);
            }
            else {
                MessageBox.Show("Please select a file name");
            }
        }

        //If the form failed to construct a boolean gets set to true and the program exits when Form1 gets loaded
        private void Form1_Load(object sender, EventArgs e)
        {
            if (failedToConstruct) {
                Close();
            }
        }
    }
}