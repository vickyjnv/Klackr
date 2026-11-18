using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Klackr.Models;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.MediaFoundation;

namespace Klackr.Services
{
    public class CachedSound
    {
        public float[] AudioData { get; }
        public WaveFormat WaveFormat { get; }

        public CachedSound(string audioFileName)
        {
            try
            {
                // Use MediaFoundationReader for better MP3 support on Windows 8+
                using var mediaFoundationReader = new MediaFoundationReader(audioFileName);
                WaveFormat = mediaFoundationReader.WaveFormat;

                // Read all audio data as bytes and convert to float
                var byteBuffer = new byte[mediaFoundationReader.WaveFormat.AverageBytesPerSecond];
                var floatBuffer = new List<float>();

                int bytesRead;
                while ((bytesRead = mediaFoundationReader.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
                {
                    // Convert bytes to float samples based on WaveFormat
                    var samples = BytesToFloatSamples(byteBuffer, bytesRead, mediaFoundationReader.WaveFormat);
                    floatBuffer.AddRange(samples);
                }

                AudioData = floatBuffer.ToArray();
                System.Diagnostics.Debug.WriteLine($"Loaded sound: {audioFileName} ({WaveFormat.SampleRate}Hz, {WaveFormat.Channels}ch, {AudioData.Length} samples)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MediaFoundationReader failed for {audioFileName}: {ex.Message}. Trying AudioFileReader...");
                // Fallback to AudioFileReader
                using var audioFileReader = new AudioFileReader(audioFileName);
                WaveFormat = audioFileReader.WaveFormat;
                var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
                var readBuffer = new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
                int samplesRead;
                while ((samplesRead = audioFileReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    wholeFile.AddRange(readBuffer.Take(samplesRead));
                }
                AudioData = wholeFile.ToArray();
                System.Diagnostics.Debug.WriteLine($"Loaded sound (fallback): {audioFileName} ({WaveFormat.SampleRate}Hz, {WaveFormat.Channels}ch, {AudioData.Length} samples)");
            }
        }

        private static float[] BytesToFloatSamples(byte[] bytes, int bytesRead, WaveFormat waveFormat)
        {
            var samples = new List<float>();
            int bytesPerSample = waveFormat.BitsPerSample / 8;
            int channels = waveFormat.Channels;

            for (int i = 0; i < bytesRead; i += bytesPerSample * channels)
            {
                if (i + bytesPerSample <= bytesRead)
                {
                    // For IEEE float format (MediaFoundation typically outputs this)
                    if (waveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                    {
                        float sample = BitConverter.ToSingle(bytes, i);
                        samples.Add(sample);
                    }
                    else if (waveFormat.BitsPerSample == 16)
                    {
                        short sample = BitConverter.ToInt16(bytes, i);
                        samples.Add(sample / 32768f);
                    }
                    else if (waveFormat.BitsPerSample == 24)
                    {
                        int sample = bytes[i] | (bytes[i + 1] << 8) | (bytes[i + 2] << 16);
                        if ((sample & 0x800000) != 0) sample |= unchecked((int)0xFF000000); // Sign extend
                        samples.Add(sample / 8388608f);
                    }
                    else if (waveFormat.BitsPerSample == 32)
                    {
                        int sample = BitConverter.ToInt32(bytes, i);
                        samples.Add(sample / 2147483648f);
                    }
                }
            }
            return samples.ToArray();
        }
    }

    public class CachedSoundSampleProvider : ISampleProvider
    {
        private readonly CachedSound _cachedSound;
        private readonly float _volume;
        private long _position;

        public CachedSoundSampleProvider(CachedSound cachedSound, float volume)
        {
            _cachedSound = cachedSound;
            _volume = Math.Clamp(volume, 0f, 1f);
        }

        public WaveFormat WaveFormat => _cachedSound.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            var availableSamples = _cachedSound.AudioData.Length - _position;
            var samplesToCopy = (int)Math.Min(availableSamples, count);
            for (int i = 0; i < samplesToCopy; i++)
            {
                buffer[offset + i] = _cachedSound.AudioData[_position + i] * _volume;
            }
            _position += samplesToCopy;
            return samplesToCopy;
        }
    }

    public class SoundEngine : IDisposable
    {
        private IWavePlayer? _outputDevice;
        private MixingSampleProvider? _mixer;
        private readonly Dictionary<string, List<CachedSound>> _soundBuffers = new();
        private readonly Dictionary<string, List<CachedSound>> _mouseSoundBuffers = new();
        private readonly Random _random = new();
        private readonly object _lock = new();

        public SoundPackType CurrentPack { get; private set; } = SoundPackType.CherryMXBrown;
        public MouseSoundPackType CurrentMousePack { get; private set; } = MouseSoundPackType.ClassicOmron;
        public float Volume { get; set; } = 0.85f;
        public float MouseVolume { get; set; } = 0.85f;
        public bool IsEnabled { get; set; } = true;
        public bool IsMouseEnabled { get; set; } = true;

        static SoundEngine()
        {
            // Initialize MediaFoundation for MP3 support
            try
            {
                MediaFoundationApi.Startup();
                System.Diagnostics.Debug.WriteLine("MediaFoundation initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MediaFoundation startup warning: {ex.Message}");
            }
        }

        public SoundEngine()
        {
            InitializeAudioDevice();
            LoadSoundPack(CurrentPack);
            LoadMouseSoundPack(CurrentMousePack);
        }

        private void InitializeAudioDevice()
        {
            try
            {
                // Standard 44.1kHz stereo mixer for seamless blending
                var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
                _mixer = new MixingSampleProvider(format) { ReadFully = true };

                try
                {
                    // WASAPI shared mode with 20ms buffer for ultra-low latency
                    var wasapi = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 20);
                    wasapi.Init(_mixer);
                    wasapi.Play();
                    _outputDevice = wasapi;
                }
                catch
                {
                    // Fallback to WaveOutEvent
                    var waveOut = new WaveOutEvent { DesiredLatency = 40, NumberOfBuffers = 3 };
                    waveOut.Init(_mixer);
                    waveOut.Play();
                    _outputDevice = waveOut;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Audio device init error: {ex.Message}");
            }
        }

        public void LoadSoundPack(SoundPackType pack)
        {
            lock (_lock)
            {
                CurrentPack = pack;
                _soundBuffers.Clear();

                var soundsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");
                if (!Directory.Exists(soundsDir)) return;

                var prefix = pack switch
                {
                    SoundPackType.CherryMXBlue => "cherry-mx-blue",
                    SoundPackType.CherryMXRed => "cherry-mx-red",
                    SoundPackType.CherryMXBlack => "cherry-mx-black",
                    SoundPackType.ToprePurple => "topre-purple",
                    SoundPackType.EverglideOreo => "everglide-oreo",
                    SoundPackType.HolyPanda => "holy-panda",
                    SoundPackType.KailhBoxWhite => "kailh-box-white",
                    SoundPackType.NKCream => "nk-cream",
                    SoundPackType.TealiosV2 => "tealios-v2",
                    SoundPackType.IBMBucklingSpring => "ibm-buckling-spring",
                    SoundPackType.SteelSeriesApexPro => "steelseries-apex-pro",
                    _ => "cherry-mx-brown"
                };

                foreach (KeyType keyType in Enum.GetValues(typeof(KeyType)))
                {
                    foreach (KeyEvent keyEvent in Enum.GetValues(typeof(KeyEvent)))
                    {
                        var key = GetBufferKey(keyType, keyEvent);
                        var soundFiles = ResolveSoundFiles(soundsDir, prefix, keyType, keyEvent);
                        var cachedList = new List<CachedSound>();

                        foreach (var file in soundFiles)
                        {
                            try
                            {
                                if (File.Exists(file))
                                {
                                    cachedList.Add(new CachedSound(file));
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error caching sound {file}: {ex.Message}");
                            }
                        }

                        if (cachedList.Count > 0)
                        {
                            _soundBuffers[key] = cachedList;
                        }
                    }
                }
            }
        }

        public void LoadMouseSoundPack(MouseSoundPackType pack)
        {
            lock (_lock)
            {
                CurrentMousePack = pack;
                _mouseSoundBuffers.Clear();

                var soundsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");
                if (!Directory.Exists(soundsDir)) return;

                var prefix = pack switch
                {
                    MouseSoundPackType.LogitechSuperlight => "mouse-superlight",
                    MouseSoundPackType.RazerOptical => "mouse-razer",
                    MouseSoundPackType.GloriousModelO => "mouse-glorious",
                    MouseSoundPackType.SilentOffice => "mouse-silent",
                    MouseSoundPackType.IntelliMouse => "mouse-intellimouse",
                    MouseSoundPackType.LogitechG203 => "mouse-g203",
                    MouseSoundPackType.BloodyOptical => "mouse-bloody",
                    MouseSoundPackType.RaptureVenom => "mouse-rapture",
                    MouseSoundPackType.TrustGXT => "mouse-trust",
                    MouseSoundPackType.AppleMagic => "mouse-apple",
                    MouseSoundPackType.KailhGM8 => "mouse-kailh-gm",
                    _ => "mouse-omron"
                };

                foreach (MouseButton button in Enum.GetValues(typeof(MouseButton)))
                {
                    foreach (MouseEventType ev in Enum.GetValues(typeof(MouseEventType)))
                    {
                        var key = GetMouseBufferKey(button, ev);
                        var btnStr = button.ToString().ToLower();
                        var evStr = ev == MouseEventType.Press ? "press" : "release";
                        var filePath = Path.Combine(soundsDir, $"{prefix}_{btnStr}_{evStr}.mp3");

                        // Fall back to left button
                        if (!File.Exists(filePath))
                        {
                            filePath = Path.Combine(soundsDir, $"{prefix}_left_{evStr}.mp3");
                        }

                        if (File.Exists(filePath))
                        {
                            try
                            {
                                _mouseSoundBuffers[key] = new List<CachedSound> { new CachedSound(filePath) };
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error caching mouse sound {filePath}: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        private List<string> ResolveSoundFiles(string dir, string prefix, KeyType keyType, KeyEvent keyEvent)
        {
            var files = new List<string>();
            var eventStr = keyEvent == KeyEvent.Press ? "press" : "release";

            if (keyType == KeyType.Space)
            {
                var p = Path.Combine(dir, $"{prefix}_space_{eventStr}.mp3");
                if (File.Exists(p)) files.Add(p);
            }
            else if (keyType == KeyType.Enter)
            {
                var p = Path.Combine(dir, $"{prefix}_enter_{eventStr}.mp3");
                if (File.Exists(p)) files.Add(p);
            }
            else if (keyType == KeyType.Backspace)
            {
                var p = Path.Combine(dir, $"{prefix}_backspace_{eventStr}.mp3");
                if (File.Exists(p)) files.Add(p);
            }

            if (files.Count == 0 || !files.Any(File.Exists))
            {
                files.Clear();
                if (keyEvent == KeyEvent.Press)
                {
                    for (int i = 1; i <= 5; i++)
                    {
                        var p = Path.Combine(dir, $"{prefix}_regular_press_{i}.mp3");
                        if (File.Exists(p)) files.Add(p);
                    }
                }
                else
                {
                    var p = Path.Combine(dir, $"{prefix}_regular_release.mp3");
                    if (File.Exists(p)) files.Add(p);
                }
            }

            return files;
        }

        public void PlaySound(KeyType keyType, KeyEvent keyEvent)
        {
            if (!IsEnabled || _mixer == null) return;

            CachedSound? soundToPlay = null;
            float volumeVariation;

            try
            {
                lock (_lock)
                {
                    var key = GetBufferKey(keyType, keyEvent);
                    if (!_soundBuffers.TryGetValue(key, out var list) || list.Count == 0)
                    {
                        // Fallback to regular key sound
                        var fallbackKey = GetBufferKey(KeyType.Regular, keyEvent);
                        if (_soundBuffers.TryGetValue(fallbackKey, out var fallbackList) && fallbackList.Count > 0)
                        {
                            list = fallbackList;
                        }
                    }

                    if (list != null && list.Count > 0)
                    {
                        soundToPlay = list[_random.Next(list.Count)];
                    }

                    // Random volume variation between 0.9 and 1.0 for natural typing feel
                    volumeVariation = (float)(0.90 + _random.NextDouble() * 0.10);
                }

                if (soundToPlay != null)
                {
                    var effectiveVolume = Volume * volumeVariation;
                    var sampleProvider = new CachedSoundSampleProvider(soundToPlay, effectiveVolume);
                    _mixer.AddMixerInput(sampleProvider);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaySound error: {ex.Message}");
            }
        }

        public void PlayMouseSound(MouseButton button, MouseEventType eventType)
        {
            if (!IsEnabled || !IsMouseEnabled || _mixer == null) return;

            CachedSound? soundToPlay = null;
            float volumeVariation;

            try
            {
                lock (_lock)
                {
                    var key = GetMouseBufferKey(button, eventType);
                    if (!_mouseSoundBuffers.TryGetValue(key, out var list) || list.Count == 0)
                    {
                        var fallbackKey = GetMouseBufferKey(MouseButton.Left, eventType);
                        if (_mouseSoundBuffers.TryGetValue(fallbackKey, out var fallbackList))
                        {
                            list = fallbackList;
                        }
                    }

                    if (list != null && list.Count > 0)
                    {
                        soundToPlay = list[_random.Next(list.Count)];
                    }

                    volumeVariation = (float)(0.92 + _random.NextDouble() * 0.08);
                }

                if (soundToPlay != null)
                {
                    var effectiveVolume = MouseVolume * volumeVariation;
                    var sampleProvider = new CachedSoundSampleProvider(soundToPlay, effectiveVolume);
                    _mixer.AddMixerInput(sampleProvider);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlayMouseSound error: {ex.Message}");
            }
        }

        private static string GetBufferKey(KeyType keyType, KeyEvent keyEvent) =>
            $"{keyType}_{keyEvent}";

        private static string GetMouseBufferKey(MouseButton button, MouseEventType eventType) =>
            $"{button}_{eventType}";

        public void Dispose()
        {
            _outputDevice?.Stop();
            _outputDevice?.Dispose();
            _soundBuffers.Clear();
            _mouseSoundBuffers.Clear();

            // Shutdown MediaFoundation
            try
            {
                MediaFoundationApi.Shutdown();
            }
            catch { }
        }
    }
}
