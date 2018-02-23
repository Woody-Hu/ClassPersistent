using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 类数据持久化核心类
/// </summary>
namespace ClassPersistentCore
{
    /// <summary>
    /// 基础数据持久化核心控制器
    /// </summary>
    public class PersistentManger
    {
        #region 私有字段
        /// <summary>
        /// 基础路径
        /// </summary>
        private string m_strBasePath = @"C:\";
        /// <summary>
        /// 基础的后扩展名
        /// </summary>
        private string m_strBaseSuffixation = ".Data";
        /// <summary>
        /// 写入流
        /// </summary>
        private Stream m_useWriteStream = null;
        /// <summary>
        /// 读取流
        /// </summary>
        private Stream m_ReadStream = null;
        /// <summary>
        /// 使用的二进制转换器
        /// </summary>
        private BinaryFormatter m_useBinFormatter = new BinaryFormatter();
        /// <summary>
        /// 使用的文件过滤委托
        /// </summary>
        private AssemblyFileFilterDelegate m_useDelegate = null;
        #endregion
        /// <summary>
        /// 构造方法配置程序集文件过滤委托
        /// </summary>
        /// <param name="inputDelegate">输入的委托</param>
        public PersistentManger(AssemblyFileFilterDelegate inputDelegate = null)
        {
            m_useDelegate = inputDelegate;
            AssemblyUtility.m_useFilterDelegate = inputDelegate;
        }
        /// <summary>
        /// 基础路径
        /// </summary>
        public string BasePath
        {
            get
            {
                return m_strBasePath;
            }
            set
            {
                m_strBasePath = value;
            }
        }
        /// <summary>
        /// 基础的后扩展名
        /// </summary>
        public string BaseSuffixation
        {
            get
            {
                return m_strBaseSuffixation;
            }
            set
            {
                m_strBaseSuffixation = value;
            }
        }
        /// <summary>
        /// 使一个类持久化
        /// </summary>
        /// <param name="inputObject">输入的需要持久化的类</param>
        /// <param name="catchException">过程中捕获到的异常</param>
        /// <param name="strClass">类的序列化字符串</param>
        /// <returns>是否类持久化成功</returns>
        public bool TryWriteAPersistentClassByString(object inputObject, out Exception catchException, out string strClass)
        {
            strClass = null;
            catchException = null;
            m_useBinFormatter = new BinaryFormatter();
            m_useBinFormatter.Binder = new UseBinder(inputObject.GetType());
            try
            {
                //string useFullName = GetFullFileName(inputFileName);
                using (m_useWriteStream = new MemoryStream())
                {
                    m_useBinFormatter.Serialize(m_useWriteStream, inputObject);
                    byte[] buffer;
                    m_useWriteStream.Position = 0;
                    buffer = new byte[m_useWriteStream.Length];
                    m_useWriteStream.Read(buffer, 0, buffer.Length);
                    m_useWriteStream.Flush();
                    strClass = Convert.ToBase64String(buffer);
                }
                m_useWriteStream = null;
                return true;
            }
            catch (Exception ex)
            {
                catchException = ex;
                return false;
            }
        }
        /// <summary>
        /// 取回一个持久化的类
        /// </summary>
        /// <typeparam name="X">需取回的类型</typeparam>
        /// <param name="strClass">对象的序列化字符串</param>
        /// <param name="findValue">取回的对象</param>
        /// <param name="catchException">在获取中捕获到的异常</param>
        /// <returns>是否成功</returns>
        public bool TryGetAPersistentClassByString<X>(string strClass, out X findValue, out Exception catchException)
            where X : class
        {
            catchException = null;
            findValue = null;
            m_useBinFormatter = new BinaryFormatter();
            m_useBinFormatter.Binder = new UseBinder(typeof(X));
            try
            {
                byte[] buffer = Convert.FromBase64String(strClass);
                using (m_ReadStream = new MemoryStream(buffer))
                {
                    findValue = m_useBinFormatter.Deserialize(m_ReadStream) as X;
                    m_ReadStream.Flush();
                }
                m_ReadStream = null;
                return null != findValue;
            }
            catch (Exception ex)
            {
                catchException = ex;
                return false;
            }
        }
        /// <summary>
        /// 使一个类持久化
        /// </summary>
        /// <param name="inputFileName">输入的持久化文件名称</param>
        /// <param name="inputObject">输入的需要持久化的类</param>
        /// <param name="catchException">过程中捕获到的异常</param>
        /// <returns>是否类持久化成功</returns>
        public bool TryWriteAPersistentClassByFile(string inputFileName, object inputObject, out Exception catchException)
        {
            catchException = null;
            m_useBinFormatter = new BinaryFormatter();
            m_useBinFormatter.Binder = new UseBinder(inputObject.GetType());
            try
            {
                string useFullName = GetFullFileName(inputFileName);
                using (m_useWriteStream = new FileStream(useFullName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    m_useBinFormatter.Serialize(m_useWriteStream, inputObject);
                }
                m_useWriteStream = null;
                return true;
            }
            catch (Exception ex)
            {
                catchException = ex;
                return false;
            }
        }
        /// <summary>
        /// 取回一个持久化的类
        /// </summary>
        /// <typeparam name="X">需取回的类型</typeparam>
        /// <param name="inputFileName">持久化类的文件名</param>
        /// <param name="findValue">取回的对象</param>
        /// <param name="catchException">在获取中捕获到的异常</param>
        /// <returns>是否成功</returns>
        public bool TryGetAPersistentClassByFile<X>(string inputFileName, out X findValue, out Exception catchException)
            where X : class
        {
            catchException = null;
            findValue = null;
            m_useBinFormatter = new BinaryFormatter();
            m_useBinFormatter.Binder = new UseBinder(typeof(X));
            try
            {
                string useFullName = GetFullFileName(inputFileName);
                using (m_ReadStream = File.OpenRead(useFullName))
                {
                    findValue = m_useBinFormatter.Deserialize(m_ReadStream) as X;
                }
                m_ReadStream = null;
                return null != findValue;
            }
            catch (Exception ex)
            {
                catchException = ex;
                return false;
            }
        }
        /// <summary>
        /// 获得全路径名称
        /// </summary>
        /// <param name="inputFileName">输入的文件名</param>
        /// <returns>输出的全路径名称</returns>
        private string GetFullFileName(string inputFileName)
        {
            return BasePath + inputFileName + BaseSuffixation;
        }
    }
}
