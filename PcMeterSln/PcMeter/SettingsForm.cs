using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PcMeter.Properties;
using System.IO.Ports;

namespace PcMeter
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            //Load COM ports into combo box
            List<string> comPortList = new List<string>(SerialPort.GetPortNames());

            if (comPortList.Count > 0)
            {
                comPortList.Sort();
                comPortComboBox.DataSource = comPortList;

                //Retrieve port from settings
                comPortComboBox.SelectedItem = Settings.Default.MeterComPort;
            }
            else
            {
                //No COM ports on computer!
                MessageBox.Show("No COM Ports were found! Make sure the PC Meter device is connected and the drivers are installed.", "No COM Ports",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                comPortComboBox.Enabled = false;
                okButton.Enabled = false;
            }            
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            if (ValidateFormData())
            {
                Settings.Default.MeterComPort = comPortComboBox.SelectedItem.ToString();
                Settings.Default.Save();
                this.Close();
            }
        }

        private bool ValidateFormData()
        {
            if (comPortComboBox.SelectedIndex != -1)
            {
                //Ok
                settingsErrorProvider.SetError(comPortComboBox, "");
                return true;
            }
            else
            {
                //Missing value
                settingsErrorProvider.SetError(comPortComboBox, "COM Port is required.");
                return false;
            }
        }
    }
}