using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Log4Debug
{
    public class SuperRichTextBox : RichTextBox
    {

        private class paintHelper : Control
        {
            public void DefaultWndProc(ref Message m)
            {
                this.DefWndProc(ref m);
            }
        }

        private const int WM_PAINT = 0x000F;
        private const int WM_VSCROLL = 0x115;
        private const int WM_DBCLICK = 0x0203;  //双击
        private const int WM_GETFOCUS = 0x0007;    //获得焦点

        private int lockPaint;
        private bool m_allowScroll = true;
        private bool needPaint;
        private paintHelper pHelp = new paintHelper();

        public void BeginUpdate()
        {
            //lockPaint++;
            lockPaint = 1;
        }

        public void EndUpdate()
        {
            //lockPaint--;
            lockPaint = 0;
            if (lockPaint <= 0)
            {
                lockPaint = 0;
                if (needPaint)
                {
                    this.Refresh();
                    needPaint = false;
                }
            }
        }

        public void BeginAllowScroll()
        {
            m_allowScroll = false;
        }

        public void EndAllowScroll()
        {
            m_allowScroll = true;
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_PAINT:
                    if (lockPaint <= 0)
                    {
                        base.WndProc(ref m);
                    }
                    else
                    {
                        needPaint = true;
                        pHelp.DefaultWndProc(ref m);
                    }
                    return;
                case WM_VSCROLL:
                    if (m_allowScroll)
                    {
                        base.WndProc(ref m);
                    }
                    return;
                case WM_GETFOCUS:
                    return;
            }

            base.WndProc(ref m);
        }
    }
}
