using System;
using System.Threading;
using System.Diagnostics;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.CoreAudioApi;
using NAudio.Extras;
using NAudio.Vorbis;
using NAudio.CoreAudioApi.Interfaces;

namespace MinorShift.Emuera.Runtime.Utils
{
	internal class AudioDeviceTracker : IMMNotificationClient
	{
		private readonly SynchronizationContext syncContext;
		private MMDeviceEnumerator enumerator;
		public MMDevice Device { get; private set; }
		public event EventHandler<MMDevice> DefaultDeviceChanged;

		public AudioDeviceTracker()
		{
			if (SynchronizationContext.Current == null)
				throw new Exception("SynchronizationContext.Current is null");

			syncContext = SynchronizationContext.Current;
			enumerator = new MMDeviceEnumerator();

			if (enumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Console))
				Device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
			else
				Device = null;

			enumerator.RegisterEndpointNotificationCallback(this);
		}

		~AudioDeviceTracker()
		{
			enumerator.UnregisterEndpointNotificationCallback(this);
		}

		public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
		{
			if (flow != DataFlow.Render || role != Role.Console)
				return;

			syncContext.Post(DispatchDefaultDeviceChanged, defaultDeviceId);
		}

		private void DispatchDefaultDeviceChanged(object defaultDeviceId)
		{
			if (Device?.ID != (string)defaultDeviceId)
			{
				if (defaultDeviceId == null)
					Device = null;
				else
					Device = enumerator.GetDevice((string)defaultDeviceId);

				var handler = DefaultDeviceChanged;
				if (handler != null)
				{
					handler(this, Device);
				}
			}
		}

		public void OnDeviceStateChanged(string deviceId, DeviceState newState) { }
		public void OnDeviceAdded(string pwstrDeviceId) { }
		public void OnDeviceRemoved(string deviceId) { }
		public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }
	}

	internal class DummyOut : IWavePlayer
	{
		private readonly SynchronizationContext syncContext;
		private volatile PlaybackState playbackState = PlaybackState.Stopped;
		private Thread playThread;
		private ISampleProvider sampleProvider;
		private int latency;
		public float Volume { get; set; }
		public PlaybackState PlaybackState { get => playbackState; }
		public WaveFormat OutputWaveFormat { get; }
		public event EventHandler<StoppedEventArgs> PlaybackStopped;

		public DummyOut(int latency)
		{
			syncContext = SynchronizationContext.Current;
			this.latency = latency;
		}

		public void Play()
		{
			if (playbackState != PlaybackState.Playing)
			{
				if (playbackState == PlaybackState.Stopped)
				{
					playThread = new Thread(PlayThread);
					playThread.IsBackground = true;
					playbackState = PlaybackState.Playing;
					playThread.Start();
				}
				else
				{
					playbackState = PlaybackState.Playing;
				}
			}
		}

		public void Stop()
		{
			if (playbackState != PlaybackState.Stopped)
			{
				playbackState = PlaybackState.Stopped;
				playThread.Join();
				playThread = null;
			}
		}
		public void Pause()
		{
			if (playbackState == PlaybackState.Playing)
				playbackState = PlaybackState.Paused;
		}
		public void Init(IWaveProvider waveProvider)
		{
			sampleProvider = waveProvider.ToSampleProvider();
		}
		public void Dispose()
		{
			Stop();
		}

		private void PlayThread()
		{
			Exception exception = null;
			try
			{
				Stopwatch stopwatch = Stopwatch.StartNew();
				TimeSpan prevTime = TimeSpan.Zero;
				var format = sampleProvider.WaveFormat;
				float[] buffer = new float[format.SampleRate * format.Channels];
				int samplesRemaining = 0;

				while (playbackState != PlaybackState.Stopped)
				{
					Thread.Sleep(latency);
					if (playbackState == PlaybackState.Playing)
					{
						if (!stopwatch.IsRunning)
						{
							stopwatch.Start();
							continue;
						}

						TimeSpan now = stopwatch.Elapsed;
						TimeSpan delta = now - prevTime;
						prevTime = now;

						samplesRemaining += (int)(delta.TotalSeconds * format.SampleRate * format.Channels);
						while (samplesRemaining > format.Channels)
						{
							int count = Math.Min(buffer.Length, samplesRemaining);
							count -= count % format.Channels;
							int samplesRead = sampleProvider.Read(buffer, 0, count);
							samplesRemaining -= samplesRead;
							if (samplesRead == 0)
							{
								playbackState = PlaybackState.Stopped;
								break;
							}
						}
					}
					else if (playbackState == PlaybackState.Paused)
					{
						if (stopwatch.IsRunning)
						{
							stopwatch.Reset();
							prevTime = TimeSpan.Zero;
						}
					}
				}
			}
			catch (Exception e)
			{
				exception = e;
			}
			finally
			{
				var handler = PlaybackStopped;
				if (handler != null)
				{
					if (syncContext == null)
						handler(this, new StoppedEventArgs(exception));
					else
						syncContext.Post(state => handler(this, new StoppedEventArgs(exception)), null);
				}
			}
		}
	}

	internal class RepeatStream : WaveStream
	{
		readonly WaveStream sourceStream;
		readonly int total_count;
		int remaining_count;

		public RepeatStream(WaveStream source, int count)
		{
			sourceStream = source;
			total_count = count;
			remaining_count = count;
		}

		public override WaveFormat WaveFormat
		{
			get { return sourceStream.WaveFormat; }
		}

		public override long Length
		{
			get { return sourceStream.Length * total_count; }
		}

		public override long Position
		{
			get
			{
				return (total_count - remaining_count) * sourceStream.Length + sourceStream.Position;
			}
			set
			{
				remaining_count = (int)(value / sourceStream.Length);
				sourceStream.Position = value % sourceStream.Length;
			}
		}

		public override bool HasData(int count)
		{
			return sourceStream.Position < sourceStream.Length;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			int total_read = 0;
			while (total_read < count)
			{
				int remaining = count - total_read;
				int read = sourceStream.Read(buffer, offset + total_read, remaining);
				if (read < remaining || sourceStream.Position >= sourceStream.Length)
				{
					if (remaining_count > 1)
					{
						remaining_count--;
						sourceStream.Position = 0;
					}
					else
					{
						return total_read + read;
					}
				}
				total_read += read;
			}
			return total_read;
		}

		protected override void Dispose(bool disposing)
		{
			sourceStream.Dispose();
			base.Dispose(disposing);
		}
	}

	internal static class SoundMixer
	{
		private static bool initialized = false;
		public static bool Initialized { get => initialized; }
		public const int SampleRate = 44100;
		private static AudioDeviceTracker deviceTracker;
		private static IWavePlayer output;
		private static MixingSampleProvider mixer;

		public static void Initialize()
		{
			if (initialized)
				return;

			mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 2));
			mixer.ReadFully = true;
			mixer.MixerInputEnded += SoundEnded;

			deviceTracker = new AudioDeviceTracker();
			InitializeOutput(deviceTracker.Device);
			deviceTracker.DefaultDeviceChanged += ChangeOutput;

			initialized = true;
		}

		private static void SoundEnded(object sender, SampleProviderEventArgs args)
		{
			((Sound)(args.SampleProvider)).Playing = false;
		}

		private static void InitializeOutput(MMDevice device)
		{
			if (SynchronizationContext.Current == null)
				throw new Exception("SynchronizationContext.Current is null");

			if (device != null)
				output = new WasapiOut(device, AudioClientShareMode.Shared, true, 50);
			else
				output = new DummyOut(200);

			output.Init(mixer);
			output.Play();
		}

		private static void ChangeOutput(object sender, MMDevice device)
		{
			if (output != null)
				output.Dispose();

			InitializeOutput(device);
		}

		public static void PlaySound(Sound sound)
		{
			sound.Playing = true;
			mixer.AddMixerInput(sound);
		}

		public static void StopSound(Sound sound)
		{
			sound.Playing = false;
			mixer.RemoveMixerInput(sound);
		}
	}

	internal class Sound : ISampleProvider
	{
		private float volume = 1.0f;

		private WaveStream stream;
		private VolumeSampleProvider volumeProvider;
		public volatile bool Playing = false;
		public WaveFormat WaveFormat { get => volumeProvider.WaveFormat; }

		public int Read(float[] buffer, int offset, int count)
		{
			return volumeProvider.Read(buffer, offset, count);
		}

		public void play(string filename, int repeat = 1)
		{
			if (!SoundMixer.Initialized)
				SoundMixer.Initialize();

			stop();

			if (filename.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
			{
				stream = new WaveFileReader(filename);
			}
			else if (filename.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
			{
				stream = new VorbisWaveReader(filename);
			}
			else
			{
				// NOTE: MediaFoundationReader currently seems to only support 16 bit audio files on wine
				var settings = new MediaFoundationReader.MediaFoundationReaderSettings();
				settings.RequestFloatOutput = true;
				stream = new MediaFoundationReader(filename, settings);
			}

			WaveStream _stream = stream;
			// LoopStream / RepeatStream might cause issues because they seek (see comment in stop method below)
			if (repeat == -1)
				_stream = new LoopStream(_stream);
			else if (repeat > 1)
				_stream = new RepeatStream(_stream, repeat);

			// MediaFoundationResampler is faster and possibly higher quality than WdlResamplingSampleProvider but currently doesn't seem to work on wine
			// var resampler = new MediaFoundationResampler(_stream, WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2));
			// volumeProvider = new VolumeSampleProvider(resampler.ToSampleProvider());

			ISampleProvider sampleProvider = _stream.ToSampleProvider();
			if (sampleProvider.WaveFormat.SampleRate != SoundMixer.SampleRate)
				sampleProvider = new WdlResamplingSampleProvider(sampleProvider, SoundMixer.SampleRate);
			if (sampleProvider.WaveFormat.Channels == 1)
				sampleProvider = sampleProvider.ToStereo();
			volumeProvider = new VolumeSampleProvider(sampleProvider);

			volumeProvider.Volume = volume;

			SoundMixer.PlaySound(this);
		}

		public void stop()
		{
			if (SoundMixer.Initialized)
				SoundMixer.StopSound(this);

			// don't try to reuse the stream because repositioning a MediaFoundationReader to the beginning sometimes causes WasapiOut to hang when the stream is next read (observed with a 48khz 24 bit FLAC file)
			if (stream != null)
			{
				stream.Dispose();
				stream = null;
			}
		}

		public void close()
		{
			stop();
			volumeProvider = null;
		}

		public bool isPlaying()
		{
			return Playing;
		}

		public void setVolume(int volume)
		{
			this.volume = Math.Clamp(volume, 0, 100) / 100.0f;
			if (volumeProvider != null)
				volumeProvider.Volume = this.volume;
		}
	}
}
