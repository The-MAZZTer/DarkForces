using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MZZT.Extensions {
	public static class IEnumerableExtesnions {
		public static void Deconstruct<T>(this IEnumerable<T> me,
			out T x1, out T x2) {

			x1 = default;
			x2 = default;
			foreach ((T x, int i) in me.Take(2).Select((x, i) => (x, i))) {
				switch (i) {
					case 0:
						x1 = x;
						break;
					case 1:
						x2 = x;
						break;
				}
			}
		}
		public static void Deconstruct<T>(this IEnumerable<T> me,
			out T x1, out T x2, out T x3) {

			x1 = default;
			x2 = default;
			x3 = default;
			foreach ((T x, int i) in me.Take(3).Select((x, i) => (x, i))) {
				switch (i) {
					case 0:
						x1= x;
						break;
					case 1:
						x2 = x;
						break;
					case 2:
						x3 = x;
						break;
				}
			}
		}
		public static void Deconstruct<T>(this IEnumerable<T> me,
			out T x1, out T x2, out T x3, out T x4) {

			x1 = default;
			x2 = default;
			x3 = default;
			x4 = default;
			foreach ((T x, int i) in me.Take(4).Select((x, i) => (x, i))) {
				switch (i) {
					case 0:
						x1 = x;
						break;
					case 1:
						x2 = x;
						break;
					case 2:
						x3 = x;
						break;
					case 3:
						x4 = x;
						break;
				}
			}
		}
		public static void Deconstruct<T>(this IEnumerable<T> me,
			out T x1, out T x2, out T x3, out T x4, out T x5) {

			x1 = default;
			x2 = default;
			x3 = default;
			x4 = default;
			x5 = default;
			foreach ((T x, int i) in me.Take(5).Select((x, i) => (x, i))) {
				switch (i) {
					case 0:
						x1 = x;
						break;
					case 1:
						x2 = x;
						break;
					case 2:
						x3 = x;
						break;
					case 3:
						x4 = x;
						break;
					case 4:
						x5 = x;
						break;
				}
			}
		}
		public static void Deconstruct<T>(this IEnumerable<T> me,
			out T x1, out T x2, out T x3, out T x4, out T x5, out T x6) {

			x1 = default;
			x2 = default;
			x3 = default;
			x4 = default;
			x5 = default;
			x6 = default;
			foreach ((T x, int i) in me.Take(6).Select((x, i) => (x, i))) {
				switch (i) {
					case 0:
						x1 = x;
						break;
					case 1:
						x2 = x;
						break;
					case 2:
						x3 = x;
						break;
					case 3:
						x4 = x;
						break;
					case 4:
						x5 = x;
						break;
					case 5:
						x6 = x;
						break;
				}
			}
		}
		public static void Deconstruct<T>(this IEnumerable<T> me,
			out T x1, out T x2, out T x3, out T x4, out T x5, out T x6, out T x7) {

			x1 = default;
			x2 = default;
			x3 = default;
			x4 = default;
			x5 = default;
			x6 = default;
			x7 = default;
			foreach ((T x, int i) in me.Take(7).Select((x, i) => (x, i))) {
				switch (i) {
					case 0:
						x1 = x;
						break;
					case 1:
						x2 = x;
						break;
					case 2:
						x3 = x;
						break;
					case 3:
						x4 = x;
						break;
					case 4:
						x5 = x;
						break;
					case 5:
						x6 = x;
						break;
					case 6:
						x7 = x;
						break;
				}
			}
		}
		public static void Deconstruct<T>(this IEnumerable<T> me,
			out T x1, out T x2, out T x3, out T x4, out T x5, out T x6, out T x7, out T x8) {

			x1 = default;
			x2 = default;
			x3 = default;
			x4 = default;
			x5 = default;
			x6 = default;
			x7 = default;
			x8 = default;
			foreach ((T x, int i) in me.Take(8).Select((x, i) => (x, i))) {
				switch (i) {
					case 0:
						x1 = x;
						break;
					case 1:
						x2 = x;
						break;
					case 2:
						x3 = x;
						break;
					case 3:
						x4 = x;
						break;
					case 4:
						x5 = x;
						break;
					case 5:
						x6 = x;
						break;
					case 6:
						x7 = x;
						break;
					case 7:
						x8 = x;
						break;
				}
			}
		}
		public static void Deconstruct<T>(this IEnumerable<T> me,
			out T x1, out T x2, out T x3, out T x4, out T x5, out T x6, out T x7, out T x8, out T x9) {

			x1 = default;
			x2 = default;
			x3 = default;
			x4 = default;
			x5 = default;
			x6 = default;
			x7 = default;
			x8 = default;
			x9 = default;
			foreach ((T x, int i) in me.Take(9).Select((x, i) => (x, i))) {
				switch (i) {
					case 0:
						x1 = x;
						break;
					case 1:
						x2 = x;
						break;
					case 2:
						x3 = x;
						break;
					case 3:
						x4 = x;
						break;
					case 4:
						x5 = x;
						break;
					case 5:
						x6 = x;
						break;
					case 6:
						x7 = x;
						break;
					case 7:
						x8 = x;
						break;
					case 8:
						x9 = x;
						break;
				}
			}
		}
		public static void Deconstruct<T>(this IEnumerable<T> me,
			out T x1, out T x2, out T x3, out T x4, out T x5, out T x6, out T x7, out T x8, out T x9, out T x10) {

			x1 = default;
			x2 = default;
			x3 = default;
			x4 = default;
			x5 = default;
			x6 = default;
			x7 = default;
			x8 = default;
			x9 = default;
			x10 = default;
			foreach ((T x, int i) in me.Take(10).Select((x, i) => (x, i))) {
				switch (i) {
					case 0:
						x1 = x;
						break;
					case 1:
						x2 = x;
						break;
					case 2:
						x3 = x;
						break;
					case 3:
						x4 = x;
						break;
					case 4:
						x5 = x;
						break;
					case 5:
						x6 = x;
						break;
					case 6:
						x7 = x;
						break;
					case 7:
						x8 = x;
						break;
					case 8:
						x9 = x;
						break;
					case 9:
						x10 = x;
						break;
				}
			}
		}
	}
}
