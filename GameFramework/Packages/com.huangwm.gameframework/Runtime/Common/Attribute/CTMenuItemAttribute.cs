using System;

namespace GF.Common
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class CTMenuItemAttribute : Attribute
	{
		internal static int _s_TempProiority = int.MaxValue - 10000000;

		public string MenuName;
		public int Priority;

		public CTMenuItemAttribute(string menu)
		{
			MenuName = menu;
			Priority = _s_TempProiority++;
		}

		public CTMenuItemAttribute(string menu, int priority)
		{
			MenuName = menu;
			Priority = priority;
		}
	}
}