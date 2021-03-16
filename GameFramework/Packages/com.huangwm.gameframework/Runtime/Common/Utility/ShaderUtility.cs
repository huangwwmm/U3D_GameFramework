using UnityEngine.Rendering;

namespace GF.Common.Utility
{
    public static class ShaderUtility
    {
        public const string UNUSED_KEYWORD_NAME = "None";

        public static bool IsValidAndUsed(ShaderKeyword keyword, string keywordName)
        {
            return keyword.IsValid()
                && !string.IsNullOrEmpty(keywordName)
                && keywordName != UNUSED_KEYWORD_NAME;
        }
    }
}