using System.Collections.Generic;

namespace Pixel3D.AssetManagement
{
	/// <summary>Allows management of assets that reference other assets</summary>
	public interface IHasReferencedAssets
	{
		/// <summary>Get a list of all assets referenced by this asset</summary>
		IEnumerable<object> GetReferencedAssets();

		/// <summary>Replace all instances of a given asset with another asset</summary>
		void ReplaceAsset(object search, object replace);
	}
}