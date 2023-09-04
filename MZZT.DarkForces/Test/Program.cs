using MZZT.DarkForces.FileFormats;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Test {
	static class Program {
		//static string output;
		static async Task Main(string[] _) {
			/*Dictionary<string, (string, byte[])> x = new();

			foreach (string lfdFile in Directory.EnumerateFiles(@"C:\Users\mzzt\dos\PROGRAMS\GAMES\DARK\LFD", "*.LFD")) {
				output = Path.Combine(AppContext.BaseDirectory, Path.GetFileName(lfdFile));
				if (!Directory.Exists(output)) {
					Directory.CreateDirectory(output);
				}

				LandruFileDirectory lfd = await LandruFileDirectory.ReadAsync(lfdFile, async lfd => {
					List<(string name, IFile file)> files = new();

					foreach ((string name, string type, uint offset, uint size) in lfd.Files.OrderBy(x => x.offset)) {
						string filename = $"{name}.{type}";
						Raw raw = await lfd.GetFileAsync<Raw>(name, type);
						if (!x.TryGetValue(filename, out (string, byte[]) y)) {
							x[filename] = (Path.GetFileName(lfdFile), raw.Data);
						} else if (!y.Item2.SequenceEqual(raw.Data)) {
							Console.WriteLine($"{filename} {y.Item1} {Path.GetFileName(lfdFile)}");
						}

						//await raw.SaveAsync(Path.Combine(output, $"{name}.{type}"));

						/*Type t = LandruFileDirectory.FileTypeNames.First(x => x.Value == type.ToUpper()).Key;
						IFile file = await lfd.GetFileAsync(t, name);
						files.Add((name, file));*
					}

					//byte[] currPal = null;
					foreach ((string name, IFile file) in files.OrderBy(x => x.file is LandruPalette ? 0 : 1)) {
						/*if (file is CreativeVoice voic) {
							List<CreativeVoice.AudioData> datas = voic.AudioBlocks;
							for (int i = 0; i < datas.Count; i++) {
								CreativeVoice.AudioData data = datas[i];
								if (data.Type == CreativeVoice.BlockTypes.Silence) {
									CreativeVoice.AudioData source;
									if (i > 0) {
										source = datas[i - 1];
									} else {
										source = datas.Skip(i).FirstOrDefault(x => x.Type != CreativeVoice.BlockTypes.Silence);
									}
									if (source == null) {
										datas.Clear();
										break;
									}

									data.BitsPerSample = source.BitsPerSample;
									data.Channels = source.Channels;
									data.Codec = source.Codec;
									data.Frequency = source.Frequency;
									// Only works for 8-bit
									data.Data = Enumerable.Repeat<byte>(0x80, data.SilenceLength * data.Channels * data.BitsPerSample / 8).ToArray();
								}

								if (data.RepeatCount > 1) {
									int count = Math.Min(5, data.RepeatCount);
									for (int j = 1; j < count; j++) {
										datas.InsertRange(i + 1, datas.Skip(data.RepeatStart).Take(i - data.RepeatStart + 1));
									}
									i += (count - 1) * (i - data.RepeatStart + 1);
								}
							}

							int index = 0;
							while (datas.Count > 0) {
								index++;

								CreativeVoice.AudioData[] wavData = datas.TakeWhile(x =>
									x.BitsPerSample == datas[0].BitsPerSample &&
									x.Channels == datas[0].Channels &&
									x.Codec == datas[0].Codec &&
									x.Frequency == datas[0].Frequency
								).ToArray();
								datas.RemoveRange(0, wavData.Length);

								bool showIndex = index > 1 || datas.Count > 0;
								string wavName = $"{name}{(showIndex ? $"-{index}" : "")}.WAV";

								using FileStream stream = new(Path.Combine(output, wavName), FileMode.Create, FileAccess.Write, FileShare.None);

								Wave wave = new() {
									BitsPerSample = wavData[0].BitsPerSample,
									Channels = wavData[0].Channels,
									SampleRate = wavData[0].Frequency,
									Data = wavData.SelectMany(x => x.Data).ToArray()
								};
								await wave.SaveAsync(stream);
							}
						} else if (file is DfGameMidi gmid) {
							Midi midi = gmid.ToMidi();
							midi.Chunks.Clear();
							await midi.SaveAsync(Path.Combine(output, $"{name}.MID"));
						} else if (file is LandruAnimation anim) {
							if (currPal == null) {
								Debug.WriteLine("No PLTT in LFD!");
							}

							for (int i = 0; i < anim.Pages.Count; i++) {
								LandruDelt delt = anim.Pages[i];

								byte[] pixels = delt.Pixels;
								BitArray mask = delt.Mask;
								int width = delt.Width;
								int height = delt.Height;
								if (width < 1 || height < 1) {
									continue;
								}

								using Bitmap bitmap = new(width, height, PixelFormat.Format32bppArgb);
								BitmapData bitmapData = bitmap.LockBits(new(Point.Empty, bitmap.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

								for (int y = 0; y < height; y++) {
									for (int x = 0; x < width; x++) {
										int offset = (height - y - 1) * width + x;
										byte color = pixels[offset];
										if (mask[offset]) {
											Marshal.Copy(currPal, color * 4, bitmapData.Scan0 + (y * bitmapData.Stride) + (x * 4), 4);
										}
									}
								}

								bitmap.UnlockBits(bitmapData);

								bitmap.Save(Path.Combine(output, $"{name}-{i}.PNG"), ImageFormat.Png);
							}
						} else if (file is LandruDelt delt) {
							if (currPal == null) {
								Debug.WriteLine("No PLTT in LFD!");
							}

							byte[] pixels = delt.Pixels;
							BitArray mask = delt.Mask;
							int width = delt.Width;
							int height = delt.Height;
							if (width < 1 || height < 1) {
								continue;
							}

							using Bitmap bitmap = new(width, height, PixelFormat.Format32bppArgb);
							BitmapData bitmapData = bitmap.LockBits(new(Point.Empty, bitmap.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

							for (int y = 0; y < height; y++) {
								for (int x = 0; x < width; x++) {
									int offset = (height - y - 1) * width + x;
									byte color = pixels[offset];
									if (mask[offset]) {
										Marshal.Copy(currPal, color * 4, bitmapData.Scan0 + (y * bitmapData.Stride) + (x * 4), 4);
									}
								}
							}

							bitmap.UnlockBits(bitmapData);

							bitmap.Save(Path.Combine(output, $"{name}.PNG"), ImageFormat.Png);
						} else*
						if (file is LandruFont font) {
							int width = font.Characters.Sum(x => x.Width + 1);
							int height = font.Height;

							using Bitmap bitmap = new(width, height, PixelFormat.Format32bppArgb);
							BitmapData bitmapData = bitmap.LockBits(new(Point.Empty, bitmap.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

							byte[] color = new byte[] { 0, 0, 0, 255 };
							int offset = 0;
							for (int i = 0; i < font.Characters.Count; i++) {
								LandruFont.Character c = font.Characters[i];
								for (int y = 0; y < height; y++) {
									for (int x = 0; x < c.Width; x++) {
										if (c.Pixels[y * font.BytesPerLine * 8 + x]) {
											Marshal.Copy(color, 0, bitmapData.Scan0 + (y * bitmapData.Stride) + ((offset + x) * 4), 4);
										}
									}
								}
								offset += c.Width + 1;
							}

							bitmap.UnlockBits(bitmapData);

							bitmap.Save(Path.Combine(output, $"{name}.PNG"), ImageFormat.Png);
						} /*else if (file is LandruPalette pltt) {
							if (currPal == null) {
								currPal = new byte[256 * 4];
								for (int i = 0; i < pltt.Palette.Length; i++) {
									currPal[i * 4] = pltt.Palette[i].B;
									currPal[i * 4 + 1] = pltt.Palette[i].G;
									currPal[i * 4 + 2] = pltt.Palette[i].R;
									currPal[i * 4 + 3] = 255;
								}
							}
						}*
					}
				});
			}*/

			//DfGobContainer dark = await DfGobContainer.ReadAsync(@"C:\Users\mzzt\dos\PROGRAMS\GAMES\DARK\DARK.GOB");
			DfGobContainer sprites = await DfGobContainer.ReadAsync(@"D:\ROMs\dos\PROGRAMS\GAMES\DARK\SPRITES.GOB");
			//DfLevelList lvl = await dark.GetFileAsync<DfLevelList>("JEDI.LVL");
			//DfLevelGoals gol = await dark.GetFileAsync<DfLevelGoals>("SECBASE.GOL");
			//DfLevel lev = await dark.GetFileAsync<DfLevel>($"{lvl.Levels[0].FileName}.LEV");
			//DfLevelInformation inf = await dark.GetFileAsync<DfLevelInformation>("SECBASE.INF");
			//inf.LoadSectorReferences(lev);
			//DfLevelObjects o = await dark.GetFileAsync<DfLevelObjects>("SECBASE.O");
			//Df3dObject _3do = await dark.GetFileAsync<Df3dObject>("DEATH.3DO");
			//_3do = await dark.GetFileAsync<Df3dObject>("KYL3DO.3DO");
			//_3do = await dark.GetFileAsync<Df3dObject>("MOUSEBOT.3DO");
			//_3do = await dark.GetFileAsync<Df3dObject>("TIELO-3.3DO");

			//Wax wax = await sprites.GetFileAsync<Wax>("STORMFIN.WAX");
			//Wax wax2 = await sprites.GetFileAsync<Wax>("REDLIT.WAX");
			DfGobContainer textures = await DfGobContainer.ReadAsync(@"D:\ROMs\dos\PROGRAMS\GAMES\DARK\TEXTURES.GOB", true);

			//DfGobContainer sounds = await DfGobContainer.ReadAsync(@"C:\Users\mzzt\dos\PROGRAMS\GAMES\DARK\SOUNDS.GOB");

			/*using (Bitmap bitmap = new(256, levels.Levels.Count, PixelFormat.Format32bppArgb)) {
				BitmapData bitmapData = bitmap.LockBits(new(Point.Empty, bitmap.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

				using (Bitmap bitmap2 = new(256, 32 * levels.Levels.Count, PixelFormat.Format32bppArgb)) {
					BitmapData bitmapData2 = bitmap2.LockBits(new(Point.Empty, bitmap2.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

					foreach ((DfLevelList.Level level, int y) in levels.Levels.Select((x, i) => (x, i))) {
						string name = level.FileName;

						DfPalette pal = await dark.GetFileAsync<DfPalette>($"{name}.PAL");
						Color[] palette = pal.Palette.Select(x => Color.FromArgb(
							(int)Math.Clamp(Math.Round(x.R * 255 / 63f), 0, 255),
							(int)Math.Clamp(Math.Round(x.G * 255 / 63f), 0, 255),
							(int)Math.Clamp(Math.Round(x.B * 255 / 63f), 0, 255)
						)).ToArray();
						byte[] paletteBuffer = new byte[4 * 256];
						foreach ((Color color, int i) in palette.Select((x, i) => (x, i))) {
							paletteBuffer[i * 4 + 0] = color.B;
							paletteBuffer[i * 4 + 1] = color.G;
							paletteBuffer[i * 4 + 2] = color.R;
							paletteBuffer[i * 4 + 3] = 255;
						}

						for (int x = 0; x < palette.Length; x++) {
							Marshal.Copy(paletteBuffer, x * 4, bitmapData.Scan0 + ((bitmap.Height - 1 - y) * bitmapData.Stride) + (x * 4), 4);
						}

						DfColormap cmp = await dark.GetFileAsync<DfColormap>($"{name}.CMP") ??
							await textures.GetFileAsync<DfColormap>($"{name}.CMP");
						foreach ((byte[] map, int lightLevel) in cmp.PaletteMaps.Select((x, i) => (x, i))) {
							for (int x = 0; x < map.Length; x++) {
								Marshal.Copy(paletteBuffer, map[x] * 4, bitmapData2.Scan0 + ((bitmap2.Height - 1 - (y * 32 + lightLevel)) * bitmapData2.Stride) + (x * 4), 4);
							}
						}
					}

					bitmap2.UnlockBits(bitmapData2);
					bitmap2.Save(Path.Combine(AppContext.BaseDirectory, "colormaps.png"), ImageFormat.Png);
				}

				bitmap.UnlockBits(bitmapData);
				bitmap.Save(Path.Combine(AppContext.BaseDirectory, "palettes.png"), ImageFormat.Png);
			}*/

			/*DfPalette pal = await dark.GetFileAsync<DfPalette>("SECBASE.PAL");
			Color[] palette = pal.Palette.Select(x => Color.FromArgb(
				(int)Math.Clamp(Math.Round(x.R * 255 / 63f), 0, 255),
				(int)Math.Clamp(Math.Round(x.G * 255 / 63f), 0, 255),
				(int)Math.Clamp(Math.Round(x.B * 255 / 63f), 0, 255)
			)).ToArray();

			DfColormap cmp = await dark.GetFileAsync<DfColormap>("SECBASE.CMP");
			Color target = palette[cmp.PaletteMaps[0][255]];
			Color[] lit = cmp.PaletteMaps[31].Select(x => palette[x]).ToArray();*/
			/*int full = lit.Skip(16).Sum(x => x.R + x.G + x.B);

			for (int i = 0; i < 1; i++) {
				Color[] x = cmp.PaletteMaps[i].Select(x => palette[x]).ToArray();

				for (int j = 32; j < 256; j++) {
					Console.WriteLine(lit[j]);
					Console.WriteLine(x[j]);
				}
			}*/

			/*Color[] transparentPalette = new Color[256];
			Array.Copy(palette, transparentPalette, 256);
			transparentPalette[0] = Color.FromArgb(0);*/

			/*for (int i = 0; i < 32; i++) {
				using FileStream stream = new(Path.Combine(AppContext.BaseDirectory, $"secbase-{i}.pal"), FileMode.Create, FileAccess.Write, FileShare.None);
				using StreamWriter writer = new(stream, Encoding.ASCII);
				await writer.WriteLineAsync("JASC-PAL");
				await writer.WriteLineAsync("0100");
				await writer.WriteLineAsync("256");
				foreach (Color color in cmp.PaletteMaps[i].Select(x => palette[x])) {
					await writer.WriteLineAsync($"{color.R} {color.G} {color.B}");
				}
			}*/

			/*byte[] paletteBuffer = new byte[4 * 256];
			foreach ((Color color, int i) in palette.Select((x, i) => (x, i))) {
				paletteBuffer[i * 4 + 0] = color.B;
				paletteBuffer[i * 4 + 1] = color.G;
				paletteBuffer[i * 4 + 2] = color.R;
				paletteBuffer[i * 4 + 3] = 255;
			}

			byte[] cmpBuffer0 = new byte[4 * 256];
			byte[] map = cmp.PaletteMaps[0];
			for (int i = 0; i < map.Length; i++) {
				Buffer.BlockCopy(paletteBuffer, map[i] * 4, cmpBuffer0, i * 4, 4);
			}
			byte[] cmpBuffer31 = new byte[4 * 256];
			map = cmp.PaletteMaps[31];
			for (int i = 0; i < map.Length; i++) {
				Buffer.BlockCopy(paletteBuffer, map[i] * 4, cmpBuffer31, i * 4, 4);
			}*/

			/*string dir = Path.Combine(AppContext.BaseDirectory, "HOTELRND.GOB");
			if (!Directory.Exists(dir)) {
				Directory.CreateDirectory(dir);
			}*/

			int bmOld = 0;
			int bmNew = 0;

			foreach ((string name, uint size) in textures.Files
				.Where(x => Path.GetExtension(x.name).ToLower() == ".bm")
				.Select(x => (x.name, x.size))) {

				bmOld += (int)size;

				DfBitmap bm = await textures.GetFileAsync<DfBitmap>(name);
				DfBitmap.CompressionModes compression = bm.Compression;
				bm.AutoCompress = true;

				int newSize;
				DfBitmap newBm;
				using (MemoryStream stream = new()) {
					await bm.SaveAsync(stream);

					newSize = (int)stream.Length;

					stream.Position = 0;
					newBm = await DfBitmap.ReadAsync(stream);
				}

				bmNew += newSize;

				bool validate = bm.Framerate == newBm.Framerate && bm.Pages.Count == newBm.Pages.Count &&
					bm.Pages.Zip(newBm.Pages).All(x =>
					x.First.Height == x.Second.Height && x.First.Width == x.Second.Width && x.First.Flags == x.Second.Flags &&
					x.First.Pixels.SequenceEqual(x.Second.Pixels));

				if (size < newSize || !validate) {
					Console.WriteLine(name);

					Console.WriteLine($"Compress: {compression} -> {bm.Compression}");
					Console.WriteLine($"Size: {size} -> {newSize}");
					if (!validate) {
						Console.WriteLine($"Validation failed.");
					}

					Console.WriteLine();
				}
			}

			int fmeOld = 0;
			int fmeNew = 0;
			int waxOld = 0;
			int waxNew = 0;

			foreach ((string name, uint size) in sprites.Files
				.Where(x => Path.GetExtension(x.name).ToLower() == ".wax" || Path.GetExtension(x.name).ToLower() == ".fme")
				.Select(x => (x.name, x.size))) {

				if (Path.GetExtension(name).ToLower() == ".fme") {
					fmeOld += (int)size;

					DfFrame fme = await sprites.GetFileAsync<DfFrame>(name);
					bool compressed = fme.Compressed;
					fme.AutoCompress = true;

					int newSize;
					DfFrame newFme;
					using (MemoryStream stream = new()) {
						await fme.SaveAsync(stream);

						newSize = (int)stream.Length;

						stream.Position = 0;
						newFme = await DfFrame.ReadAsync(stream);
					}

					fmeNew += newSize;

					bool validate = fme.Flip == newFme.Flip && fme.Height == newFme.Height && fme.InsertionPointX == newFme.InsertionPointX &&
						fme.InsertionPointY == newFme.InsertionPointY && fme.Width == newFme.Width && fme.Pixels.SequenceEqual(newFme.Pixels);

					if (size < newSize || !validate) {
						Console.WriteLine(name);

						Console.WriteLine($"Compression: {compressed} -> {fme.Compressed}");
						Console.WriteLine($"Size: {size} -> {newSize}");
						if (!validate) {
							Console.WriteLine($"Validation failed.");
						}

						Console.WriteLine();
					}
				}

				if (Path.GetExtension(name).ToLower() == ".wax") {
					waxOld += (int)size;

					DfWax wax = await sprites.GetFileAsync<DfWax>(name);

					(int waxes, int sequences, int frames, int cells) oldCount = (
						wax.Waxes.Distinct().Count(),
						wax.Waxes.SelectMany(x => x.Sequences).Distinct().Count(),
						wax.Waxes.SelectMany(x => x.Sequences).SelectMany(x => x.Frames).Distinct().Count(),
						wax.Waxes.SelectMany(x => x.Sequences).SelectMany(x => x.Frames).Select(x => x.Pixels).Distinct().Count()
					);

					wax = wax.Reduplicate();
					wax = wax.Deduplicate();

					(int waxes, int sequences, int frames, int cells) newCount = (
						wax.Waxes.Distinct().Count(),
						wax.Waxes.SelectMany(x => x.Sequences).Distinct().Count(),
						wax.Waxes.SelectMany(x => x.Sequences).SelectMany(x => x.Frames).Distinct().Count(),
						wax.Waxes.SelectMany(x => x.Sequences).SelectMany(x => x.Frames).Select(x => x.Pixels).Distinct().Count()
					);

					foreach (DfFrame frame in wax.Waxes.SelectMany(x => x.Sequences).SelectMany(x => x.Frames)) {
						frame.AutoCompress = true;
					}

					int newSize;
					DfWax newWax;
					using (MemoryStream stream = new()) {
						await wax.SaveAsync(stream);

						newSize = (int)stream.Length;

						waxNew += newSize;

						stream.Position = 0;
						newWax = await DfWax.ReadAsync(stream);
					}

					bool validate = wax.Waxes.Count == newWax.Waxes.Count && wax.Waxes.Zip(newWax.Waxes).All(x =>
						x.First.Framerate == x.Second.Framerate && x.First.WorldHeight == x.Second.WorldHeight &&
						x.First.WorldWidth == x.Second.WorldWidth &&
						x.First.Sequences.Count == x.Second.Sequences.Count && x.First.Sequences.Zip(x.Second.Sequences).All(x =>
							x.First.Frames.Count == x.Second.Frames.Count && x.First.Frames.Zip(x.Second.Frames).All(x =>
								x.First.Flip == x.Second.Flip && x.First.Height == x.Second.Height &&
								x.First.InsertionPointX == x.Second.InsertionPointX && x.First.InsertionPointY == x.Second.InsertionPointY &&
								x.First.Width == x.Second.Width && x.First.Pixels.SequenceEqual(x.Second.Pixels)
							)
						));

					if (size < newSize || !validate || oldCount.waxes < newCount.waxes || oldCount.sequences < newCount.sequences || oldCount.frames < newCount.frames || oldCount.cells < newCount.cells) {
						Console.WriteLine(name);

						Console.WriteLine($"Size: {size} -> {newSize}");
						Console.WriteLine($"WAXes: {oldCount.waxes} -> {newCount.waxes}");
						Console.WriteLine($"Sequences: {oldCount.sequences} -> {newCount.sequences}");
						Console.WriteLine($"Frames: {oldCount.frames} -> {newCount.frames}");
						Console.WriteLine($"Cells: {oldCount.cells} -> {newCount.cells}");
						if (!validate) {
							Console.WriteLine($"Validation failed.");
						}

						Console.WriteLine();
					}
				}

				//Raw raw = await dark.GetFileAsync<Raw>(name);
				//await raw.SaveAsync(Path.Combine(dir, name));

				/*if (sounds.Files.First(x => x.name == name).size == 0) {
					continue;
				}

				if (name.ToUpper().EndsWith(".GMD")) {
					DfGameMidi gmd = await sounds.GetFileAsync<DfGameMidi>(name);
					Midi midi = gmd.ToMidi();
					midi.Chunks.Clear();
					await midi.SaveAsync(Path.Combine(dir, $"{Path.GetFileNameWithoutExtension(name)}.MID"));
				}

				if (name.ToUpper().EndsWith(".VOC")) {
					CreativeVoice voc = await sounds.GetFileAsync<CreativeVoice>(name);
					List<CreativeVoice.AudioData> datas = voc.AudioBlocks;
					for (int i = 0; i < datas.Count; i++) {
						CreativeVoice.AudioData data = datas[i];
						if (data.Type == CreativeVoice.BlockTypes.Silence) {
							CreativeVoice.AudioData source;
							if (i > 0) {
								source = datas[i - 1];
							} else {
								source = datas.Skip(i).FirstOrDefault(x => x.Type != CreativeVoice.BlockTypes.Silence);
							}
							if (source == null) {
								datas.Clear();
								break;
							}

							data.BitsPerSample = source.BitsPerSample;
							data.Channels = source.Channels;
							data.Codec = source.Codec;
							data.Frequency = source.Frequency;
							// Only works for 8-bit
							data.Data = Enumerable.Repeat<byte>(0x80, data.SilenceLength * data.Channels * data.BitsPerSample / 8).ToArray();
						}

						if (data.RepeatCount > 1) {
							int count = Math.Min(5, data.RepeatCount);
							for (int j = 1; j < count; j++) {
								datas.InsertRange(i + 1, datas.Skip(data.RepeatStart).Take(i - data.RepeatStart + 1));
							}
							i += (count - 1) * (i - data.RepeatStart + 1);
						}
					}

					int index = 0;
					while (datas.Count > 0) {
						index++;

						CreativeVoice.AudioData[] wavData = datas.TakeWhile(x =>
							x.BitsPerSample == datas[0].BitsPerSample &&
							x.Channels == datas[0].Channels &&
							x.Codec == datas[0].Codec &&
							x.Frequency == datas[0].Frequency
						).ToArray();
						datas.RemoveRange(0, wavData.Length);

						bool showIndex = index > 1 || datas.Count > 0;
						string wavName = $"{Path.GetFileNameWithoutExtension(name)}{(showIndex ? $"-{index}" : "")}.WAV";

						using FileStream stream = new(Path.Combine(dir, wavName), FileMode.Create, FileAccess.Write, FileShare.None);

						Wave wave = new Wave();
						wave.BitsPerSample = wavData[0].BitsPerSample;
						wave.Channels = wavData[0].Channels;
						wave.SampleRate = wavData[0].Frequency;
						wave.Data = wavData.SelectMany(x => x.Data).ToArray();
						await wave.SaveAsync(stream);
					}
				}*/

				/*if (Path.GetExtension(name).ToUpper() == ".BM" && name == "CESUNSET.BM") {
					DfBitmap bm = await textures.GetFileAsync<DfBitmap>(name);

					bm.AutoCompress = false;
					bool canCompress = bm.Pages.Count == 1;

					Console.WriteLine($"- {name}: {bm.Compression} {bm.Pages.Count} {size}");

					DfBitmap.CompressionModes[] modes;
					if (canCompress) {
						modes = Enum.GetValues<DfBitmap.CompressionModes>();
					} else {
						modes = new[] { DfBitmap.CompressionModes.None };
					}

					using MemoryStream stream = new();
					foreach (DfBitmap.CompressionModes compression in modes) {
						stream.Position = 0;
						stream.SetLength(0);

						bm.Compression = compression;

						await bm.SaveAsync(stream);

						stream.Position = 0;

						DfBitmap bm2 = await DfBitmap.ReadAsync(stream);
						bool match = bm.Framerate == bm2.Framerate && bm.Compression == bm2.Compression && bm.Pages.Count == bm2.Pages.Count &&
							bm.Pages.Zip(bm2.Pages).All(x => x.First.Width == x.Second.Width && x.First.Height == x.Second.Height && x.First.Flags == x.Second.Flags);
						if (!match) {
							Console.WriteLine($"X {name}: {compression} {stream.Length}");
						} else {
							bool pixelsMatch = bm.Pages.Zip(bm2.Pages).All(x => x.First.Pixels.SequenceEqual(x.Second.Pixels));
							if (!pixelsMatch) {
								Console.WriteLine($"X {name}: {compression} {stream.Length} BITMAP MISMATCH");

								stream.Position = 0;

								using (FileStream fileStream = new($@"C:\temp\{Path.GetFileNameWithoutExtension(name)}.{compression}.BM", FileMode.Create, FileAccess.Write, FileShare.None)) {
									await stream.CopyToAsync(fileStream);
								}

								using (Bitmap bitmap = new(bm.Pages[0].Width, bm.Pages[0].Height, PixelFormat.Format8bppIndexed)) {
									Color[] colors = palette;
									if (bm.Pages[0].Flags.HasFlag(DfBitmap.Flags.Transparent)) {
										colors = transparentPalette;
									}

									ColorPalette colorPalette = bitmap.Palette;
									for (int i = 0; i < 256; i++) {
										Array.Copy(colors, colorPalette.Entries, 256);
									}
									bitmap.Palette = colorPalette;

									BitmapData bitmapData = bitmap.LockBits(new(Point.Empty, bitmap.Size), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

									for (int y = 0; y < bitmap.Height; y++) {
										Marshal.Copy(bm.Pages[0].Pixels, y * bitmap.Width, bitmapData.Scan0 + (bitmapData.Stride * (bitmap.Height - y - 1)), bitmap.Width);
									}

									bitmap.UnlockBits(bitmapData);
									bitmap.Save(@$"C:\temp\{Path.GetFileNameWithoutExtension(name)}.SRC.PNG", ImageFormat.Png);
								}

								using (Bitmap bitmap = new(bm.Pages[0].Width, bm.Pages[0].Height, PixelFormat.Format8bppIndexed)) {
									Color[] colors = palette;
									if (bm.Pages[0].Flags.HasFlag(DfBitmap.Flags.Transparent)) {
										colors = transparentPalette;
									}

									ColorPalette colorPalette = bitmap.Palette;
									for (int i = 0; i < 256; i++) {
										Array.Copy(colors, colorPalette.Entries, 256);
									}
									bitmap.Palette = colorPalette;

									BitmapData bitmapData = bitmap.LockBits(new(Point.Empty, bitmap.Size), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

									for (int y = 0; y < bitmap.Height; y++) {
										Marshal.Copy(bm2.Pages[0].Pixels, y * bitmap.Width, bitmapData.Scan0 + (bitmapData.Stride * (bitmap.Height - y - 1)), bitmap.Width);
									}

									bitmap.UnlockBits(bitmapData);
									bitmap.Save(@$"C:\temp\{Path.GetFileNameWithoutExtension(name)}.{compression}.PNG", ImageFormat.Png);
								}

							} else {
								Console.WriteLine($"  {name}: {compression} {stream.Length}");
							}
						}
					} 

					/*DfBitmap.Page page = bm.Pages[0];
					int width = page.Width;
					int height = page.Height;

					using (Bitmap bitmap = new(width, height, PixelFormat.Format32bppArgb)) {
						BitmapData bitmapData = bitmap.LockBits(new(Point.Empty, bitmap.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

						for (int y = 0; y < height; y++) {
							for (int x = 0; x < width; x++) {
								byte color = page.Pixels[(height - y - 1) * width + x];
								if (color > 0 || !page.Flags.HasFlag(DfBitmap.Flags.Transparent)) {
									Marshal.Copy(cmpBuffer0, color * 4, bitmapData.Scan0 + (y * bitmapData.Stride) + (x * 4), 4);
								}
							}
						}

						bitmap.UnlockBits(bitmapData);
						bitmap.Save(Path.Combine(dir, $"{Path.GetFileNameWithoutExtension(name)}-0.PNG"), ImageFormat.Png);
					}

					using (Bitmap bitmap = new(width, height, PixelFormat.Format32bppArgb)) {
						BitmapData bitmapData = bitmap.LockBits(new(Point.Empty, bitmap.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

						for (int y = 0; y < height; y++) {
							for (int x = 0; x < width; x++) {
								byte color = page.Pixels[(height - y - 1) * width + x];
								if (color > 0 || !page.Flags.HasFlag(DfBitmap.Flags.Transparent)) {
									Marshal.Copy(cmpBuffer31, color * 4, bitmapData.Scan0 + (y * bitmapData.Stride) + (x * 4), 4);
								}
							}
						}

						bitmap.UnlockBits(bitmapData);
						bitmap.Save(Path.Combine(dir, $"{Path.GetFileNameWithoutExtension(name)}-31.PNG"), ImageFormat.Png);*/
			}

			Console.WriteLine($"BMs: {bmOld} -> {bmNew} ({1 - (bmNew / (float)bmOld):p})");
			Console.WriteLine($"FMEs: {fmeOld} -> {fmeNew} ({1 - (fmeNew / (float)fmeOld):p})");
			Console.WriteLine($"WAXs: {waxOld} -> {waxNew} ({1 - (waxNew / (float)waxOld):p})");

			//}

			/*Wax wax = await sprites.GetFileAsync<Wax>(name);

			Fme fme = wax.Waxes[0].Sequences[0].Frames[0];

			byte[] pixels = fme.Pixels;
			int width = fme.Width;
			int height = fme.Height;

			using Bitmap bitmap = new(width, height, PixelFormat.Format32bppArgb);
			BitmapData bitmapData = bitmap.LockBits(new(Point.Empty, bitmap.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					byte color = pixels[(height - y - 1) * width + x];
					if (color > 0) {
						Marshal.Copy(paletteBuffer, color * 4, bitmapData.Scan0 + (y * bitmapData.Stride) + (x * 4), 4);
					}
				}
			}

			bitmap.UnlockBits(bitmapData);

			bitmap.Save(Path.Combine(dir, $"{Path.GetFileNameWithoutExtension(name)}.PNG"), ImageFormat.Png);*/
			//}

			/*Process.Start(new ProcessStartInfo() {
				FileName = dir,
				UseShellExecute = true
			});*/
		}
	}
}
