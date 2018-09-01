namespace Pixel3D.Audio
{
    public static class AudioDevice
    {
	    private static bool? _available;

	    public static bool Available
	    {
		    get
		    {
			    if (_available.HasValue)
				    return _available.Value;
			    _available = audioDeviceCheck?.Invoke();
			    return _available.GetValueOrDefault();
		    }
	    }

	    public static IsAudioDeviceAvailable audioDeviceCheck;
    }
}
