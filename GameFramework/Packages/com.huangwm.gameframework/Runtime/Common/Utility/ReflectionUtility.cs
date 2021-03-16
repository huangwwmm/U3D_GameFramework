using System;
using System.Collections.Generic;
using System.Reflection;

namespace GF.Common.Utility
{
    public static class ReflectionUtility
    {
        public static void CollectionTypeWithAttribute(List<TypeAndAttributeData> result
           , Assembly assembly
           , Type attributeType
           , bool inherit)
        {
            Type[] types = assembly.GetTypes();
            for (int iType = 0; iType < types.Length; iType++)
            {
                Type iterType = types[iType];
                Attribute attribute = iterType.GetCustomAttribute(attributeType, inherit);

                if (attribute != null)
                {
                    result.Add(new TypeAndAttributeData(iterType, attribute));
                }
            }
        }

        public static void CollectionMethodWithAttribute(List<MethodAndAttributeData> result
            , Assembly assembly
            , BindingFlags bindingFlags
            , Type attributeType
            , bool inherit)
        {
            Type[] types = assembly.GetTypes();
            for (int iType = 0; iType < types.Length; iType++)
            {
                Type iterType = types[iType];
                MethodInfo[] methods = iterType.GetMethods(bindingFlags);
                for (int iMethod = 0; iMethod < methods.Length; iMethod++)
                {
                    MethodInfo iterMethod = methods[iMethod];
                    Attribute attribute = iterMethod.GetCustomAttribute(attributeType, inherit);
                    if (attribute != null)
                    {
                        result.Add(new MethodAndAttributeData(iterMethod, attribute));
                    }
                }
            }
        }

        public static void CollectionSubclass(List<Type> result
            , Assembly assembly
            , Type parentType)
        {
            Type[] types = assembly.GetTypes();
            for (int iType = 0; iType < types.Length; iType++)
            {
                Type iterType = types[iType];
                if (iterType.IsSubclassOf(parentType))
                {
                    result.Add(iterType);
                }
            }
        }

        public struct TypeAndAttributeData
        {
            public Type Type;
            public Attribute Attribute;

            public TypeAndAttributeData(Type type, Attribute attribute)
            {
                Type = type;
                Attribute = attribute;
            }
        }

        public struct MethodAndAttributeData
        {
            public MethodInfo Method;
            public Attribute Attribute;

            public MethodAndAttributeData(MethodInfo method, Attribute attribute)
            {
                Method = method;
                Attribute = attribute;
            }
        }
    }
}