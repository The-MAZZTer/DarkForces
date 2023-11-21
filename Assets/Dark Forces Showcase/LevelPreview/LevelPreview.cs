using MZZT.DarkForces.FileFormats;
using MZZT.IO.FileProviders;
using MZZT.IO.FileSystemProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if !UNITY_WEBGL
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;

#else
using System.Runtime.InteropServices;
#endif
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using Debug = UnityEngine.Debug;
using NVector2 = System.Numerics.Vector2;

namespace MZZT.DarkForces.Showcase {
	/// <summary>
	/// Script which powers the Level Explorer showcase.
	/// </summary>
	public class LevelPreview : Singleton<LevelPreview> {
		[SerializeField]
		private FpsCameraControl fpsCamera;
		[SerializeField]
		private OrbitCameraControl orbitCamera;

		private async void Start() {
#if UNITY_WEBGL
#if !UNITY_EDITOR
			WebGLInput.captureAllKeyboardInput = false;
#endif
#else
			this.context = SynchronizationContext.Current;
#endif

			Application.wantsToQuit += this.Application_wantsToQuit;

#if UNITY_WEBGL
			FileManager.Instance.Provider = new UnityWebRequestFileSystemProvider();
#else
			FileManager.Instance.Provider = new PhysicalFileSystemProvider();
#endif

			await this.LoadSettingsAsync();

			await this.InitApiAsync();

			await this.DispatchEventAsync("OnReady");

#if UNITY_EDITOR
			await this.ReloadDataFilesAsync();

			//await this.AddModFileAsync(@"D:\ROMs\dos\PROGRAMS\GAMES\DARK\Levels\assassin\assassin.gob");

			await this.LoadLevelListAsync();

			await this.LoadLevelAsync(0);
#endif
		}

		private bool Application_wantsToQuit() {
			return this.allowQuit;
		}

#if !UNITY_WEBGL
		private int apiPort = 8761;
#endif
		private async Task LoadSettingsAsync() {

#if UNITY_WEBGL && !UNITY_EDITOR
			Stream stream = await FileManager.Instance.NewFileStreamAsync(Path.Combine(Application.dataPath, "settings.json"), FileMode.Open, FileAccess.Read, FileShare.Read);
#else
			Stream stream = await FileManager.Instance.NewFileStreamAsync(Path.Combine(Application.dataPath, "../settings.json"), FileMode.Open, FileAccess.Read, FileShare.Read);
#endif
			LevelPreviewSettings settings;
			using (stream) {
				DataContractJsonSerializer serializer = new(typeof(LevelPreviewSettings), new DataContractJsonSerializerSettings() {
					UseSimpleDictionaryFormat = true
				});
				settings = (LevelPreviewSettings)serializer.ReadObject(stream);
			}

			FileLoader.DarkForcesDataFiles = settings.DataFiles;
			await this.SetDarkForcesPathAsync(settings.DarkForcesPath);
#if !UNITY_WEBGL
			this.apiPort = settings.ApiPort;
#endif
			this.SetBackground(settings.BackgroundR, settings.BackgroundG, settings.BackgroundB);
			this.SetShowWaitBitmap(settings.ShowWaitBitmap);
			this.SetExtendSkyPit(settings.ExtendSkyPit);
			this.SetShowSprites(settings.ShowSprites);
			this.SetShow3dos(settings.Show3dos);
			this.SetDifficulty(settings.Difficulty);
			this.SetAnimateVues(settings.AnimateVues);
			this.SetAnimate3doUpdates(settings.Animate3doUpdates);
			this.SetFullBrightLighting(settings.FullBrightLighting);
			this.SetBypassColormapDithering(settings.BypassColormapDithering);
			await this.SetPlayMusicAsync(settings.PlayMusic);
			await this.SetPlayFightTrackAsync(settings.PlayFightTrack);
			this.SetVolume(settings.Volume);
			this.SetVisibleLayer(settings.VisibleLayer);
			this.SetLookSensitivity(settings.LookSensitivityX, settings.LookSensitivityY);
			this.SetInvertYLook(settings.InvertYLook);
			this.SetMoveSensitivity(settings.MoveSensitivityX, settings.MoveSensitivityY, settings.MoveSensitivityZ);
			this.SetYawLimits(settings.YawLimitMin, settings.YawLimitMax);
			this.SetRunMultiplier(settings.RunMultiplier);
			this.SetZoomSensitivity(settings.ZoomSensitivity);
			this.SetUseOrbitCamera(settings.UseOrbitCamera);
			this.SetUseMouseCapture(settings.UseMouseCapture);
		}
		private bool playMusic = true;

		public async void OnApiCall(string json) {
			DataContractJsonSerializer serializer = new(typeof(ApiCall), new DataContractJsonSerializerSettings() {
				UseSimpleDictionaryFormat = true
			});
			ApiCall api;
			using (MemoryStream mem = new(Encoding.UTF8.GetBytes(json))) {
				api = (ApiCall)serializer.ReadObject(mem);
			}

#if UNITY_WEBGL
			try {
				await this.MakeApiCallAsync(api.Api, api.Args);
			} finally {
				OnApiCallFinished(api.Id);
			}
#else
			await this.MakeApiCallAsync(api.Api, api.Args);
#endif
		}

		private async Task InitApiAsync() {
#if UNITY_WEBGL
			await Task.CompletedTask;
			return;
#else
			HttpListener listener = new();
			listener.Prefixes.Add($"http://localhost:{this.apiPort}/");
			listener.Start();

			listener.BeginGetContext(new AsyncCallback(this.ApiRequestCallback), listener);
			await Task.CompletedTask;
#endif
		}

#if !UNITY_WEBGL
		[DataContract]
		private class RawInputData {
			[DataMember]
			public int[] HeaderIndices { get; set; }
			[DataMember]
			public int[] DataIndices { get; set; }
			[DataMember]
			public byte[] Buffer { get; set; }
		}
#endif

		private async Task MakeApiCallAsync(string api, string[] args) {
			switch (api) {
#if !UNITY_WEBGL
				case nameof(this.SetEventHandler): {
					this.SetEventHandler(args[0]);
				} break;
				case nameof(this.RawInput): {
					if (args.Length < 1) {
						throw new FormatException();
					}
					DataContractJsonSerializer serializer = new(typeof(RawInputData), new DataContractJsonSerializerSettings() {
						UseSimpleDictionaryFormat = true
					});
					using MemoryStream stream = new(Encoding.UTF8.GetBytes(args[0]));
					RawInputData data = (RawInputData)serializer.ReadObject(stream);

					this.RawInput(data);
				} break;
#endif
				case nameof(this.Quit): {
					this.Quit();
				} break;
				case nameof(this.CaptureMouse): {
					this.CaptureMouse();
				} break;
				case nameof(this.ReleaseMouse): {
					this.ReleaseMouse();
				} break;
				case "ReloadDataFiles": {
					await this.ReloadDataFilesAsync();
				} break;
				case "AddModFile": {
					if (args.Length < 1) {
						throw new FormatException();
					}
					await this.AddModFileAsync(args[0]);
				} break;
				case "LoadLevelList": {
					await this.LoadLevelListAsync();
				} break;
				case "LoadLevel": {
					if (args.Length < 1 || !int.TryParse(args[0], out int index)) {
						throw new FormatException();
					}
					await this.LoadLevelAsync(index);
				} break;
				case "ReloadLevelInPlace": {
					await this.ReloadLevelInPlaceAsync();
				} break;
				case "InitEmptyLevel": {
					if (args.Length < 3 || !int.TryParse(args[1], out int index)) {
						throw new FormatException();
					}
					await this.InitEmptyLevelAsync(args[0], index, args[2]);
				} break;
				case "SetDarkForcesPath": {
					if (args.Length < 1) {
						throw new FormatException();
					}
					await this.SetDarkForcesPathAsync(args[0]);
				} break;
				case nameof(this.SetBackground): {
					if (args.Length < 3 || !float.TryParse(args[0], out float r) || !float.TryParse(args[1], out float g) ||
						!float.TryParse(args[2], out float b)) {

						throw new FormatException();
					}
					this.SetBackground(r, g, b);
				} break;
				case nameof(this.SetShowWaitBitmap): {
					if (args.Length < 1 || !bool.TryParse(args[0], out bool value)) {
						throw new FormatException();
					}
					this.SetShowWaitBitmap(value);
				} break;
				case nameof(this.SetExtendSkyPit): {
					if (args.Length < 1 || !int.TryParse(args[0], out int value)) {
						throw new FormatException();
					}
					this.SetExtendSkyPit(value);
				} break;
				case nameof(this.SetShowSprites): {
					if (args.Length < 1 || !bool.TryParse(args[0], out bool value)) {
						throw new FormatException();
					}
					this.SetShowSprites(value);
				} break;
				case nameof(this.SetShow3dos): {
					if (args.Length < 1 || !bool.TryParse(args[0], out bool value)) {
						throw new FormatException();
					}
					this.SetShow3dos(value);
				} break;
				case nameof(this.SetDifficulty): {
					if (args.Length < 1 || !int.TryParse(args[0], out int value)) {
						throw new FormatException();
					}
					this.SetDifficulty((ObjectGenerator.Difficulties)value);
				} break;
				case nameof(this.SetAnimateVues): {
					if (args.Length < 1 || !bool.TryParse(args[0], out bool value)) {
						throw new FormatException();
					}
					this.SetAnimateVues(value);
				} break;
				case nameof(this.SetAnimate3doUpdates): {
					if (args.Length < 1 || !bool.TryParse(args[0], out bool value)) {
						throw new FormatException();
					}
					this.SetAnimate3doUpdates(value);
				} break;
				case nameof(this.SetFullBrightLighting): {
					if (args.Length < 1 || !bool.TryParse(args[0], out bool value)) {
						throw new FormatException();
					}
					this.SetFullBrightLighting(value);
				} break;
				case nameof(this.SetBypassColormapDithering): {
					if (args.Length < 1 || !bool.TryParse(args[0], out bool value)) {
						throw new FormatException();
					}
					this.SetBypassColormapDithering(value);
				} break;
				case "SetPlayMusic": {
					if (args.Length < 1 || !bool.TryParse(args[0], out bool value)) {
						throw new FormatException();
					}
					await this.SetPlayMusicAsync(value);
				} break;
				case "SetPlayFightTrack": {
					if (args.Length < 1 || !bool.TryParse(args[0], out bool value)) {
						throw new FormatException();
					}
					await this.SetPlayFightTrackAsync(value);
				} break;
				case nameof(this.SetVolume): {
					if (args.Length < 1 || !float.TryParse(args[0], out float value)) {
						throw new FormatException();
					}
					this.SetVolume(value);
				} break;
				case nameof(this.SetVisibleLayer): {
					if (args.Length < 1 || !int.TryParse(args[0], out int value)) {
						this.SetVisibleLayer(null);
					} else {
						this.SetVisibleLayer(value);
					}
				} break;
				case nameof(this.SetLookSensitivity): {
					if (args.Length < 2 || !float.TryParse(args[0], out float x) || !float.TryParse(args[1], out float y)) {
						throw new FormatException();
					}
					this.SetLookSensitivity(x, y);
				} break;
				case nameof(this.SetInvertYLook): {
					if (args.Length < 1 || !bool.TryParse(args[0], out bool value)) {
						throw new FormatException();
					}
					this.SetInvertYLook(value);
				} break;
				case nameof(this.SetMoveSensitivity): {
					if (args.Length < 3 || !float.TryParse(args[0], out float x) || !float.TryParse(args[1], out float y) ||
						!float.TryParse(args[2], out float z)) {

						throw new FormatException();
					}
					this.SetMoveSensitivity(x, y, z);
				} break;
				case nameof(this.SetYawLimits): {
					if (args.Length < 2 || !float.TryParse(args[0], out float min) || !float.TryParse(args[1], out float max)) {
						throw new FormatException();
					}
					this.SetYawLimits(min, max);
				} break;
				case nameof(this.SetRunMultiplier): {
					if (args.Length < 1 || !float.TryParse(args[0], out float value)) {
						throw new FormatException();
					}
					this.SetRunMultiplier(value);
				} break;
				case nameof(this.SetZoomSensitivity): {
					if (args.Length < 1 || !float.TryParse(args[0], out float value)) {
						throw new FormatException();
					}
					this.SetZoomSensitivity(value);
				} break;
				case nameof(this.SetUseOrbitCamera): {
					if (args.Length < 1 || !bool.TryParse(args[0], out bool value)) {
						throw new FormatException();
					}
					this.SetUseOrbitCamera(value);
				} break;
				case nameof(this.SetUseMouseCapture): {
					if (args.Length < 1 || !bool.TryParse(args[0], out bool value)) {
						throw new FormatException();
					}
					this.SetUseMouseCapture(value);
				} break;
				case "ReloadLevelGeometry": {
					if (args.Length < 1) {
						throw new FormatException();
					}
					DataContractJsonSerializer serializer = new(typeof(LevelInfo), new DataContractJsonSerializerSettings() {
						UseSimpleDictionaryFormat = true
					});
					using MemoryStream stream = new(Encoding.UTF8.GetBytes(args[0]));
					LevelInfo levelInfo = (LevelInfo)serializer.ReadObject(stream);

					await this.ReloadLevelGeometryAsync(levelInfo);
				} break;
				case "SetLevelMetadata": {
					if (args.Length < 5 || !float.TryParse(args[3], out float x) || !float.TryParse(args[4], out float y)) {
						throw new FormatException();
					}

					await this.SetLevelMetadataAsync(args[0], args[1], args[2], x, y);
				} break;
				case "ReloadSector": {
					if (args.Length < 2 || !int.TryParse(args[0], out int index)) {
						throw new FormatException();
					}
					DataContractJsonSerializer serializer = new(typeof(SectorInfo), new DataContractJsonSerializerSettings() {
						UseSimpleDictionaryFormat = true
					});
					using MemoryStream stream = new(Encoding.UTF8.GetBytes(args[1]));
					SectorInfo sectorInfo = (SectorInfo)serializer.ReadObject(stream);

					await this.ReloadSectorAsync(index, sectorInfo);
				} break;
				case "SetSector": {
					if (args.Length < 2 || !int.TryParse(args[0], out int index)) {
						throw new FormatException();
					}
					DataContractJsonSerializer serializer = new(typeof(SectorInfo), new DataContractJsonSerializerSettings() {
						UseSimpleDictionaryFormat = true
					});
					using MemoryStream stream = new(Encoding.UTF8.GetBytes(args[1]));
					SectorInfo sectorInfo = (SectorInfo)serializer.ReadObject(stream);

					await this.SetSectorAsync(index, sectorInfo);
				} break;
				case "MoveSector": {
					if (args.Length < 4 || !int.TryParse(args[0], out int index) || !float.TryParse(args[1], out float x) ||
						!float.TryParse(args[2], out float y) || !float.TryParse(args[3], out float z)) {

						throw new FormatException();
					}

					await this.MoveSectorAsync(index, x, y, z);
				} break;
				case nameof(this.DeleteSector): {
					if (args.Length < 1 || !int.TryParse(args[0], out int index)) {
						throw new FormatException();
					}

					this.DeleteSector(index);
				} break;
				case "SetSectorFloor": {
					if (args.Length < 2 || !int.TryParse(args[0], out int index)) {
						throw new FormatException();
					}
					DataContractJsonSerializer serializer = new(typeof(HorizontalSurfaceInfo), new DataContractJsonSerializerSettings() {
						UseSimpleDictionaryFormat = true
					});
					using MemoryStream stream = new(Encoding.UTF8.GetBytes(args[1]));
					HorizontalSurfaceInfo floor = (HorizontalSurfaceInfo)serializer.ReadObject(stream);

					await this.SetSectorFloorAsync(index, floor);
				} break;
				case "SetSectorCeiling": {
					if (args.Length < 2 || !int.TryParse(args[0], out int index)) {
						throw new FormatException();
					}
					DataContractJsonSerializer serializer = new(typeof(HorizontalSurfaceInfo), new DataContractJsonSerializerSettings() {
						UseSimpleDictionaryFormat = true
					});
					using MemoryStream stream = new(Encoding.UTF8.GetBytes(args[1]));
					HorizontalSurfaceInfo ceiling = (HorizontalSurfaceInfo)serializer.ReadObject(stream);

					await this.SetSectorCeilingAsync(index, ceiling);
				} break;
				case "ReloadWall": {
					if (args.Length < 3 || !int.TryParse(args[0], out int sectorIndex) || !int.TryParse(args[1], out int wallIndex)) {
						throw new FormatException();
					}
					DataContractJsonSerializer serializer = new(typeof(WallInfo), new DataContractJsonSerializerSettings() {
						UseSimpleDictionaryFormat = true
					});
					using MemoryStream stream = new(Encoding.UTF8.GetBytes(args[2]));
					WallInfo wall = (WallInfo)serializer.ReadObject(stream);

					await this.ReloadWallAsync(sectorIndex, wallIndex, wall);
				} break;
				case "InsertWall": {
					if (args.Length < 3 || !int.TryParse(args[0], out int sectorIndex) || !int.TryParse(args[1], out int wallIndex)) {
						throw new FormatException();
					}
					DataContractJsonSerializer serializer = new(typeof(WallInfo), new DataContractJsonSerializerSettings() {
						UseSimpleDictionaryFormat = true
					});
					using MemoryStream stream = new(Encoding.UTF8.GetBytes(args[2]));
					WallInfo wall = (WallInfo)serializer.ReadObject(stream);

					await this.InsertWallAsync(sectorIndex, wallIndex, wall);
				} break;
				case "DeleteWall": {
					if (args.Length < 2 || !int.TryParse(args[0], out int sectorIndex) || !int.TryParse(args[1], out int wallIndex)) {
						throw new FormatException();
					}

					await this.DeleteWallAsync(sectorIndex, wallIndex);
				} break;
				case "SetVertex": {
					if (args.Length < 5 || !int.TryParse(args[0], out int sectorIndex) || !int.TryParse(args[1], out int wallIndex) ||
						!bool.TryParse(args[2], out bool rightVertex) || !float.TryParse(args[3], out float x) ||
						!float.TryParse(args[4], out float z)) {

						throw new FormatException();
					}

					await this.SetVertexAsync(sectorIndex, wallIndex, rightVertex, x, z);
				} break;
				case "ReloadLevelObjects": {
					if (args.Length < 1) {
						throw new FormatException();
					}
					DataContractJsonSerializer serializer = new(typeof(ObjectInfo[]), new DataContractJsonSerializerSettings() {
						UseSimpleDictionaryFormat = true
					});
					using MemoryStream stream = new(Encoding.UTF8.GetBytes(args[0]));
					ObjectInfo[] objects = (ObjectInfo[])serializer.ReadObject(stream);

					await this.ReloadLevelObjectsAsync(objects);
				} break;
				case "SetObject": {
					if (args.Length < 2 || !int.TryParse(args[0], out int index)) {
						throw new FormatException();
					}
					DataContractJsonSerializer serializer = new(typeof(ObjectInfo), new DataContractJsonSerializerSettings() {
						UseSimpleDictionaryFormat = true
					});
					using MemoryStream stream = new(Encoding.UTF8.GetBytes(args[1]));
					ObjectInfo obj = (ObjectInfo)serializer.ReadObject(stream);

					await this.SetObjectAsync(index, obj);
				} break;
				case nameof(this.DeleteObject): {
					if (args.Length < 1 || !int.TryParse(args[0], out int index)) {
						throw new FormatException();
					}

					this.DeleteObject(index);
				} break;
				case nameof(this.ResetCamera): {
					this.ResetCamera();
				} break;
				case nameof(this.MoveCamera): {
					if (args.Length < 3 || !float.TryParse(args[0], out float x) || !float.TryParse(args[1], out float y) ||
						!float.TryParse(args[2], out float z)) {

						throw new FormatException();
					}
					this.MoveCamera(x, y, z);
				} break;
				case nameof(this.RotateCamera): {
					if (args.Length < 4 || !float.TryParse(args[0], out float w) || !float.TryParse(args[1], out float x) ||
						!float.TryParse(args[2], out float y) || !float.TryParse(args[3], out float z)) {

						throw new FormatException();
					}
					this.RotateCamera(w, x, y, z);
				} break;
				case nameof(this.RotateCameraEuler): {
					if (args.Length < 3 || !float.TryParse(args[0], out float pitch) || !float.TryParse(args[1], out float yaw) ||
						!float.TryParse(args[2], out float roll)) {

						throw new FormatException();
					}
					this.RotateCameraEuler(pitch, yaw, roll);
				} break;
				case nameof(this.MoveAndRotateCamera): {
					if (args.Length < 7 || !float.TryParse(args[0], out float posX) || !float.TryParse(args[1], out float posY) ||
						!float.TryParse(args[2], out float posZ) || !float.TryParse(args[3], out float rotW) ||
						!float.TryParse(args[4], out float rotX) || !float.TryParse(args[5], out float rotY) ||
						!float.TryParse(args[6], out float rotZ)) {

						throw new FormatException();
					}
					this.MoveAndRotateCamera(posX, posY, posZ, rotW, rotX, rotY, rotZ);
				} break;
				case nameof(this.MoveAndRotateCameraEuler): {
					if (args.Length < 6 || !float.TryParse(args[0], out float x) || !float.TryParse(args[1], out float y) ||
						!float.TryParse(args[2], out float z) || !float.TryParse(args[3], out float pitch) ||
						!float.TryParse(args[4], out float yaw) || !float.TryParse(args[5], out float roll)) {

						throw new FormatException();
					}
					this.MoveAndRotateCameraEuler(x, y, z, pitch, y, roll);
				} break;
				case nameof(this.PointCameraAt): {
					if (args.Length < 3 || !float.TryParse(args[0], out float x) || !float.TryParse(args[1], out float y) ||
						!float.TryParse(args[2], out float z)) {

						throw new FormatException();
					}
					this.PointCameraAt(x, y, z);
				} break;
				default:
					throw new FormatException();
			}
		}

#if !UNITY_WEBGL
		private void RawInput(RawInputData data) {
			unsafe {
				fixed (int* rawInputHeaderIndices = data.HeaderIndices) {
					fixed (int* rawInputDataIndices = data.DataIndices) {
						fixed (byte* rawInputData = data.Buffer) {
							UnityEngine.Windows.Input.ForwardRawInput((uint*)rawInputHeaderIndices, (uint*)rawInputDataIndices, (uint)data.DataIndices.Length, rawInputData, (uint)data.Buffer.Length);
						}
					}
				}
			}
		}

		private SynchronizationContext context;
		private async void ApiRequestCallback(IAsyncResult ar) {
			HttpListener listener = (HttpListener)ar.AsyncState;
			HttpListenerContext context = listener.EndGetContext(ar);
			if (context.Request.HttpMethod != "POST" ||
				(context.Request.HasEntityBody && context.Request.ContentType != "application/x-www-form-urlencoded")) {

				context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
			} else {
				string api = context.Request.Url.AbsolutePath.Trim('/');
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
					if (args.TryGetValue(i, out string value)) {
						listArgs.Add(value);
					} else {
						break;
					}
				}

				TaskCompletionSource<bool> taskSource = new();

				bool success = false;
				try {
					this.context.Post(this.MakeApiCall, (api, listArgs.ToArray(), taskSource));
					await taskSource.Task;

					success = true;
				} catch (FormatException) {
					context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
				} catch (Exception ex) {
					context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
					Debug.LogException(ex);
				}

				if (success) {
					context.Response.StatusCode = (int)HttpStatusCode.NoContent;
				}
			}
			context.Response.Close();

			listener.BeginGetContext(new AsyncCallback(this.ApiRequestCallback), listener);
		}

		private async void MakeApiCall(object state) {
			(string api, string[] args, TaskCompletionSource<bool> taskSource) = ((string, string[], TaskCompletionSource<bool>))state;
			try {
				await this.MakeApiCallAsync(api, args);
			} catch (Exception ex) {
				taskSource.SetException(ex);
				return;
			}
			taskSource.SetResult(true);
		}
#endif

#if UNITY_WEBGL
		[DllImport("__Internal")]
		private static extern void OnApiCallFinished(int id);
		[DllImport("__Internal")]
		private static extern void OnEvent0Args(string eventName);
		[DllImport("__Internal")]
		private static extern void OnEvent1Args(string eventName, string arg0);
		[DllImport("__Internal")]
		private static extern void OnEvent2Args(string eventName, string arg0, string arg1);
		[DllImport("__Internal")]
		private static extern void OnEvent3Args(string eventName, string arg0, string arg1, string arg2);
#else
		private string eventHandler;
		public void SetEventHandler(string value) {
			this.eventHandler = value;
		}
		private HttpClient httpClient;
#endif

		private async Task DispatchEventAsync(string eventName, params object[] args) {
			string[] a = args?.Select(x => {
				if (x == null) {
					return null;
				}

				Type type = x.GetType();
				if (type.IsValueType || x is string) {
					return x.ToString();
				}

				DataContractJsonSerializer serializer = new(type, new DataContractJsonSerializerSettings() {
					UseSimpleDictionaryFormat = true
				});
				using MemoryStream stream = new();
				serializer.WriteObject(stream, x);
				stream.Position = 0;
				return Encoding.UTF8.GetString(stream.ToArray());
			}).ToArray() ?? Array.Empty<string>();

			Debug.Log($"{eventName}({string.Join(", ", a)})");

#if UNITY_WEBGL
#if !UNITY_EDITOR
			switch (a.Length) {
				case 0:
					OnEvent0Args(eventName);
					break;
				case 1:
					OnEvent1Args(eventName, a[0]);
					break;
				case 2:
					OnEvent2Args(eventName, a[0], a[1]);
					break;
				case 3:
					OnEvent3Args(eventName, a[0], a[1], a[2]);
					break;
			}
#endif
			await Task.CompletedTask;
#else
			if (string.IsNullOrEmpty(this.eventHandler)) {
				return;
			}

			if (this.httpClient == null) {
				this.httpClient = new();
			}
			this.httpClient.BaseAddress = new Uri(this.eventHandler);

			FormUrlEncodedContent form = new(a.Select((x, i) => new KeyValuePair<string, string>(i.ToString(), x)));
			using HttpResponseMessage response = await this.httpClient.PostAsync(eventName, form);
#endif
		}

		private bool allowQuit = false;
		public void Quit() {
			this.allowQuit = true;
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}

		public void CaptureMouse() {
			if (this.fpsCamera.isActiveAndEnabled) {
				FpsCameraControl.CaptureMouse();
			}
		}

		public void ReleaseMouse() {
			if (this.fpsCamera.isActiveAndEnabled) {
				FpsCameraControl.ReleaseMouse();
			}
		}

		public async Task ReloadDataFilesAsync() {
			ResourceCache.Instance.Clear();
			FileLoader.Instance.Clear();
			await FileLoader.Instance.LoadStandardFilesAsync();
		}

		public async Task AddModFileAsync(string path) {
			await FileLoader.Instance.AddGobFileAsync(path);
		}

		private async Task ShowWarningsAsync(string name = null) {
			if (name == null) {
				name = LevelLoader.Instance.CurrentLevelName;
			}

			foreach (ResourceCache.LoadWarning warning in ResourceCache.Instance.Warnings) {
				if (warning.Fatal) {
					await this.DispatchEventAsync("OnLoadError", warning.FileName, warning.Line, warning.Message);
				} else {
					await this.DispatchEventAsync("OnLoadWarning", warning.FileName, warning.Line, warning.Message);
				}
			}
			ResourceCache.Instance.ClearWarnings();
		}

		public async Task LoadLevelListAsync() {
			await PauseMenu.Instance.BeginLoadingAsync();

			ResourceCache.Instance.ClearWarnings();

			await LevelLoader.Instance.LoadLevelListAsync(true);

			this.musicIndex = -1;

			await this.DispatchEventAsync("OnLevelListLoaded", new object[] { LevelLoader.Instance.LevelList.Levels.Select(x => new LevelListLevelInfo() {
				FileName = x.FileName,
				DisplayName = x.DisplayName
			}).ToArray() });

			await this.ShowWarningsAsync("JEDI.LVL");

			PauseMenu.Instance.EndLoading();
		}

		/// <summary>
		/// Load a level and generate Unity objects.
		/// </summary>
		public async Task LoadLevelAsync(int levelIndex) {
			// Clear out existing level data.
			LevelMusic.Instance.Stop();
			LevelGeometryGenerator.Instance.Clear();
			ObjectGenerator.Instance.Clear();

			await PauseMenu.Instance.BeginLoadingAsync();

			this.musicIndex = levelIndex;
			if (this.playMusic) {
				await LevelMusic.Instance.PlayAsync(levelIndex);
			}

			await LevelLoader.Instance.LoadLevelAsync(levelIndex);
			if (LevelLoader.Instance.Level != null) {
				await LevelLoader.Instance.LoadColormapAsync();
				if (LevelLoader.Instance.ColorMap != null) {
					await LevelLoader.Instance.LoadPaletteAsync();
					if (LevelLoader.Instance.Palette != null) {
						await LevelGeometryGenerator.Instance.GenerateAsync();

						await LevelLoader.Instance.LoadObjectsAsync();
						if (LevelLoader.Instance.Objects != null) {
							await ObjectGenerator.Instance.GenerateAsync();
						}
					}
				}
			}

			this.AddClickEvents();

			await this.ShowWarningsAsync();

			this.ResetCamera();

			await this.DispatchEventAsync("OnLevelLoaded", LevelLoader.Instance.Level?.Sectors.Select(x => x.Layer).Distinct().OrderBy(x => x).ToArray() ?? Array.Empty<int>());

			PauseMenu.Instance.EndLoading();
		}

		public async Task ReloadLevelInPlaceAsync() {
			if (LevelLoader.Instance.CurrentLevelIndex < 0) {
				return;
			}

			// Clear out existing level data.
			LevelMusic.Instance.Stop();
			LevelGeometryGenerator.Instance.Clear();
			ObjectGenerator.Instance.Clear();

			await PauseMenu.Instance.BeginLoadingAsync();

			if (this.playMusic) {
				await LevelMusic.Instance.PlayAsync(LevelLoader.Instance.CurrentLevelIndex);
			}

			await LevelLoader.Instance.LoadLevelAsync(LevelLoader.Instance.CurrentLevelIndex);
			if (LevelLoader.Instance.Level != null) {
				await LevelLoader.Instance.LoadColormapAsync();
				if (LevelLoader.Instance.ColorMap != null) {
					await LevelLoader.Instance.LoadPaletteAsync();
					if (LevelLoader.Instance.Palette != null) {
						await LevelGeometryGenerator.Instance.GenerateAsync();

						await LevelLoader.Instance.LoadObjectsAsync();
						if (LevelLoader.Instance.Objects != null) {
							await ObjectGenerator.Instance.GenerateAsync();
						}
					}
				}
			}

			this.AddClickEvents();

			await this.ShowWarningsAsync();

			await this.DispatchEventAsync("OnLevelLoaded", LevelLoader.Instance.Level?.Sectors.Select(x => x.Layer).Distinct().OrderBy(x => x).ToArray() ?? Array.Empty<int>());

			PauseMenu.Instance.EndLoading();
		}

		public async Task InitEmptyLevelAsync(string name, int musicIndex, string paletteName) {
			// Clear out existing level data.
			LevelMusic.Instance.Stop();
			LevelGeometryGenerator.Instance.Clear();
			ObjectGenerator.Instance.Clear();

			await PauseMenu.Instance.BeginLoadingAsync();

			this.musicIndex = musicIndex;
			if (this.playMusic) {
				await LevelMusic.Instance.PlayAsync(musicIndex);
			}

			LevelLoader.Instance.LoadLevel(new DfLevel() {
				LevelFile = name,
				MusicFile = $"STALK-{musicIndex + 1:00}.GMD",
				PaletteFile = $"{paletteName}.PAL"
			});

			LevelLoader.Instance.LoadObjects(new DfLevelObjects() {
				LevelFile = name
			});

			await LevelLoader.Instance.LoadColormapAsync();
			await LevelLoader.Instance.LoadPaletteAsync();

			this.ResetCamera();

			await this.DispatchEventAsync("OnLevelLoaded", LevelLoader.Instance.Level?.Sectors.Select(x => x.Layer).Distinct().OrderBy(x => x).ToArray() ?? Array.Empty<int>());

			PauseMenu.Instance.EndLoading();
		}

		private void AddClickEvents(SectorRenderer sector) {
			int sectorIndex = LevelLoader.Instance.Level.Sectors.IndexOf(sector.Sector);

			FloorCeilingRenderer floorCeiling = sector.GetComponent<FloorCeilingRenderer>();
			foreach (GameObject mesh in floorCeiling.FloorObjects) {
				EventTrigger trigger = mesh.AddComponent<EventTrigger>();
				EventTrigger.Entry entry = new() {
					eventID = EventTriggerType.PointerDown
				};
				trigger.triggers.Add(entry);
				entry.callback.AddListener(e => {
					if (((PointerEventData)e).button == PointerEventData.InputButton.Left) {
						this.OnFloorClicked(sectorIndex);
					}
				});
			}
			foreach (GameObject mesh in floorCeiling.CeilingObjects) {
				EventTrigger trigger = mesh.AddComponent<EventTrigger>();
				EventTrigger.Entry entry = new() {
					eventID = EventTriggerType.PointerDown
				};
				trigger.triggers.Add(entry);
				entry.callback.AddListener(e => {
					if (((PointerEventData)e).button == PointerEventData.InputButton.Left) {
						this.OnCeilingClicked(sectorIndex);
					}
				});
			}

			foreach (WallRenderer wall in sector.GetComponentsInChildren<WallRenderer>(true)) {
				int wallIndex = sector.Sector.Walls.IndexOf(wall.Wall);

				foreach (MeshRenderer mesh in wall.GetComponentsInChildren<MeshRenderer>(true)) {
					EventTrigger trigger = mesh.gameObject.AddComponent<EventTrigger>();
					EventTrigger.Entry entry = new() {
						eventID = EventTriggerType.PointerDown
					};
					trigger.triggers.Add(entry);
					entry.callback.AddListener(e => {
						if (((PointerEventData)e).button == PointerEventData.InputButton.Left) {
							this.OnWallClicked(sectorIndex, wallIndex);
						}
					});
				}
			}
		}

		private void AddLevelGeometryClickEvents() {
			foreach (SectorRenderer sector in LevelGeometryGenerator.Instance.GetComponentsInChildren<SectorRenderer>(true)) {
				this.AddClickEvents(sector);
			}
		}

		private void AddClickEvents(ObjectRenderer renderer) {
			int objectIndex = LevelLoader.Instance.Objects.Objects.IndexOf(renderer.Object);

			EventTrigger trigger = renderer.gameObject.AddComponent<EventTrigger>();
			EventTrigger.Entry entry = new() {
				eventID = EventTriggerType.PointerDown
			};
			trigger.triggers.Add(entry);
			entry.callback.AddListener(e => {
				if (((PointerEventData)e).button == PointerEventData.InputButton.Left) {
					this.OnObjectClicked(objectIndex);
				}
			});
		}

		private void AddLevelObjectClickEvents() {
			foreach (ObjectRenderer renderer in ObjectGenerator.Instance.GetComponentsInChildren<ObjectRenderer>(true)) {
				this.AddClickEvents(renderer);
			}
		}

		private void AddClickEvents() {
			this.AddLevelGeometryClickEvents();
			this.AddLevelObjectClickEvents();
		}

		private async void OnFloorClicked(int sectorIndex) {
			await this.DispatchEventAsync(nameof(this.OnFloorClicked), sectorIndex);
		}

		private async void OnCeilingClicked(int sectorIndex) {
			await this.DispatchEventAsync(nameof(this.OnCeilingClicked), sectorIndex);
		}

		private async void OnWallClicked(int sectorIndex, int wallIndex) {
			await this.DispatchEventAsync(nameof(this.OnWallClicked), sectorIndex, wallIndex);
		}

		private async void OnObjectClicked(int objectIndex) {
			await this.DispatchEventAsync(nameof(this.OnObjectClicked), objectIndex);
		}

		public async Task SetDarkForcesPathAsync(string value) {
#if UNITY_WEBGL && !UNITY_EDITOR
			string path = Path.Combine(Application.dataPath, value);
#else
			string path = Path.Combine(Application.dataPath, "..", value);
#endif
			if (FileLoader.Instance.DarkForcesFolder == value) {
				return;
			}

			FileLoader.Instance.DarkForcesFolder = value;

			UnityWebRequestFileSystemProvider.ClearFileCache();

			if (FileLoader.Instance.Gobs.Any()) {
				await this.ReloadDataFilesAsync();
			}
		}

		public void SetBackground(float r, float g, float b) {
			Camera.main.backgroundColor = new Color(r, g, b);
		}

		public void SetShowWaitBitmap(bool value) {
			PauseMenu.Instance.EnableLoadingScreen = value;
		}

		public void SetExtendSkyPit(float value) {
			if (FloorCeilingRenderer.SkyPitExtend == value) {
				return;
			}

			FloorCeilingRenderer.SkyPitExtend = value;
			foreach (FloorCeilingRenderer x in FindObjectsOfType<FloorCeilingRenderer>(true)) {
				_ = x.RenderAsync(x.GetComponent<SectorRenderer>().Sector);
			}
		}

		public void SetShowSprites(bool value) {
			if (ObjectGenerator.Instance.ShowSprites == value) {
				return;
			}

			ObjectGenerator.Instance.ShowSprites = value;
			ObjectGenerator.Instance.RefreshVisibility();
		}
		public void SetShow3dos(bool value) {
			if (ObjectGenerator.Instance.Show3dos == value) {
				return;
			}

			ObjectGenerator.Instance.Show3dos = value;
			ObjectGenerator.Instance.RefreshVisibility();
		}
		public void SetDifficulty(ObjectGenerator.Difficulties value) {
			if (ObjectGenerator.Instance.Difficulty == value) {
				return;
			}

			ObjectGenerator.Instance.Difficulty = value;
			ObjectGenerator.Instance.RefreshVisibility();
		}

		public void SetAnimate3doUpdates(bool value) {
			ObjectGenerator.Instance.Animate3doUpdates = value;
		}
		public void SetAnimateVues(bool value) {
			ObjectGenerator.Instance.AnimateVues = value;
		}

		public void SetFullBrightLighting(bool value) {
			if (ResourceCache.Instance.FullBright == value) {
				return;
			}

			ResourceCache.Instance.FullBright = value;
			ResourceCache.Instance.RegenerateMaterials();
		}
		public void SetBypassColormapDithering(bool value) {
			if (ResourceCache.Instance.BypassCmpDithering == value) {
				return;
			}

			ResourceCache.Instance.BypassCmpDithering = value;
			ResourceCache.Instance.RegenerateMaterials();
		}

		private int musicIndex;
		public async Task SetPlayMusicAsync(bool value) {
			if (this.playMusic == value) {
				return;
			}

			this.playMusic = value;
			if (!value) {
				LevelMusic.Instance.Stop();
			} else if (this.musicIndex >= 0) {
				await LevelMusic.Instance.PlayAsync(this.musicIndex);
			}
		}
		public async Task SetPlayFightTrackAsync(bool value) {
			if (LevelMusic.Instance.FightMusic == value) {
				return;
			}

			LevelMusic.Instance.FightMusic = value;
			if (LevelMusic.Instance.IsPlaying) {
				await LevelMusic.Instance.PlayAsync(this.musicIndex);
			}
		}
		public void SetVolume(float value) {
			foreach (AudioSource source in LevelMusic.Instance.GetComponentsInChildren<AudioSource>(true)) {
				source.volume = value;
			}
		}
		public void SetVisibleLayer(int? value) {
			if ((value == null && LevelGeometryGenerator.Instance.ShowAllLayers) ||
				(value != null && !LevelGeometryGenerator.Instance.ShowAllLayers && LevelGeometryGenerator.Instance.Layer == value.Value)) {

				return;
			}

			LevelGeometryGenerator.Instance.Layer = value.HasValue ? value.Value : 0;
			LevelGeometryGenerator.Instance.ShowAllLayers = value == null;

			LevelGeometryGenerator.Instance.RefreshVisiblity();
			ObjectGenerator.Instance.RefreshVisibility();
		}

		public void SetLookSensitivity(float x, float y) {
			this.fpsCamera.LookSensitivity = new Vector2(x, y);
			this.orbitCamera.LookSensitivity = new Vector2(x, y);
		}

		public void SetInvertYLook(bool value) {
			this.fpsCamera.InvertY = value;
			this.orbitCamera.InvertY = value;
		}

		public void SetMoveSensitivity(float x, float y, float z) {
			this.fpsCamera.MoveSensitivity = new Vector2(x, z);
			this.fpsCamera.UpDownSensitivity = y;
			this.orbitCamera.MoveSensitivity = new Vector2(x, z);
			this.orbitCamera.UpDownSensitivity = y;
		}

		public void SetYawLimits(float min, float max) {
			this.fpsCamera.VerticalAngleClamp = new Vector2(min, max);
			this.orbitCamera.VerticalAngleClamp = new Vector2(min, max);
		}

		public void SetRunMultiplier(float value) {
			this.fpsCamera.RunningMultiplier = value;
			this.orbitCamera.RunningMultiplier = value;
		}

		public void SetZoomSensitivity(float value) {
			this.orbitCamera.ZoomSensitivity = value;
		}

		public void SetUseOrbitCamera(bool value) {
			if (this.orbitCamera.isActiveAndEnabled == value) {
				return;
			}

			if (value) {
				this.fpsCamera.enabled = false;
				Vector3 pos = Camera.main.transform.position;
				Camera.main.transform.position = pos - Camera.main.transform.forward * 100;
				this.orbitCamera.enabled = true;
				this.orbitCamera.FocusPoint = pos;
			} else {
				this.orbitCamera.enabled = false;
				Camera.main.transform.position = this.orbitCamera.FocusPoint;
				this.fpsCamera.enabled = true;
			}
		}

		public void SetUseMouseCapture(bool value) {
			this.fpsCamera.HoldButtonToLook = !value;
		}

		public async Task ReloadLevelGeometryAsync(LevelInfo levelInfo) {
			await PauseMenu.Instance.BeginLoadingAsync();

			LevelGeometryGenerator.Instance.Clear();

			await LevelLoader.Instance.LoadColormapAsync();
			await LevelLoader.Instance.LoadPaletteAsync();

			LevelLoader.Instance.LoadLevel(new() {
				LevelFile = levelInfo.LevelFile,
				MusicFile = levelInfo.MusicFile,
				PaletteFile = levelInfo.PaletteFile,
				Parallax = new NVector2(levelInfo.ParallaxX, levelInfo.ParallaxY)
			});

			this.TransformSectors(levelInfo.Sectors);

			await LevelGeometryGenerator.Instance.GenerateAsync();

			this.AddLevelGeometryClickEvents();

			await this.ShowWarningsAsync();

			this.ResetCamera();

			await this.DispatchEventAsync("OnLevelLoaded", LevelLoader.Instance.Level?.Sectors.Select(x => x.Layer).Distinct().OrderBy(x => x).ToArray() ?? Array.Empty<int>());

			PauseMenu.Instance.EndLoading();
		}

		private void TransformSectors(SectorInfo[] sectors) {
			DfLevel level = LevelLoader.Instance.Level;

			Dictionary<DfLevel.Wall, (int sectorIndex, int wallIndex)> adjoins = new();

			level.Sectors.Clear();
			level.Sectors.AddRange(sectors.Select(x => {
				DfLevel.Sector sector = new() {
					AltLightLevel = x.AltLightLevel,
					AltY = x.AltY,
					Flags = x.Flags,
					Layer = x.Layer,
					LightLevel = x.LightLevel,
					Name = x.Name,
					UnusedFlags2 = x.UnusedFlags2
				};

				sector.Ceiling.TextureOffset = new NVector2(x.Ceiling.TextureOffsetX, x.Ceiling.TextureOffsetY);
				sector.Ceiling.TextureFile = x.Ceiling.TextureFile;
				sector.Ceiling.TextureUnknown = x.Ceiling.TextureUnknown;
				sector.Ceiling.Y = x.Ceiling.Y;

				sector.Floor.TextureOffset = new NVector2(x.Floor.TextureOffsetX, x.Floor.TextureOffsetY);
				sector.Floor.TextureFile = x.Floor.TextureFile;
				sector.Floor.TextureUnknown = x.Floor.TextureUnknown;
				sector.Floor.Y = x.Floor.Y;

				Dictionary<NVector2, DfLevel.Vertex> vertices = new();

				sector.Walls.AddRange(x.Walls.Select(x => {
					DfLevel.Wall wall = new(sector) {
						AdjoinFlags = x.AdjoinFlags,
						LightLevel = x.LightLevel,
						TextureAndMapFlags = x.TextureAndMapFlags,
						UnusedFlags2 = x.UnusedFlags2
					};

					if (x.AdjoinedWall >= 0) {
						adjoins[wall] = (x.AdjoinedSector, x.AdjoinedWall);
					}

					wall.BottomEdgeTexture.TextureOffset = new NVector2(x.BottomEdgeTexture.TextureOffsetX, x.BottomEdgeTexture.TextureOffsetY);
					wall.BottomEdgeTexture.TextureFile = x.BottomEdgeTexture.TextureFile;
					wall.BottomEdgeTexture.TextureUnknown = x.BottomEdgeTexture.TextureUnknown;

					NVector2 vector = new(x.LeftVertexX, x.LeftVertexZ);
					if (!vertices.TryGetValue(vector, out DfLevel.Vertex vertex)) {
						vertices[vector] = vertex = new DfLevel.Vertex() {
							Position = vector
						};
					}
					wall.LeftVertex = vertex;

					wall.MainTexture.TextureOffset = new NVector2(x.MainTexture.TextureOffsetX, x.MainTexture.TextureOffsetY);
					wall.MainTexture.TextureFile = x.MainTexture.TextureFile;
					wall.MainTexture.TextureUnknown = x.MainTexture.TextureUnknown;

					vector = new(x.RightVertexX, x.RightVertexZ);
					if (!vertices.TryGetValue(vector, out vertex)) {
						vertices[vector] = vertex = new DfLevel.Vertex() {
							Position = vector
						};
					}
					wall.RightVertex = vertex;

					wall.SignTexture.TextureOffset = new NVector2(x.SignTexture.TextureOffsetX, x.SignTexture.TextureOffsetY);
					wall.SignTexture.TextureFile = x.SignTexture.TextureFile;
					wall.SignTexture.TextureUnknown = x.SignTexture.TextureUnknown;

					wall.TopEdgeTexture.TextureOffset = new NVector2(x.TopEdgeTexture.TextureOffsetX, x.TopEdgeTexture.TextureOffsetY);
					wall.TopEdgeTexture.TextureFile = x.TopEdgeTexture.TextureFile;
					wall.TopEdgeTexture.TextureUnknown = x.TopEdgeTexture.TextureUnknown;

					return wall;
				}));

				return sector;
			}));

			foreach ((DfLevel.Wall wall, (int sectorIndex, int wallIndex)) in adjoins) {
				wall.Adjoined = level.Sectors[sectorIndex].Walls[wallIndex];
			}
		}

		private async Task SetLevelMetadataAsync(string levelFile, string musicFile, string paletteFile, float parallaxX, float parallaxY) {
			await PauseMenu.Instance.BeginLoadingAsync();

			DfLevel level = LevelLoader.Instance.Level;
			level.LevelFile = levelFile;
			level.MusicFile = musicFile;
			if (level.PaletteFile != paletteFile) {
				level.PaletteFile = paletteFile;

				LevelGeometryGenerator.Instance.Clear();
				ObjectGenerator.Instance.Clear();

				await LevelLoader.Instance.LoadColormapAsync();
				await LevelLoader.Instance.LoadPaletteAsync();

				await LevelGeometryGenerator.Instance.GenerateAsync();
				await ObjectGenerator.Instance.GenerateAsync();
			}

			if (level.Parallax.X != parallaxX || level.Parallax.Y != parallaxY) {
				level.Parallax = new NVector2(parallaxX, parallaxY);

				if (Parallaxer.Instance != null) {
					Parallaxer.Instance.Parallax = level.Parallax.ToUnity();
				}
			}

			PauseMenu.Instance.EndLoading();
		}

		private async Task ReloadSectorAsync(int sectorIndex, SectorInfo sectorInfo) {
			DfLevel level = LevelLoader.Instance.Level;
			Dictionary<DfLevel.Wall, int> adjoins = new();
			DfLevel.Sector sector;
			if (sectorIndex > level.Sectors.Count) {
				sectorIndex = level.Sectors.Count;
				sector = new();
				level.Sectors.Add(sector);
			} else {
				sector = level.Sectors[sectorIndex];

				foreach (DfLevel.Sector otherSector in level.Sectors) {
					if (otherSector == sector) {
						continue;
					}

					foreach (DfLevel.Wall wall in otherSector.Walls) {
						if (wall.Adjoined != null && wall.Adjoined.Sector == sector) {
							adjoins.Add(wall, wall.Adjoined.Sector.Walls.IndexOf(wall.Adjoined));
						}
					}
				}
			}

			sector.AltLightLevel = sectorInfo.AltLightLevel;
			sector.AltY = sectorInfo.AltY;
			sector.Flags = sectorInfo.Flags;
			sector.Layer = sectorInfo.Layer;
			sector.LightLevel = sectorInfo.LightLevel;
			sector.Name = sectorInfo.Name;
			sector.UnusedFlags2 = sectorInfo.UnusedFlags2;

			sector.Ceiling.TextureOffset = new NVector2(sectorInfo.Ceiling.TextureOffsetX, sectorInfo.Ceiling.TextureOffsetY);
			sector.Ceiling.TextureFile = sectorInfo.Ceiling.TextureFile;
			sector.Ceiling.TextureUnknown = sectorInfo.Ceiling.TextureUnknown;
			sector.Ceiling.Y = sectorInfo.Ceiling.Y;

			sector.Floor.TextureOffset = new NVector2(sectorInfo.Floor.TextureOffsetX, sectorInfo.Floor.TextureOffsetY);
			sector.Floor.TextureFile = sectorInfo.Floor.TextureFile;
			sector.Floor.TextureUnknown = sectorInfo.Floor.TextureUnknown;
			sector.Floor.Y = sectorInfo.Floor.Y;

			Dictionary<NVector2, DfLevel.Vertex> vertices = new();

			sector.Walls.Clear();
			sector.Walls.AddRange(sectorInfo.Walls.Select(x => {
				DfLevel.Wall wall = new(sector) {
					AdjoinFlags = x.AdjoinFlags,
					LightLevel = x.LightLevel,
					TextureAndMapFlags = x.TextureAndMapFlags,
					UnusedFlags2 = x.UnusedFlags2
				};

				if (x.AdjoinedWall >= 0) {
					if (x.AdjoinedSector == sectorIndex) {
						adjoins[wall] = x.AdjoinedWall;
					} else {
						DfLevel.Wall otherWall = level.Sectors[x.AdjoinedSector].Walls[x.AdjoinedWall];
						wall.Adjoined = otherWall;
						otherWall.Adjoined = wall;
					}
				}

				wall.BottomEdgeTexture.TextureOffset = new NVector2(x.BottomEdgeTexture.TextureOffsetX, x.BottomEdgeTexture.TextureOffsetY);
				wall.BottomEdgeTexture.TextureFile = x.BottomEdgeTexture.TextureFile;
				wall.BottomEdgeTexture.TextureUnknown = x.BottomEdgeTexture.TextureUnknown;

				NVector2 vector = new(x.LeftVertexX, x.LeftVertexZ);
				if (!vertices.TryGetValue(vector, out DfLevel.Vertex vertex)) {
					vertices[vector] = vertex = new DfLevel.Vertex() {
						Position = vector
					};
				}
				wall.LeftVertex = vertex;

				wall.MainTexture.TextureOffset = new NVector2(x.MainTexture.TextureOffsetX, x.MainTexture.TextureOffsetY);
				wall.MainTexture.TextureFile = x.MainTexture.TextureFile;
				wall.MainTexture.TextureUnknown = x.MainTexture.TextureUnknown;

				vector = new(x.RightVertexX, x.RightVertexZ);
				if (!vertices.TryGetValue(vector, out vertex)) {
					vertices[vector] = vertex = new DfLevel.Vertex() {
						Position = vector
					};
				}
				wall.RightVertex = vertex;

				wall.SignTexture.TextureOffset = new NVector2(x.SignTexture.TextureOffsetX, x.SignTexture.TextureOffsetY);
				wall.SignTexture.TextureFile = x.SignTexture.TextureFile;
				wall.SignTexture.TextureUnknown = x.SignTexture.TextureUnknown;

				wall.TopEdgeTexture.TextureOffset = new NVector2(x.TopEdgeTexture.TextureOffsetX, x.TopEdgeTexture.TextureOffsetY);
				wall.TopEdgeTexture.TextureFile = x.TopEdgeTexture.TextureFile;
				wall.TopEdgeTexture.TextureUnknown = x.TopEdgeTexture.TextureUnknown;

				return wall;
			}));

			foreach ((DfLevel.Wall wall, int wallIndex) in adjoins) {
				if (sector.Walls[wallIndex].Adjoined == wall) {
					wall.Adjoined = sector.Walls[wallIndex];
				} else {
					wall.Adjoined = null;
				}
			}

			SectorRenderer renderer = await LevelGeometryGenerator.Instance.RefreshSectorAsync(sectorIndex, sector);
			this.AddClickEvents(renderer);
		}

		private async Task SetSectorAsync(int sectorIndex, SectorInfo sectorInfo) {
			DfLevel level = LevelLoader.Instance.Level;
			DfLevel.Sector sector = level.Sectors[sectorIndex];

			sector.AltLightLevel = sectorInfo.AltLightLevel;
			sector.AltY = sectorInfo.AltY;
			sector.Flags = sectorInfo.Flags;
			sector.Layer = sectorInfo.Layer;
			sector.LightLevel = sectorInfo.LightLevel;
			sector.Name = sectorInfo.Name;
			sector.UnusedFlags2 = sectorInfo.UnusedFlags2;

			SectorRenderer renderer = await LevelGeometryGenerator.Instance.RefreshSectorAsync(sectorIndex, sector);
			this.AddClickEvents(renderer);
		}

		private async Task MoveSectorAsync(int sectorIndex, float deltaX, float deltaY, float deltaZ) {
			DfLevel level = LevelLoader.Instance.Level;
			DfLevel.Sector sector = level.Sectors[sectorIndex];

			sector.Ceiling.Y += deltaY;
			sector.Floor.Y += deltaY;
			foreach (DfLevel.Vertex vertex in sector.Walls.SelectMany(x => new[] { x.LeftVertex, x.RightVertex }).Distinct()) {
				vertex.Position += new NVector2(deltaX, deltaZ);
			}

			SectorRenderer renderer = await LevelGeometryGenerator.Instance.RefreshSectorAsync(sectorIndex, sector);
			this.AddClickEvents(renderer);
		}

		private void DeleteSector(int sectorIndex) {
			DfLevel level = LevelLoader.Instance.Level;
			level.Sectors.RemoveAt(sectorIndex);

			LevelGeometryGenerator.Instance.DeleteSector(sectorIndex);
		}

		private async Task SetSectorCeilingAsync(int sectorIndex, HorizontalSurfaceInfo ceiling) {
			DfLevel level = LevelLoader.Instance.Level;
			DfLevel.Sector sector = level.Sectors[sectorIndex];
			sector.Ceiling.TextureFile = ceiling.TextureFile;
			sector.Ceiling.TextureOffset = new NVector2(ceiling.TextureOffsetX, ceiling.TextureOffsetY);
			sector.Ceiling.TextureUnknown = ceiling.TextureUnknown;
			sector.Ceiling.Y = ceiling.Y;

			SectorRenderer renderer = await LevelGeometryGenerator.Instance.RefreshSectorAsync(sectorIndex, sector);
			this.AddClickEvents(renderer);
		}

		private async Task SetSectorFloorAsync(int sectorIndex, HorizontalSurfaceInfo floor) {
			DfLevel level = LevelLoader.Instance.Level;
			DfLevel.Sector sector = level.Sectors[sectorIndex];
			sector.Floor.TextureFile = floor.TextureFile;
			sector.Floor.TextureOffset = new NVector2(floor.TextureOffsetX, floor.TextureOffsetY);
			sector.Floor.TextureUnknown = floor.TextureUnknown;
			sector.Floor.Y = floor.Y;

			SectorRenderer renderer = await LevelGeometryGenerator.Instance.RefreshSectorAsync(sectorIndex, sector);
			this.AddClickEvents(renderer);
		}

		public async Task ReloadWallAsync(int sectorIndex, int wallIndex, WallInfo wallInfo) {
			DfLevel level = LevelLoader.Instance.Level;
			DfLevel.Sector sector = level.Sectors[sectorIndex];
			DfLevel.Wall wall;
			if (wallIndex > sector.Walls.Count) {
				sectorIndex = sector.Walls.Count;
				wall = new(sector);
				sector.Walls.Add(wall);
			} else {
				wall = sector.Walls[wallIndex];
			}

			wall.AdjoinFlags = wallInfo.AdjoinFlags;
			wall.LightLevel = wallInfo.LightLevel;
			wall.TextureAndMapFlags = wallInfo.TextureAndMapFlags;
			wall.UnusedFlags2 = wallInfo.UnusedFlags2;

			if (wall.Adjoined != null) {
				wall.Adjoined.Adjoined = null;
				wall.Adjoined = null;
			}
			if (wallInfo.AdjoinedWall >= 0) {
				wall.Adjoined = level.Sectors[wallInfo.AdjoinedSector].Walls[wallInfo.AdjoinedWall];
				wall.Adjoined.Adjoined = wall;
			}

			wall.BottomEdgeTexture.TextureOffset = new NVector2(wallInfo.BottomEdgeTexture.TextureOffsetX, wallInfo.BottomEdgeTexture.TextureOffsetY);
			wall.BottomEdgeTexture.TextureFile = wallInfo.BottomEdgeTexture.TextureFile;
			wall.BottomEdgeTexture.TextureUnknown = wallInfo.BottomEdgeTexture.TextureUnknown;

			if (wall.LeftVertex == null) {
				DfLevel.Wall lastWall = sector.Walls[wallIndex - 1];
				DfLevel.Wall nextWall = sector.Walls.First(x => lastWall.RightVertex == x.LeftVertex);
				wall.LeftVertex = lastWall.RightVertex;
				wall.RightVertex = nextWall.LeftVertex = new();
			}
			wall.LeftVertex.Position = new NVector2(wallInfo.LeftVertexX, wallInfo.LeftVertexZ);

			wall.MainTexture.TextureOffset = new NVector2(wallInfo.MainTexture.TextureOffsetX, wallInfo.MainTexture.TextureOffsetY);
			wall.MainTexture.TextureFile = wallInfo.MainTexture.TextureFile;
			wall.MainTexture.TextureUnknown = wallInfo.MainTexture.TextureUnknown;

			wall.RightVertex.Position = new NVector2(wallInfo.RightVertexX, wallInfo.RightVertexZ);

			wall.SignTexture.TextureOffset = new NVector2(wallInfo.SignTexture.TextureOffsetX, wallInfo.SignTexture.TextureOffsetY);
			wall.SignTexture.TextureFile = wallInfo.SignTexture.TextureFile;
			wall.SignTexture.TextureUnknown = wallInfo.SignTexture.TextureUnknown;

			wall.TopEdgeTexture.TextureOffset = new NVector2(wallInfo.TopEdgeTexture.TextureOffsetX, wallInfo.TopEdgeTexture.TextureOffsetY);
			wall.TopEdgeTexture.TextureFile = wallInfo.TopEdgeTexture.TextureFile;
			wall.TopEdgeTexture.TextureUnknown = wallInfo.TopEdgeTexture.TextureUnknown;

			SectorRenderer renderer = await LevelGeometryGenerator.Instance.RefreshSectorAsync(sectorIndex, sector);
			this.AddClickEvents(renderer);
		}

		public async Task InsertWallAsync(int sectorIndex, int wallIndex, WallInfo wallInfo) {
			DfLevel level = LevelLoader.Instance.Level;
			DfLevel.Sector sector = level.Sectors[sectorIndex];
			DfLevel.Wall wall = new(sector);
			sector.Walls.Insert(wallIndex, wall);

			wall.AdjoinFlags = wallInfo.AdjoinFlags;
			wall.LightLevel = wallInfo.LightLevel;
			wall.TextureAndMapFlags = wallInfo.TextureAndMapFlags;
			wall.UnusedFlags2 = wallInfo.UnusedFlags2;

			if (wall.Adjoined != null) {
				wall.Adjoined.Adjoined = null;
				wall.Adjoined = null;
			}
			if (wallInfo.AdjoinedWall >= 0) {
				wall.Adjoined = level.Sectors[wallInfo.AdjoinedSector].Walls[wallInfo.AdjoinedWall];
				wall.Adjoined.Adjoined = wall;
			}

			wall.BottomEdgeTexture.TextureOffset = new NVector2(wallInfo.BottomEdgeTexture.TextureOffsetX, wallInfo.BottomEdgeTexture.TextureOffsetY);
			wall.BottomEdgeTexture.TextureFile = wallInfo.BottomEdgeTexture.TextureFile;
			wall.BottomEdgeTexture.TextureUnknown = wallInfo.BottomEdgeTexture.TextureUnknown;

			if (wall.LeftVertex == null) {
				DfLevel.Wall lastWall = sector.Walls[wallIndex - 1];
				DfLevel.Wall nextWall = sector.Walls.First(x => lastWall.RightVertex == x.LeftVertex);
				wall.LeftVertex = lastWall.RightVertex;
				wall.RightVertex = nextWall.LeftVertex = new();
			}
			wall.LeftVertex.Position = new NVector2(wallInfo.LeftVertexX, wallInfo.LeftVertexZ);

			wall.MainTexture.TextureOffset = new NVector2(wallInfo.MainTexture.TextureOffsetX, wallInfo.MainTexture.TextureOffsetY);
			wall.MainTexture.TextureFile = wallInfo.MainTexture.TextureFile;
			wall.MainTexture.TextureUnknown = wallInfo.MainTexture.TextureUnknown;

			wall.RightVertex.Position = new NVector2(wallInfo.RightVertexX, wallInfo.RightVertexZ);

			wall.SignTexture.TextureOffset = new NVector2(wallInfo.SignTexture.TextureOffsetX, wallInfo.SignTexture.TextureOffsetY);
			wall.SignTexture.TextureFile = wallInfo.SignTexture.TextureFile;
			wall.SignTexture.TextureUnknown = wallInfo.SignTexture.TextureUnknown;

			wall.TopEdgeTexture.TextureOffset = new NVector2(wallInfo.TopEdgeTexture.TextureOffsetX, wallInfo.TopEdgeTexture.TextureOffsetY);
			wall.TopEdgeTexture.TextureFile = wallInfo.TopEdgeTexture.TextureFile;
			wall.TopEdgeTexture.TextureUnknown = wallInfo.TopEdgeTexture.TextureUnknown;

			SectorRenderer renderer = await LevelGeometryGenerator.Instance.RefreshSectorAsync(sectorIndex, sector);
			this.AddClickEvents(renderer);
		}

		private async Task DeleteWallAsync(int sectorIndex, int wallIndex) {
			DfLevel level = LevelLoader.Instance.Level;
			DfLevel.Sector sector = level.Sectors[sectorIndex];
			DfLevel.Wall wall = sector.Walls[wallIndex];

			DfLevel.Wall prevWall = sector.Walls.First(x => x.RightVertex == wall.LeftVertex);
			DfLevel.Wall nextWall = sector.Walls.First(x => x.LeftVertex == wall.RightVertex);

			sector.Walls.RemoveAt(wallIndex);

			prevWall.RightVertex = nextWall.LeftVertex;

 			SectorRenderer renderer = await LevelGeometryGenerator.Instance.RefreshSectorAsync(sectorIndex, sector);
			this.AddClickEvents(renderer);
		}

		private async Task SetVertexAsync(int sectorIndex, int wallIndex, bool isRightVertex, float x, float z) {
			DfLevel level = LevelLoader.Instance.Level;
			DfLevel.Sector sector = level.Sectors[sectorIndex];
			DfLevel.Wall wall = sector.Walls[wallIndex];
			if (!isRightVertex) {
				wall.LeftVertex.Position = new(x, z);
			} else {
				wall.RightVertex.Position = new(x, z);
			}

			SectorRenderer renderer = await LevelGeometryGenerator.Instance.RefreshSectorAsync(sectorIndex, sector);
			this.AddClickEvents(renderer);
		}

		public async Task ReloadLevelObjectsAsync(ObjectInfo[] objects) {
			await PauseMenu.Instance.BeginLoadingAsync();

			ObjectGenerator.Instance.Clear();

			LevelLoader.Instance.LoadObjects(new() {
				LevelFile = LevelLoader.Instance.Level.LevelFile
			});

			this.TransformObjects(objects);

			await ObjectGenerator.Instance.GenerateAsync();

			this.AddLevelObjectClickEvents();

			PauseMenu.Instance.EndLoading();
		}

		private void TransformObjects(ObjectInfo[] objects) {
			DfLevelObjects levelObjects = LevelLoader.Instance.Objects;

			levelObjects.Objects.Clear();
			levelObjects.Objects.AddRange(objects.Select(x => new DfLevelObjects.Object() {
				Difficulty = x.Difficulty,
				EulerAngles = new(x.Pitch, x.Yaw, x.Roll),
				FileName = x.FileName,
				Logic = x.Logic,
				Position = new(x.PositionX, x.PositionY, x.PositionZ),
				Type = x.Type
			}));
		}

		private async Task SetObjectAsync(int objectIndex, ObjectInfo objectInfo) {
			DfLevelObjects levelObjects = LevelLoader.Instance.Objects;
			DfLevelObjects.Object obj;
			if (objectIndex >= levelObjects.Objects.Count) {
				objectIndex = levelObjects.Objects.Count;
				obj = new();
				levelObjects.Objects.Add(obj);
			} else {
				obj = levelObjects.Objects[objectIndex];
			}

			obj.Difficulty = objectInfo.Difficulty;
			obj.EulerAngles = new(objectInfo.Pitch, objectInfo.Yaw, objectInfo.Roll);
			obj.FileName = objectInfo.FileName;
			obj.Logic = objectInfo.Logic;
			obj.Position = new(objectInfo.PositionX, objectInfo.PositionY, objectInfo.PositionZ);
			obj.Type = objectInfo.Type;

			ObjectRenderer renderer = await ObjectGenerator.Instance.RefreshObjectAsync(objectIndex, obj);
			if (renderer != null) {
				this.AddClickEvents(renderer);
			}
		}

		private void DeleteObject(int objectIndex) {
			DfLevelObjects levelObjects = LevelLoader.Instance.Objects;
			levelObjects.Objects.RemoveAt(objectIndex);

			ObjectGenerator.Instance.DeleteObject(objectIndex);
		}

		private void ResetCamera() {
			Vector3 position = ObjectRenderer.Eye != null ? ObjectRenderer.Eye.transform.position - ObjectGenerator.KYLE_EYE_POSITION * LevelGeometryGenerator.GEOMETRY_SCALE : Vector3.zero;
			Quaternion rotation = ObjectRenderer.Eye != null ? ObjectRenderer.Eye.transform.rotation : Quaternion.LookRotation(Vector3.forward, Vector3.up);
			if (this.fpsCamera.isActiveAndEnabled) {
				Camera.main.transform.SetPositionAndRotation(position, rotation);
			} else {
				Bounds[] bounds = FindObjectsOfType<MeshRenderer>(true).Select(x => x.bounds).ToArray();
				float distance = 100;
				if (bounds.Length > 0) {
					float xMin = bounds.Min(x => x.min.x);
					float zMin = bounds.Min(x => x.min.z);
					float yMax = bounds.Max(x => x.max.y);
					float xMax = bounds.Max(x => x.max.x);
					float zMax = bounds.Max(x => x.max.z);
					// Don't use this as focal point... some maps may have sectors out in the middle of nowhere.
					//Vector3 center = new Vector3((xMin + xMax) / 2, yMax, (zMin + zMax) / 2);
					distance = new[] {
						(new Vector3(xMin, 0, zMin) - position).magnitude,
						(new Vector3(xMin, 0, zMax) - position).magnitude,
						(new Vector3(xMax, 0, zMin) - position).magnitude,
						(new Vector3(xMax, 0, zMax) - position).magnitude
					}.Max() + 2;
					distance = Math.Min(distance, 100); // in case a level has sectors in the middle of nowhere.
				}
				this.orbitCamera.FocusPoint = position;
				Camera.main.transform.rotation = rotation;
				Camera.main.transform.position = position - Camera.main.transform.forward * distance;
			}
		}

		private void MoveCamera(float x, float y, float z) {
			Camera.main.transform.position = new(x * LevelGeometryGenerator.GEOMETRY_SCALE, -y * LevelGeometryGenerator.GEOMETRY_SCALE, z * LevelGeometryGenerator.GEOMETRY_SCALE);
			if (this.orbitCamera.isActiveAndEnabled) {
				Camera.main.transform.LookAt(this.orbitCamera.FocusPoint);
			}
		}

		private void RotateCamera(float w, float x, float y, float z) {
			Camera.main.transform.rotation = new(w, x, y, z);
			if (this.orbitCamera.isActiveAndEnabled) {
				float distance = (this.orbitCamera.FocusPoint - Camera.main.transform.position).magnitude;
				Camera.main.transform.position = this.orbitCamera.FocusPoint - Camera.main.transform.forward * distance;
			}
		}

		private void RotateCameraEuler(float pitch, float yaw, float roll) {
			Quaternion rotation = Quaternion.Euler(pitch, yaw, roll);
			this.RotateCamera(rotation.w, rotation.x, rotation.y, rotation.z);
		}

		private void MoveAndRotateCamera(float posX, float posY, float posZ, float rotW, float rotX, float rotY, float rotZ) {
			float distance = 0;
			if (this.orbitCamera.isActiveAndEnabled) {
				distance = (this.orbitCamera.FocusPoint - Camera.main.transform.position).magnitude;
			}
			Camera.main.transform.SetPositionAndRotation(new(posX * LevelGeometryGenerator.GEOMETRY_SCALE, -posY * LevelGeometryGenerator.GEOMETRY_SCALE, posZ * LevelGeometryGenerator.GEOMETRY_SCALE), new(rotW, rotX, rotY, rotZ));
			if (this.orbitCamera.isActiveAndEnabled) {
				this.orbitCamera.FocusPoint = Camera.main.transform.position + Camera.main.transform.forward * distance;
			}
		}

		private void MoveAndRotateCameraEuler(float x, float y, float z, float pitch, float yaw, float roll) {
			Quaternion rotation = Quaternion.Euler(pitch, yaw, roll);
			this.MoveAndRotateCamera(x * LevelGeometryGenerator.GEOMETRY_SCALE, -y * LevelGeometryGenerator.GEOMETRY_SCALE, z * LevelGeometryGenerator.GEOMETRY_SCALE, rotation.w, rotation.x, rotation.y, rotation.z);
		}

		private void PointCameraAt(float x, float y, float z) {
			Vector3 target = new(x * LevelGeometryGenerator.GEOMETRY_SCALE, -y * LevelGeometryGenerator.GEOMETRY_SCALE, z * LevelGeometryGenerator.GEOMETRY_SCALE);
			if (this.orbitCamera.isActiveAndEnabled) {
				this.orbitCamera.FocusPoint = target;
			}
			Camera.main.transform.LookAt(target);
		}

		private CursorLockMode lastLockMode = CursorLockMode.None;
		private void Update() {
			if (Cursor.lockState != this.lastLockMode) {
				this.lastLockMode = Cursor.lockState;

				_ = this.DispatchEventAsync("OnCursorLockStateChanged", (int)Cursor.lockState);
			}
		}
	}
}
