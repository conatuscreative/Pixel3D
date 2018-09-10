// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Pixel3D.Serialization.BuiltIn;
using Pixel3D.Serialization.BuiltIn.DelegateHandling;
using Pixel3D.Serialization.Discovery;

#if NET40 || NET45 || NET462
#else
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
#endif

namespace Pixel3D.Serialization.Generator
{
	internal static class GeneratorSequence
	{
		private static bool IsOurAssembly(this Assembly assembly, string baseLocation)
		{
			return baseLocation == null
				? assembly.Location == string.Empty
				: assembly.Location.StartsWith(baseLocation);
		}

		public static GeneratorResult Run(Assembly[] assemblies, Type[] predefinedRoots, GeneratorReports reports,
			bool createAssembly, string outputAssemblyName)
		{
			var baseLocation =
				assemblies.First()
					.Location; // <- should probably use all passed assemblies as potential base locations (also detect accidently passing a system one)
			if (baseLocation == "") // <- loaded from embedded resources
				baseLocation = null;
			else
				baseLocation = Path.GetDirectoryName(baseLocation);


			#region Initial Reporting - Assembly Info

			if (reports != null)
			{
				reports.Log.WriteLine(DateTime.Now);
				reports.Log.WriteLine();

				reports.Log.WriteLine("Initial Assemblies (" + assemblies.Count() + ")");
				foreach (var a in assemblies)
					reports.Log.WriteLine("  " + a);
				reports.Log.WriteLine();

				reports.Log.WriteLine("Base Location = " + baseLocation ?? "<<resources>>");
				reports.Log.WriteLine();
			}

			#endregion


			#region Search for [SerializationRoot] attributes

			var rootTypes = new HashSet<Type>();
			foreach (var assembly in assemblies)
			foreach (var type in assembly.GetTypes()
				.Where(t => t.GetCustomAttributes(typeof(SerializationRootAttribute), false).Length > 0))
				rootTypes.Add(type);

			if (predefinedRoots != null)
				foreach (var type in predefinedRoots)
					rootTypes.Add(type);

			if (reports != null)
			{
				reports.Log.WriteLine("Initial roots (" + rootTypes.Count + ")");
				foreach (var type in rootTypes)
					reports.Log.WriteLine("  " + type);
				reports.Log.WriteLine();
			}

			#endregion


			#region Custom Method Discovery

			if (reports != null)
				reports.Log.WriteLine("Running custom method discovery");

			var customMethodAssemblies = new HashSet<Assembly>(assemblies);
			customMethodAssemblies.Add(typeof(SerializeList)
				.Assembly); // <- Get the assembly that contains all our "built-in" serializers

			var customMethods = CustomMethodDiscovery.Run(customMethodAssemblies,
				reports != null ? reports.CustomMethodDiscovery : null, reports != null ? reports.Error : null);

			// Attach hard-coded array serializer methods:
			customMethods =
				SerializationMethodProviders.Combine(SerializeArray.CreateSerializationMethodProviders(),
					customMethods);

			#endregion


			#region Type Discovery - First Pass

			var td = new TypeDiscovery(customMethods, assemblies);

			if (reports != null)
			{
				reports.Log.WriteLine("Running Type Discovery - First Pass");
				reports.TypeDiscovery.WriteLine("---------------------------");
				reports.TypeDiscovery.WriteLine("TYPE DISCOVERY - FIRST PASS");
				reports.TypeDiscovery.WriteLine("---------------------------");
				reports.TypeDiscovery.WriteLine();
			}

			td.DiscoverFromRoots(rootTypes,
				reports != null ? reports.TypeDiscovery : null, reports != null ? reports.Error : null);

			#endregion


			#region Delegate Discovery and Type Discovery Second Pass

			List<DelegateUsage> delegateDiscoveryResult = null;
			IEnumerable<Type> allDelegateTargetTypes = null;

			if (td.FoundDelegates)
			{
				Debug.WriteLine("IMPORTANT: Serializer generator is doing delegate discovery! May be undesireable.");

				#region Delegate Discovery

				if (reports != null) reports.Log.WriteLine("Running Delegate Discovery");

				// Only search for delegates in our own assemblies:
				var delegateDiscoveryAssemblies = td.Assemblies.Where(a => a.IsOurAssembly(baseLocation));

				if (reports != null)
				{
					reports.DelegateDiscovery.WriteLine("Searching in assemblies:");
					foreach (var a in delegateDiscoveryAssemblies)
						reports.DelegateDiscovery.WriteLine("  " + a);
					reports.DelegateDiscovery.WriteLine();
					reports.DelegateDiscovery.WriteLine();
				}

				delegateDiscoveryResult = DelegateDiscovery.Run(delegateDiscoveryAssemblies,
					reports != null ? reports.DelegateDiscovery : null, reports != null ? reports.Error : null);

				if (reports != null)
					DelegateDiscovery.WriteDelegateUsageGrouped(delegateDiscoveryResult,
						reports.DelegateDiscoveryGrouped);

				#endregion


				#region Type Discovery - Second Pass

				if (reports != null)
				{
					reports.Log.WriteLine("Running Type Discovery - Second Pass");
					reports.TypeDiscovery.WriteLine("----------------------------");
					reports.TypeDiscovery.WriteLine("TYPE DISCOVERY - SECOND PASS");
					reports.TypeDiscovery.WriteLine("----------------------------");
					reports.TypeDiscovery.WriteLine();
				}

				allDelegateTargetTypes = delegateDiscoveryResult.Select(du => du.targetType)
					.Where(t => t != null) // <- Disregard delegates with static targets
					.Distinct();

				td.DiscoverFromRoots(allDelegateTargetTypes,
					reports != null ? reports.TypeDiscovery : null, reports != null ? reports.Error : null);

				#endregion
			}
			else
			{
				if (reports != null) reports.Log.WriteLine("Was able to skip delegate discovery!");
			}

			#endregion


			#region Type Classification

			if (reports != null)
				reports.Log.WriteLine("Running type classification");

			var tc = new TypeClassifier(td);
			tc.RunClassification();

			if (reports != null)
			{
				tc.WriteReport(reports.TypeClassification, reports.Error);

				CustomMethodDiscovery.CheckForDerivedCustomInitializers(customMethods.ReferenceTypeInitializeMethods,
					tc.ReferenceTypes, reports.Error);
			}

			#endregion


			#region Delegate Classification

			Dictionary<Type, DelegateTypeInfo> delegateTypeTable = null;

			if (delegateDiscoveryResult != null)
			{
				var dc = new DelegateClassification(td.delegateFieldTypes, delegateDiscoveryResult);

				delegateTypeTable = dc.GenerateDelegateTypeTable();

				if (reports != null)
				{
					dc.Report(reports.DelegateClassification, reports.Error);

					// NOTE: Any methods that appear in this list consitute a security risk, as they can be sent across the network!
					//       Make sure that all these methods are "safe" (nothing that can, say, access the filesystem)
					foreach (var methodName in delegateTypeTable.SelectMany(dt => dt.Value.methodInfoList)
						.Select(mi => mi.method).Distinct()
						.Select(m => m.DeclaringType + "." + m.Name).OrderBy(s => s))
						reports.DelegateMethods.WriteLine(methodName);
				}

				// Attach delegate serializer method generator
				// NOTE: Doing this after type discovery, which automatically ignores delegates anyway, but before IL generation, which requires the methods to call
				// NOTE: Generated delegate serializers come *after* custom methods, because the user may specify custom serialization for any given delegate
				customMethods = SerializationMethodProviders.Combine(customMethods,
					DelegateSerialization.CreateSerializationMethodProviders());
			}

			#endregion


			#region Module Table

			var moduleTable = td.Assemblies.Where(a => a.IsOurAssembly(baseLocation)).SelectMany(a => a.GetModules())
				.NetworkOrder(module => module.Name).ToList();

			#endregion


			#region Report Final Log Info

			if (reports != null)
			{
				reports.Log.WriteLine();
				reports.Log.WriteLine();

				var outsideTypes = td.valueTypes.Concat(td.referenceTypes)
					.Select(t =>
						(t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>)
							? t.GetGenericArguments()[0]
							: t))
					.Where(t => !customMethods.HasTypeSerializer(t))
					.Where(t => !t.Assembly.IsOurAssembly(baseLocation));

				reports.Log.WriteLine("Types outside of \"" + baseLocation + "\" without custom serializers (" +
				                      outsideTypes.Count() + ")");
				foreach (var type in outsideTypes)
					reports.Log.WriteLine("  " + type);
				reports.Log.WriteLine();


				if (delegateDiscoveryResult != null)
				{
					var openConstructedDelegates = delegateDiscoveryResult.Where(d =>
						d.delegateType.ContainsGenericParameters || d.delegateMethod.ContainsGenericParameters);

					reports.Log.WriteLine("Open constructed delegates (" + openConstructedDelegates.Count() + ")");
					foreach (var d in openConstructedDelegates)
						reports.Log.WriteLine("  " + d.delegateType + " -> " + d.delegateMethod + " -> " +
						                      (d.targetType != null ? d.targetType.ToString() : "(null)"));
					reports.Log.WriteLine();
				}


				reports.Log.WriteLine();
				reports.Log.WriteLine("Assemblies (" + td.Assemblies.Count() + ")");
				foreach (var a in td.Assemblies)
					reports.Log.WriteLine("  " + a);
				reports.Log.WriteLine();


				reports.Log.WriteLine();
				reports.Log.WriteLine("Module Table (" + moduleTable.Count() + ")");
				foreach (var module in moduleTable)
					reports.Log.WriteLine("  " + module);
				reports.Log.WriteLine();


				var notInInitialAssemblies = td.Assemblies.Where(a => a.IsOurAssembly(baseLocation))
					.Where(a => !assemblies.Contains(a));
				if (notInInitialAssemblies.Count() > 0)
				{
					reports.Error.WriteLine("WARNING: Visited assemblies in \"" + baseLocation +
					                        "\" that were not in initial list of assemblies (see log for suggested command line)");
					reports.Error.WriteLine(
						"  (It is possible that custom serialize methods, delegate instantiations, and derived types in these assemblies were missed)");
					foreach (var a in notInInitialAssemblies)
						reports.Error.WriteLine("  " + a);
					reports.Error.WriteLine();
				}


				reports.Log.WriteLine();
				reports.Log.WriteLine("Suggested initial assemblies for command line:");
				foreach (var a in td.Assemblies.Where(a => a.IsOurAssembly(baseLocation)))
					reports.Log.Write("\"" + a.Location + "\" ");
				reports.Log.WriteLine();
				reports.Log.WriteLine();


				// Final Counts:
				if (delegateDiscoveryResult != null)
				{
					reports.Log.WriteLine();
					reports.Log.WriteLine("Delegate Usage Count = " + delegateDiscoveryResult.Count);
					reports.Log.WriteLine("Distinct Delegate Method Count = " +
					                      delegateDiscoveryResult.Select(du => du.delegateMethod).Distinct().Count());
					reports.Log.WriteLine("Distinct Delegate Target Type Count = " +
					                      delegateDiscoveryResult.Select(du => du.targetType).Distinct().Count());
				}

				reports.Log.WriteLine();
				reports.Log.WriteLine("Total Serializable Type Count = " +
				                      (td.referenceTypes.Count + td.valueTypes.Count));
				reports.Log.WriteLine();

				reports.Log.WriteLine();
			}

			#endregion


			#region Generate Assembly

			var serializerMethodGenerator = new SerializerMethodGenerator(tc, customMethods, allDelegateTargetTypes);
			GeneratorResult generatorResult;


			if (createAssembly)
			{
				if (reports != null)
					reports.Log.WriteLine("Generating assembly...");

#if NET40 || NET45 || NET462
				var an = new AssemblyName(outputAssemblyName);
				var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Save);
				var moduleBuilder = assemblyBuilder.DefineDynamicModule(outputAssemblyName + ".dll");
				var methodCreatorCreator = new MethodBuilderCreatorCreator(moduleBuilder, "GenSerialize");
				generatorResult = serializerMethodGenerator.Generate(methodCreatorCreator);
				methodCreatorCreator.Finish();
				assemblyBuilder.Save(outputAssemblyName + ".dll");
#else
// https://github.com/dotnet/roslyn/issues/10881
// https://github.com/dotnet/corert/tree/master/src/ILVerify
				throw new NotSupportedException("Cannot convert dynamic assembly to disk bytes in Roslyn without parsing a syntax tree!");

				AssemblyName an = new AssemblyName(outputAssemblyName);
	            AssemblyBuilder assemblyBuilder =
 AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.RunAndCollect);
	            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(outputAssemblyName + ".dll");
	            var methodCreatorCreator = new MethodBuilderCreatorCreator(moduleBuilder, "GenSerialize");
	            generatorResult = serializerMethodGenerator.Generate(methodCreatorCreator);
	            methodCreatorCreator.Finish();

	            Assembly assembly = moduleBuilder.Assembly;
				var compilation = CSharpCompilation.Create(outputAssemblyName);
#endif
			}
			else
			{
				if (reports != null)
					reports.Log.WriteLine("Generating dynamic methods...");

				var methodCreatorCreator = new DynamicMethodCreatorCreator();
				generatorResult = serializerMethodGenerator.Generate(methodCreatorCreator);
			}

			if (reports != null)
			{
				reports.Log.WriteLine();
				reports.Log.WriteLine("Done!");
			}

			#endregion

			generatorResult.delegateTypeTable = delegateTypeTable;
			generatorResult.moduleTable = moduleTable;
			return generatorResult;
		}
	}
}