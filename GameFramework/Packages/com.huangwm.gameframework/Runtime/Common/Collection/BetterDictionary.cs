using System;
using System.Collections.Generic;
using UnityEngine;

namespace GF.Common.Collection
{
    [Serializable]
    public class BetterDictionary<TKey, TValue>
    {
        private int[] m_Buckets;
        private Entry[] m_Entries;
        private int m_Count;
        private int m_Version;
        private int m_FreeList;
        private int m_FreeCount;

        private IEqualityComparer<TKey> m_Comparer;

        public TValue this[TKey key]
        {
            get
            {
                int index = FindEntry(key);
                if (index >= 0)
                {
                    return m_Entries[index].value;
                }
                else
                {
                    throw new KeyNotFoundException();
                }
            }
            set
            {
                Insert(key, value, false);
            }
        }

        public BetterDictionary() : this(0, null) { }

        public BetterDictionary(int capacity) : this(capacity, null) { }

        public BetterDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity");
            }
            if (capacity > 0)
            {
                Initialize(capacity);
            }
            m_Comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        public int Count
        {
            get { return m_Count - m_FreeCount; }
        }

        public void Add(TKey key, TValue value)
        {
            Insert(key, value, true);
        }

        public bool Remove(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (m_Buckets != null)
            {
                int hashCode = m_Comparer.GetHashCode(key) & 0x7FFFFFFF;
                int bucket = hashCode % m_Buckets.Length;
                int last = -1;
                for (int i = m_Buckets[bucket]; i >= 0; last = i, i = m_Entries[i].next)
                {
                    if (m_Entries[i].hashCode == hashCode && m_Comparer.Equals(m_Entries[i].key, key))
                    {
                        if (last < 0)
                        {
                            m_Buckets[bucket] = m_Entries[i].next;
                        }
                        else
                        {
                            m_Entries[last].next = m_Entries[i].next;
                        }
                        m_Entries[i].hashCode = -1;
                        m_Entries[i].next = m_FreeList;
                        m_Entries[i].key = default(TKey);
                        m_Entries[i].value = default(TValue);
                        m_FreeList = i;
                        m_FreeCount++;
                        m_Version++;
                        return true;
                    }
                }
            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            int index = FindEntry(key);
            if (index >= 0)
            {
                value = m_Entries[index].value;
                return true;
            }
            value = default(TValue);
            return false;
        }

        public void Clear()
        {
            if (m_Count > 0)
            {
                for (int i = 0; i < m_Buckets.Length; i++)
                {
                    m_Buckets[i] = -1;
                }
                Array.Clear(m_Entries, 0, m_Count);
                m_FreeList = -1;
                m_Count = 0;
                m_FreeCount = 0;
                m_Version++;
            }
        }

        private void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            if (index < 0 || index > array.Length)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            if (array.Length - index < Count)
            {
                throw new ArgumentException("Arg Array Plus Off Too Small");
            }

            int count = m_Count;
            Entry[] entries = m_Entries;
            for (int i = 0; i < count; i++)
            {
                if (entries[i].hashCode >= 0)
                {
                    array[index++] = new KeyValuePair<TKey, TValue>(entries[i].key, entries[i].value);
                }
            }
        }

        public Entry[] GetEntries()
        {
            return m_Entries;
        }

        private void Insert(TKey key, TValue value, bool add)
        {
            if (key == null)
            {
                throw new System.ArgumentNullException("key");
            }

            if (m_Buckets == null) Initialize(0);
            int hashCode = m_Comparer.GetHashCode(key) & 0x7FFFFFFF;
            int targetBucket = hashCode % m_Buckets.Length;

            for (int i = m_Buckets[targetBucket]; i >= 0; i = m_Entries[i].next)
            {
                if (m_Entries[i].hashCode == hashCode && m_Comparer.Equals(m_Entries[i].key, key))
                {
                    if (add)
                    {
                        throw new System.ArgumentException("Argument Adding Duplicate");
                    }
                    m_Entries[i].value = value;
                    m_Version++;
                    return;
                }
            }
            int index;
            if (m_FreeCount > 0)
            {
                index = m_FreeList;
                m_FreeList = m_Entries[index].next;
                m_FreeCount--;
            }
            else
            {
                if (m_Count == m_Entries.Length)
                {
                    Resize();
                    targetBucket = hashCode % m_Buckets.Length;
                }
                index = m_Count;
                m_Count++;
            }

            m_Entries[index].hashCode = hashCode;
            m_Entries[index].next = m_Buckets[targetBucket];
            m_Entries[index].key = key;
            m_Entries[index].value = value;
            m_Buckets[targetBucket] = index;
            m_Version++;
        }

        private int FindEntry(TKey key)
        {
            if (key == null)
            {
                throw new System.ArgumentNullException("key");
            }

            if (m_Buckets != null)
            {
                int hashCode = m_Comparer.GetHashCode(key) & 0x7FFFFFFF;
                for (int i = m_Buckets[hashCode % m_Buckets.Length]; i >= 0; i = m_Entries[i].next)
                {
                    if (m_Entries[i].hashCode == hashCode && m_Comparer.Equals(m_Entries[i].key, key)) return i;
                }
            }
            return -1;
        }

        private void Resize()
        {
            Resize(GF.Common.Utility.HashUtility.ExpandPrime(m_Count), false);
        }

        private void Resize(int newSize, bool forceNewHashCodes)
        {
            UnityEngine.Debug.Assert(newSize >= m_Entries.Length, "newSize >= entries.Length");
            int[] newBuckets = new int[newSize];
            for (int i = 0; i < newBuckets.Length; i++) newBuckets[i] = -1;
            Entry[] newEntries = new Entry[newSize];
            for (int iEntry = 0; iEntry < newEntries.Length; iEntry++)
            {
                newEntries[iEntry].hashCode = -1;
            }
            Array.Copy(m_Entries, 0, newEntries, 0, m_Count);
            if (forceNewHashCodes)
            {
                for (int i = 0; i < m_Count; i++)
                {
                    if (newEntries[i].hashCode != -1)
                    {
                        newEntries[i].hashCode = (m_Comparer.GetHashCode(newEntries[i].key) & 0x7FFFFFFF);
                    }
                }
            }
            for (int i = 0; i < m_Count; i++)
            {
                if (newEntries[i].hashCode >= 0)
                {
                    int bucket = newEntries[i].hashCode % newSize;
                    newEntries[i].next = newBuckets[bucket];
                    newBuckets[bucket] = i;
                }
            }
            m_Buckets = newBuckets;
            m_Entries = newEntries;
        }

        private void Initialize(int capacity)
        {
            int size = GF.Common.Utility.HashUtility.GetPrime(capacity);
            m_Buckets = new int[size];
            for (int i = 0; i < m_Buckets.Length; i++) m_Buckets[i] = -1;
            m_Entries = new Entry[size];
            for (int iEntry = 0; iEntry < m_Entries.Length; iEntry++)
            {
                m_Entries[iEntry].hashCode = -1;
            }
            m_FreeList = -1;
        }

        [Serializable]
        public struct Entry
        {
            /// <summary>
            /// Lower 31 bits of hash code, -1 if unused
            /// </summary>
            public int hashCode;
            /// <summary>
            /// Index of next entry, -1 if last
            /// </summary>
            public int next;
            /// <summary>
            /// Key of entry
            /// </summary>
            public TKey key;
            /// <summary>
            /// Value of entry
            /// </summary>
            public TValue value;
        }
    }
}