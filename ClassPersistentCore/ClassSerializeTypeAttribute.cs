using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassPersistentCore
{
    /// <summary>
    /// 类型序列化涉及类型特性
    /// 用于指定序列化时可能涉及到的类型
    /// 字段特性，不可继承，不可重复使用
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ClassSerializeTypeAttribute : Attribute
    {
        /// <summary>
        /// 内部封装的类型表
        /// </summary>
        Type[] m_thisTypes = null;
        /// <summary>
        /// 内部封装的类型表
        /// </summary>
        public Type[] ThisTypes
        {
            get
            {
                return m_thisTypes;
            }
            set
            {
                m_thisTypes = value;
            }
        }
    }
}
