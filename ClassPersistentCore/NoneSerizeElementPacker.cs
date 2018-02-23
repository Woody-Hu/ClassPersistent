using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ClassPersistentCore
{
    [Serializable]
    /// <summary>
    /// 装饰模式
    /// 第三方库无法增加Serializable属性的类封装
    /// 序列化方式：该类会首先考虑使用"Pars"方法(配对ToString方法)，
    /// 若没找到，会在全命名空间下
    /// 寻找实现IClassInformationTransformet泛型接口的类
    /// *注意具体实现的类要保留无参的公开方法
    /// 若过程中出现异常或没有找到则尝试将
    /// 将内部数据还原为Null
    /// </summary>
    /// <typeparam name="X">使用的泛型类型</typeparam>
    public sealed class NoneSerizeElementPacker<X> : ISerializable
         where X : class
    {
        #region 私有字段
        [NonSerialized]
        /// <summary>
        /// 此封装的核心值
        /// </summary>
        private X m_thisValue = null;
        /// <summary>
        /// Parse方法
        /// </summary>
        private const string m_keyMethodName = "Parse";
        /// <summary>
        /// 在流化时表征类的特征字符串
        /// </summary>
        private const string m_keyStrOfClass = "this";
        /// <summary>
        /// 使用的转换器类型
        /// </summary>
        internal static Type m_useType = null;
        /// <summary>
        /// 使用的转换器对象
        /// </summary>
        internal static IClassInformationTransformer<X> m_useTransformer = null;
        #endregion
        /// <summary>
        /// 此封装的核心值
        /// </summary>
        public X ThisValue
        {
            get
            {
                return m_thisValue;
            }
            private set
            {
                m_thisValue = value;
            }
        }
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="input"></param>
        public NoneSerizeElementPacker(X input)
        {
            ThisValue = input;
        }
        /// <summary>
        /// 序列化用构造方法
        /// </summary>
        /// <param name="info">序列化信息</param>
        /// <param name="context">序列化上下文</param>
        public NoneSerizeElementPacker(SerializationInfo info, StreamingContext context)
        {
            try
            {
                ThisValue = null;
                //获取文本回置方法
                MethodInfo useMethodInfo = FindPareMethod();
                //初始化
                string keyString = info.GetString(m_keyStrOfClass);
                //若有用静态的Parse方法转换回来
                if (null != useMethodInfo)
                {
                    ThisValue = (X)useMethodInfo.Invoke(null, null);
                }
                //若没有使用特定转换接口
                else
                {
                    //获取转换器实例
                    IClassInformationTransformer<X> tempTransformer = null;
                    tempTransformer = CreatOneTransformer();
                    //转换
                    if (null != tempTransformer)
                    {
                        ThisValue = tempTransformer.TransformStringToClass(keyString);
                    }
                }
            }
            catch (Exception)
            {
                ThisValue = null;
            }
        }
        /// <summary>
        /// 序列化用方法
        /// </summary>
        /// <param name="info">序列化信息</param>
        /// <param name="context">序列化上下文</param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            //获取文本回置方法
            MethodInfo useMethodInfo = FindPareMethod();
            //初始化
            string keyString = string.Empty;
            //若有 ToString
            if (null != useMethodInfo)
            {
                keyString = ThisValue.ToString();
            }
            //若没有使用特定转换接口
            else
            {
                //获取转换器实例
                IClassInformationTransformer<X> tempTransformer = null;
                tempTransformer = CreatOneTransformer();
                //转换
                if (null != tempTransformer)
                {
                    keyString = tempTransformer.TransformClassToString(ThisValue);
                }
            }
            //核心数据流出
            info.AddValue(m_keyStrOfClass, keyString);
        }
        /// <summary>
        /// 创建一个泛型转换器实例
        /// </summary>
        /// <returns></returns>
        private IClassInformationTransformer<X> CreatOneTransformer()
        {
            IClassInformationTransformer<X> tempTransformer = null;
            //若有对象缓存
            if (null != m_useTransformer)
            {
                return m_useTransformer;
            }
            if (null == m_useType)
            {
                m_useType = FindOneUseType();
            }
            if (null != m_useType)
            {
                try
                {
                    //远程生成一个转换器实例
                    tempTransformer = (IClassInformationTransformer<X>)Activator.CreateInstance(m_useType);
                }
                catch (Exception)
                {
                    tempTransformer = null;
                }
            }
            //使用缓存对象
            m_useTransformer = tempTransformer;
            return tempTransformer;
        }
        /// <summary>
        /// 获得一个按接口要求的类
        /// </summary>
        /// <returns></returns>
        private Type FindOneUseType()
        {
            Type returnValue = null;
            //获得所有符合要求的Type
            List<Type> lstUseType = TypeFinder.GetTypeFinder().
                FindTypesInheritFromInput(typeof(IClassInformationTransformer<X>));
            //数量检查
            if (null != lstUseType && 0 != lstUseType.Count)
            {
                //用第一个
                returnValue = lstUseType[0];
            }
            return returnValue;
        }
        /// <summary>
        /// 在泛型中寻找Pars方法
        /// </summary>
        /// <returns></returns>
        private MethodInfo FindPareMethod()
        {
            MethodInfo useMethodInfo = null;
            try
            {
                //获取Type实例
                Type useType = typeof(X);
                //获得Parse方法
                useMethodInfo = useType.GetMethod(m_keyMethodName, new Type[] { typeof(string) });
            }
            catch (Exception)
            {
                useMethodInfo = null;
            }

            return useMethodInfo;
        }
    }
}
