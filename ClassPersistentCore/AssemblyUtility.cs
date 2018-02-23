using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ClassPersistentCore
{
    /// <summary>
    /// 程序集加载工具
    /// </summary>
    internal static class AssemblyUtility
    {
        /// <summary>
        /// 文件过滤委托
        /// </summary>
        internal static AssemblyFileFilterDelegate m_useFilterDelegate = null;
        /// <summary>
        /// 加载的程序集缓存
        /// </summary>
        private static List<Assembly> m_lstAssemblyCatch = null;
        /// <summary>
        /// 获取所有的程序集
        /// </summary>
        /// <param name="ifReCalculate">是否重新计算</param>
        /// <returns></returns>
        internal static List<Assembly> GetAllAssembly(bool ifReCalculate = false)
        {
            //清空缓存
            if (ifReCalculate)
            {
                m_lstAssemblyCatch = null;
            }
            //若有缓存
            if (null != m_lstAssemblyCatch)
            {
                return m_lstAssemblyCatch;
            }
            List<Assembly> lstAllAssembly = new List<Assembly>();
            //获得入口程序集
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            //获取文件位置
            FileInfo tempFileInfo = new FileInfo(executingAssembly.Location);
            //获取母文件夹目录
            DirectoryInfo tempDirectoryInfo = tempFileInfo.Directory;
            //临时程序集
            Assembly oneAssembly = null;
            lstAllAssembly.Add(executingAssembly);
            //获得执行程序集的所有引用程序集
            foreach (var oneAssemblname in executingAssembly.GetReferencedAssemblies())
            {
                oneAssembly = null;
                try
                {
                    oneAssembly = Assembly.Load(oneAssemblname);
                }
                catch (Exception)
                {
                    oneAssembly = null;
                }
                if (null != oneAssembly)
                {
                    lstAllAssembly.Add(oneAssembly);
                }
            }
            //获得执行程序集下所有的目录程序集
            foreach (var oneFileInfo in tempDirectoryInfo.GetFiles())
            {
                oneAssembly = null;
                try
                {
                    if (null != m_useFilterDelegate && true == m_useFilterDelegate(oneFileInfo))
                    {
                        ;
                    }
                    else
                    {
                        oneAssembly = Assembly.LoadFrom(oneFileInfo.FullName);
                    }
                }
                catch (Exception)
                {
                    oneAssembly = null;
                }
                if (null != oneAssembly && !lstAllAssembly.Contains(oneAssembly))
                {
                    lstAllAssembly.Add(oneAssembly);
                }
            }
            m_lstAssemblyCatch = lstAllAssembly;
            return lstAllAssembly;
        }
        /// <summary>
        /// 获取一个类型涵盖的所有类型：递归
        /// </summary>
        /// <param name="input">输入的类型</param>
        /// <param name="lstTypes">返回的类型列表</param>
        /// <param name="useDic">使用的缓存字典</param>
        internal static void GetAllInnerTypes(Type input, ref List<Type> lstTypes, ref Dictionary<string, Type> useDic)
        {
            //输入检查
            if (null == input || lstTypes.Contains(input))
            {
                return;
            }
            else
            {
                string tagString = input.Assembly.FullName + input.FullName;
                //若已被缓存
                if (useDic.ContainsKey(tagString))
                {
                    return;
                }
                //输入自身
                lstTypes.Add(input);
                useDic.Add(tagString, input);
                //输入组件元类型
                if (input.HasElementType)
                {
                    GetAllInnerTypes(input.GetElementType(), ref lstTypes, ref useDic);
                }
                //获取所有的泛型参数组
                foreach (var oneType in input.GetGenericArguments())
                {
                    GetAllInnerTypes(oneType, ref lstTypes, ref useDic);
                }
                //获得所有的成员类型组
                foreach (var oneInfo in input.GetRuntimeFields())
                {
                    GetAllInnerTypes(oneInfo.FieldType, ref lstTypes, ref useDic);
                }
                if (null != input.BaseType)
                {
                    GetAllInnerTypes(input.BaseType, ref lstTypes, ref useDic);
                }
                //去重
                lstTypes = lstTypes.Distinct().ToList();
            }

        }
        /// <summary>
        /// 尝试在程序集中获取指定类型
        /// </summary>
        /// <param name="typeName">输入的类型名称</param>
        /// <param name="oneAssembly">输入的程序集</param>
        /// <returns>获取的类型</returns>
        internal static Type TryGetType(string typeName, Assembly oneAssembly)
        {
            Type returnValue;
            try
            {
                returnValue = oneAssembly.GetType(typeName);
            }
            catch (Exception)
            {
                returnValue = null;
            }
            return returnValue;
        }
    }
}

