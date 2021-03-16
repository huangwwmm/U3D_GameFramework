using System;

namespace GF.Common
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CTCustomFillAttribute : Attribute
    {
        public int Priority;

        public CTCustomFillAttribute()
        {
            Priority = CTMenuItemAttribute._s_TempProiority++;
        }

        public CTCustomFillAttribute(int priority)
        {
            Priority = priority;
        }
    }
}