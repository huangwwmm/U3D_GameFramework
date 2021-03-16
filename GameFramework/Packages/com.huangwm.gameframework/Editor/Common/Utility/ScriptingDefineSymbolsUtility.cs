using System.Collections.Generic;
using UnityEditor;

namespace GFEditor.Common.Utility
{
    public static class ScriptingDefineSymbolsUtility
    {
        public readonly static List<string> s_TemporarySymbols = new List<string>();

        public static void GetScriptingDefineSymbols(List<string> symbols)
        {
            GetScriptingDefineSymbols(EditorUserBuildSettings.selectedBuildTargetGroup, symbols);
        }

        public static void GetScriptingDefineSymbols(BuildTargetGroup targetGroup, List<string> symbols)
        {
            string symbol = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            symbols.AddRange(symbol.Split(';'));
        }

        public static void SetScriptingDefineSymbols(List<string> symbols)
        {
            SetScriptingDefineSymbols(EditorUserBuildSettings.selectedBuildTargetGroup, symbols);
        }

        public static void SetScriptingDefineSymbols(BuildTargetGroup targetGroup, List<string> symbols)
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, string.Join(";", symbols));
        }

        public static bool HasScriptingDefineSymbol(string symbol)
        {
            return HasScriptingDefineSymbol(EditorUserBuildSettings.selectedBuildTargetGroup, symbol);
        }

        public static bool HasScriptingDefineSymbol(BuildTargetGroup targetGroup, string symbol)
        {
            s_TemporarySymbols.Clear();
            GetScriptingDefineSymbols(targetGroup, s_TemporarySymbols);
            return s_TemporarySymbols.Contains(symbol);
        }

        public static void SetScriptingDefineSymbol(string symbol, bool enable)
        {
            SetScriptingDefineSymbol(EditorUserBuildSettings.selectedBuildTargetGroup, symbol, enable);
        }

        public static void SetScriptingDefineSymbol(BuildTargetGroup targetGroup, string symbol, bool enable)
        {
            s_TemporarySymbols.Clear();
            GetScriptingDefineSymbols(targetGroup, s_TemporarySymbols);
            bool contains = s_TemporarySymbols.Contains(symbol);
            if (contains && !enable)
            {
                s_TemporarySymbols.Remove(symbol);
            }
            else if (!contains && enable)
            {
                s_TemporarySymbols.Add(symbol);
            }
            SetScriptingDefineSymbols(targetGroup, s_TemporarySymbols);
        }

        public static void SetScriptingDefineSymbol(List<string> symbols, string symbol, bool enable, bool apply)
        {
            SetScriptingDefineSymbol(EditorUserBuildSettings.selectedBuildTargetGroup, symbols, symbol, enable, apply);

        }

        public static void SetScriptingDefineSymbol(BuildTargetGroup targetGroup, List<string> symbols, string symbol, bool enable, bool apply)
        {
            bool contains = symbols.Contains(symbol);
            if (contains && !enable)
            {
                symbols.Remove(symbol);
            }
            else if (!contains && enable)
            {
                symbols.Add(symbol);
            }

            if (apply)
            {
                SetScriptingDefineSymbols(targetGroup, symbols);
            }
        }

        public static void SwitchScriptingDefineSymbol(string symbol)
        {
            SwitchScriptingDefineSymbol(EditorUserBuildSettings.selectedBuildTargetGroup, symbol);
        }

        public static void SwitchScriptingDefineSymbol(BuildTargetGroup targetGroup, string symbol)
        {
            s_TemporarySymbols.Clear();
            GetScriptingDefineSymbols(targetGroup, s_TemporarySymbols);
            if (s_TemporarySymbols.Contains(symbol))
            {
                s_TemporarySymbols.Remove(symbol);
            }
            else
            {
                s_TemporarySymbols.Add(symbol);
            }
            SetScriptingDefineSymbols(targetGroup, s_TemporarySymbols);
        }
    }
}