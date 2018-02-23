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
    /// 特定类型检索器 - 单例模式
    /// 饿汉模式
    /// </summary>
    public class TypeFinder
    {
        #region 私有字段
        /// <summary>
        /// 所有的Type
        /// </summary>
        private List<Type> m_lstAllType = new List<Type>();
        /// <summary>
        /// Type：继承组列表缓存
        /// </summary>
        private Dictionary<Type, List<Type>> m_dicKeyTypeForListTypes = new Dictionary<Type, List<Type>>();
        /// <summary>
        /// 单例模式标签
        /// </summary>
        private static TypeFinder m_singleTag = null;
        /// <summary>
        /// 单例模式构造方法
        /// </summary>
        private TypeFinder()
        {
            m_lstAllType = GetAllTypes();
        }
        #endregion
        /// <summary>
        /// 单例模式获得接口
        /// </summary>
        /// <param name="ifReset">是否重置单例</param>
        /// <returns>类型检索器</returns>
        public static TypeFinder GetTypeFinder(bool ifReset = false)
        {
            if (null == m_singleTag || ifReset)
            {
                m_singleTag = new TypeFinder();
            }
            return m_singleTag;
        }
        /// <summary>
        /// 寻找所有继承特定类型的类型列表
        /// </summary>
        /// <param name="inputBaseType">输入的基类型</param>
        /// <returns>找到的类型列表</returns>
        public List<Type> FindTypesInheritFromInput(Type inputBaseType)
        {
            //缓存检查
            if (m_dicKeyTypeForListTypes.ContainsKey(inputBaseType))
            {
                return m_dicKeyTypeForListTypes[inputBaseType];
            }
            else
            {
                List<Type> returnValue = new List<Type>();
                foreach (var oneType in m_lstAllType)
                {
                    if (inputBaseType.IsAssignableFrom(oneType))
                    {
                        returnValue.Add(oneType);
                    }
                }
                m_dicKeyTypeForListTypes.Add(inputBaseType, returnValue);
                return returnValue;
            }

        }
        /// <summary>
        /// 获得所有程序集中符合特定标签的类
        /// </summary>
        /// <returns>找到的类</returns>
        private List<Type> GetAllTypes()
        {
            List<Type> lstReutrnValue = new List<Type>();
            List<Assembly> lstAllAssembly = new List<Assembly>();
            //获得所有程序集
            lstAllAssembly = AssemblyUtility.GetAllAssembly();
            //获得程序集内所有的类型
            foreach (var tempAssembly in lstAllAssembly)
            {
                //防止反射异常
                try
                {
                    //获得所有被标签定义的类
                    foreach (var oneType in tempAssembly.GetTypes())
                    {
                        if (null != oneType && typeof(ITransformerTag).IsAssignableFrom(oneType))
                        {
                            lstReutrnValue.Add(oneType);
                        }
                    }
                }
                catch (Exception)
                {
                    ;
                }
            }
            return lstReutrnValue;
        }
    }
}
