using System;
using GF.Common.Utility;

namespace GF.Core
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class InitializePackageAttribute : Attribute
    {
        public string Name;
        public int Proiority;

        public InitializePackageAttribute(string name, int proiority)
        {
            Proiority = proiority;
            Name = name;
        }
    }
}