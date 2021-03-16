using GF.Common.Data;
using GF.Common.Debug;
using GF.Common.Utility;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace GF.Net.Tcp.Rpc
{
    public static class RpcUtiltiy
    {
        private static Dictionary<string, MethodInfo> ms_AllStaticMethods;

        public static Dictionary<string, MethodInfo> GetOrCollectAllStaticMethods()
        {
            if (ms_AllStaticMethods == null)
            {
                ms_AllStaticMethods = new Dictionary<string, MethodInfo>();

                List<ReflectionUtility.MethodAndAttributeData> staticMethodDatas = new List<ReflectionUtility.MethodAndAttributeData>();
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int iAssembly = 0; iAssembly < assemblies.Length; iAssembly++)
                {
                    ReflectionUtility.CollectionMethodWithAttribute(staticMethodDatas
                       , assemblies[iAssembly]
                       , BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public
                       , typeof(StaticMethodAttribute)
                       , false);
                }

                StringBuilder methodSignBuilder = StringUtility.AllocStringBuilder();
                for (int iMethod = 0; iMethod < staticMethodDatas.Count; iMethod++)
                {
                    ReflectionUtility.MethodAndAttributeData iterStaticMethodData = staticMethodDatas[iMethod];
                    StaticMethodAttribute iterAttribute = iterStaticMethodData.Attribute as StaticMethodAttribute;
                    methodSignBuilder.Append(string.IsNullOrEmpty(iterAttribute.Alias)
                        ? iterStaticMethodData.Method.Name
                        : iterAttribute.Alias);

                    ParameterInfo[] parameterInfos = iterStaticMethodData.Method.GetParameters();
                    for (int iParameter = 0; iParameter < parameterInfos.Length; iParameter++)
                    {
                        ParameterInfo iterParameter = parameterInfos[iParameter];
                        Type iterParameterType = iterParameter.ParameterType;
                        if (iterParameterType.IsValueType)
                        {
                            if (iterParameterType == typeof(byte))
                            {
                                AppendValueType(methodSignBuilder, ValueType.Byte);
                            }
                            else if (iterParameterType == typeof(int))
                            {
                                AppendValueType(methodSignBuilder, ValueType.Int);
                            }
                            else if (iterParameterType == typeof(short))
                            {
                                AppendValueType(methodSignBuilder, ValueType.Short);
                            }
                            else if (iterParameterType == typeof(long))
                            {
                                AppendValueType(methodSignBuilder, ValueType.Long);
                            }
                            else if (iterParameterType == typeof(float))
                            {
                                AppendValueType(methodSignBuilder, ValueType.Float);
                            }
                            else if (iterParameterType == typeof(double))
                            {
                                AppendValueType(methodSignBuilder, ValueType.Double);
                            }
                            // struct
                            else if (!iterParameterType.IsPrimitive)
                            {
                                MDebug.Assert(false
                                    , "Rpc"
                                    , $"Not support ParameterType {iterParameterType}");
                            }
                            else
                            {
                                MDebug.LogWarning("Rpc"
                                    , $"Not support ParameterType: {iterParameterType} (IsValueType). But maybe we can support it.");
                            }
                        }
                        else if (iterParameterType.IsArray)
                        {
                            AppendValueType(methodSignBuilder, ValueType.FixedValueTypeArray);
                        }
                        else
                        {
                            MDebug.Assert(false
                                , "Rpc"
                                , $"Not support ParameterType {iterParameterType}");
                        }
                    }

                    string methodSign = methodSignBuilder.ToString();
                    methodSignBuilder.Clear();
#if GF_DEBUG
                    MDebug.Assert(!ms_AllStaticMethods.ContainsKey(methodSign)
                        , "Rpc"
                        , "!ms_AllStaticMethods.ContainsKey(methodSign)");
#endif
                    ms_AllStaticMethods[methodSign] = iterStaticMethodData.Method;
                }

                methodSignBuilder.Clear();
                foreach (KeyValuePair<string, MethodInfo> kv in ms_AllStaticMethods)
                {
                    methodSignBuilder.AppendLine(kv.Key);
                }
                MDebug.Log("Rpc"
                    , "All static methods:\n" + methodSignBuilder);
            }

            return ms_AllStaticMethods;
        }

        public static void AppendParameterInfo(StringBuilder stringBuilder, RpcValue parameterInfo)
        {
            switch (parameterInfo.ValueType)
            {
                case ValueType.Byte:
                case ValueType.Short:
                case ValueType.Int:
                case ValueType.Long:
                case ValueType.Float:
                case ValueType.Double:
                    AppendValueType(stringBuilder, parameterInfo.ValueType);
                    break;
                case ValueType.FixedValueTypeArray:
                case ValueType.VariableValueTypeArray:
                    ArrayPool<RpcValue>.Node parameters = parameterInfo.ArrayValue;
                    break;
                default:
                    MDebug.Assert(false
                        , "Rpc"
                        , "Not support ValueType: " + parameterInfo.ValueType);
                    break;
            }
        }

        public static void AppendValueType(StringBuilder stringBuilder, ValueType valueType)
        {
            switch (valueType)
            {
                case ValueType.Byte:
                case ValueType.Short:
                case ValueType.Int:
                case ValueType.Long:
                case ValueType.Float:
                case ValueType.Double:
                    stringBuilder.Append('_').Append((byte)valueType);
                    break;
                // 所有数组都当成同一种类型，方便处理
                case ValueType.FixedValueTypeArray:
                case ValueType.VariableValueTypeArray:
                    stringBuilder.Append('_').Append((byte)ValueType.FixedValueTypeArray);
                    break;
                default:
                    MDebug.Assert(false
                        , "Rpc"
                        , "Not support ValueType: " + valueType);
                    break;
            }
        }
    }
}