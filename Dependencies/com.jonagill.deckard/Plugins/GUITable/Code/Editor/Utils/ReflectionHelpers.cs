using System;
using System.Reflection;
using System.Collections.Generic;

namespace EditorGUITable
{

	public static class ReflectionHelpers
	{

		public static Type GetType(string TypeName)
		{

			// Try Type.GetType() first. This will work with types defined
			// by the Mono runtime, in the same assembly as the caller, etc.
			var type = Type.GetType(TypeName);

			// If it worked, then we're done here
			if (type != null)
				return type;

			// If the TypeName is a full name, then we can try loading the defining assembly directly
			if (TypeName.Contains("."))
			{

				// Get the name of the assembly (Assumption is that we are using 
				// fully-qualified type names)
				var assemblyName = TypeName.Substring(0, TypeName.IndexOf('.'));

				// Attempt to load the indicated Assembly
				var assembly = Assembly.Load(assemblyName);
				if (assembly == null)
					return null;

				// Ask that assembly to return the proper Type
				type = assembly.GetType(TypeName);
				if (type != null)
					return type;

			}

			// If we still haven't found the proper type, we can enumerate all of the 
			// loaded assemblies and see if any of them define the type
			var currentAssembly = Assembly.GetExecutingAssembly();
			var referencedAssemblies = currentAssembly.GetReferencedAssemblies();
			foreach (var assemblyName in referencedAssemblies)
			{

				// Load the referenced assembly
				var assembly = Assembly.Load(assemblyName);
				if (assembly != null)
				{
					// See if that assembly defines the named type
					type = assembly.GetType(TypeName);
					if (type != null)
						return type;
				}
			}

			// The type just couldn't be found...
			return null;

		}

		public static FieldInfo GetFieldInfoFromPropertyPath(Type host, string path, out Type type)
		{
			FieldInfo field = null;
			type = host;
			string[] parts = path.Split('.');
			for (int i = 0; i < parts.Length; i++)
			{
				string member = parts[i];

				// Special handling of array elements.
				// The "Array" and "data[x]" parts of the propertyPath don't correspond to any types,
				// so they should be skipped by the code that drills down into the types.
				// However, we want to change the type from the type of the array to the type of the array element before we do the skipping.
				if (i < parts.Length - 1 && member == "Array" && parts[i + 1].StartsWith("data["))
				{
					if (type.IsArrayOrList())
						type = type.GetArrayOrListElementType();

					// Skip rest of handling for this part ("Array") and the next part ("data[x]").
					i++;
					continue;
				}

				// GetField on class A will not find private fields in base classes to A,
				// so we have to iterate through the base classes and look there too.
				// Private fields are relevant because they can still be shown in the Inspector,
				// and that applies to private fields in base classes too.
				FieldInfo foundField = null;
				for (Type currentType = type; foundField == null && currentType != null; currentType = currentType.BaseType)
					foundField = currentType.GetField(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

				if (foundField == null)
				{
					type = null;
					return null;
				}

				field = foundField;
				type = field.FieldType;
			}
			return field;
		}

		public static bool IsArrayOrList(this Type listType)
		{
			if (listType.IsArray)
			{
				return true;
			}
			else if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
			{
				return true;
			}
			return false;
		}

		public static Type GetArrayOrListElementType(this Type listType)
		{
			if (listType.IsArray)
			{
				return listType.GetElementType();
			}
			else if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
			{
				return listType.GetGenericArguments()[0];
			}
			return null;
		}

	}

}
