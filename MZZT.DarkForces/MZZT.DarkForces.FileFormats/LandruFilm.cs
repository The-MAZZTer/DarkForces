using MZZT.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// A Landru FILM file.
	/// </summary>
	public class LandruFilm : DfFile<LandruFilm> {
		/// <summary>
		/// Magic number in header.
		/// </summary>
		public const short MAGIC = 4;

		/// <summary>
		/// A FILM file header.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct Header {
			/// <summary>
			/// The magic number.
			/// </summary>
			public short Magic;
			/// <summary>
			/// The time length of the file in 1/10s of a second.
			/// </summary>
			public ushort FilmLength;
			/// <summary>
			/// The number of objects in the file.
			/// </summary>
			public ushort ObjectCount;

			/// <summary>
			/// If the header is valid.
			/// </summary>
			public bool IsMagicValid => this.Magic == MAGIC;
		}

		/// <summary>
		/// The header for an object.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
		public struct ObjectHeader {
			/// <summary>
			/// The object type as a char array.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public char[] RawType;
			/// <summary>
			/// The object name as a char array.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public char[] RawName;
			/// <summary>
			/// The size of the object.
			/// </summary>
			public int TotalLength;
			/// <summary>
			/// The type of the object.
			/// </summary>
			public BlockTypes BlockType;
			/// <summary>
			/// The number of cmmands the object has.
			/// </summary>
			public ushort NumberOfCommands;
			/// <summary>
			/// The size of the object's block.
			/// </summary>
			public ushort BlockLength;

			/// <summary>
			/// The object type.
			/// </summary>
			public string Type {
				get => new(this.RawType);
				set => this.RawType = value.ToCharArray();
			}
			/// <summary>
			/// The object name.
			/// </summary>
			public string Name {
				get => new(this.RawName);
				set => this.RawName = value.ToCharArray();
			}
		}

		/// <summary>
		/// The different blocks in the film.
		/// </summary>
		public enum BlockTypes : short {
			/// <summary>
			/// End cutscene marker.
			/// </summary>
			End = 1,
			/// <summary>
			/// The first entry in a FILM.
			/// </summary>
			View = 2,
			/// <summary>
			/// Display an image, animation, or custom action.
			/// </summary>
			DeltAnimCust = 3,
			/// <summary>
			/// Load palette.
			/// </summary>
			Pltt = 4,
			/// <summary>
			/// Play VOIC file.
			/// </summary>
			Voic = 5
		}

		/// <summary>
		/// A command to execute during the cutscene.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct CommandHeader {
			/// <summary>
			/// The length of the command.
			/// </summary>
			public ushort CommandLength;
			/// <summary>
			/// The command to execute.
			/// </summary>
			public CommandTypes Command;
		}

		public enum CommandTypes : short {
			/// <summary>
			/// Unknown.
			/// </summary>
			Unused00,
			/// <summary>
			/// Unknown.
			/// </summary>
			Unused01,
			/// <summary>
			/// End cutscene.
			/// </summary>
			End,
			/// <summary>
			/// Delay until a specific time.
			/// </summary>
			Time,
			/// <summary>
			/// Move an object to coordinates.
			/// </summary>
			Move,
			/// <summary>
			/// Change the speed an object moves.
			/// </summary>
			Speed,
			/// <summary>
			/// Changes the z position of an object.
			/// </summary>
			Layer,
			/// <summary>
			/// Displays a frame of an ANIM.
			/// </summary>
			Frame,
			/// <summary>
			/// Animate an ANIM.
			/// </summary>
			Animate,
			/// <summary>
			/// Triggers a music cue.
			/// </summary>
			Cue,
			/// <summary>
			/// Unknown.
			/// </summary>
			Var,
			/// <summary>
			/// Clips an image.
			/// </summary>
			Window,
			/// <summary>
			/// Unknown.
			/// </summary>
			Unknown0C,
			Switch,
			/// <summary>
			/// Unknown.
			/// </summary>
			Unknown0E,
			/// <summary>
			/// Loads a palette.
			/// </summary>
			Palette,
			/// <summary>
			/// Unknown.
			/// </summary>
			Unknown10,
			/// <summary>
			/// Unknown.
			/// </summary>
			Unknown11,
			/// <summary>
			/// Cuts between two scenes.
			/// </summary>
			Cut,
			/// <summary>
			/// Unknown.
			/// </summary>
			Unknown13,
			/// <summary>
			/// Stops a loop.
			/// </summary>
			Loop,
			/// <summary>
			/// Unknown.
			/// </summary>
			Unknown15,
			/// <summary>
			/// Unknown.
			/// </summary>
			Unknown16,
			/// <summary>
			/// Unknown.
			/// </summary>
			Unknown17,
			/// <summary>
			/// Unknown.
			/// </summary>
			Preload,
			/// <summary>
			/// Plays a sound.
			/// </summary>
			Sound,
			/// <summary>
			/// Unknown.
			/// </summary>
			Unknown1A,
			/// <summary>
			/// Unknown.
			/// </summary>
			Unknown1B,
			/// <summary>
			/// Plays a sound in stereo.
			/// </summary>
			Stereo
		}

		private Header header;

		/// <summary>
		/// The time length of the file in 1/10s of a second.
		/// </summary>
		public ushort FilmLength {
			get => this.header.FilmLength;
			set => this.header.FilmLength = value;
		}
		
		/// <summary>
		/// The objects in the FILM.
		/// </summary>
		public List<FilmObject> Objects { get; } = new();

		/// <summary>
		/// A FILM object.
		/// </summary>
		public class FilmObject {
			internal ObjectHeader header;

			/// <summary>
			/// The type of object.
			/// </summary>
			public string Type {
				get => this.header.Type;
				set => this.header.Type = value;
			}
			/// <summary>
			/// The name of the object.
			/// </summary>
			public string Name {
				get => this.header.Name;
				set => this.header.Name = value;
			}

			/// <summary>
			/// The commands run on the object.
			/// </summary>
			public List<Command> Commands { get; } = new();
		}

		/// <summary>
		/// A command to run on an object.
		/// </summary>
		public class Command {
			internal CommandHeader header;

			/// <summary>
			/// The type of command.
			/// </summary>
			public CommandTypes Type {
				get => this.header.Command;
				set => this.header.Command = value;
			}

			/// <summary>
			/// The command parameters.
			/// </summary>
			public short[] Parameters { get; set; }
		}

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			this.header = await stream.ReadAsync<Header>();
			if (!this.header.IsMagicValid) {
				throw new FormatException("FILM header not found!");
			}

			this.Objects.Clear();
			for (int i = 0; i < this.header.ObjectCount; i++) {
				FilmObject obj;
				this.Objects.Add(obj = new() {
					header = await stream.ReadAsync<ObjectHeader>()
				});

				for (int j = 0; j < obj.header.NumberOfCommands; j++) {
					Command command;
					obj.Commands.Add(command = new() {
						header = await stream.ReadAsync<CommandHeader>()
					});

					int parameters = (command.header.CommandLength - 4) / 2;
					byte[] buffer = new byte[parameters * 2];
					await stream.ReadAsync(buffer, 0, buffer.Length);

					command.Parameters = Enumerable.Range(0, parameters).Select(x => BitConverter.ToInt16(buffer, x * 2)).ToArray();
				}
			}

			ObjectHeader end = await stream.ReadAsync<ObjectHeader>(Endianness.Keep, 0x12);
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			this.header.Magic = MAGIC;
			this.header.ObjectCount = (ushort)this.Objects.Count;

			await stream.WriteAsync(this.header);

			foreach (FilmObject obj in this.Objects) {
				switch (obj.header.Type) {
					case "END":
						obj.header.BlockType = BlockTypes.End;
						obj.header.Name = "UNTITLED";
						break;
					case "VIEW":
						obj.header.BlockType = BlockTypes.View;
						obj.header.Name = "UNTITLED";
						break;
					case "DELT":
					case "ANIM":
						obj.header.BlockType = BlockTypes.DeltAnimCust;
						break;
					case "CUST":
						obj.header.BlockType = BlockTypes.DeltAnimCust;
						obj.header.Name = "CUSTOM";
						break;
					case "PLTT":
						obj.header.BlockType = BlockTypes.Pltt;
						break;
					case "VOIC":
						obj.header.BlockType = BlockTypes.Voic;
						break;
					default:
						this.AddWarning($"Invalid block type {obj.header.Type}!");
						continue;
				}
				obj.header.NumberOfCommands = (ushort)obj.Commands.Count;
				obj.header.BlockLength = (ushort)obj.Commands.Sum(x => x.Parameters.Length * 2 + 4);
				obj.header.TotalLength = obj.header.BlockLength + 0x16;

				await stream.WriteAsync(obj.header);

				foreach (Command command in obj.Commands) {
					command.header.CommandLength = (ushort)(command.Parameters.Length * 2 + 4);

					await stream.WriteAsync(command.header);

					foreach (short parameter in command.Parameters) {
						await stream.WriteAsync(BitConverter.GetBytes(parameter), 0, 2);
					}
				}
			}

			await stream.WriteAsync(new ObjectHeader() {
				Type = "END",
				Name = "UNTITLED",
				TotalLength = 0x12,
				BlockType = BlockTypes.End
			}, Endianness.Keep, 0x12);
		}
	}
}
