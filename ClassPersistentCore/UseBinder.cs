using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ClassPersistentCore
{
    /// <summary>
    /// 使用的绑定连接器
    /// </summary>
    internal class UseBinder : SerializationBinder
    {
        #region 泛型切割用字段
        /// <summary>
        /// 泛型用的特殊字段
        /// </summary>
        const string m_strGenericTag = @"`\d+\[";
        /// <summary>
        /// 泛型用捕获组字符串
        /// </summary>
        const string m_strGenericGroupPattern = @"\[((?<Open>\[)|(?<-Open>\])|[^\[\]]+)*(?(Open)(?!))\]";
        /// <summary>
        /// 泛型用类型捕获字段(非贪婪)
        /// </summary>
        const string m_strGenericTypePattern = @"^.+?\`\d+";
        /// <summary>
        /// 分割字符
        /// </summary>
        const char m_splitTag = ',';
        /// <summary>
        /// 最小分割结果
        /// </summary>
        const int m_minSplitCount = 5;
        /// <summary>
        /// 程序集部分数量
        /// </summary>
        const int m_nAssemblyCount = 4;
        /// <summary>
        /// 泛型正则表达式匹配器
        /// </summary>
        private static Regex m_useGenericTagRegex = new Regex(m_strGenericTag);
        /// <summary>
        /// 使用的泛型平衡组匹配器
        /// </summary>
        private static Regex m_useGenericGroupPatternRegex = new Regex(m_strGenericGroupPattern);
        /// <summary>
        /// 泛型用类型匹配器
        /// </summary>
        private static Regex m_useGenericTypePatternRegex = new Regex(m_strGenericTypePattern);
        #endregion
        #region 特性装配用字段
        /// <summary>
        /// 使用的底层封装泛型基类
        /// </summary>
        private static Type m_NoneSerizeElementPackerType = typeof(NoneSerizeElementPacker<>);
        /// <summary>
        /// 使用的底层转换器类型基类
        /// </summary>
        private static Type m_useTransformerType = typeof(IClassInformationTransformer<>);
        /// <summary>
        /// 使用的底层转换器类型字段名称
        /// </summary>
        private const string m_strNoneSerizeElementPackerTypeFiled = "m_useType";
        /// <summary>
        /// 使用的底层转换器类型字段名称
        /// </summary>
        private const string m_strNoneSerizeElementPackerObjectFiled = "m_useTransformer";
        #endregion
        #region 私有字段
        /// <summary>
        /// 所有的程序集
        /// </summary>
        private static List<Assembly> m_lstAllAssembly = new List<Assembly>();
        /// <summary>
        /// 输入的Tpe列表
        /// </summary>
        private List<Type> m_lstType = new List<Type>();
        /// <summary>
        /// 类型缓存
        /// </summary>
        private static Dictionary<string, Type> m_dicType = new Dictionary<string, Type>();
        /// <summary>
        /// 已调整类型缓存
        /// </summary>
        private static HashSet<Type> m_hashSetType = new HashSet<Type>();
        #endregion
        /// <summary>
        /// 静态构造，加载程序集
        /// </summary>
        static UseBinder()
        {
            //静态加载所有程序集
            m_lstAllAssembly = AssemblyUtility.GetAllAssembly();
        }
        /// <summary>
        /// 获得自身Type列表
        /// </summary>
        /// <param name="input"></param>
        internal UseBinder(Type input)
        {
            if (null != input)
            {
                if (!m_hashSetType.Contains(input))
                {
                    //根据特性放入依赖类
                    AutoGetAttributeTypes(input);
                    //根据特性设置转换器
                    AutoSetTransformer(input);
                    //hash表存入
                    m_hashSetType.Add(input);
                }

                //加载输入所有涉及的Type
                AssemblyUtility.GetAllInnerTypes(input, ref m_lstType, ref m_dicType);
            }
        }
        /// <summary>
        /// 微软架构对接接口负责Type的查找
        /// </summary>
        /// <param name="assemblyName">程序集名</param>
        /// <param name="typeName">类型名</param>
        /// <returns>找到的type</returns>
        public override Type BindToType(string assemblyName, string typeName)
        {
            string tempString = assemblyName + typeName;
            Type returnValue = null;
            Assembly useAssembly = null;
            //若已被缓存
            if (m_dicType.TryGetValue(tempString, out returnValue))
            {
                return returnValue;
            }
            //寻找输入的程序集
            foreach (var oneAssembly in m_lstAllAssembly)
            {
                if (oneAssembly.GetName().FullName.Equals(assemblyName))
                {
                    useAssembly = oneAssembly;
                    break;
                }
            }
            //若找到首先尝试在程序集中选
            if (null != useAssembly)
            {
                returnValue = AssemblyUtility.TryGetType(typeName, useAssembly);
            }
            //若没找到在全程序集中寻找
            if (null == returnValue)
            {
                foreach (var oneAssembly in m_lstAllAssembly)
                {
                    returnValue = AssemblyUtility.TryGetType(typeName, oneAssembly);
                    if (null != returnValue)
                    {
                        break;
                    }
                }
            }
            //若仍没找到则在涉及类中寻找
            if (null == returnValue)
            {
                foreach (var oneType in m_lstType)
                {
                    if (oneType.FullName.Equals(typeName))
                    {
                        returnValue = oneType;
                    }
                }
            }
            //泛型制备
            if (null == returnValue)
            {
                returnValue = GetGenericType(assemblyName, typeName);
            }
            if (null != returnValue)
            {
                //加入缓存
                m_dicType.Add(tempString, returnValue);
            }
            return returnValue;
        }
        #region 私有方法
        /// <summary>
        /// 制备泛型类型
        /// </summary>
        /// <param name="assemblyName">输入的程序集字符串</param>
        /// <param name="typeName">输入的类型字符串</param>
        /// <returns>制备得到的泛型类型</returns>
        private Type GetGenericType(string assemblyName, string typeName)
        {
            try
            {
                //若不是泛型类型
                if (false == IfTypeNameIsGeneric(typeName))
                {
                    return null;
                }
                Type RetrunValue = null;
                string useGenerTypeName;
                List<KeyValuePair<string, string>> useLstParamType = new List<KeyValuePair<string, string>>();
                //拆解类型字符串
                if (false == TrySplitTypeStr(typeName, out useGenerTypeName, out useLstParamType))
                {
                    return null;
                }
                //寻找泛型基类型
                RetrunValue = BindToType(assemblyName, useGenerTypeName);
                //若没有找到
                if (null == RetrunValue)
                {
                    return null;
                }
                //制备泛型参数
                List<Type> lstParameterType = new List<Type>();
                Type tempParameterType = null;
                foreach (var oneKVP in useLstParamType)
                {
                    tempParameterType = BindToType(oneKVP.Key, oneKVP.Value);
                    if (null == tempParameterType)
                    {
                        return null;
                    }
                    lstParameterType.Add(tempParameterType);
                }
                //制备泛型类型
                RetrunValue = RetrunValue.MakeGenericType(lstParameterType.ToArray());
                return RetrunValue;
            }
            //异常保护
            catch (Exception)
            {
                return null;
            }
        }
        /// <summary>
        /// 判断输入的类型描述字符串是否是泛型类型字符串
        /// </summary>
        /// <param name="inputTypeName"></param>
        /// <returns></returns>
        private bool IfTypeNameIsGeneric(string inputTypeName)
        {
            return m_useGenericTagRegex.IsMatch(inputTypeName);
        }
        /// <summary>
        /// 拆分类型字符串
        /// </summary>
        /// <param name="inputTypeName"></param>
        /// <param name="parameterString"></param>
        /// <returns></returns>
        private bool TrySplitTypeStr(string inputTypeName, out string strGenericTypeName, out List<KeyValuePair<string, string>> parameterString)
        {
            strGenericTypeName = null;
            parameterString = new List<KeyValuePair<string, string>>();
            //获得泛型基类型字符串
            if (m_useGenericTypePatternRegex.IsMatch(inputTypeName))
            {
                strGenericTypeName = m_useGenericTypePatternRegex.Match(inputTypeName).Value;
            }
            else
            {
                return false;
            }
            //泛型参数字符串声明
            string tempGenericParameterstr = null;
            List<string> lstParametersString = new List<string>();
            KeyValuePair<string, string> tempKVP;
            //获得泛型参数字符串组
            if (m_useGenericGroupPatternRegex.IsMatch(inputTypeName))
            {
                //贪婪捕获
                tempGenericParameterstr = m_useGenericGroupPatternRegex.Match(inputTypeName).Value;
                tempGenericParameterstr = ResetString(tempGenericParameterstr);
                //获得内部嵌套字符串组
                foreach (Match oneMatch in m_useGenericGroupPatternRegex.Matches(tempGenericParameterstr))
                {
                    lstParametersString.Add(ResetString(oneMatch.Value));
                }
                foreach (var oneString in lstParametersString)
                {
                    if (false == GetParameterStringKVP(oneString, out tempKVP))
                    {
                        return false;
                    }
                    else
                    {
                        parameterString.Add(tempKVP);
                    }
                }
                return parameterString.Count != 0;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 将输入字符串拆解为 程序集 - 类型全名称值键对
        /// </summary>
        /// <param name="input">输入的字符串</param>
        /// <param name="result">拆分的结果</param>
        /// <returns>是否成功</returns>
        private bool GetParameterStringKVP(string input, out KeyValuePair<string, string> result)
        {
            result = new KeyValuePair<string, string>();
            string[] splitResult = input.Split(m_splitTag);
            if (splitResult.Length < m_minSplitCount)
            {
                return false;
            }
            int assemblyIndex = splitResult.Length - m_nAssemblyCount;
            string assemblyString = AppendString(splitResult, assemblyIndex, splitResult.Length);
            string typeString = AppendString(splitResult, 0, assemblyIndex);
            result = new KeyValuePair<string, string>(assemblyString, typeString);
            return true;
        }
        /// <summary>
        /// 利用字符串组制作一个字符串
        /// </summary>
        /// <param name="splitResult">字符串组</param>
        /// <param name="useStartIndex">起始索引</param>
        /// <param name="useEndIndex">终止索引</param>
        /// <returns>拼合成的字符串</returns>
        private string AppendString(string[] splitResult, int useStartIndex, int useEndIndex)
        {
            string returnValue = string.Empty;
            for (int tempIndex = useStartIndex; tempIndex < useEndIndex; tempIndex++)
            {
                returnValue = AppendString(splitResult, returnValue, tempIndex, useEndIndex - 1);
            }
            returnValue = TrimString(returnValue);
            return returnValue;
        }
        /// <summary>
        /// 附加字符串按照索引附加之间补“，”
        /// </summary>
        /// <param name="splitResult">拆分的结果</param>
        /// <param name="inputString">输入的字符串</param>
        /// <param name="tempIndex">当前索引</param>
        /// <param name="useBoundary">索引边界</param>
        /// <returns>附加后的结果</returns>
        private string AppendString(string[] splitResult, string inputString, int tempIndex, int useBoundary)
        {
            inputString = inputString + splitResult[tempIndex];
            if (tempIndex != useBoundary)
            {
                inputString = inputString + m_splitTag;
            }
            return inputString;
        }
        /// <summary>
        /// 切断字符串去除两端空白和“,”
        /// </summary>
        /// <param name="assemblyString">输入的字符串</param>
        /// <returns>调整后的字符串</returns>
        private string TrimString(string assemblyString)
        {
            assemblyString = assemblyString.Trim();
            assemblyString = assemblyString.Trim(m_splitTag);
            return assemblyString;
        }
        /// <summary>
        /// 调整捕获到的字符串去除两端的“[”“]”
        /// </summary>
        /// <param name="inputString">输入的字符串</param>
        /// <returns>调整的结果</returns>
        private string ResetString(string inputString)
        {
            inputString = inputString.Remove(0, 1);
            inputString = inputString.Remove(inputString.Length - 1, 1);
            return inputString;
        }
        /// <summary>
        /// 根据特性设置字段
        /// </summary>
        /// <param name="inputOjbectType"></param>
        private void AutoSetTransformer(Type inputOjbectType)
        {
            foreach (var oneFieldInfo in inputOjbectType.GetRuntimeFields())
            {
                try
                {
                    AdjuestField(oneFieldInfo);
                }
                //异常保护
                catch (Exception)
                {
                    ;
                }

            }
        }
        /// <summary>
        /// 调整字段
        /// </summary>
        /// <param name="oneFieldInfo"></param>
        private void AdjuestField(FieldInfo oneFieldInfo)
        {
            //判断是否是需要处理的泛型类型
            if (oneFieldInfo.FieldType.Name.Equals(m_NoneSerizeElementPackerType.Name) && oneFieldInfo.FieldType.IsGenericType)
            {
                //获取泛型参数
                Type genericType = oneFieldInfo.FieldType.GetGenericArguments()[0];
                //获取泛型上需要的特性
                Attribute oneAttribute = oneFieldInfo.GetCustomAttribute(typeof(ObjectSerializeAttribute));
                //获取验证
                if (null == oneAttribute)
                {
                    return;
                }
                //特性类型转换
                ObjectSerializeAttribute useAttribute = oneAttribute as ObjectSerializeAttribute;
                //获取验证
                if (null == useAttribute.UseTransformer)
                {
                    return;
                }
                //获得转换器的类型
                Type baseTransformerType = m_useTransformerType.MakeGenericType(genericType);
                //获得特性封装的转换器类型
                Type TransformerInput = useAttribute.UseTransformer.GetType();
                //若类型匹配
                if (baseTransformerType.IsAssignableFrom(TransformerInput))
                {
                    //制作封装泛型
                    Type usePackerType = m_NoneSerizeElementPackerType.MakeGenericType(genericType);
                    //获取类型字段
                    FieldInfo typeFiledInfo = usePackerType.GetField(m_strNoneSerizeElementPackerTypeFiled, BindingFlags.Static | BindingFlags.NonPublic);
                    typeFiledInfo.SetValue(null, TransformerInput);
                    //获取转换器字段
                    FieldInfo objectFiledInfo = usePackerType.GetField(m_strNoneSerizeElementPackerObjectFiled, BindingFlags.Static | BindingFlags.NonPublic);
                    objectFiledInfo.SetValue(null, useAttribute.UseTransformer);
                }
            }
            else
            {
                return;
            }
        }
        /// <summary>
        /// 通过附加特性获取所有的附加类型
        /// </summary>
        /// <param name="inputType">输入的类型</param>
        private void AutoGetAttributeTypes(Type inputType)
        {
            try
            {
                Attribute oneAttribute = inputType.GetCustomAttribute(typeof(ClassSerializeTypeAttribute));
                if (null == oneAttribute)
                {
                    return;
                }
                ClassSerializeTypeAttribute useClassSeriializeAttribute = oneAttribute as ClassSerializeTypeAttribute;
                if (null == useClassSeriializeAttribute)
                {
                    return;
                }
                Type[] useTypes = useClassSeriializeAttribute.ThisTypes;
                if (null == useTypes)
                {
                    return;
                }
                string tempString;
                foreach (var oneType in useTypes)
                {
                    if (null == oneType)
                    {
                        continue;
                    }
                    //调整目前缓存
                    AssemblyUtility.GetAllInnerTypes(oneType, ref m_lstType, ref m_dicType);
                }
            }
            catch (Exception)
            {
                ;
            }

        }
        #endregion
    }
}
