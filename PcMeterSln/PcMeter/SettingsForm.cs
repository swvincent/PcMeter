/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// PcMeter SettingsForm.cs
//
// PC Meter application written by Scott W. Vincent.
//
// http://www.swvincent.com/pcmeter
// Visit the webpage for more information including info on the arduino-based meter I built that is driven by this program.
//
// Email: scott@swvincent.com
//
// MIT License
// 
// Copyright (c) 2018 Scott W. Vincent
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PcMeter.Properties;           //Settings load/save
using System.IO.Ports;              //For serial port list

namespace PcMeter
{
    public partial class SettingsForm : Form
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
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


        /// <summary>
        /// Close form without saving changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        /// <summary>
        /// Attempt to save changes and close form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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