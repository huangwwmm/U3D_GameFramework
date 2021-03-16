namespace GF.Net.Tcp.Rpc
{
    /// <summary>
    /// 用byte足够了，没那么数据类型
    /// <see cref="RpcValue"/>
    /// </summary>
    public enum ValueType : byte
    {
        Byte,
        Short,
        Int,
        Long,
        Float,
        Double,

        /// <summary>
        /// 这个数组的每个成员都是相同类型的
        /// 
        /// 相对于<see cref="VariableValueTypeArray"/>的优势：
        ///     序列化后更小
        ///     
        /// 内存结构：
        ///     <see cref="RpcValue.ValueType"/>
        ///     元素个数(int)
        ///     元素的<see cref="ValueType"/>
        ///     for(元素的Value)
        ///     
        /// 不支持元素个数为0。如果一定要，请使用<see cref="Null"/>
        /// </summary>
        FixedValueTypeArray,
        /// <summary>
        /// 这个数组的每个成员可能是不同的类型
        /// 
        /// 内存结构：
        ///     <see cref="RpcValue.ValueType"/>
        ///     元素个数(int)
        ///     for(元素的<see cref="ValueType"/>|元素的Value)
        ///     
        /// <see cref="FixedValueTypeArray"/>
        /// </summary>
        VariableValueTypeArray,
    }
}