using GF.Common.Debug;
using GF.Common.Utility;
using System;
using System.Collections.Generic;

namespace GF.XLuaFramework
{
    /// <summary>
    /// 解析lua脚本，包括：
    ///     宏定义#if
    /// 具体用法：<see cref="GF.Core.KernelInitializeData.LuaScriptingDefine"/>
    /// </summary>
    internal class LuaScriptParser
    {
        private const string DEINFE_BEGIN = "--[#if ";
        private const string DEINFE_END = "--]#endif";
        private readonly char[] SEPARATOR = new char[] { '\n' };

        public static LuaScriptParser s_Instance;

        private HashSet<string> m_ScriptingDefine;
        private Stack<bool> m_EnableCodeStackCache;
        private Queue<ItemType> m_ItemCache;
        private Stack<ItemType> m_ItemOperatorCache;
        /// <summary>
        /// 当前正在处理的行
        /// </summary>
        private string m_DefineLine;

        public LuaScriptParser(HashSet<string> luaScriptingDefine)
        {
            MDebug.Assert(s_Instance == null, "XLua", "s_Instance == null");
            s_Instance = this;

#if UNITY_EDITOR
            luaScriptingDefine.Add("UNITY_EDITOR");
#endif

#if GF_DEBUG
            luaScriptingDefine.Add("GF_DEBUG");
#endif

            m_ScriptingDefine = luaScriptingDefine;

            m_EnableCodeStackCache = new Stack<bool>();
            m_ItemCache = new Queue<ItemType>();
            m_ItemOperatorCache = new Stack<ItemType>();
        }

        public void Release()
        {
            MDebug.Assert(s_Instance == this, "XLua", "s_Instance == this");
            s_Instance = null;

            m_DefineLine = null;
            m_ItemOperatorCache = null;
            m_ItemCache = null;
            m_EnableCodeStackCache = null;
            m_ScriptingDefine = null;
        }

        /// <summary>
        /// 解析字符串中的宏定义
        /// </summary>
        public string Parse(string script)
        {
            string[] lines = script.Split(SEPARATOR, StringSplitOptions.None);
            System.Text.StringBuilder stringBuilder = StringUtility.AllocStringBuilder();
            bool enableCode = true;
            m_EnableCodeStackCache.Push(true);
            for (int iLine = 0; iLine < lines.Length; iLine++)
            {
                string iterLine = lines[iLine];
                string iterLineTrim = iterLine.Trim();
                if (iterLineTrim.StartsWith(DEINFE_BEGIN))
                {
                    m_DefineLine = iterLineTrim.Substring(DEINFE_BEGIN.Length);
                    enableCode &= ParseDefineLine();
                    m_EnableCodeStackCache.Push(enableCode);
                }
                else if (iterLineTrim.StartsWith(DEINFE_END))
                {
                    MDebug.Assert(m_EnableCodeStackCache.Count > 1, "XLua", "m_EnableCodeStack.Count > 0");
                    m_EnableCodeStackCache.Pop();
                    enableCode = m_EnableCodeStackCache.Peek();
                }
                else if (enableCode)
                {
                    stringBuilder.Append(iterLine);
                }

                stringBuilder.Append('\n');
            }

            m_EnableCodeStackCache.Pop();
            MDebug.Assert(m_EnableCodeStackCache.Count == 0, "XLua", "m_EnableCodeStack.Count == 0");

#if GF_DEBUG
            MDebug.Log("XLua", stringBuilder.ToString());
#endif
            return StringUtility.ReleaseStringBuilder(stringBuilder);
        }

        private bool ParseDefineLine()
        {
            #region Convert to Reverse Polish notation
            // 这里用sring会产生大量的GC，但是这个函数只在初始化Lua脚本的时候调用，不会在游戏中频繁调用
            string lastString = string.Empty;
            bool lastStringIsDefine = false;
            for (int iChar = 0; iChar < m_DefineLine.Length; iChar++)
            {
                char iterChar = m_DefineLine[iChar];

                switch (iterChar)
                {
                    case '!':
                        if (lastString == string.Empty)
                        {
                            PushOperator(ItemType.Not);
                        }
                        else
                        {
                            ThrowParseDefineError();
                        }
                        break;
                    case '(':
                        if (lastString == string.Empty)
                        {
                            PushOperator(ItemType.LParentheses);
                        }
                        else
                        {
                            ThrowParseDefineError();
                        }
                        break;
                    case ')':
                        ParseOperator(ref lastString, ref lastStringIsDefine, ItemType.RParentheses);
                        break;
                    case '&':
                        ParseReduplicationOperator(ref lastString
                             , ref lastStringIsDefine
                             , ItemType.And
                             , "&");

                        break;
                    case '|':
                        ParseReduplicationOperator(ref lastString
                            , ref lastStringIsDefine
                            , ItemType.Or
                            , "|");
                        break;
                    case ' ':
                        if (lastString != string.Empty)
                        {
                            if (lastStringIsDefine)
                            {
                                ParseValue(ref lastString, ref lastStringIsDefine);
                            }
                            else
                            {
                                ThrowParseDefineError();
                            }
                        }
                        break;
                    default:
                        if (lastString != string.Empty)
                        {
                            if (lastStringIsDefine)
                            {
                                lastString += iterChar;
                            }
                            else
                            {
                                ThrowParseDefineError();
                            }
                        }
                        else
                        {
                            lastString += iterChar;
                            lastStringIsDefine = true;
                        }
                        break;
                }
            }

            ParseValue(ref lastString, ref lastStringIsDefine);
            MDebug.Assert(lastString == string.Empty, "XLua", "lastString == string.Empty");

            while (m_ItemOperatorCache.Count > 0)
            {
                m_ItemCache.Enqueue(m_ItemOperatorCache.Pop());
            }
            #endregion

            #region Caculate
            while (m_ItemCache.Count > 0)
            {
                ItemType itemType = m_ItemCache.Dequeue();
                if (itemType == ItemType.True
                    || itemType == ItemType.False)
                {
                    m_ItemOperatorCache.Push(itemType);
                }
                else if (itemType == ItemType.Not)
                {
                    MDebug.Assert(m_ItemOperatorCache.Count > 0, "XLua", "m_ItemOperatorCache.Count > 0");
                    ItemType operatorValue = m_ItemOperatorCache.Pop();
                    if (operatorValue == ItemType.True)
                    {
                        m_ItemOperatorCache.Push(ItemType.False);
                    }
                    else if (operatorValue == ItemType.False)
                    {
                        m_ItemOperatorCache.Push(ItemType.True);
                    }
                    else
                    {
                        ThrowParseDefineError();
                    }
                }
                else if (itemType == ItemType.And
                    || itemType == ItemType.Or)
                {
                    MDebug.Assert(m_ItemOperatorCache.Count > 1, "XLua", "m_ItemOperatorCache.Count > 1");
                    ItemType operatorValue1 = m_ItemOperatorCache.Pop();
                    ItemType operatorValue2 = m_ItemOperatorCache.Pop();
                    if ((operatorValue1 == ItemType.True || operatorValue1 == ItemType.False)
                        && (operatorValue2 == ItemType.True || operatorValue2 == ItemType.False))
                    {
                        if (itemType == ItemType.And)
                        {
                            m_ItemOperatorCache.Push((operatorValue1 == ItemType.True) && (operatorValue2 == ItemType.True)
                                 ? ItemType.True
                                 : ItemType.False);
                        }
                        else if (itemType == ItemType.Or)
                        {
                            m_ItemOperatorCache.Push((operatorValue1 == ItemType.True) || (operatorValue2 == ItemType.True)
                                ? ItemType.True
                                : ItemType.False);
                        }
                        else
                        {
                            ThrowParseDefineError();
                        }
                    }
                    else
                    {
                        ThrowParseDefineError();
                    }
                }
                else
                {
                    ThrowParseDefineError();
                }
            }
            #endregion

            if (m_ItemOperatorCache.Count != 1)
            {
                ThrowParseDefineError();
            }

            ItemType resultItem = m_ItemOperatorCache.Pop();
            bool result = false;
            switch(resultItem)
            {
                case ItemType.True:
                    result = true;
                    break;
                case ItemType.False:
                    result = false;
                    break;
                default:
                    ThrowParseDefineError();
                    break;
            }
            MDebug.Log("XLua", $"Parse define: {result} = {m_DefineLine}");
            return result;
        }

        private void ThrowParseDefineError()
        {
            throw new Exception("Parse lua script define failed. Define:\n" + m_DefineLine);
        }

        /// <summary>
        /// 解析单项宏定义 True 或 False
        /// </summary>
        private void ParseValue(ref string lastString
            , ref bool lastStringIsDefine)
        {
            if (lastStringIsDefine)
            {
                m_ItemCache.Enqueue(m_ScriptingDefine.Contains(lastString)
                    ? ItemType.True
                    : ItemType.False);
                lastString = string.Empty;
                lastStringIsDefine = false;
            }
        }

        /// <summary>
        /// 解析单个字符的运算符
        /// </summary>
        private void ParseOperator(ref string lastString
            , ref bool lastStringIsDefine
            , ItemType operatorType)
        {
            ParseValue(ref lastString, ref lastStringIsDefine);

            if (lastString == string.Empty)
            {
                PushOperator(operatorType);
            }
            else
            {
                ThrowParseDefineError();
            }
        }

        /// <summary>
        /// 解析 && || 这种两个重复字符的运算符
        /// </summary>
        private void ParseReduplicationOperator(ref string lastString
            , ref bool lastStringIsDefine
            , ItemType operatorType
            , string operatorChar)
        {
            ParseValue(ref lastString, ref lastStringIsDefine);

            if (lastString == operatorChar)
            {
                PushOperator(operatorType);
                lastString = string.Empty;
            }
            else if (lastString == string.Empty)
            {
                lastString = operatorChar;
                lastStringIsDefine = false;
            }
            else
            {
                ThrowParseDefineError();
            }
        }

        private void PushOperator(ItemType operatorType)
        {
            if (m_ItemOperatorCache.Count == 0
                || operatorType == ItemType.LParentheses)
            {
                m_ItemOperatorCache.Push(operatorType);
            }
            else if (operatorType == ItemType.RParentheses)
            {
                bool hasL = false;
                while (m_ItemOperatorCache.Count > 0)
                {
                    ItemType itemType = m_ItemOperatorCache.Pop();
                    hasL = itemType == ItemType.LParentheses;
                    if (hasL)
                    {
                        break;
                    }

                    m_ItemCache.Enqueue(itemType);
                }

                if (!hasL)
                {
                    ThrowParseDefineError();
                }
            }
            else if (operatorType == ItemType.And
                || operatorType == ItemType.Or
                || operatorType == ItemType.Not)
            {
                bool pushed = false;
                while (m_ItemOperatorCache.Count > 0)
                {
                    ItemType itemType = m_ItemOperatorCache.Peek();
                    if (itemType == ItemType.LParentheses)
                    {
                        break;
                    }

                    if (ItemTypeToPriority(itemType) >= ItemTypeToPriority(operatorType))
                    {
                        m_ItemCache.Enqueue(m_ItemOperatorCache.Pop());
                    }
                    else
                    {
                        pushed = true;
                        m_ItemOperatorCache.Push(operatorType);
                        break;
                    }
                }
                if (!pushed)
                {
                    m_ItemOperatorCache.Push(operatorType);
                }
            }
            else
            {
                ThrowParseDefineError();
            }
        }

        /// <summary>
        /// 运算符的优先级
        /// </summary>
        private int ItemTypeToPriority(ItemType itemType)
        {
            switch(itemType)
            {
                case ItemType.And:
                case ItemType.Or:
                    return 0;
                case ItemType.Not:
                    return 1;
                default:
                    ThrowParseDefineError();
                    return -1;
            }
        }

        private enum ItemType
        {
            True,
            False,
            LParentheses,
            RParentheses,

            And,
            Or,
            Not,
        }
    }
}