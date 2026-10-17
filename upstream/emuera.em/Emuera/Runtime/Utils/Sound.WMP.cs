using WMPLib;

namespace MinorShift.Emuera.Runtime.Utils;

internal class Sound
{
	private WindowsMediaPlayer player = new();
	public void play(string filename, int repeat = 1)
	{
		player.URL = filename;
		if (repeat == -1)
		{
			player.settings.playCount = 1;
			player.settings.setMode("loop", true);
		}
		else
		{
			player.settings.setMode("loop", false);
			player.settings.playCount = repeat;
		}
		player.controls.play();
	}

	public void stop()
	{
		player.controls.stop();
	}

	public void close()
	{
		player.close();
	}

	public bool isPlaying()
	{
		switch (player.playState)
		{
			case WMPPlayState.wmppsUndefined: return false;
			case WMPPlayState.wmppsStopped: return false;
			case WMPPlayState.wmppsPlaying: return true;
			case WMPPlayState.wmppsPaused: return false;
			case WMPPlayState.wmppsScanForward: return false;
			case WMPPlayState.wmppsScanReverse: return false;
			case WMPPlayState.wmppsBuffering: return true;
			case WMPPlayState.wmppsWaiting: return true;
			case WMPPlayState.wmppsMediaEnded: return false;
			case WMPPlayState.wmppsTransitioning: return true;
			case WMPPlayState.wmppsReady: return false;
			case WMPPlayState.wmppsReconnecting: return true;
			default: return false;
		}
	}

	public void setVolume(int volume)
	{
		player.settings.volume = volume;
	}
}
