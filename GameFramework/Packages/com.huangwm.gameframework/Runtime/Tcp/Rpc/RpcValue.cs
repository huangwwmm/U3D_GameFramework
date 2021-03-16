using GF.Common.Data;
using GF.Common.Debug;
using GF.Common.Utility;
using System;
using System.Runtime.InteropServices;

namespace GF.Net.Tcp.Rpc
{
    [StructLayout(LayoutKind.Explicit)]
    public class RpcValue : IObjectPoolItem
    {
        private const int VALUE_FIELD_OFFSET = 0;

        [FieldOffset(VALUE_FIELD_OFFSET)]
        public byte ByteValue;
        [FieldOffset(VALUE_FIELD_OFFSET)]
        public short ShortValue;
        [FieldOffset(VALUE_FIELD_OFFSET)]
        public int IntValue;
        [FieldOffset(VALUE_FIELD_OFFSET)]
        public long LongValue;
        [FieldOffset(VALUE_FIELD_OFFSET)]
        public float FloatValue;
        [FieldOffset(VALUE_FIELD_OFFSET)]
        public double DoubleValue;
        [FieldOffset(VALUE_FIELD_OFFSET)]
        public ArrayPool<RpcValue>.Node ArrayValue;

        [FieldOffset(sizeof(long))]
        public ValueType ValueType;

        public void OnAlloc()
        {
        }

        public void OnRelease()
        {
        }

        /// <summary>
        /// 反序列化
        /// <see cref="Serialize"/>
        /// </summary>
        public void Deserialize(ArrayPool<RpcValue> rpcValueArrayPool
            , ObjectPool<RpcValue> rpcValuePool
            , byte[] buffer
            , ref int offset
            , bool enableDeserializeValueType = true)
        {
            if (enableDeserializeValueType)
            {
                ValueType = (ValueType)buffer[offset];
                offset++;
            }

            switch (ValueType)
            {
                case ValueType.Byte:
                    ByteValue = buffer[offset];
                    offset++;
                    break;
                case ValueType.Short:
                    ShortValue = BitConverter.ToInt16(buffer, offset);
                    offset += sizeof(short);
                    break;
                case ValueType.Int:
                    IntValue = BitConverter.ToInt32(buffer, offset);
                    offset += sizeof(int);
                    break;
                case ValueType.Long:
                    LongValue = BitConverter.ToInt64(buffer, offset);
                    offset += sizeof(long);
                    break;
                case ValueType.Float:
                    FloatValue = BitConverter.ToSingle(buffer, offset);
                    offset += sizeof(float);
                    break;
                case ValueType.Double:
                    DoubleValue = BitConverter.ToDouble(buffer, offset);
                    offset += sizeof(double);
                    break;
                case ValueType.FixedValueTypeArray:
                    {
                        int elementCount = BitConverter.ToInt32(buffer, offset);
                        offset += sizeof(int);

                        ValueType elementValueType = (ValueType)buffer[offset];
                        offset++;
                        ArrayValue = rpcValueArrayPool.AllocBuffer(elementCount);
                        int endPoint = ArrayValue.GetOffset() + ArrayValue.GetSize();
                        RpcValue[] elements = ArrayValue.GetBuffer();
                        for (int iElement = ArrayValue.GetOffset(); iElement < endPoint; iElement++)
                        {
                            RpcValue iterElement = rpcValuePool.Alloc();
                            elements[iElement] = iterElement;

                            iterElement.ValueType = elementValueType;
                            iterElement.Deserialize(rpcValueArrayPool
                                , rpcValuePool
                                , buffer
                                , ref offset
                                , false);
                        }
                    }
                    break;
                case ValueType.VariableValueTypeArray:
                    {
                        int elementCount = BitConverter.ToInt32(buffer, offset);
                        offset += sizeof(int);

                        ArrayValue = rpcValueArrayPool.AllocBuffer(elementCount);
                        int endPoint = ArrayValue.GetOffset() + ArrayValue.GetSize();
                        RpcValue[] elements = ArrayValue.GetBuffer();
                        for (int iElement = ArrayValue.GetOffset(); iElement < endPoint; iElement++)
                        {
                            RpcValue iterElement = rpcValuePool.Alloc();
                            elements[iElement] = iterElement;

                            iterElement.Deserialize(rpcValueArrayPool
                                , rpcValuePool
                                , buffer
                                , ref offset);
                        }
                    }
                    break;
                default:
                    throw new Exception("Not support ValueType: " + ValueType);
            }
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="buffer">目标Buffer</param>
        /// <param name="offset">写入位置</param>
        /// <param name="enableSerializeValueType">
        /// 是否序列化<see cref="ValueType"/>
        /// 需要这个开关是因为序列化<see cref="ValueType.FixedValueTypeArray"/>时，不需要序列化数组中每个元素的<see cref="ValueType"/>
        /// </param>
        public void Serialize(byte[] buffer
            , ref int offset
            , bool enableSerializeValueType = true)
        {
            if (enableSerializeValueType)
            {
                MBitConverter.WriteTo(buffer, ref offset, (byte)ValueType);
            }

            switch (ValueType)
            {
                case ValueType.Byte:
                    MBitConverter.WriteTo(buffer, ref offset, ByteValue);
                    break;
                case ValueType.Short:
                    MBitConverter.WriteTo(buffer, ref offset, ShortValue);
                    break;
                case ValueType.Int:
                    MBitConverter.WriteTo(buffer, ref offset, IntValue);
                    break;
                case ValueType.Long:
                    MBitConverter.WriteTo(buffer, ref offset, LongValue);
                    break;
                case ValueType.Float:
                    MBitConverter.WriteTo(buffer, ref offset, FloatValue);
                    break;
                case ValueType.Double:
                    MBitConverter.WriteTo(buffer, ref offset, DoubleValue);
                    break;
                case ValueType.FixedValueTypeArray:
                    {
                        int elementCount = ArrayValue.GetSize();
                        MBitConverter.WriteTo(buffer, ref offset, elementCount);
                        MDebug.Assert(elementCount > 0
                            , "Rpc"
                            , "elementCount > 0");

                        int startPoint = ArrayValue.GetOffset();
                        int endPoint = startPoint + elementCount;
                        RpcValue[] elements = ArrayValue.GetBuffer();
                        ValueType elementValueType = elements[startPoint].ValueType;
                        MBitConverter.WriteTo(buffer, ref offset, (byte)elementValueType);
                        for (int iElement = startPoint; iElement < endPoint; iElement++)
                        {
                            RpcValue iterElement = elements[iElement];
#if GF_DEBUG
                            MDebug.Assert(iterElement.ValueType == elementValueType
                                , "Rpc"
                                , "iterElement.ValueType == elementValueType");
#endif
                            iterElement.Serialize(buffer, ref offset, false);
                        }
                    }
                    break;
                case ValueType.VariableValueTypeArray:
                    {
                        int elementCount = ArrayValue.GetSize();
                        MBitConverter.WriteTo(buffer, ref offset, elementCount);
                        MDebug.Assert(elementCount > 0
                            , "Rpc"
                            , "elementCount > 0");

                        int startPoint = ArrayValue.GetOffset();
                        int endPoint = startPoint + elementCount;
                        RpcValue[] elements = ArrayValue.GetBuffer();
                        for (int iElement = startPoint; iElement < endPoint; iElement++)
                        {
                            RpcValue iterElement = elements[iElement];
                            iterElement.Serialize(buffer, ref offset);
                        }
                    }
                    break;
                default:
                    throw new Exception("Not support ValueType: " + ValueType);
            }
        }

        internal void Release(ArrayPool<RpcValue> rpcValueArrayPool
            , ObjectPool<RpcValue> rpcValuePool)
        {
            switch (ValueType)
            {
                case ValueType.Byte:
                case ValueType.Short:
                case ValueType.Int:
                case ValueType.Long:
                case ValueType.Float:
                case ValueType.Double:
                    MDebug.Assert(ArrayValue == null
                        , "Rpc"
                        , "ArrayValue == null");
                    break;
                case ValueType.FixedValueTypeArray:
                case ValueType.VariableValueTypeArray:
                    int startPoint = ArrayValue.GetOffset();
                    int endPoint = startPoint + ArrayValue.GetSize();
                    RpcValue[] elements = ArrayValue.GetBuffer();
                    for (int iElement = startPoint; iElement < endPoint; iElement++)
                    {
                        RpcValue iterElement = elements[iElement];
                        iterElement.Release(rpcValueArrayPool, rpcValuePool);
                        rpcValuePool.Release(iterElement);
                        elements[iElement] = null;
                    }
                    rpcValueArrayPool.ReleaseBuffer(ArrayValue);
                    ArrayValue = null;
                    break;
                default:
                    throw new Exception("Not support ValueType: " + ValueType);
            }
        }
    }
}