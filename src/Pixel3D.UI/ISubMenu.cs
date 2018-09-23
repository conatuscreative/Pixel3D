namespace Pixel3D.UI
{
	public interface ISubMenu
	{
		bool IsActive { get; }
		void ExitToParentMenu(IReadOnlyContext updateContext);
	}
}