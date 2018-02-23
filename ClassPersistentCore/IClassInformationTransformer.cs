using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassPersistentCore
{
    /// <summary>
    /// 信息转换接口
    /// </summary>
    public interface IClassInformationTransformer<X> : ITransformerTag
        where X : class
    {
        /// <summary>
        /// 将类信息转换为字符串
        /// </summary>
        /// <param name="input">输入的类</param>
        /// <returns>返回的字符串型类描述</returns>
        string TransformClassToString(X input);
        /// <summary>
        /// 将字符串转换为类信息
        /// </summary>
        /// <param name="input">输入的字符串</param>
        /// <returns>返回的类</returns>
        X TransformStringToClass(string input);
    }
}
