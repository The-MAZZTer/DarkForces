using MZZT.FileFormats;
using System.Collections.Generic;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// A warning generated when loading or saving a file.
	/// </summary>
	public struct Warning {
		/// <summary>
		/// The line number of the warning (if applicable).
		/// </summary>
		public int Line;
		/// <summary>
		/// The warning message.
		/// </summary>
		public string Message;
	}

	/// <summary>
	/// A common interface for all DF file types.
	/// </summary>
	public interface IDfFile {
		/// <summary>
		/// Warnings generated the last time the file was loaded or saved.
		/// </summary>
		IEnumerable<Warning> Warnings { get; }
	}

	/// <summary>
	/// Abstract generic class for all DF file types.
	/// </summary>
	/// <typeparam name="T">The concrete file type.</typeparam>
	public abstract class DfFile<T> : File<T>, IDfFile where T : File<T>, new() {
		private readonly List<Warning> warnings = new();
		/// <summary>
		/// Warnings generated the last time the file was loaded or saved.
		/// </summary>
		public IEnumerable<Warning> Warnings => this.warnings;
		protected void ClearWarnings() {
			this.warnings.Clear();
		}
		protected virtual void AddWarning(string warning, int line = 0) {
			this.warnings.Add(new() {
				Line = line,
				Message = warning
			});
		}
	}
}
