using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pixel3D.UI
{
	public interface IAbandonChanges
	{
		bool IsDirty { get; }
		void AbandonChanges();
		void PreserveChanges();
	}
}
