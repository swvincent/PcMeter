using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

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