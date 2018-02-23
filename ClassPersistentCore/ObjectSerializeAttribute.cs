using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassPersistentCore
{
    /// <summary>
    /// 对象序列化方法特性
    /// 用于指定字段使用的转换器
    /// 字段特性，不可继承，不可重复使用
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ObjectSerializeAttribute : Attribute
    {
        /// <summary>
        /// 内部使用的转换器通用接口
        /// </summary>
        private ITransformerTag m_UseTransformer = null;
        /// <summary>
        /// 转换器的类型
        /// </summary>
        private Type m_thisType = null;
        /// <summary>
        /// 构造方法
        /// </summary>
        public ObjectSerializeAttribute()
        {
            ;
        }
        /// <summary>
        /// 使用的转换器父级基类
        /// </summary>
        public ITransformerTag UseTransformer
        {
            get
            {
                return m_UseTransformer;
            }
            private set
            {
                m_UseTransformer = value;
            }
        }
        /// <summary>
        /// 转换器的类型
        /// </summary>
        public Type ThisType
        {
            get
            {
                return m_thisType;
            }
            set
            {
                m_thisType = value;
                //尝试生成对象
                try
                {
                    m_UseTransformer = Activator.CreateInstance(m_thisType) as ITransformerTag;
                }
                catch (Exception)
                {
                    ;
                }
            }
        }
    }
}
