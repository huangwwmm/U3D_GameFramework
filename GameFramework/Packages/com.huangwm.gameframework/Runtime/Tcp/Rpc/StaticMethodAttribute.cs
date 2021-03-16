using System;
using System.Collections.Generic;
using System.Reflection;

namespace GF.Net.Tcp.Rpc
{
    [AttributeUsage(AttributeTargets.Method)]
    public class StaticMethodAttribute : Attribute
    {
        private static Dictionary<string, MethodInfo> ms_AllStaticMethods;

        /// <summary>
        /// 函数的别名
        /// 如果为空，则使用函数名
        /// </summary>
        public readonly string Alias;

        public StaticMethodAttribute()
            : this(string.Empty)
        {

        }

        public StaticMethodAttribute(string alias)
        {
            Alias = alias;
        }
    }
}