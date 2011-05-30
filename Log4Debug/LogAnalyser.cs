using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace Log4Debug
{
    enum LogLevel
    {
        Error,
        Warn,
        Info,
        Debug,
        Undefine,
    }

    class LogAnalyser
    {
#region 常量及变量定义

        //日志级别常量
        private const string ErrorLevelSymbol = "[Error]";
        private const string WarnLevelSymbol = "[ Warn]";
        private const string InfoLevelSymbol = "[ Info]";
        private const string DebugLevelSymbol = "[Debug]";

        //日志链表
        private List<string> m_logList = null;
        private List<LogLevel> m_logLevelList = null;
        private object m_logListMutex = new object();

        //日志Buffer
        private const int MaxToBeContinuedLogBytes = 10 * 1024 * 1024;  //10MB
        private Byte[] m_logBytes = new Byte[MaxToBeContinuedLogBytes];
        private int m_usedLogBytes = 0;
        private object m_logBytesMutex = new object();

        //各种标志
        private const int MaxLogItemBytes = 1024;   //每条日志的最大长度,1K
        private int m_logBytesOverflowTimes = 0;    //日志Buffer溢出次数
        private ulong m_logBytesOverflowBytes = 0;  //日志Buffer溢出字节数
        private int m_debugItems = 0;               //分析出的Debug条目数
        private int m_warnItems = 0;                //分析出的warn条目数
        private int m_infoItems = 0;                //分析出的info条目数
        private int m_errorItems = 0;               //分析出的error条目数
        private int m_undefineItems = 0;            //分析出的undefine条目数
        private ulong m_totalLogBytes = 0;          //已分析的总字节数
        private ulong m_totalInputBytes = 0;        //已输入的总字节数
        private int m_totalItems = 0;               //已分析的总条目数
        private object m_logCommonMutex = new object();

        //分析线程相关
        private Thread m_analyseThread = null;
        private bool m_goonAnalyse = true;
        private object m_analyseMutex = new object();
#endregion

#region 日志链表操作
        /// <summary>
        /// 获取已分析完成的Log链表
        /// </summary>
        /// <param name="logList">输出日志链表，null为无可取数据</param>
        /// <param name="logLevelList">输出日志链表中每条日志所对应的级别</param>
        /// <returns>标示执行是否成功</returns>
        public bool GetLogs(out List<string> logList, out List<LogLevel> logLevelList)
        {
            lock (m_logListMutex)
            {
                logList = m_logList;
                logLevelList = m_logLevelList;

                m_logList = null;
                m_logLevelList = null;
            }

            return true;
        }

        /// <summary>
        /// 添加一条分析好的日志到日志链表中
        /// </summary>
        /// <param name="log">日志内容</param>
        /// <param name="level">日志级别</param>
        /// <returns>添加成功或失败</returns>
        public bool AddLog(string log, LogLevel level)
        {
            Debug.Assert(null != log);

            lock (m_logListMutex)
            {
                if (null == m_logList)
                {
                    Debug.Assert(null == m_logLevelList);

                    m_logList = new List<string>();
                    m_logLevelList = new List<LogLevel>();
                }

                m_logList.Add(log);
                m_logLevelList.Add(level);
            }

            lock (m_logCommonMutex)
            {
                ++m_totalItems;
                switch (level)
                {
                    case LogLevel.Debug:
                        ++m_debugItems;
                        break;
                    case LogLevel.Info:
                        ++m_infoItems;
                        break;
                    case LogLevel.Warn:
                        ++m_warnItems;
                        break;
                    case LogLevel.Error:
                        ++m_errorItems;
                        break;
                    case LogLevel.Undefine:
                        ++m_undefineItems;
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }
            }
            

            return true;
        }
#endregion

#region 日志Buffer操作

        /// <summary>
        /// 将日志数据推到待分析的buffer中
        /// </summary>
        /// <param name="logData">日志数据</param>
        /// <param name="dataLen">数据长度</param>
        /// <returns>压入buffer成功或失败</returns>
        public bool PushData(Byte[] logData, int dataLen)
        {
            if (dataLen <= 0)
            {
                Debug.Assert(null == logData);
                return false;
            }
            Debug.Assert(null != logData);

            //数据压入
            int bufferOverflowBytes = 0;
            lock (m_logBytesMutex)
            {
                if (m_usedLogBytes + dataLen > MaxToBeContinuedLogBytes)
                {
                    bufferOverflowBytes += dataLen;
                }
                else
                {
                    Array.Copy(logData, 0, m_logBytes, m_usedLogBytes, dataLen);
                    m_usedLogBytes += dataLen;
                }
            }

            //溢出检查
            if (bufferOverflowBytes > 0)
            {
                lock (m_logCommonMutex)
                {
                    ++m_logBytesOverflowTimes;
                    m_logBytesOverflowBytes += (ulong)bufferOverflowBytes;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// 从日志buffer中取出数据进行分析
        /// </summary>
        /// <param name="logData">输出的数据</param>
        /// <param name="dataLen">输入为最大输出长度，输出为实际输出长度</param>
        /// <returns>取数据成功或失败</returns>
        public bool PopData(ref Byte[] logData, ref int dataLen)
        {
            Debug.Assert(null != logData);

            lock (m_logBytesMutex)
            {
                Debug.Assert(dataLen >= m_usedLogBytes);
                if (m_usedLogBytes > 0)
                {
                    Array.Copy(m_logBytes, logData, m_usedLogBytes);
                    dataLen = m_usedLogBytes;

                    m_usedLogBytes = 0;
                }
            }

            return true;
        }
#endregion

#region 日志分析相关标志、错误信息等获取

        /// <summary>
        /// 获取日志分析过程中产生的相关错误
        /// </summary>
        /// <param name="overflowBytes">日志buffer溢出字节数</param>
        /// <param name="overflowTimes">日志buffer溢出次数</param>
        /// <returns></returns>
        public bool GetErrors(ref ulong overflowBytes, ref int overflowTimes)
        {
            lock (m_logCommonMutex)
            {
                overflowBytes = m_logBytesOverflowBytes;
                overflowTimes = m_logBytesOverflowTimes;
            }

            return true;
        }

        /// <summary>
        /// 分析日志的一些过程信息
        /// </summary>
        /// <param name="debugItems">debug条目数</param>
        /// <param name="infoItems">info条目数</param>
        /// <param name="warnItems">warn条目数</param>
        /// <param name="errorItems">error条目数</param>
        /// <param name="undefineItems">undefine条目数</param>
        /// <param name="totalItems">总条目数</param>
        /// <param name="totalAnalysedBytes">已分析的总字节数</param>
        /// <param name="totalInputBytes">已输入的总字节数</param>
        /// <returns></returns>
        public bool GetAnalyseInfos(ref int debugItems, ref int infoItems, 
            ref int warnItems, ref int errorItems, ref int undefineItems, 
            ref int totalItems, ref ulong totalAnalysedBytes, ref ulong totalInputBytes)
        {
            lock (m_logCommonMutex)
            {
                debugItems = m_debugItems;
                infoItems = m_infoItems;
                warnItems = m_warnItems;
                errorItems = m_errorItems;
                undefineItems = m_undefineItems;
                totalItems = m_totalItems;
                totalAnalysedBytes = m_totalLogBytes;
                totalInputBytes = m_totalInputBytes;
            }

            return true;
        }
#endregion

#region 日志分析线程

        public bool GoonAnalysing()
        {
            lock (m_analyseMutex)
            {
                return m_goonAnalyse;
            }
        }

        static private void LogAnalyseThreadProc(object data)
        {
            LogAnalyser logAnalyser = (LogAnalyser)data;

            Byte[] logData = new Byte[MaxToBeContinuedLogBytes];
            int unAnalysedBytes = 0;

            while (logAnalyser.GoonAnalysing())
            {
                Thread.Sleep(300);

                logAnalyser.AddLog("[Debug]___eaaaaaaaaaaafda\n", LogLevel.Debug);
                logAnalyser.AddLog("[ Info]___ebbbbbbbbbbbbbeaa\n", LogLevel.Info);
                logAnalyser.AddLog("[ Warn]___eaccccccccccca\n", LogLevel.Warn);
                logAnalyser.AddLog("[Error]hggggggggggggggg\n", LogLevel.Error);
                logAnalyser.AddLog("[Undef]ffffffffffffffffffffffff\n", LogLevel.Undefine);
            }
        }

        public bool Start()
        {
            Debug.Assert(null == m_analyseThread);

            lock (m_analyseMutex)
            {
                m_goonAnalyse = true;
            }

            m_analyseThread = new Thread(LogAnalyser.LogAnalyseThreadProc);
            m_analyseThread.Start((object)this);

            return true;
        }

        public bool Stop()
        {
            Debug.Assert(null != m_analyseThread);

            lock (m_analyseMutex)
            {
                m_goonAnalyse = false;
            }

            m_analyseThread.Join();
            return true;
        }
#endregion
    }
}
