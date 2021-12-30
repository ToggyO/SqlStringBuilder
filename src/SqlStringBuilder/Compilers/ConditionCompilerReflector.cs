using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SqlStringBuilder.Compilers
{
	// TODO: add description.
	public class ConditionCompilerReflector
	{
		private readonly Type _compilerType;

		private readonly Dictionary<string, MethodInfo> _methodsCache = new ();

		private readonly object _lock = new ();

		public ConditionCompilerReflector(Compiler compiler)
		{
			_compilerType = compiler.GetType();
		}

		public MethodInfo GetMethodInfo(Type componentType, string methodName, Type contextType)
		{
			var cacheKey = methodName + "::" + componentType.FullName;

			lock (_lock)
			{
				if (_methodsCache.ContainsKey(cacheKey))
					return _methodsCache[cacheKey];

				return _methodsCache[cacheKey] = FindMethodInfo(componentType, methodName, contextType);
			}
		}

		public MethodInfo FindMethodInfo(Type componentType, string methodName, Type contextType)
		{
			// TODO: check the difference between "GetMethods" and "GetRuntimeMethods"
			var methodInfo = _compilerType.GetRuntimeMethods().FirstOrDefault(x => x.Name == methodName);

			if (methodInfo == null)
				throw new Exception($"Failed to locate a compiler for '{methodName}'.");


			if (methodInfo.GetGenericArguments().Any())
			{
				var genericTypeArguments = new Type[] { };

				if (contextType.IsConstructedGenericType)
					genericTypeArguments = contextType.GenericTypeArguments;

				if (componentType.IsConstructedGenericType)
					genericTypeArguments = genericTypeArguments.Concat(componentType.GenericTypeArguments).ToArray();

				if (genericTypeArguments.Length > 0)
					return methodInfo.MakeGenericMethod(genericTypeArguments);
			}

			return methodInfo;
		}
	}
}
