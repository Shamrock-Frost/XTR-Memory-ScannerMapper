using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MemoryScanner
{
    /*
        This is the form in which the user selects a process 
    */
    public partial class Form2 : Form
    {
        public Process resultProcess;

        private List<Process> processes;

        //Form2 constructor
        public Form2()
        {
            InitializeComponent();

            resultProcess = null;

            ShowProcesses();

            //Manually add event handlers to remove redudancy
            button1.Click += new System.EventHandler(SetResultAndClose);
            listView1.DoubleClick += new System.EventHandler(SetResultAndClose);

            var SetStyle = typeof(ListView).GetMethod("SetStyle", BindingFlags.Instance | BindingFlags.NonPublic);
            SetStyle.Invoke(listView1, new object[] { ControlStyles.OptimizedDoubleBuffer, true });
        }

        //When button2 is clicked, ShowsProcesses gets called, which shows all processes
        private void button2_Click(object sender, EventArgs e)
        {
            ShowProcesses();
        }

        //Lists all processes in listView1
        private void ShowProcesses()
        {
            //Clear the listView
            listView1.Items.Clear();

            //If the list is refreshed all processes have to be disposed
            if (processes != null) {
                processes.ForEach(p => p.Dispose());
            }

            //Get all running processes
            processes = Process.GetProcesses().ToList();

            //Don't show ourselves
            processes.RemoveAll(p => p.Id == Process.GetCurrentProcess().Id);

            var imageList = new ImageList();
            imageList.ImageSize = new Size(32, 32);
            listView1.SmallImageList = imageList;

            for (int i = 0, imageIndex = 0; i < processes.Count; ++i) {
                var listViewItem = new ListViewItem();
                Image image;
                //If an exception gets thrown we just ignore the process. This happens often
                //due to having no access to certain processes
                try {
                    string mainModulePath;

                    unsafe
                    {
                        mainModulePath = new string(NativeHelper.GetProcessMainModulePath(processes[i].Handle));
                    }

                    //Get the icon of the executable
                    Icon icon = Icon.ExtractAssociatedIcon(mainModulePath);

                    image = icon.ToBitmap();

                    listViewItem.Text = Path.GetFileName(mainModulePath);
                    listViewItem.SubItems.Add(processes[i].Id.ToString());

                    listViewItem.ImageIndex = imageIndex++;
                }
                catch (Exception ex){
                    //If something failed, we dispose the process, remove it from the list and go back by one element
                    processes[i].Dispose();
                    processes.RemoveAt(i);
                    --i;
                    continue;
                }
                
                //If no exception has been thrown, we add the image and add the process to the listView
                imageList.Images.Add(image);
                listView1.Items.Add(listViewItem);
            }
        }

        //Sets the result to the selected process and closes the program
        private void SetResultAndClose(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices[0] != -1) {
                resultProcess = processes[listView1.SelectedIndices[0]];

                //remove the element from the list so that it doesn't get disposed
                processes.RemoveAt(listView1.SelectedIndices[0]);

                Close();
            }
            else {
                MessageBox.Show("Please select a process");
            }

        }

        //When the form gets closed, all Process objects get disposed
        private void Form2_FormClosed(object sender, FormClosedEventArgs e)
        {
            //When the form is closed, dispose all elements
            if (processes != null) {
                processes.ForEach((p) => p.Dispose());
            } 
        }
    }
}
