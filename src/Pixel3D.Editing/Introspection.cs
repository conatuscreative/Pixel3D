// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Pixel3D.Editing
{
	public static class Introspection
	{
		private static Type[] _externalTypes;

		private static readonly Dictionary<string, Assembly> VisitedAssemblies;
		private static readonly HashSet<string> VisitedFilePaths;

		static Introspection()
		{
			AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += CurrentDomainOnReflectionOnlyAssemblyResolve;
			VisitedAssemblies = new Dictionary<string, Assembly>();
			VisitedFilePaths = new HashSet<string>();
		}

		public static void Invalidate()
		{
			VisitedAssemblies.Clear();
			VisitedFilePaths.Clear();
			_externalTypes = null;
		}

		private static Assembly CurrentDomainOnReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
		{
			Assembly assembly;
			if (VisitedAssemblies.TryGetValue(args.Name, out assembly))
				return assembly;

			var assemblyName = new AssemblyName(args.Name);
			if (!IsValidArchitecture(assemblyName))
				return null;

			try
			{
				assembly = Assembly.ReflectionOnlyLoad(assemblyName.FullName);
			}
			catch (FileNotFoundException)
			{
				var baseDirectory = Path.GetDirectoryName(args.RequestingAssembly.Location);
				if (baseDirectory == null)
					throw new NullReferenceException("could not resolve requesting assembly's directory");

				var guessFileName = Path.Combine(baseDirectory, assemblyName.Name + ".dll");
				if (File.Exists(guessFileName))
					assembly = Assembly.ReflectionOnlyLoadFrom(guessFileName);
				else
					foreach (var assemblyFile in Directory.GetFiles(baseDirectory))
						if (AssemblyName.GetAssemblyName(assemblyFile) == assemblyName)
							assembly = Assembly.ReflectionOnlyLoadFrom(assemblyFile);

				if (assembly == null)
					throw new InvalidOperationException(
						"unable to resolve a referenced assembly in the introspection folder");
			}

			VisitedAssemblies.Add(assembly.FullName, assembly);
			VisitedFilePaths.Add(assembly.Location);
			return assembly;
		}

		public static IEnumerable<Type> GetExportedTypes(string introspectionDir,
			Func<Assembly, bool> assemblyFilterFunc)
		{
			if (_externalTypes == null)
				if (!string.IsNullOrWhiteSpace(introspectionDir))
				{
					var list = new HashSet<Type>();
					foreach (var filePath in Directory.GetFiles(introspectionDir, "*.dll", SearchOption.AllDirectories))
						try
						{
							if (VisitedFilePaths.Contains(filePath))
								continue;

							var assembly = Assembly.ReflectionOnlyLoadFrom(filePath);
							VisitedAssemblies.Add(assembly.FullName, assembly);

							var assemblyName = assembly.GetName();
							if (!IsValidArchitecture(assemblyName))
								continue;

							if (!assemblyFilterFunc(assembly))
								continue;

							var types = assembly.GetExportedTypes();
							foreach (var type in types)
								list.Add(type);
						}
						catch (Exception e)
						{
							Trace.TraceError(e.ToString());
						}
						finally
						{
							VisitedFilePaths.Add(filePath);
						}

					_externalTypes = list.ToArray();
				}

			return _externalTypes;
		}

		private static bool IsValidArchitecture(AssemblyName assemblyName)
		{
			var isx64 = Environment.Is64BitProcess;
			switch (assemblyName.ProcessorArchitecture)
			{
				case ProcessorArchitecture.X86:
					if (isx64)
						return false;
					break;
				case ProcessorArchitecture.IA64:
				case ProcessorArchitecture.Amd64:
					if (!isx64)
						return false;
					break;
			}

			return true;
		}
	}
}