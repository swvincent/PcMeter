﻿/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// PcMeter Program.cs
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
//using System.Collections.Generic;
//using System.Linq;
using System.Windows.Forms;
using MutexManager;

namespace PcMeter
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (!SingleInstance.Start())
            {
                MessageBox.Show("Another instance of PC Meter is already running. Program will close.",
                    "PC Meter already running", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                var applicationContext = new CustomApplicationContext();
                Application.Run(applicationContext);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Program Terminated Unexpectedly",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            SingleInstance.Stop();
        }
    }
}
