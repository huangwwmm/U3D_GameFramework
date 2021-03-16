using System.Reflection;
using static GF.Common.Utility.ReflectionUtility;

namespace GF.Core
{
    internal struct InitializePackageData
    {
        public MethodInfo Method;
        public int Proiority;
        public string Name;

        public InitializePackageData(MethodAndAttributeData data)
        {
            Method = data.Method;
            InitializePackageAttribute attribute = data.Attribute as InitializePackageAttribute;
            Proiority = attribute.Proiority;
            Name = attribute.Name;
        }


        internal static int ComparerByPriority(InitializePackageData x
            , InitializePackageData y)
        {
            return x.Proiority - y.Proiority;
        }
    }
}