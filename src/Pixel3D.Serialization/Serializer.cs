// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Reflection;
using System.Threading;
using Pixel3D.Serialization.Generator;
using Pixel3D.Serialization.Static;

namespace Pixel3D.Serialization
{
	public static class Serializer
	{
		public static GeneratorResult GenerateDynamicMethods(Assembly[] subjectAssemblies, Type[] suplementalRootTypes,
			GeneratorReports reports)
		{
			return GeneratorSequence.Run(subjectAssemblies, suplementalRootTypes, reports, false, null);
		}

		public static GeneratorResult GenerateAssembly(Assembly[] subjectAssemblies, Type[] suplementalRootTypes,
			GeneratorReports reports, string outputAssemblyName)
		{
			return GeneratorSequence.Run(subjectAssemblies, suplementalRootTypes, reports, true, outputAssemblyName);
		}

		public static void Reset()
		{
			staticInitializeStarted = false;
			staticInitializeFinished = false;
			staticInitializeThread = null;
		}

		#region Static Serializer

		private static readonly object staticLockObject = new object();

		private static bool staticInitializeStarted;
		private static bool staticInitializeFinished;
		private static Thread staticInitializeThread;

		public static void BeginStaticInitialize(Assembly[] subjectAssemblies, Type[] supplementalRootTypes,
			bool generateReport)
		{
			lock (staticLockObject)
			{
				if (staticInitializeStarted)
					throw new InvalidOperationException("Static initialization was already started");
				staticInitializeStarted = true;

				staticInitializeThread = new Thread(() =>
				{
					using (var reports = generateReport ? new GeneratorReports("Serializer Generation Report") : null)
					{
						var generatorResult = GenerateDynamicMethods(subjectAssemblies, supplementalRootTypes, reports);
						_staticMethodLookup = generatorResult.serializationMethods;
						StaticDispatchTable.serializeDispatchTable = generatorResult.serializeDispatchTable;
						StaticDispatchTable.deserializeDispatchDelegate = generatorResult.deserializeDispatch;
						StaticModuleTable.SetModuleTable(generatorResult.moduleTable);
						StaticDelegateTable.delegateTypeTable = generatorResult.delegateTypeTable;
					}
				});
				staticInitializeThread.Name = "Serializer Gen";
				staticInitializeThread.Start();
			}
		}

		public static void EndStaticInitialize()
		{
			lock (staticLockObject)
			{
				if (!staticInitializeStarted)
					throw new InvalidOperationException("Static initialization was not started");
				if (staticInitializeFinished)
					return; // Already done :)

				staticInitializeThread.Join(); // Memory barrier
				staticInitializeThread = null;

				staticInitializeFinished = true;
			}
		}


		private static SerializationMethodProviders
			_staticMethodLookup; // <- This field is written by the generator thread

		internal static SerializationMethodProviders StaticMethodLookup
		{
			get
			{
				EndStaticInitialize(); // Force the intiailize thread to finish
				return _staticMethodLookup;
			}
		}

		#endregion
	}
}