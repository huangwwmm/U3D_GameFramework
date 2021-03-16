using System;

namespace GF.DebugPanel
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class StartupTabAttribute : Attribute
    {
        public string TabName;
        public bool OnlyRuntime;

        public StartupTabAttribute(string tabName, bool onlyRuntime)
        {
            TabName = tabName;
            OnlyRuntime = onlyRuntime;
        }
    }
}
