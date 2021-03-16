using System;
using System.Reflection;

namespace LitJson
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
	public class JsonPropertyAttribute : Attribute
	{
		public string JsonProperty;

		public JsonPropertyAttribute(string jsonProperty)
		{
			JsonProperty = jsonProperty;
		}
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
	public class JsonIgnoreAttribute : Attribute
	{
		public static bool Ignore(MemberInfo info)
		{
			JsonIgnoreAttribute jsonIgnoreAttribute = info.GetCustomAttribute<JsonIgnoreAttribute>();
			if (jsonIgnoreAttribute == null)
			{
				return false;
			}
			else
			{
				return jsonIgnoreAttribute.GetIgnore();
			}
		}

		public virtual bool GetIgnore()
		{
			return true;
		}
	}
}