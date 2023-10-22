using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace MZZT {
	/// <summary>
	/// The endianness for BinarySerializer to use when reading/writing data.
	/// </summary>
	public enum Endianness {
		/// <summary>
		/// Uses the system endianness for reading/writing.
		/// </summary>
		Keep,
		/// <summary>
		/// Forces big endianness for reading/writing.
		/// </summary>
		Big,
		/// <summary>
		/// Forces little endianness for reading/writing.
		/// </summary>
		Little
	}

	/// <summary>
	/// Allows for serializing and deserializing structs to byte arrays.
	/// </summary>
	public static class BinarySerializer {
		private static void AdjustEndianness(Type type, Endianness endianness, byte[] data, int offset = 0,
			int length = -1) {

			// If the endianness is already as desired, do nothing.
			if (endianness == Endianness.Keep || (endianness == Endianness.Little) == BitConverter.IsLittleEndian) {
				return;
			}

			// Does the type define the size of a char? We should take note of that.
			StructLayoutAttribute attr = type.StructLayoutAttribute;

			CharSet charSet = attr?.CharSet ?? CharSet.Auto;
			int charSize = charSet switch {
				CharSet.None => 1,
				CharSet.Ansi => 1,
				CharSet.Unicode => 2,
				_ => 2
			};

			foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)) {
				MarshalAsAttribute fieldAttr = field.GetCustomAttribute<MarshalAsAttribute>();

				int fieldOffset = Marshal.OffsetOf(type, field.Name).ToInt32();
				fieldOffset += offset;
				// If the data won't fit in the byte array, skip copying it.
				if (length >= 0 && fieldOffset >= offset + length) {
					continue;
				}

				Type fieldType = field.FieldType;
				if (fieldType.IsEnum) {
					fieldType = Enum.GetUnderlyingType(fieldType);
				}

				// For arrays and strings inline, don't swap byte order.
				bool swap = true;
				if (fieldAttr != null) {
					swap = fieldAttr.Value switch {
						UnmanagedType.ByValArray => false,
						UnmanagedType.ByValTStr => false,
						_ => true
					};
				}

				int size;
				if (!swap) {
					if (fieldType.IsArray) {
						// If it's an array, iterate through each element and swap the bytes.
						Type elementType = fieldType.GetElementType();
						int elementSize = elementType == typeof(char) ? charSize : Marshal.SizeOf(elementType);
						if (elementSize > 1) {
							size = fieldAttr.SizeConst * elementSize;
							for (int i = fieldOffset; i <= offset + length - elementSize && i < size; i += elementSize) {
								Array.Reverse(data, i, elementSize);
							}
						}
					}
					continue;
				}

				size = fieldType == typeof(char) ? charSize : Marshal.SizeOf(fieldType);

				// If the type is complex, we should recurse.
				bool subFields = !fieldType.IsValueType && fieldType
					.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
					.Any();
				if (subFields) {
					AdjustEndianness(fieldType, endianness, data, fieldOffset,
						Math.Min(offset + length - fieldOffset, size));
					continue;
				}

				Array.Reverse(data, fieldOffset, size);
			}
		}

		/// <summary>
		/// Reads a struct from a byte array.
		/// </summary>
		/// <typeparam name="T">The type of the struct to read.</typeparam>
		/// <param name="buffer">The byte array to read it from.</param>
		/// <param name="offset">The offset of the read operation in the byte array.</param>
		/// <param name="endianness">The endianness the data is stored with in the byte array.</param>
		/// <returns>An instance of the struct with the read data.</returns>
		public static T Deserialize<T>(byte[] buffer, int offset = 0, Endianness endianness = Endianness.Keep) {
			int size = Marshal.SizeOf<T>();
			AdjustEndianness(typeof(T), endianness, buffer, offset, size);

			GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			try {
				return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject() + offset);
			} finally {
				handle.Free();
			}
		}

		/// <summary>
		/// Write a struct to a byte array.
		/// </summary>
		/// <typeparam name="T">The type of the struct to write.</typeparam>
		/// <param name="value">The value to write.</param>
		/// <param name="endianness">The endianness to use when writing the data.</param>
		/// <returns>The created byte array.</returns>
		public static byte[] Serialize<T>(T value, Endianness endianness = Endianness.Keep) {
			int size = Marshal.SizeOf<T>();
			byte[] buffer = new byte[size];
			GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			try {
				Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), false);
			} finally {
				handle.Free();
			}

			AdjustEndianness(typeof(T), endianness, buffer, 0, size);
			return buffer;
		}
	}
}
