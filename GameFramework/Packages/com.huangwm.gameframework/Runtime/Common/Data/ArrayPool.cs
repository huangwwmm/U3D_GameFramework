using GF.Common.Debug;
using GF.Common.Utility;
using System;
using System.Text;

namespace GF.Common.Data
{
    /// <summary>
    /// 分配一个大的Buffer<see cref="m_Buffer"/>
    ///     使用时从其中申请一小块<see cref="AllocBuffer()"/>
    ///     用完再放回来<see cref="ReleaseBuffer()"/>
    ///     
    /// TODO 检测<see cref="EnsureCapacity()"/>前的Buffer是否有内存泄露
    /// TODO 现在只把不被使用的空间合并了，之后考虑把被使用的也合并，这样能减少遍历时Node的数量
    /// </summary>
    public class ArrayPool<T>
    {
        private static ObjectPool<Node> ms_NodePool;

        private T[] m_Buffer;
        private Node m_Root;

#if GF_DEBUG
        public static void UnitTest()
        {
            ArrayPool<T> buffer = new ArrayPool<T>(4096);
            Node n1 = buffer.AllocBuffer(2048);
            MDebug.Log("ArrayPool", "alloc n1");
            buffer.VerifyLegality();

            Node n2 = buffer.AllocBuffer(1024);
            MDebug.Log("ArrayPool", "alloc n2");
            buffer.VerifyLegality();

            Node n3 = buffer.AllocBuffer(1024);
            MDebug.Log("ArrayPool", "alloc n3");
            buffer.VerifyLegality();

            Node n4 = buffer.AllocBuffer(512);
            MDebug.Log("ArrayPool", "alloc n4");
            buffer.VerifyLegality();

            buffer.ReleaseBuffer(n1);
            MDebug.Log("ArrayPool", "release n1");
            buffer.VerifyLegality();

            buffer.ReleaseBuffer(n2);
            MDebug.Log("ArrayPool", "release n2");
            buffer.VerifyLegality();

            n1 = buffer.AllocBuffer(2048 + 1024);
            MDebug.Log("ArrayPool", "alloc n1");
            buffer.VerifyLegality();

            Node n5 = buffer.AllocBuffer(8192 - 2048 - 1024 - 1024 - 512);
            MDebug.Log("ArrayPool", "alloc n5");
            buffer.VerifyLegality();

            Node n6 = buffer.AllocBuffer(1);
            MDebug.Log("ArrayPool", "alloc n6");
            buffer.VerifyLegality();

            buffer.ReleaseBuffer(n1);
            buffer.ReleaseBuffer(n3);
            buffer.ReleaseBuffer(n4);
            buffer.ReleaseBuffer(n5);
            buffer.ReleaseBuffer(n6);
            buffer.VerifyLegality();

            buffer.Release();
        }
#endif

        public ArrayPool(int capacity)
        {
            if (ms_NodePool == null)
            {
                ms_NodePool = new ObjectPool<Node>(4, long.MaxValue);
            }

            m_Buffer = new T[capacity];
            m_Root = ms_NodePool.Alloc().SetData(this, 0, capacity, false);
        }

        public void Release()
        {
#if GF_DEBUG
            if (m_Root._After != null)
            {
                MDebug.Assert(m_Root._After == null
                    , "ArrayPool"
                    , "m_Root._After == null");

                StringBuilder stringBuilder = StringUtility.AllocStringBuilder();
                stringBuilder.Append("ArrayPool<").Append(typeof(T).FullName).Append(">has memory leak!\n");
                Node iterNode = m_Root;
                while (iterNode != null)
                {
                    stringBuilder.Append("Offset:").Append(iterNode.GetOffset())
                        .Append("\tSize:").Append(iterNode.GetSize())
                        .Append("\tIsUsed:").Append(iterNode.IsUsed())
                        .Append('\n');
                    iterNode = iterNode._After;
                }
                MDebug.LogError("ArrayPool"
                    , StringUtility.ReleaseStringBuilder(stringBuilder));
            }
#endif

            m_Buffer = null;
            ms_NodePool.Release(m_Root);
            m_Root = null;
        }

        public T[] GetBuffer()
        {
            return m_Buffer;
        }

        public Node AllocBuffer(int size)
        {
            MDebug.Assert(size > 0
                , "ArrayPool"
                , "size > 0");

            Node iterNode = m_Root;
#if GF_DEBUG
            int remainTime = 10000;
#endif
            while (true)
            {
#if GF_DEBUG
                if (remainTime-- == 0)
                {
                    throw new Exception("代码逻辑可能写错了");
                }
#endif

                // 可以在当前Node分配
                if (!iterNode.IsUsed()
                    && iterNode.GetSize() >= size)
                {
                    int remainSize = iterNode.GetSize() - size;
                    Node afterNode = iterNode._After;
                    // 处理分配后的剩余空间
                    if (remainSize != 0)
                    {
                        // 不能和下一个Node合并，把剩余空间当成一个新的Node
                        if (afterNode == null
                            || afterNode.IsUsed())
                        {
                            afterNode = ms_NodePool.Alloc().SetData(this
                                , iterNode.GetOffset() + size
                                , remainSize
                                , false
                                , iterNode
                                , afterNode);
                            iterNode._After = afterNode;
                            if (afterNode._After != null)
                            {
                                afterNode._After._Before = afterNode;
                            }
                        }
                        // 剩余空间合并到下一个Node
                        else
                        {
#if GF_DEBUG
                            MDebug.Assert(afterNode.GetOffset() - iterNode.GetSize() == iterNode.GetOffset()
                                , "ArrayPool"
                                , "afterNode.GetOffset() - iterNode.GetSize() == iterNode.GetOffset()");
#endif
                            afterNode.SetData(this
                                , iterNode.GetOffset() + size
                                , afterNode.GetSize() + remainSize
                                , false
                                , afterNode._Before
                                , afterNode._After);
                        }
                    }

                    iterNode.SetData(this
                        , iterNode.GetOffset()
                        , size
                        , true
                        , iterNode._Before
                        , iterNode._After);
                    break;
                }

                // 寻找下一个Node
                if (iterNode._After != null)
                {
                    iterNode = iterNode._After;
                }
                // iterNode是LastNode，尝试扩容
                else
                {
                    // 扩容后空间肯定能分配一个size的Buffer
                    EnsureCapacity(m_Buffer.Length + size);
                    // 最后一个Node已经被使用了，分配一个新Node
                    if (iterNode.IsUsed())
                    {
                        iterNode = ms_NodePool.Alloc().SetData(this
                            , iterNode.GetOffset() + iterNode.GetSize()
                            , size
                            , true
                            , iterNode
                            , null);
                        iterNode._Before._After = iterNode;
                    }
                    // 把最后一个Node扩容成size大小
                    else
                    {
                        iterNode.SetData(this
                            , iterNode.GetOffset()
                            , size
                            , true
                            , iterNode._Before
                            , null);
                    }

                    int lastOffset = iterNode.GetOffset() + iterNode.GetSize();
                    if (lastOffset < m_Buffer.Length)
                    {
                        // 把扩容后的剩余空间当成一个新Node
                        Node lastNode = ms_NodePool.Alloc().SetData(this
                            , lastOffset
                            , m_Buffer.Length - lastOffset
                            , false
                            , iterNode
                            , null);
                        iterNode._After = lastNode;
                    }
                    break;
                }
            }

            MDebug.LogVerbose("ArrayPool"
                , $"Alloc buffer offset:{iterNode.GetOffset()} size:{iterNode.GetSize()}");

            return iterNode;
        }

        public void ReleaseBuffer(Node buffer)
        {
            MDebug.Assert(buffer.IsUsed() && buffer.GetOwner() == this
                , "ArrayPool"
                , "buffer.IsUsed() && buffer.GetOwner() == this");

            buffer.SetUsed(false);

            Node before = buffer._Before;
            if (before != null
                && !before.IsUsed())
            {
                before.SetData(this
                    , before.GetOffset()
                    , before.GetSize() + buffer.GetSize()
                    , false
                    , before._Before
                    , buffer._After);
                ms_NodePool.Release(buffer);
                buffer = before;
                if (buffer._After != null)
                {
                    buffer._After._Before = buffer;
                }
            }

            Node after = buffer._After;
            if (after != null
                && !after.IsUsed())
            {
                buffer.SetData(this
                    , buffer.GetOffset()
                    , buffer.GetSize() + after.GetSize()
                    , false
                    , buffer._Before
                    , after._After);
                ms_NodePool.Release(after);
                if (buffer._After != null)
                {
                    buffer._After._Before = buffer;
                }
            }
        }

#if GF_DEBUG
        public void VerifyLegality()
        {
            Node iterNode = m_Root;
            Node beforeNode = null;
            int nodeCount = 0;
            while (iterNode != null)
            {
                nodeCount++;
                if (beforeNode != null)
                {
                    MDebug.Assert(iterNode._Before == beforeNode
                        , "ArrayPool"
                        , "iterNode._Before == beforeNode");

                    MDebug.Assert(beforeNode.GetOffset() + beforeNode.GetSize() == iterNode.GetOffset()
                        , "ArrayPool"
                        , "beforeNode.GetOffset() + beforeNode.GetSize() == iterNode.GetOffset()");
                }

                beforeNode = iterNode;
                iterNode = iterNode._After;
            }

            MDebug.Assert(nodeCount == ms_NodePool.GetUsingCount()
                , "ArrayPool"
                , "nodeCount == m_NodePool.GetUsingCount()");

            MDebug.Assert(beforeNode.GetOffset() + beforeNode.GetSize() == m_Buffer.Length
                , "ArrayPool"
                , "beforeNode.GetOffset() + beforeNode.GetSize() == m_Buffer.Length");
        }
#endif

        private void EnsureCapacity(int capacity)
        {
            int size = m_Buffer.Length;
            while (size <= capacity)
            {
                size *= 2;
            }
            MDebug.Log("ArrayPool", "Ensure capacity " + size);
            T[] newBuffer = new T[size];
            Array.Copy(m_Buffer, 0, newBuffer, 0, m_Buffer.Length);
            m_Buffer = newBuffer;
        }

        public class Node : IObjectPoolItem
        {
            private ArrayPool<T> m_Owner;

            /// <summary>
            /// <see cref="ArrayPool.m_Buffer"/>的Offset
            /// </summary>
            private int m_Offset;
            /// <summary>
            /// <see cref="ArrayPool.m_Buffer"/>的Size
            /// </summary>
            private int m_Size;
            /// <summary>
            /// 是否被使用
            /// </summary>
            private bool m_Used;

            internal Node _Before;
            internal Node _After;

            public ArrayPool<T> GetOwner()
            {
                return m_Owner;
            }

            public T[] GetBuffer()
            {
                return m_Owner.GetBuffer();
            }

            public bool IsUsed()
            {
                return m_Used;
            }

            public int GetSize()
            {
                return m_Size;
            }

            public int GetOffset()
            {
                return m_Offset;
            }

            public void OnAlloc()
            {
            }

            public void OnRelease()
            {
                m_Owner = null;
                _Before = null;
                _After = null;
            }

            internal Node SetData(ArrayPool<T> owner
                , int offset
                , int size
                , bool used
                , Node before = null
                , Node after = null)
            {
                m_Owner = owner;

                m_Offset = offset;
                m_Size = size;
                m_Used = used;

                _Before = before;
                _After = after;

                return this;
            }

            internal void SetUsed(bool used)
            {
                m_Used = used;
            }
        }
    }
}
