using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MemoryScanner
{
    public partial class Form4 : Form
    {
        public Address resultAddress;

        private bool containsText = false;

        //Form4 ctor
        public Form4()
        {
            InitializeComponent();

            resultAddress = null;
        }

        //When button1 is clicked, the input is stored in the resultAddress and the form is closed
        private void button1_Click(object sender, EventArgs e)
        {
			if (!containsText) {
				MessageBox.Show("Please enter an address");
				return;
			}
			
			//construct new Address from the input and store it in resultAddress
            resultAddress = new Address();
            unsafe {
                resultAddress.address = (void*) Convert.ToUInt32(textBox1.Text, textBox1.Text.StartsWith("0x") ? 0x10 : 10);
                resultAddress.typeIndex = comboBox1.SelectedIndex;
                resultAddress.signed = checkBox1.Checked;
                resultAddress.size = (comboBox1.SelectedIndex > 5) ? (int) Convert.ToUInt32(textBox2.Text, textBox2.Text.StartsWith("0x") ? 0x10 : 10) : -1;
            }

            Close();
        }

        //Depending on the selected type the form has to be adjusted
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
			//for some types you can't select if it's signed or not
            if (comboBox1.SelectedIndex >= 4 && comboBox1.SelectedIndex != 8) {
                checkBox1.Enabled = false;
                checkBox1.Checked = false;
            }
            else {
                checkBox1.Enabled = true;
            }

            //allow length
            if (comboBox1.SelectedIndex > 5) {
                textBox2.Enabled = true;
            }
            else {
                textBox2.Enabled = false;
                textBox2.Text = "";
            }

            button1.Enabled = true;
        }

		//don't allow empty text
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            containsText = textBox1.Text != "";
        }
    }
}
