using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Log4Debug;
using System.Diagnostics;
using Log4Debug.Properties;

namespace Log4Debug
{
    public partial class Main : Form
    {
        private LogAnalyser m_logAnalyser = null;

        private bool m_textBoxCanScroll = true;
        private object m_scrollMutex = new object();

        private bool m_printErr = true;
        private bool m_printWarn = true;
        private bool m_printInfo = true;
        private bool m_printDebug = true;
        private object m_printLevelMutex = new object();

        public Main()
        {
            InitializeComponent();

            connectToolStripMenuItem.Text = Resources.Connect;
        }

        private void CloseWorkers()
        {
            if (connectToolStripMenuItem.Text.Equals(Resources.CutConnect))
            {
                timer.Stop();


                Debug.Assert(null != m_logAnalyser);
                m_logAnalyser.Stop();
                m_logAnalyser = null;

            }

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseWorkers();
            Close();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox about = new AboutBox();
            about.ShowDialog();
            
        }

        private void errorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void warnToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void infoToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void debugToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void timer_Tick(object sender, EventArgs e)
        {
            bool canScroll = true;
            lock (m_scrollMutex)
            {
                canScroll = m_textBoxCanScroll;
            }


            List<string> logList = null;
            List<LogLevel> logLevelList = null;


            m_logAnalyser.GetLogs(out logList, out logLevelList);
            if (null != logList)
            {

                richTextBox.BeginUpdate();
                for (int i = 0; i < logList.Count; ++i)
                {
                    bool canPrint = true;
                    switch(logLevelList[i])
                    {
                        case LogLevel.Debug:
                            richTextBox.SelectionColor = Color.Green;

                            lock (m_printLevelMutex)
                            {
                                canPrint = m_printDebug;
                            }
                            break;
                        case LogLevel.Info:
                            richTextBox.SelectionColor = Color.White;

                            lock (m_printLevelMutex)
                            {
                                canPrint = m_printInfo;
                            }
                            break;
                        case LogLevel.Warn:
                            richTextBox.SelectionColor = Color.Yellow;

                            lock (m_printLevelMutex)
                            {
                                canPrint = m_printWarn;
                            }
                            break;
                        case LogLevel.Error:
                            richTextBox.SelectionColor = Color.Red;

                            lock (m_printLevelMutex)
                            {
                                canPrint = m_printErr;
                            }
                            break;
                        case LogLevel.Undefine:
                            richTextBox.SelectionColor = Color.Gray;
                            break;
                        default:
                            Debug.Assert(false);
                            break;
                    }


                    if (canPrint)
                    {
                        richTextBox.AppendText(logList[i]);
                    }
                    
                }

                
                //richTextBox.Focus();
                richTextBox.EndUpdate();
                

                if (canScroll)
                {
                    richTextBox.ScrollToCaret();
                }
                
            
                

                logLevelList = null;
                logList = null;
            }
        }

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (connectToolStripMenuItem.Text.Equals(Resources.Connect))
            {
                Debug.Assert(null == m_logAnalyser);
                m_logAnalyser = new LogAnalyser();
                m_logAnalyser.Start();

                timer.Start();

                connectToolStripMenuItem.Text = Resources.CutConnect;
            }
            else if (connectToolStripMenuItem.Text.Equals(Resources.CutConnect))
            {
                CloseWorkers();
                

                connectToolStripMenuItem.Text = Resources.Connect;
            }
            else
            {
                Debug.Assert(false);
            }

        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseWorkers();
        }

        private void 剪切ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox.SelectAll();
            richTextBox.Copy();
            richTextBox.Clear();
        }

        private void 复制ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox.SelectAll();
            richTextBox.Copy();
        }

        private void 清空ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox.Clear();
        }

        private void richTextBox_MouseDown(object sender, MouseEventArgs e)
        {
            
        }

        private void richTextBox_VScroll(object sender, EventArgs e)
        {

        }

        private void scrollStartPauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (scrollStartPauseToolStripMenuItem.Text.Equals(Resources.StartScroll))
            {
                lock (m_scrollMutex)
                {
                    m_textBoxCanScroll = true;
                }
                scrollStartPauseToolStripMenuItem.Text = Resources.StopScroll;

                richTextBox.BeginAllowScroll();
            }
            else if (scrollStartPauseToolStripMenuItem.Text.Equals(Resources.StopScroll))
            {
                lock (m_scrollMutex)
                {
                    m_textBoxCanScroll = false;
                }
                scrollStartPauseToolStripMenuItem.Text = Resources.StartScroll;

                richTextBox.EndAllowScroll();
            }
            else
            {
                Debug.Assert(false);
            }
        }

        private void richTextBox_Click(object sender, EventArgs e)
        {
            
        }

        private void errorToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            
            lock (m_printLevelMutex)
            {
                m_printErr = errorToolStripMenuItem.Checked == true ? true : false;
            }
            
        }

        private void warnToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            lock (m_printLevelMutex)
            {
                m_printWarn = warnToolStripMenuItem.Checked == true ? true : false;
            }
        }

        private void infoToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            lock (m_printLevelMutex)
            {
                m_printInfo = infoToolStripMenuItem.Checked == true ? true : false;
            }
        }

        private void debugToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            lock (m_printLevelMutex)
            {
                m_printDebug = debugToolStripMenuItem.Checked == true ? true : false;
            }
        }

       
    }
}
