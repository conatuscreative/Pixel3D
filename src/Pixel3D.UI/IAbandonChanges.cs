namespace Pixel3D.UI
{
	public interface IAbandonChanges
	{
		bool IsDirty { get; }
		void AbandonChanges();
		void PreserveChanges();
	}
}
