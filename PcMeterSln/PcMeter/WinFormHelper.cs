/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// PcMeter WinFormHelper.cs
//
// Contains helper code needed by PcMeter.
//
// PC Meter application
//
// http://www.swvincent.com/pcmeter
// Visit the webpage for more information including info on the arduino-based meter I built that is driven by this program.
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
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;     //For messagebox

namespace PcMeter
{
    public static class WinFormHelper
    {

        #region Error Handling

        /// <summary>
        /// Simple error message display.
        /// </summary>
        /// <param name="processDescription">Description of process that error occured in</param>
        /// <param name="caught">Exception that was caught</param>
        public static void DisplayErrorMessage(string processDescription, Exception caught)
        {
            StringBuilder b = new StringBuilder();

            b.Append("An error was caught.  Details:\n\nProcess Desc.: " + processDescription);
            
            Exception c = caught;

            while (c != null)
            {
                string message = "\n\nError Desc.: " + c.Message + "\n\n" +
                "Error Type:" + c.GetType().ToString() + "\n\n" +
                "Stack Trace:\n" + c.StackTrace;
                b.Append(message);
                c = c.InnerException;
            }

            MessageBox.Show(b.ToString(), "Error Caught", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion Error Handling

    }
}
