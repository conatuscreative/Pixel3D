namespace Pixel3D.UI
{
	public interface IResetToDefaults
	{
		bool IsWaitingToReset { get; set; }
		void ResetToDefaults(IReadOnlyContext context);
	}
}