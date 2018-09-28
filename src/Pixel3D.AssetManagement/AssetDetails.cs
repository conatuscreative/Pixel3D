// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
namespace Pixel3D.AssetManagement
{
	public class AssetDetails
	{
		public string FriendlyName { get; set; }
		public AssetClassification Classification { get; set; }
		public string Path { get; set; }
		public object Asset { get; set; }
	}
}