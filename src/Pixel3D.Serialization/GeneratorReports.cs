// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.IO;

namespace Pixel3D.Serialization
{
	public class GeneratorReports : IDisposable
	{
		public GeneratorReports(string directory)
		{
			Directory = directory;
			System.IO.Directory.CreateDirectory(directory);

			Log = new StreamWriter(Directory + @"\Log.txt");
			TypeDiscovery = new StreamWriter(Directory + @"\Type Discovery.txt");
			DelegateDiscovery = new StreamWriter(Directory + @"\Delegate Discovery.txt");
			DelegateDiscoveryGrouped = new StreamWriter(Directory + @"\Delegate Discovery Grouped.txt");
			DelegateClassification = new StreamWriter(Directory + @"\Delegate Classification.txt");
			DelegateMethods = new StreamWriter(Directory + @"\Delegate Methods.txt");
			TypeClassification = new StreamWriter(Directory + @"\Type Classification.txt");
			CustomMethodDiscovery = new StreamWriter(Directory + @"\Custom Method Discovery.txt");
			Error = new StreamWriter(Directory + @"\Errors.txt");
		}

		public string Directory { get; set; }

		public StreamWriter Log { get; set; }
		public StreamWriter TypeDiscovery { get; set; }
		public StreamWriter DelegateDiscovery { get; set; }
		public StreamWriter DelegateDiscoveryGrouped { get; set; }
		public StreamWriter DelegateClassification { get; set; }
		public StreamWriter DelegateMethods { get; set; }
		public StreamWriter TypeClassification { get; set; }
		public StreamWriter CustomMethodDiscovery { get; set; }
		public StreamWriter Error { get; set; }

		public void Dispose()
		{
			Log.Dispose();
			TypeDiscovery.Dispose();
			DelegateDiscovery.Dispose();
			DelegateDiscoveryGrouped.Dispose();
			DelegateClassification.Dispose();
			DelegateMethods.Dispose();
			TypeClassification.Dispose();
			CustomMethodDiscovery.Dispose();
			Error.Dispose();
		}
	}
}