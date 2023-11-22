using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MZZT.DarkForces.Showcase;

public class LevelPreview : IDisposable {
	[DllImport("user32.dll", EntryPoint = "FindWindowEx", ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
	private extern static IntPtr FindWindowEx([Optional] IntPtr hWndParent, [Optional] IntPtr hWndChildAfter, [Optional] string? lpszClass,
		[Optional] string? lpszWindow);

	private Task? task;
	/*private Process? process;

	public bool HasExited => this.process?.HasExited ?? true;*/
	public bool HasExited => this.task?.IsCompleted ?? true;

	[DllImport("UnityPlayer.dll")]
	private static extern int UnityMain(IntPtr hInstance, IntPtr hPrevInstance,
		[MarshalAs(UnmanagedType.LPWStr)] string lpCmdline, int nShowCmd);

	public async Task StartAsync(IntPtr handle) {
		this.context = SynchronizationContext.Current;

		// Need to forward WM_INPUT messages to Unity. Register to receive them.
		/*RAWINPUTDEVICE[] rawInputDevices = [
			new RAWINPUTDEVICE() {
				usUsagePage = HID_USAGE_PAGE_GENERIC,
				usUsage = HID_USAGE_GENERIC_KEYBOARD,
				dwFlags = 0,
				hwndTarget = windowHandle
			}
		];

		if (!RegisterRawInputDevices(rawInputDevices, rawInputDevices.Length, Marshal.SizeOf(typeof(RAWINPUTDEVICE)))) {
			throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to register raw input devices.");
		}*/

		/*if (this.process != null && !this.process.HasExited) {
			throw new InvalidOperationException();
		}*/

		/*#if DEBUG
				string path = @"D:\Projects\Unity\DarkForcesShowcase\Build\LevelPreviewWindows\Dark Forces Showcase.exe";
		#else
				string path = Path.Combine(Application.StartupPath, @"Level Preview\Level Preview.exe";
		#endif*/

		//this.process = Process.Start(path, $"--parentHWND {containerHandle.ToInt64()} delayed");

		string args = $"--parentHWND {handle.ToInt64()} delayed";
		this.task = this.StartUnityAsync(args);

		this.apiClient = new HttpClient() {
			BaseAddress = new Uri($"http://localhost:{this.ApiPort}")
		};

		//this.process.WaitForInputIdle();

		this.UnityWindow = FindWindowEx(handle, IntPtr.Zero, null, null);
		while (this.UnityWindow == IntPtr.Zero && !this.task.IsCompleted/*!this.process.HasExited*/) {
			await Task.Delay(25);

			this.UnityWindow = FindWindowEx(handle, IntPtr.Zero, null, null);
		}

		if (!this.task.IsCompleted/*!this.process.HasExited*/) {
			await this.StartEventListenerAsync();
		}
	}

	private async Task StartUnityAsync(string args) {
		await Task.Yield();

		try {
			_ = UnityMain(Process.GetCurrentProcess().Handle, IntPtr.Zero, args, 1);
		} catch (Exception ex) {
			MessageBox.Show(ex.ToString());
			Application.Exit();
		}
	}

	public ushort ApiPort { get; set; } = 5387;
	public ushort EventPort { get; set; } = 43567;
	public IntPtr UnityWindow { get; set; }

	private HttpClient? apiClient;
	private async Task ApiCallAsync(string name, params string[] args) {
		if (this.apiClient == null) {
			throw new InvalidOperationException();
		}

		FormUrlEncodedContent form = new FormUrlEncodedContent(args.Select((x, i) => new KeyValuePair<string, string>(i.ToString(), x)));
		using HttpResponseMessage message = await this.apiClient.PostAsync(name, form);
	}

	/*[StructLayout(LayoutKind.Sequential)]
	private struct RAWINPUTDEVICE {
		public ushort usUsagePage;
		public ushort usUsage;
		public uint dwFlags;
		public IntPtr hwndTarget;
	}
	private const ushort HID_USAGE_PAGE_GENERIC = 0x01;
	private const ushort HID_USAGE_GENERIC_MOUSE = 0x02;
	private const ushort HID_USAGE_GENERIC_KEYBOARD = 0x06;
	private const uint RID_INPUT = 0x10000003;
	private const int RIM_TYPEMOUSE = 0;
	private const int RIM_TYPEKEYBOARD = 1;

	[StructLayout(LayoutKind.Sequential)]
	private struct RAWINPUTHEADER {
		public uint dwType;
		public int dwSize;
		public IntPtr hDevice;
		public IntPtr wParam;
	}

	[Flags]
	private enum RawKeyboardFlags : ushort {
		RI_KEY_MAKE = 0,
		RI_KEY_BREAK = 1,
		RI_KEY_E0 = 2,
		RI_KEY_E1 = 4,
		RI_KEY_TERMSRV_SET_LED = 8,
		RI_KEY_TERMSRV_SHADOW = 0x10,
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct RAWKEYBOARD {
		public ushort MakeCode;
		public RawKeyboardFlags Flags;
		public ushort Reserved;
		public ushort VKey;
		public uint Message;
		public uint ExtraInformation;
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool RegisterRawInputDevices([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] RAWINPUTDEVICE[] pRawInputDevices, int uiNumDevices, int cbSize);

	[DllImport("User32.dll", SetLastError = true)]
	private static extern int GetRawInputData(IntPtr hRawInput, uint uiCommand, [MarshalAs(UnmanagedType.LPArray)] byte[] pData, ref int pcbSize, int cbSizeHeader);

	[DllImport("User32.dll", SetLastError = true)]
	static extern int GetRawInputBuffer([MarshalAs(UnmanagedType.LPArray)] byte[] pData, ref int pcbSize, int cbSizeHeader);

	private readonly byte[] rawInputBuffer = new byte[8192];
	public async Task ProcessRawInputAsync(IntPtr lParam) {
		int rawInputEventCount = 0;
		int rawInputEventsSize = 0;

		int length = this.rawInputBuffer.Length;
		int result = GetRawInputData(lParam, RID_INPUT, this.rawInputBuffer, ref length, Marshal.SizeOf<RAWINPUTHEADER>());
		if (result < 0) {
			throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get raw input data.");
		}

		this.CopyEventFromRawInputBuffer(0, Marshal.SizeOf<RAWINPUTHEADER>(), ref rawInputEventCount, ref rawInputEventsSize);

		while (true) {
			int rawInputCount = GetRawInputBuffer(this.rawInputBuffer, ref length, Marshal.SizeOf<RAWINPUTHEADER>());
			if (rawInputCount == 0) {
				break;
			} else if (rawInputCount < 0) {
				throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get raw input buffer.");
			}

			int offset = 0;
			for (int i = 0; i < rawInputCount; i++) {
				int rawInputDataOffset = (Environment.Is64BitProcess ? 2 : 1) * Marshal.SizeOf<RAWINPUTHEADER>();

				offset += this.CopyEventFromRawInputBuffer(offset, rawInputDataOffset, ref rawInputEventCount, ref rawInputEventsSize);
			}
		}

		if (!this.ready) {
			return;
		}

		DataContractJsonSerializer serializer = new(typeof(RawInputData), new DataContractJsonSerializerSettings() {
			UseSimpleDictionaryFormat = true
		});
		using MemoryStream stream = new();
		serializer.WriteObject(stream, new RawInputData() {
			HeaderIndices = this.s_RawInputHeaderIndices.Take(rawInputEventCount).ToArray(),
			DataIndices = this.s_RawInputDataIndices.Take(rawInputEventCount).ToArray(),
			Buffer = this.s_RawInputEvents.Take(rawInputEventsSize).ToArray()
		});

		await this.ApiCallAsync("RawInput", Encoding.UTF8.GetString(stream.ToArray()));
	}

	[DataContract]
	private class RawInputData {
		[DataMember]
		public int[]? HeaderIndices { get; set; }
		[DataMember]
		public int[]? DataIndices { get; set; }
		[DataMember]
		public byte[]? Buffer { get; set; }
	}

	private byte[] s_RawInputEvents = new byte[8192];
	private int[] s_RawInputHeaderIndices = new int[100];
	private int[] s_RawInputDataIndices = new int[100];
	private int CopyEventFromRawInputBuffer(int offset, int dataOffset, ref int rawInputEventCount, ref int rawInputEventsSize) {
		RAWINPUTHEADER header;

		GCHandle handle = GCHandle.Alloc(this.rawInputBuffer, GCHandleType.Pinned);
		try {
			header = Marshal.PtrToStructure<RAWINPUTHEADER>(handle.AddrOfPinnedObject() + offset);
		} finally {
			handle.Free();
		}

		if (rawInputEventsSize + header.dwSize > this.s_RawInputEvents.Length) {
			Array.Resize(ref this.s_RawInputEvents, Math.Max(rawInputEventsSize + header.dwSize, 2 * this.s_RawInputEvents.Length));
		}

		if (rawInputEventCount == this.s_RawInputHeaderIndices.Length) {
			Array.Resize(ref this.s_RawInputHeaderIndices, 2 * this.s_RawInputHeaderIndices.Length);
			Array.Resize(ref this.s_RawInputDataIndices, 2 * this.s_RawInputHeaderIndices.Length);
		}

		this.s_RawInputHeaderIndices[rawInputEventCount] = rawInputEventsSize;
		this.s_RawInputDataIndices[rawInputEventCount] = rawInputEventsSize + dataOffset;
		Array.Copy(this.rawInputBuffer, offset, this.s_RawInputEvents, rawInputEventsSize, header.dwSize);

		rawInputEventCount++;
		rawInputEventsSize += header.dwSize;
		return header.dwSize;
	}*/

	public async Task QuitAsync() {
		await this.ApiCallAsync("Quit");
	}

	public async Task CaptureMouseAsync() {
		await this.ApiCallAsync("CaptureMouse");
	}

	public async Task ReleaseMouseAsync() {
		await this.ApiCallAsync("ReleaseMouse");
	}

	public async Task ReloadDataFilesAsync() {
		await this.ApiCallAsync("ReloadDataFiles");
	}

	public async Task AddModFileAsync(string path) {
		await this.ApiCallAsync("AddModFile", path);
	}

	public async Task LoadLevelListAsync() {
		await this.ApiCallAsync("LoadLevelList");
	}

	public async Task LoadLevelAsync(int index) {
		await this.ApiCallAsync("LoadLevel", index.ToString());
	}

	public async Task ReloadLevelInPlaceAsync() {
		await this.ApiCallAsync("ReloadLevelInPlace");
	}

	public async Task InitEmptyLevelAsync(string name, int musicIndex, string paletteName) {
		await this.ApiCallAsync("InitEmptyLevel", name, musicIndex.ToString(), paletteName);
	}

	public async Task SetDarkForcesPathAsync(string path) {
		await this.ApiCallAsync("SetDarkForcesPath", path);
	}
	
	public async Task SetBackgroundAsync(float r, float g, float b) {
		await this.ApiCallAsync("SetBackground", r.ToString(), g.ToString(), b.ToString());
	}

	public async Task SetBackgroundAsync(Color value) {
		await this.ApiCallAsync("SetBackground", (value.R / 255f).ToString(), (value.G / 255f).ToString(), (value.B / 255f).ToString());
	}

	public async Task SetShowWaitBitmapAsync(bool value) {
		await this.ApiCallAsync("SetShowWaitBitmap", value.ToString());
	}

	public async Task SetExtendSkyPitAsync(float value) {
		await this.ApiCallAsync("SetExtendSkyPit", value.ToString());
	}

	public async Task SetShowSpritesAsync(bool value) {
		await this.ApiCallAsync("SetShowSprites", value.ToString());
	}

	public async Task SetShow3dosAsync(bool value) {
		await this.ApiCallAsync("SetShow3dos", value.ToString());
	}

	public async Task SetDifficultyAsync(Difficulties value) {
		await this.ApiCallAsync("SetDifficulty", ((int)value).ToString());
	}

	public async Task SetAnimateVuesAsync(bool value) {
		await this.ApiCallAsync("SetAnimateVues", value.ToString());
	}

	public async Task SetAnimate3doUpdatesAsync(bool value) {
		await this.ApiCallAsync("SetAnimate3doUpdates", value.ToString());
	}

	public async Task SetFullBrightLightingAsync(bool value) {
		await this.ApiCallAsync("SetFullBrightLighting", value.ToString());
	}

	public async Task SetBypassColormapDitheringAsync(bool value) {
		await this.ApiCallAsync("SetBypassColormapDithering", value.ToString());
	}

	public async Task SetPlayMusicAsync(bool value) {
		await this.ApiCallAsync("SetPlayMusic", value.ToString());
	}

	public async Task SetPlayFightTrackAsync(bool value) {
		await this.ApiCallAsync("SetPlayFightTrack", value.ToString());
	}

	public async Task SetVolumeAsync(float value) {
		await this.ApiCallAsync("SetVolume", value.ToString());
	}

	public async Task SetVisibleLayerAsync(int? value) {
		if (value == null) {
			await this.ApiCallAsync("SetVisibleLayer");
		} else {
			await this.ApiCallAsync("SetVisibleLayer", value.Value.ToString());
		}
	}

	public async Task SetLookSensitivityAsync(Vector2 value) {
		await this.ApiCallAsync("SetLookSensitivity", value.X.ToString(), value.Y.ToString());
	}

	public async Task SetInvertYLookAsync(bool value) {
		await this.ApiCallAsync("SetInvertYLook", value.ToString());
	}

	public async Task SetMoveSensitivityAsync(Vector3 value) {
		await this.ApiCallAsync("SetMoveSensitivity", value.X.ToString(), value.Y.ToString(), value.Z.ToString());
	}

	public async Task SetYawLimitsAsync(float min, float max) {
		await this.ApiCallAsync("SetYawLimits", min.ToString(), max.ToString());
	}

	public async Task SetRunMultiplierAsync(float value) {
		await this.ApiCallAsync("SetRunMultiplier", value.ToString());
	}

	public async Task SetZoomSensitivityAsync(float value) {
		await this.ApiCallAsync("SetZoomSensitivity", value.ToString());
	}

	public async Task SetUseOrbitCameraAsync(bool value) {
		await this.ApiCallAsync("SetUseOrbitCamera", value.ToString());
	}

	public async Task SetUseMouseCaptureAsync(bool value) {
		await this.ApiCallAsync("SetUseMouseCapture", value.ToString());
	}
	
	public async Task SetShowHudAsync(bool value) {
		await this.ApiCallAsync("SetShowHud", value.ToString());
	}

	public async Task SetHudAlignAsync(int value) {
		await this.ApiCallAsync("SetHudAlign", value.ToString());
	}

	public async Task SetHudAlignAsync(string value) {
		await this.ApiCallAsync("SetHudAlign", value);
	}

	public async Task SetHudFontSizeAsync(float value) {
		await this.ApiCallAsync("SetHudFontSize", value.ToString());
	}

	public async Task SetHudColorAsync(float r, float g, float b, float a) {
		await this.ApiCallAsync("SetHudColor", r.ToString(), g.ToString(), b.ToString(), a.ToString());
	}

	public async Task SetHudColorAsync(Color value) {
		await this.ApiCallAsync("SetHudColor", (value.R / 255f).ToString(), (value.G / 255f).ToString(), (value.B / 255f).ToString(), (value.A / 255f).ToString());
	}

	public async Task SetShowHudCoordinatesAsync(bool value) {
		await this.ApiCallAsync("SetShowHudCoordinates", value.ToString());
	}

	public async Task SetHudFpsCoordinatesAsync(string value) {
		await this.ApiCallAsync("SetHudFpsCoordinates", value);
	}

	public async Task SetHudOrbitCoordinatesAsync(string value) {
		await this.ApiCallAsync("SetHudOrbitCoordinates", value);
	}

	public async Task SetShowHudRaycastHitAsync(bool value) {
		await this.ApiCallAsync("SetShowHudRaycastHit", value.ToString());
	}

	public async Task SetHudRaycastFloorAsync(string value) {
		await this.ApiCallAsync("SetHudRaycastFloor", value);
	}

	public async Task SetHudRaycastCeilingAsync(string value) {
		await this.ApiCallAsync("SetHudRaycastCeiling", value);
	}

	public async Task SetHudRaycastWallAsync(string value) {
		await this.ApiCallAsync("SetHudRaycastWall", value);
	}

	public async Task SetHudRaycastObjectAsync(string value) {
		await this.ApiCallAsync("SetHudRaycastObject", value);
	}

	public async Task ReloadLevelGeometryAsync(LevelInfo level) {
		DataContractJsonSerializer serializer = new(typeof(LevelInfo), new DataContractJsonSerializerSettings() {
			UseSimpleDictionaryFormat = true
		});
		using MemoryStream stream = new();
		serializer.WriteObject(stream, level);
		
		await this.ApiCallAsync("ReloadLevelGeometry", Encoding.UTF8.GetString(stream.ToArray()));
	}

	public async Task SetLevelMetadataAsync(string levelFile, string musicFile, string paletteFile, Vector2 parallax) {
		await this.ApiCallAsync("SetLevelMetadata", levelFile, musicFile, paletteFile, parallax.X.ToString(), parallax.Y.ToString());
	}

	public async Task ReloadSectorAsync(int index, SectorInfo sector) {
		DataContractJsonSerializer serializer = new(typeof(SectorInfo), new DataContractJsonSerializerSettings() {
			UseSimpleDictionaryFormat = true
		});
		using MemoryStream stream = new();
		serializer.WriteObject(stream, sector);

		await this.ApiCallAsync("ReloadSector", index.ToString(), Encoding.UTF8.GetString(stream.ToArray()));
	}

	public async Task SetSectorAsync(int index, SectorInfo sector) {
		DataContractJsonSerializer serializer = new(typeof(SectorInfo), new DataContractJsonSerializerSettings() {
			UseSimpleDictionaryFormat = true
		});
		using MemoryStream stream = new();
		serializer.WriteObject(stream, sector);

		await this.ApiCallAsync("SetSector", index.ToString(), Encoding.UTF8.GetString(stream.ToArray()));
	}

	public async Task MoveSectorAsync(int index, Vector3 delta) {
		await this.ApiCallAsync("MoveSector", index.ToString(), delta.X.ToString(), delta.Y.ToString(), delta.Z.ToString());
	}

	public async Task DeleteSectorAsync(int index) {
		await this.ApiCallAsync("DeleteSector", index.ToString());
	}

	public async Task SetSectorFloorAsync(int index, HorizontalSurfaceInfo floor) {
		DataContractJsonSerializer serializer = new(typeof(HorizontalSurfaceInfo), new DataContractJsonSerializerSettings() {
			UseSimpleDictionaryFormat = true
		});
		using MemoryStream stream = new();
		serializer.WriteObject(stream, floor);

		await this.ApiCallAsync("SetSectorFloor", index.ToString(), Encoding.UTF8.GetString(stream.ToArray()));
	}

	public async Task SetSectorCeilingAsync(int index, HorizontalSurfaceInfo ceiling) {
		DataContractJsonSerializer serializer = new(typeof(HorizontalSurfaceInfo), new DataContractJsonSerializerSettings() {
			UseSimpleDictionaryFormat = true
		});
		using MemoryStream stream = new();
		serializer.WriteObject(stream, ceiling);

		await this.ApiCallAsync("SetSectorCeiling", index.ToString(), Encoding.UTF8.GetString(stream.ToArray()));
	}

	public async Task ReloadWallAsync(int sectorIndex, int wallIndex, WallInfo wall) {
		DataContractJsonSerializer serializer = new(typeof(WallInfo), new DataContractJsonSerializerSettings() {
			UseSimpleDictionaryFormat = true
		});
		using MemoryStream stream = new();
		serializer.WriteObject(stream, wall);

		await this.ApiCallAsync("ReloadWall", sectorIndex.ToString(), wallIndex.ToString(), Encoding.UTF8.GetString(stream.ToArray()));
	}

	public async Task InsertWallAsync(int sectorIndex, int wallIndex, WallInfo wall) {
		DataContractJsonSerializer serializer = new(typeof(WallInfo), new DataContractJsonSerializerSettings() {
			UseSimpleDictionaryFormat = true
		});
		using MemoryStream stream = new();
		serializer.WriteObject(stream, wall);

		await this.ApiCallAsync("InsertWall", sectorIndex.ToString(), wallIndex.ToString(), Encoding.UTF8.GetString(stream.ToArray()));
	}

	public async Task DeleteWallAsync(int sectorIndex, int wallIndex) {
		await this.ApiCallAsync("DeleteWall", sectorIndex.ToString(), wallIndex.ToString());
	}

	public async Task SetVertexAsync(int sectorIndex, int wallIndex, bool rightVertex, Vector2 value) {
		await this.ApiCallAsync("SetVertex", sectorIndex.ToString(), wallIndex.ToString(), rightVertex.ToString(), value.X.ToString(), value.Y.ToString());
	}

	public async Task ReloadLevelObjectsAsync(IEnumerable<ObjectInfo> objects) {
		DataContractJsonSerializer serializer = new(typeof(ObjectInfo[]), new DataContractJsonSerializerSettings() {
			UseSimpleDictionaryFormat = true
		});
		using MemoryStream stream = new();
		serializer.WriteObject(stream, objects.ToArray());

		await this.ApiCallAsync("ReloadLevelObjects", Encoding.UTF8.GetString(stream.ToArray()));
	}

	public async Task SetObjectAsync(int index, ObjectInfo value) {
		DataContractJsonSerializer serializer = new(typeof(ObjectInfo), new DataContractJsonSerializerSettings() {
			UseSimpleDictionaryFormat = true
		});
		using MemoryStream stream = new();
		serializer.WriteObject(stream, value);

		await this.ApiCallAsync("SetObject", index.ToString(), Encoding.UTF8.GetString(stream.ToArray()));
	}

	public async Task DeleteObjectAsync(int index) {
		await this.ApiCallAsync("DeleteObject", index.ToString());
	}

	public async Task ResetCameraAsync() {
		await this.ApiCallAsync("ResetCamera");
	}

	public async Task MoveCameraAsync(Vector3 value) {
		await this.ApiCallAsync("MoveCamera", value.X.ToString(), value.Y.ToString(), value.Z.ToString());
	}

	public async Task RotateCameraAsync(Quaternion value) {
		await this.ApiCallAsync("RotateCamera", value.W.ToString(), value.X.ToString(), value.Y.ToString(), value.Z.ToString());
	}

	public async Task RotateCameraAsync(Vector3 value) {
		await this.ApiCallAsync("RotateCameraEuler", value.X.ToString(), value.Y.ToString(), value.Z.ToString());
	}

	public async Task MoveAndRotateCameraAsync(Vector3 pos, Quaternion rot) {
		await this.ApiCallAsync("MoveAndRotateCamera", pos.X.ToString(), pos.Y.ToString(), pos.Z.ToString(),
			rot.W.ToString(), rot.X.ToString(), rot.Y.ToString(), rot.Z.ToString());
	}

	public async Task MoveAndRotateCameraAsync(Vector3 pos, Vector3 rot) {
		await this.ApiCallAsync("MoveAndRotateCameraEuler", pos.X.ToString(), pos.Y.ToString(), pos.Z.ToString(),
			rot.X.ToString(), rot.Y.ToString(), rot.Z.ToString());
	}

	public async Task PointCameraAtAsync(Vector3 value) {
		await this.ApiCallAsync("PointCameraAt", value.X.ToString(), value.Y.ToString(), value.Z.ToString());
	}

	private HttpListener? eventListener;
	private SynchronizationContext? context;
	//private bool ready = false;
	private async Task StartEventListenerAsync() {
		string url = $"http://localhost:{this.EventPort}/";
		this.eventListener = new();
		this.eventListener.Prefixes.Add(url);
		this.eventListener.Start();

		this.eventListener.BeginGetContext(new AsyncCallback(this.EventCallback), this.eventListener);

		do {
			await Task.Delay(25);

			try {
				await this.ApiCallAsync("SetEventHandler", url);
			} catch (Exception) {
				continue;
			}
			break;
		} while (true);

		//this.ready = true;
	}

	private void EventCallback(IAsyncResult ar) {
		HttpListener listener = (HttpListener)ar.AsyncState!;
		HttpListenerContext context;
		try {
			context = listener.EndGetContext(ar);
		} catch (HttpListenerException) {
			return;
		}
		if (context.Request.HttpMethod != "POST" ||
			(context.Request.HasEntityBody && context.Request.ContentType != "application/x-www-form-urlencoded")) {

			context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
		} else {
			string eventName = context.Request.Url!.AbsolutePath.Trim('/');

			Dictionary<int, string> args = new();
			if (context.Request.HasEntityBody) {
				using StreamReader reader = new(context.Request.InputStream, context.Request.ContentEncoding);
				string body = reader.ReadToEnd();
				foreach (string item in body.Split('&')) {
					int index = item.IndexOf('=');
					if (index < 0) {
						continue;
					}
					string key = WebUtility.UrlDecode(item.Substring(0, index));
					string value = WebUtility.UrlDecode(item.Substring(index + 1));
					if (!int.TryParse(key, out index)) {
						continue;
					}
					args[index] = value;
				}
			}
			List<string> listArgs = new();
			for (int i = 0; true; i++) {
				if (args.TryGetValue(i, out string? value)) {
					listArgs.Add(value);
				} else {
					break;
				}
			}

			bool success = false;
			try {
				this.context!.Post(this.DispatchEvent, (eventName, listArgs.ToArray()));
				success = true;
			} catch (FormatException) {
				context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
			} catch (Exception ex) {
				context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
				Debug.WriteLine(ex);
			}

			if (success) {
				context.Response.StatusCode = (int)HttpStatusCode.NoContent;
			}
		}
		context.Response.Close();

		listener.BeginGetContext(new AsyncCallback(this.EventCallback), listener);
	}

	private void DispatchEvent(object? state) {
		(string eventName, string[] args) = ((string, string[]))state!;

		switch (eventName) {
			case "OnReady": {
				//this.Ready?.Invoke(this, new());
			} break;
			case "OnCursorLockStateChanged": {
				this.CursorLockStateChanged?.Invoke(this, new(int.Parse(args[0])));
			} break;
			case "OnLoadError": {
				this.LoadError?.Invoke(this, new(args[0], int.Parse(args[1], NumberStyles.Integer), args[2]));
			} break;
			case "OnLoadWarning": {
				this.LoadWarning?.Invoke(this, new(args[0], int.Parse(args[1], NumberStyles.Integer), args[2]));
			} break;
			case "OnLevelListLoaded": {
				DataContractJsonSerializer serializer = new(typeof(LevelListLevelInfo[]), new DataContractJsonSerializerSettings() {
					UseSimpleDictionaryFormat = true
				});
				using MemoryStream stream = new(Encoding.UTF8.GetBytes(args[0]));
				LevelListLevelInfo[] levels = (LevelListLevelInfo[])serializer.ReadObject(stream)!;

				this.LevelListLoaded?.Invoke(this, new(levels));
			} break;
			case "OnLevelLoaded": {
				DataContractJsonSerializer serializer = new(typeof(int[]), new DataContractJsonSerializerSettings() {
					UseSimpleDictionaryFormat = true
				});
				using MemoryStream stream = new(Encoding.UTF8.GetBytes(args[0]));
				int[] layers = (int[])serializer.ReadObject(stream)!;

				this.LevelLoaded?.Invoke(this, new(layers));
			} break;
			case "OnFloorClicked": {
				this.FloorClicked?.Invoke(this, new(int.Parse(args[0])));
			} break;
			case "OnCeilingClicked": {
				this.CeilingClicked?.Invoke(this, new(int.Parse(args[0])));
			} break;
			case "OnWallClicked": {
				this.WallClicked?.Invoke(this, new(int.Parse(args[0]), int.Parse(args[1])));
			} break;
			case "OnObjectClicked": {
				this.ObjectClicked?.Invoke(this, new(int.Parse(args[0])));
			}
			break;
		}
	}
	//public event EventHandler? Ready;
	public event EventHandler<CursorLockStateEventArgs>? CursorLockStateChanged;
	public event EventHandler<LoadEventArgs>? LoadError;
	public event EventHandler<LoadEventArgs>? LoadWarning;
	public event EventHandler<LevelListEventArgs>? LevelListLoaded;
	public event EventHandler<LayersEventArgs>? LevelLoaded;
	public event EventHandler<SectorEventArgs>? FloorClicked;
	public event EventHandler<SectorEventArgs>? CeilingClicked;
	public event EventHandler<WallEventArgs>? WallClicked;
	public event EventHandler<ObjectEventArgs>? ObjectClicked;

	private bool disposedValue;

	protected virtual void Dispose(bool disposing) {
		if (!this.disposedValue) {
			if (disposing) {
				if (this.eventListener != null) {
					this.eventListener.Stop();
					this.eventListener = null;
				}

				/*if (this.process != null && !this.process.HasExited) {
					this.process.Kill();
					this.process = null;
				}*/

				if (this.task != null && !this.task.IsCompleted) {
					_ = this.QuitAsync();

					this.task.Wait();
					this.task = null;
				}
			}

			this.disposedValue = true;
		}
	}

	public void Dispose() {
		this.Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}

public class CursorLockStateEventArgs(int state) : EventArgs {
	public int State { get; } = state;
}

public class LoadEventArgs(string file, int line, string message) : EventArgs {
	public string File { get; } = file;
	public int Line { get; } = line;
	public string Message { get; } = message;
}

public class LevelListEventArgs(LevelListLevelInfo[] levels) : EventArgs {
	public LevelListLevelInfo[] Levels { get; } = levels;
}

public class LayersEventArgs(int[] layers) : EventArgs {
	public int[] Layers { get; } = layers;
}

public class SectorEventArgs(int sectorIndex) : EventArgs {
	public int SectorIndex { get; } = sectorIndex;
}

public class WallEventArgs(int sectorIndex, int walIndex) : SectorEventArgs(sectorIndex) {
	public int WallIndex { get; } = walIndex;
}

public class ObjectEventArgs(int objectIndex) : EventArgs {
	public int ObjectIndex { get; } = objectIndex;
}

public enum Difficulties {
	None,
	Easy,
	Medium,
	Hard,
	All
};

[DataContract]
public class LevelListLevelInfo {
	[DataMember]
	public string? FileName { get; set; }
	[DataMember]
	public string? DisplayName { get; set; }
}


[DataContract]
public class LevelInfo {
	[DataMember]
	public string LevelFile { get; set; } = string.Empty;
	[DataMember]
	public string PaletteFile { get; set; } = string.Empty;
	[DataMember]
	public string MusicFile { get; set; } = string.Empty;
	[DataMember]
	public float ParallaxX { get; set; }
	[DataMember]
	public float ParallaxY { get; set; }
	[DataMember]
	public List<SectorInfo> Sectors { get; set; } = new();
}

[DataContract]
public class WallSurfaceInfo {
	[DataMember]
	public string? TextureFile { get; set; }
	[DataMember]
	public float TextureOffsetX { get; set; }
	[DataMember]
	public float TextureOffsetY { get; set; }
	[DataMember]
	public int TextureUnknown { get; set; }
}

[DataContract]
public class HorizontalSurfaceInfo : WallSurfaceInfo {
	[DataMember]
	public float Y { get; set; }
}

[DataContract]
public class WallInfo {
	[DataMember]
	public float LeftVertexX { get; set; }
	[DataMember]
	public float LeftVertexZ { get; set; }
	[DataMember]
	public float RightVertexX { get; set; }
	[DataMember]
	public float RightVertexZ { get; set; }
	[DataMember]
	public WallSurfaceInfo MainTexture { get; set; } = new();
	[DataMember]
	public WallSurfaceInfo TopEdgeTexture { get; set; } = new();
	[DataMember]
	public WallSurfaceInfo BottomEdgeTexture { get; set; } = new();
	[DataMember]
	public WallSurfaceInfo SignTexture { get; set; } = new();
	[DataMember]
	public int AdjoinedSector { get; set; } = -1;
	[DataMember]
	public int AdjoinedWall { get; set; } = -1;
	[DataMember]
	public WallTextureAndMapFlags TextureAndMapFlags { get; set; }
	[DataMember]
	public int UnusedFlags2 { get; set; }
	[DataMember]
	public WallAdjoinFlags AdjoinFlags { get; set; }
	[DataMember]
	public short LightLevel { get; set; }
}

[DataContract]
public class SectorInfo {
	[DataMember]
	public string Name { get; set; } = string.Empty;
	[DataMember]
	public int LightLevel { get; set; }
	[DataMember]
	public HorizontalSurfaceInfo Floor { get; set; } = new();
	[DataMember]
	public HorizontalSurfaceInfo Ceiling { get; set; } = new();
	[DataMember]
	public float AltY { get; set; }
	[DataMember]
	public SectorFlags Flags { get; set; }
	[DataMember]
	public int UnusedFlags2 { get; set; }
	[DataMember]
	public int AltLightLevel { get; set; }
	[DataMember]
	public int Layer { get; set; }
	[DataMember]
	public List<WallInfo> Walls { get; set; } = new();
}

[DataContract]
public class ObjectInfo {
	[DataMember]
	public ObjectTypes Type { get; set; }
	[DataMember]
	public string FileName { get; set; } = string.Empty;
	[DataMember]
	public float PositionX { get; set; }
	[DataMember]
	public float PositionY { get; set; }
	[DataMember]
	public float PositionZ { get; set; }
	[DataMember]
	public float Pitch { get; set; }
	[DataMember]
	public float Yaw { get; set; }
	[DataMember]
	public float Roll { get; set; }
	[DataMember]
	public ObjectDifficulties Difficulty { get; set; }
	[DataMember]
	public string Logic { get; set; } = string.Empty;
}

/// <summary>
/// Sector flags.
/// </summary>
[Flags]
public enum SectorFlags {
	/// <summary>
	/// Ceiling opens up to the sky.
	/// </summary>
	CeilingIsSky = 0x1,
	/// <summary>
	/// Quick and easy door sector.
	/// </summary>
	SectorIsDoor = 0x2,
	/// <summary>
	/// Walls reflect energy shots.
	/// </summary>
	WallsReflectShots = 0x4,
	/// <summary>
	/// Adjoined sky sectors have their skies adjoined.
	/// </summary>
	AdjoinAdjacentSkies = 0x8,
	/// <summary>
	/// Icy floor.
	/// </summary>
	FloorIsIce = 0x10,
	/// <summary>
	/// Snow floor (no effect?).
	/// </summary>
	FloorIsSnow = 0x20,
	/// <summary>
	/// Sector reacts to explosions.
	/// </summary>
	SectorIsExplodingWall = 0x40,
	/// <summary>
	/// Floor opens up to a pit.
	/// </summary>
	FloorIsPit = 0x80,
	/// <summary>
	/// Adjoined pit sectors have their pits adjoined.
	/// </summary>
	AdjoinAdjacentPits = 0x100,
	/// <summary>
	/// Damage is done if the sector crushes the player.
	/// </summary>
	ElevatorsCrush = 0x200,
	/// <summary>
	/// All non adjoined walls are drawn as sky/pit.
	/// </summary>
	DrawWallsAsSkyPit = 0x400,
	/// <summary>
	/// Sector does low damage.
	/// </summary>
	LowDamage = 0x800,
	/// <summary>
	/// Sector does high damage.
	/// </summary>
	HighDamage = 0x1000,
	/// <summary>
	/// Sector has damaging gas.
	/// </summary>
	GasDamage = 0x1800,
	/// <summary>
	/// Enemies can't trigger triggers.
	/// </summary>
	DenyEnemyTrigger = 0x2000,
	/// <summary>
	/// Enemies can trigger triggers.
	/// </summary>
	AllowEnemyTrigger = 0x4000,
	/// <summary>
	/// Unknown.
	/// </summary>
	Subsector = 0x8000,
	/// <summary>
	/// Unknown.
	/// </summary>
	SafeSector = 0x10000,
	/// <summary>
	/// Unknown.
	/// </summary>
	Rendered = 0x20000,
	/// <summary>
	/// Unknown.
	/// </summary>
	Player = 0x40000,
	/// <summary>
	/// Counts toward secret counter.
	/// </summary>
	Secret = 0x80000
}

/// <summary>
/// Wall texture/map flags.
/// </summary>
[Flags]
public enum WallTextureAndMapFlags {
	/// <summary>
	/// Shows texture even when adjoined.
	/// </summary>
	ShowTextureOnAdjoin = 0x1,
	/// <summary>
	/// Sign is full bright?
	/// </summary>
	IlluminatedSign = 0x2,
	/// <summary>
	/// Flip texture.
	/// </summary>
	FlipTextureHorizontally = 0x4,
	/// <summary>
	/// Elevator adjusts wall light instead of sector light?
	/// </summary>
	ElevatorChangesWallLight = 0x8,
	/// <summary>
	/// Texture anchored at bottom of sector instead of top.
	/// </summary>
	WallTextureAnchored = 0x10,
	/// <summary>
	/// Elevators which morph walls can move this wall.
	/// </summary>
	WallMorphsWithElevator = 0x20,
	/// <summary>
	/// Elevators which scroll textures affect top edge texture.
	/// </summary>
	ElevatorScrollsTopEdgeTexture = 0x40,
	/// <summary>
	/// Elevators which scroll textures affect main texture.
	/// </summary>
	ElevatorScrollsMainTexture = 0x80,
	/// <summary>
	/// Elevators which scroll textures affect bottom edge texture.
	/// </summary>
	ElevatorScrollsBottomEdgeTexture = 0x100,
	/// <summary>
	/// Elevators which scroll textures affect sign texture.
	/// </summary>
	ElevatorScrollsSignTexture = 0x200,
	/// <summary>
	/// Wall is hidden on map.
	/// </summary>
	HiddenOnMap = 0x400,
	/// <summary>
	/// Wall shown as normal on map.
	/// </summary>
	NormalOnMap = 0x800,
	/// <summary>
	/// Sign texture anchored to top of sector instead of bottom.
	/// </summary>
	SignTextureAnchored = 0x1000,
	/// <summary>
	/// Wall damages player on contact.
	/// </summary>
	DamagePlayer = 0x2000,
	/// <summary>
	/// Show as ledge on map.
	/// </summary>
	LedgeOnMap = 0x4000,
	/// <summary>
	/// Show as door/elevator on map.
	/// </summary>
	DoorOnMap = 0x8000
}

/// <summary>
/// Wall flags dealing with adjoins.
/// </summary>
[Flags]
public enum WallAdjoinFlags {
	/// <summary>
	/// Allow player to walk up regardless of height difference.
	/// </summary>
	SkipStepCheck = 0x1,
	/// <summary>
	/// Player and enemies can't cross.
	/// </summary>
	BlockPlayerAndEnemies = 0x2,
	/// <summary>
	/// Enemies can't cross.
	/// </summary>
	BlockEnemies = 0x4,
	/// <summary>
	/// Weapons fire can't cross.
	/// </summary>
	BlockShots = 0x8
}

/// <summary>
/// Different types of objects.
/// </summary>
public enum ObjectTypes {
	/// <summary>
	/// Invisible object.
	/// </summary>
	Spirit,
	/// <summary>
	/// Respawn checkpoint.
	/// </summary>
	Safe,
	/// <summary>
	/// Animated sprite.
	/// </summary>
	Sprite,
	/// <summary>
	/// Non-animated sprite.
	/// </summary>
	Frame,
	/// <summary>
	/// 3D model.
	/// </summary>
	ThreeD,
	/// <summary>
	/// Ambient sound.
	/// </summary>
	Sound
}

/// <summary>
/// Difficulty levels for objects.
/// </summary>
public enum ObjectDifficulties {
	Easy = -1,
	EasyMedium = -2,
	EasyMediumHard = 0,
	MediumHard = 2,
	Hard = 3
}
