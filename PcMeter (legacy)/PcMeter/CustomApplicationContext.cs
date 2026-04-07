using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;           //Performance counters
using System.IO;                    //Serial
using System.IO.Ports;              //Serial
using PcMeter.Properties;           //Settings load/save

namespace PcMeter
{
    /// <summary>
    /// Framework for running application as a tray app.
    /// Based on Michael Soren's excellent article and code from here:
    /// https://www.red-gate.com/simple-talk/dotnet/.net-framework/creating-tray-applications-in-.net-a-practical-guide/
    /// </summary>
    /// <remarks>
    /// Tray app code adapted from "Creating Applications with NotifyIcon in Windows Forms", Jessica Fosler,
    /// http://web.archive.org/web/20101218130444/http://windowsclient.net/articles/notifyiconapplications.aspx
    /// </remarks>
    public class CustomApplicationContext : ApplicationContext
    {

        #region Local Declarations

        private System.ComponentModel.IContainer components;	//a list of components to dispose when the context is disposed
        private NotifyIcon notifyIcon;				            //the icon that sits in the system tray
        private Timer meterTimer;                               //Timer for sending data to meter
        private PerformanceCounter cpuCounter;                  //For CPU stats
        PerfomanceInfoData perfData;                            //For memory %
        private SerialPort meterPort;                           //Serial port

        //Menu
        private ToolStripLabel cpuLabel;                        //For displaying CPU%
        private ToolStripLabel memLabel;                        //For displaying Memory%
        private ToolStripMenuItem connectMenuItem;              //Menu item to connect/disconnect com port
        private ToolStripMenuItem settingsMenuItem;             //Menu item to open settings form

        //Forms
        private AboutForm aboutForm;
        private SettingsForm settingsForm;

        #endregion Local Declarations

        #region Constructor and Init

        public CustomApplicationContext()
        {
            InitializeContext();

            //Connect serial on startup
            InitSerial();               

            if (InitCounters())
            {
                meterTimer.Start();
            }
            else
            {
                //TODO: Exit application?
            }
        }

        private void InitializeContext()
        {
            components = new System.ComponentModel.Container();

            //Notify icon
            notifyIcon = new NotifyIcon(components)
            {
                ContextMenuStrip = new ContextMenuStrip(),
                Icon = new Icon("pcmeter.ico"),
                Text = "PC Meter",
                Visible = true
            };

            //Context Menu for notify icon
            cpuLabel = new ToolStripLabel("CPU: ?");
            memLabel = new ToolStripLabel("Memory: ?");

            connectMenuItem = new ToolStripMenuItem("&Connected", null, connectMenuItem_Click);
            settingsMenuItem = new ToolStripMenuItem("&Settings", null, settingsMenuItem_Click);

            ContextMenuStrip menuStrip = notifyIcon.ContextMenuStrip;

            menuStrip.ShowCheckMargin = true;
            menuStrip.ShowImageMargin = false;
            menuStrip.Items.Add(cpuLabel);
            menuStrip.Items.Add(memLabel);
            menuStrip.Items.Add(new ToolStripSeparator());
            menuStrip.Items.Add(connectMenuItem);
            menuStrip.Items.Add(new ToolStripSeparator());
            menuStrip.Items.Add(settingsMenuItem);
            menuStrip.Items.Add("&About", null, showAboutItem_Click);
            menuStrip.Items.Add("&Exit", null, exitItem_Click);

            notifyIcon.DoubleClick += notifyIcon_DoubleClick;
            notifyIcon.MouseUp += notifyIcon_MouseUp;

            //meterTimer
            meterTimer = new Timer(components);
            meterTimer.Interval = 500;
            meterTimer.Tick += new System.EventHandler(meterTimer_Tick);
        }

        #endregion Constructor

        #region Timer and Update Stats

        private void meterTimer_Tick(object sender, EventArgs e)
        {
            UpdateAndSentStats();
        }

        private void UpdateAndSentStats()
        {
            try
            {
                string cpuPerc = Math.Round(cpuCounter.NextValue(), MidpointRounding.AwayFromZero).ToString();

                perfData = PsApiWrapper.GetPerformanceInfo();
                string memPerc = Math.Round(100 - (((decimal)perfData.PhysicalAvailableBytes / (decimal)perfData.PhysicalTotalBytes) * 100), MidpointRounding.AwayFromZero).ToString();

                cpuLabel.Text = string.Format("CPU: {0}%", cpuPerc);
                memLabel.Text = string.Format("Memory: {0}%", memPerc);

                if (meterPort.IsOpen)
                {
                    string portData = string.Format("C{0}\rM{1}\r", cpuPerc, memPerc);
                    meterPort.Write(portData);
                }
            }
            catch (IOException caught)
            {
                if (caught.Message.Contains("A device attached to the system is not functioning."))
                {
                    //I've seen this error after the computer resumes from sleep. Then the serial communiction starts working
                    //again. So I just ignore it. Maybe I should close serial on sleep and reopen on wake, but that gets
                    //complicated and this seems okay. I need to test it on more computers to make sure. So yeah maybe a
                    //sloppy way to handle it but if it works I'm sticking with it.
                }
                else
                {
                    //Serial failed, maybe device was unplugged?  Disable serial, notify user and resume.
                    //Disable timer (so we only see error once)
                    meterTimer.Stop();

                    //Dispose serial
                    DisposeSerial();

                    MessageBox.Show(string.Format("Communication with the device has been lost.  Has it been unplugged?\n\nDetails: {0}",
                        caught.Message), "PC Meter Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    //Now that serial is disconnected, it's safe to reenable timer.
                    meterTimer.Enabled = true;
                }
            }
            catch (Exception caught)
            {
                //Disable timer (so we only see error once)
                meterTimer.Stop();
                WinFormHelper.DisplayErrorMessage("Update meters", caught);
                //TODO: Handle better?
                //Now that the timer is disabled, the program isn't very useful.  Close.
                ExitThread();
            }
        }

        #endregion Timer and Update Stats

        #region The Child Forms

        private void ShowAboutForm()
        {
            if (aboutForm == null)
            {
                aboutForm = new AboutForm();
                aboutForm.Closed += aboutForm_Closed; //avoid reshowing disposed form
                aboutForm.Show();
            }
            else
                aboutForm.Activate();
        }


        private void ShowSettingsForm()
        {
            if (settingsForm == null)
            {
                settingsForm = new SettingsForm();
                settingsForm.FormClosed += settingsForm_Closed; //avoid reshowing disposed form
                settingsForm.Show();
            }
            else
                settingsForm.Activate();
        }


        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowAboutForm();
        }


        // From http://stackoverflow.com/questions/2208690/invoke-notifyicons-context-menu
        private void notifyIcon_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mi.Invoke(notifyIcon, null);
            }
        }

        private void showAboutItem_Click(object sender, EventArgs e)
        {
            ShowAboutForm();
        }

        private void settingsMenuItem_Click(object sender, EventArgs e)
        {
            ShowSettingsForm();
        }

        // null out the forms so we know to create a new one.
        private void aboutForm_Closed(object sender, EventArgs e)
        {
            aboutForm = null;
        }

        private void settingsForm_Closed(object sender, EventArgs e)
        {
            settingsForm = null;
        }

        #endregion The Child Forms

        #region Exit

        protected override void Dispose(bool disposing)
        {
            //Dispose things specific to this app
            DisposeCounters();
            DisposeSerial();

            //Dispose things like the notify icon.
            if (disposing && components != null)
                components.Dispose();
        }

        private void exitItem_Click(object sender, EventArgs e)
        {
            ExitThread();
        }

        protected override void ExitThreadCore()
        {
            // before we exit, let forms clean themselves up.
            if (aboutForm != null) { aboutForm.Close(); }

            notifyIcon.Visible = false; // should remove lingering tray icon
            base.ExitThreadCore();
        }

        #endregion Exit

        #region Serial

        private void connectMenuItem_Click(object sender, EventArgs e)
        {
            if (connectMenuItem.Checked)
                DisposeSerial();
            else
                InitSerial();
        }

        private void RefreshMenuOptions()
        {
            bool portActive = meterPort != null && meterPort.IsOpen;
            connectMenuItem.Checked = portActive;
            settingsMenuItem.Enabled = !portActive;
        }

        /// <summary>
        /// Initialize serial port and connect.  Display error if any occur.
        /// If program is minimized, then display brief message using balloon tip instead.
        /// </summary>
        /// <returns></returns>
        private bool InitSerial()
        {
            string portName = Settings.Default.MeterComPort;

            try
            {
                meterPort = new SerialPort(portName, 9600);
                meterPort.ReadTimeout = 500;
                meterPort.WriteTimeout = 500;
                meterPort.Open();
                notifyIcon.BalloonTipText = "PC Meter connected to " + meterPort.PortName;
                notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                notifyIcon.ShowBalloonTip(2000);
                RefreshMenuOptions();
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                //Port is likely already in use.
                MessageBox.Show(string.Format("Access to {0} is denied. It may already be in use by another application or process.", meterPort.PortName),
                    "PC Meter Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                RefreshMenuOptions();
                return false;
            }
            catch (IOException)
            {
                //Port likely doesn't exist.
                MessageBox.Show(string.Format("{0} could not be opened.  Check to be sure that it is a valid COM port.", meterPort.PortName),
                    "PC Meter Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                RefreshMenuOptions();
                return false;
            }
            catch (Exception caught)
            {
                WinFormHelper.DisplayErrorMessage("Initializing serial port", caught);
                RefreshMenuOptions();
                return false;
            }
        }

        private bool DisposeSerial()
        {
            try
            {
                if (meterPort != null && meterPort.IsOpen)
                {
                    //Wait for write buffer to clear, then close and dispose
                    //Note: It's important that timeout it set to avoid deadlock
                    while (meterPort.BytesToWrite > 0) { }
                    meterPort.Dispose();
                }
                RefreshMenuOptions();
                return true;
            }
            catch (IOException)
            {
                //IOException can occur on .Dispose if port no longer exists (such as when a USB virtual COM port is
                //unplugged.) I've found no way to avoid it. When it happens, the port name still shows in the list of
                //available ports, the port object isn't null, and .IsOpen is still true. So I just silently catch
                //the exception and return true since they're really nothing to close or dispose anways.
                RefreshMenuOptions();
                return true;
            }
            catch (Exception caught)
            {
                WinFormHelper.DisplayErrorMessage("Disposing and closing serial port", caught);
                RefreshMenuOptions();
                return false;
            }
        }

        #endregion Serial

        #region Counters Init/Dispose

        private bool InitCounters()
        {
            try
            {
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                return true;
            }
            catch
            {
                MessageBox.Show("The performance counter(s) could not be initialized. The program cannot continue.", "Perf. Counter Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Dispose counter objects
        /// </summary>
        private void DisposeCounters()
        {
            try
            {
                // dispose of the counters
                if (cpuCounter != null)
                { cpuCounter.Dispose(); }
            }
            finally
            { PerformanceCounter.CloseSharedResources(); }
        }

        #endregion Counters Init/Dispose

    }
}