using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassPersistentCore
{
    /// <summary>
    /// 程序集过滤委托
    /// </summary>
    /// <param name="inputFile">输入的文件</param>
    /// <returns>是否需要过滤</returns>
    public delegate bool AssemblyFileFilterDelegate(FileInfo inputFile);
}
