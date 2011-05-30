﻿using System;
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

        private void timer_Tick(object sender, EventArgs e)
        {
            List<string> logList = null;
            List<LogLevel> logLevelList = null;

            m_logAnalyser.GetLogs(out logList, out logLevelList);
            if (null != logList)
            {
                for (int i = 0; i < logList.Count; ++i)
                {
                    switch(logLevelList[i])
                    {
                        case LogLevel.Debug:
                            richTextBox.SelectionColor = Color.Green;
                            break;
                        case LogLevel.Info:
                            richTextBox.SelectionColor = Color.White;
                            break;
                        case LogLevel.Warn:
                            richTextBox.SelectionColor = Color.Yellow;
                            break;
                        case LogLevel.Error:
                            richTextBox.SelectionColor = Color.Red;
                            break;
                        case LogLevel.Undefine:
                            richTextBox.SelectionColor = Color.Gray;
                            break;
                        default:
                            Debug.Assert(false);
                            break;
                    }

                    richTextBox.AppendText(logList[i]);
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
    }
}
