using static MZZT.DarkForces.FileFormats.DfLevel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using MZZT.DarkForces.FileFormats;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace MZZT.DarkForces {
	/// <summary>
	/// Creates the geometry for floors and ceilings.
	/// </summary>
	public class FloorCeilingRenderer : MonoBehaviour {
		// Based off of sample code from https://www.habrador.com/tutorials/math/5-line-line-intersection/
		private static bool IsIntersecting(Vector2 l1_start, Vector2 l1_end, Vector2 l2_start, Vector2 l2_end) {
			//Direction of the lines
			Vector2 l1_dir = (l1_end - l1_start).normalized;
			Vector2 l2_dir = (l2_end - l2_start).normalized;

			//If we know the direction we can get the normal vector to each line
			Vector2 l1_normal = new(-l1_dir.y, l1_dir.x);
			Vector2 l2_normal = new(-l2_dir.y, l2_dir.x);


			//Step 1: Rewrite the lines to a general form: Ax + By = k1 and Cx + Dy = k2
			//The normal vector is the A, B
			float A = l1_normal.x;
			float B = l1_normal.y;

			float C = l2_normal.x;
			float D = l2_normal.y;

			//To get k we just use one point on the line
			float k1 = (A * l1_start.x) + (B * l1_start.y);
			float k2 = (C * l2_start.x) + (D * l2_start.y);

			// Rework this bit for Dark Forces.
			// We don't need to check if the lines are the same unless they are also parallel.
			// If they are the same we want to see if the segments overlap, since if so we don't want to use 
			// this line as a polygon line.

			//Step 2: are the lines parallel? -> no solutions
			if (IsParallel(l1_normal, l2_normal)) {
				//Step 3: are the lines the same line? -> infinite amount of solutions
				//Pick one point on each line and test if the vector between the points is orthogonal to one of the normals
				if (IsOrthogonal(l1_start - l2_start, l1_normal)) {
					//Debug.Log("Same line so infinite amount of solutions!");
					if (IsBetween(l1_start, l1_end, l2_start) || IsBetween(l1_start, l1_end, l2_end) ||
						IsBetween(l2_start, l2_end, l1_start) || IsBetween(l2_start, l2_end, l1_end)) {

						return true;
					}
				}

				//Debug.Log("The lines are parallel so no solutions!");

				return false;
			}

			//Step 4: calculate the intersection point -> one solution
			float x_intersect = (D * k1 - B * k2) / (A * D - B * C);
			float y_intersect = (-C * k1 + A * k2) / (A * D - B * C);

			Vector2 intersectPoint = new(x_intersect, y_intersect);


			//Step 5: but we have line segments so we have to check if the intersection point is within the segment
			return IsBetween(l1_start, l1_end, intersectPoint) && IsBetween(l2_start, l2_end, intersectPoint);
		}

		//Are 2 vectors parallel?
		private static bool IsParallel(Vector2 v1, Vector2 v2) {
			//2 vectors are parallel if the angle between the vectors are 0 or 180 degrees
			if (Vector2.Angle(v1, v2) == 0f || Vector2.Angle(v1, v2) == 180f) {
				return true;
			}

			return false;
		}

		//Are 2 vectors orthogonal?
		private static bool IsOrthogonal(Vector2 v1, Vector2 v2) {
			//2 vectors are orthogonal is the dot product is 0
			//We have to check if close to 0 because of floating numbers
			return Mathf.Abs(Vector2.Dot(v1, v2)) < 0.000001f;
		}

		//Is a point c between 2 other points a and b?
		private static bool IsBetween(Vector2 a, Vector2 b, Vector2 c) {
			bool isBetween = false;

			//Entire line segment
			Vector2 ab = b - a;
			//The intersection and the first point
			Vector2 ac = c - a;

			//Need to check 2 things: 
			//1. If the vectors are pointing in the same direction = if the dot product is positive
			//2. If the length of the vector between the intersection and the first point is smaller than the entire line
			if (Vector2.Dot(ab, ac) > 0f && ab.sqrMagnitude >= ac.sqrMagnitude) {
				isBetween = true;
			}

			return isBetween;
		}

		private static (bool, float) TestCandidateVertices(IEnumerable<List<Vertex>> shapes, Vertex previous, Vertex[] candidate, Vertex next) {
			// This function determines if a candidate polygon is entirely within the bounds of the sector.

			Vector2[] vectors = candidate.Select(x => x.Position.ToUnity()).ToArray();
			
			// These three points are three consecutive points around the outer edge of the sector
			// (specifically the parts not yet made into floor/ceiling).

			// Make sure this shape isn't inside out. All of the angles will be less than 180 degrees if so.
			Vector2 wall1dir = vectors[1] - vectors[0];
			Vector2 wall2dir = vectors[2] - vectors[1];
			float angle = Vector2.SignedAngle(wall1dir, wall2dir);
			if (angle > 0 && angle < 180) {
				return (false, 0);
			}

			// Now we want to determine if the new line (between vectors 2 and 0) travels outside the sector.
			// We'll start by looking at vector 0's end of the line.
			// Vector 0 already connects to vector 1 and previous. We'll get that angle below.
			// First get the angle between vector 1 and vector 2 from vector 0.

			wall1dir = vectors[1] - vectors[0];
			wall2dir = vectors[2] - vectors[0];
			float angle1 = Vector2.SignedAngle(wall2dir, wall1dir);
			if (angle1 < 0) {
				angle1 += 360;
			}

			// Next get the angle of the existing known good lines.
			Vector2 vNext = previous.Position.ToUnity();
			Vector2 wall3dir = vNext - vectors[0];
			float angle2 = Vector2.SignedAngle(wall3dir, wall1dir);
			if (angle2 < 0) {
				angle2 += 360;
			}

			// This determines if the new line would be between previous and vector 1's angles.
			// If so the line goes inside the sector. Otherwise it goes outside.
			if (angle2 < angle1) {
				return (false, 0);
			}

			// Do the same thing fromn the vector 2 side.
			// First get the angle between the new line and the vector 1 line.
			wall1dir = vectors[0] - vectors[2];
			wall2dir = vectors[1] - vectors[2];
			angle1 = Vector2.SignedAngle(wall2dir, wall1dir);
			if (angle1 < 0) {
				angle1 += 360;
			}

			// Next the angle between vector 1 and next.
			vNext = next.Position.ToUnity();
			wall3dir = vNext - vectors[2];
			angle2 = Vector2.SignedAngle(wall2dir, wall3dir);
			if (angle2 < 0) {
				angle2 += 360;
			}

			// We expect the vector 0 line to be in between the vector 1 line and the next line.
			// If so the line is inside the sector.
			if (angle2 < angle1) {
				return (false, 0);
			}

			// Now we just need to be sure the new line does not stray outside the sector during its transit across it.
			// We determine this by seeing if it crosses any lines of the sector.

			Vector2 start1 = vectors[2];
			Vector2 end1 = vectors[0];
			// Iterate through all borders of the sector.
			foreach (List<Vertex> shape in shapes) {
				for (int i = 0; i < shape.Count; i++) {
					/*if (i == (pos + outerShape.Count - 1) % outerShape.Count | i == pos % outerShape.Count || i == (pos + 1) % outerShape.Count || i == (pos + 2) % outerShape.Count) {
						continue;
					}*/
					Vertex start2 = shape[i];
					Vertex end2;
					if (i + 1 == shape.Count) {
						end2 = shape[0];
					} else {
						end2 = shape[i + 1];
					}
					if (candidate.Contains(start2) || candidate.Contains(end2)) {
						// We already tested these before.
						continue;
					}

					// Check to see if the new line crosses.
					if (IsIntersecting(start1, end1, start2.Position.ToUnity(), end2.Position.ToUnity())) {
						return (false, 0);
					}
				}
			}

			return (true, angle);
		}

		private static (bool, float) TestCandidateVertices2(IEnumerable<List<Vertex>> shapes,
			Vertex previous, Vertex[] candidate, Vertex next, Vertex previous2, Vertex next2) {

			// This is an alternate method specifically for dealing with subsectors.

			Vector2[] vectors = candidate.Select(x => x.Position.ToUnity()).ToArray();

			// vector 2 is a vertex on the outer wall of the sector, while the other two are on an inner subsector.
			// previous 2 and next 2 are the neighbors of vector 2.
			// previous and next are the neighbors of the subsector vertices.

			// This first part works similarly to the beginning of the other function.

			Vector2 wall1dir = vectors[1] - vectors[0];
			Vector2 wall2dir = vectors[2] - vectors[1];
			float angle = Vector2.SignedAngle(wall1dir, wall2dir);
			if (angle > 0 && angle < 180) {
				return (false, 0);
			}

			wall1dir = vectors[1] - vectors[0];
			wall2dir = vectors[2] - vectors[0];
			float angle1 = Vector2.SignedAngle(wall2dir, wall1dir);
			if (angle1 < 0) {
				angle1 += 360;
			}

			Vector2 neighbor = previous.Position.ToUnity();
			Vector2 wall3dir = neighbor - vectors[0];
			float angle2 = Vector2.SignedAngle(wall3dir, wall1dir);
			if (angle2 < 0) {
				angle2 += 360;
			}
			if (angle2 < angle1) {
				return (false, 0);
			}

			wall1dir = vectors[0] - vectors[1];
			wall2dir = vectors[2] - vectors[1];
			angle1 = Vector2.SignedAngle(wall1dir, wall2dir);
			if (angle1 < 0) {
				angle1 += 360;
			}

			neighbor = next.Position.ToUnity();
			wall3dir = neighbor - vectors[1];
			angle2 = Vector2.SignedAngle(wall1dir, wall3dir);
			if (angle2 < 0) {
				angle2 += 360;
			}
			if (angle2 < angle1) {
				return (false, 0);
			}

			// Now the changes start.
			// We want to join an inner subsector cutout to the outer shape.
			// This will make the remaining algorithm much simpler with one contiguous shape.
			// If we don't do this a candidate polygon may contain a subsector and we'd never know it.

			// Get the angle of the sector outer wall join.
			neighbor = previous2.Position.ToUnity();
			Vector2 neighbor2 = next2.Position.ToUnity();
			wall1dir = vectors[2] - neighbor;
			wall2dir = vectors[2] - neighbor2;
			angle1 = Vector2.SignedAngle(wall1dir, wall2dir);
			if (angle1 < 0) {
				angle1 += 360;
			}

			// Ensure the lines between the outer and inner walls don't start out by leaving the sector.

			wall3dir = vectors[2] - vectors[0];

			angle2 = Vector2.SignedAngle(wall1dir, wall3dir);
			if (angle2 < 0) {
				angle2 += 360;
			}
			if (angle2 > angle1) {
				return (false, 0);
			}

			wall3dir = vectors[2] - vectors[1];

			angle2 = Vector2.SignedAngle(wall1dir, wall3dir);
			if (angle2 < 0) {
				angle2 += 360;
			}
			if (angle2 > angle1) {
				return (false, 0);
			}

			// This is similar to the other function but we have two candidate new lines between the outer vertex
			// and inner wall that need testing.

			Vector2 start1 = vectors[2];
			Vector2 end1a = vectors[0];
			Vector2 end1b = vectors[1];
			foreach (List<Vertex> shape in shapes) {
				for (int i = 0; i < shape.Count; i++) {
					/*if (i == (pos + outerShape.Count - 1) % outerShape.Count | i == pos % outerShape.Count || i == (pos + 1) % outerShape.Count || i == (pos + 2) % outerShape.Count) {
						continue;
					}*/
					Vertex start2 = shape[i];
					Vertex end2;
					if (i + 1 == shape.Count) {
						end2 = shape[0];
					} else {
						end2 = shape[i + 1];
					}
					if (candidate.Contains(start2) || candidate.Contains(end2)) {
						continue;
					}

					if (IsIntersecting(start1, end1a,
						start2.Position.ToUnity(),
						end2.Position.ToUnity())) {

						return (false, 0);
					}

					if (IsIntersecting(start1, end1b,
						start2.Position.ToUnity(),
						end2.Position.ToUnity())) {

						return (false, 0);
					}
				}
			}

			return (true, angle);
		}

		/// <summary>
		/// Generate geometry for a sector.
		/// </summary>
		/// <param name="sector">The sector.</param>
		/// <returns>An array of vertex indices, in groups of three, defining triangles for the polygons.</returns>
		public static int[] SplitIntoFloorTris(Sector sector) {
			// Allow quick lookup of a wall based on its left vertex.
			Dictionary<Vertex, int> map = sector.Walls
				.GroupBy(x => x.LeftVertex)
				.ToDictionary(x => x.Key, x => sector.Walls.IndexOf(x.First()));

			// Shapes are the outer perimeter of the sector as well as any subsectors.
			// Shapes have contiguous walls.
			List<List<Wall>> shapes = new();
			List<Wall> pendingWalls = sector.Walls.ToList();

			// We want to know what we're dealing with so figure out the shapes we can make with the walls.

			// First do some easy checks for invalid or duplicate walls we can safely remove from consideration.
			for (int i = 0; i < pendingWalls.Count; i++) {
				Wall wall = pendingWalls[i];
				// Simple check for invalid wall.
				if (wall.LeftVertex == wall.RightVertex) {
					// Ignore it.
					pendingWalls.Remove(wall);
					i--;
					continue;
				}

				// Walls that occupy the same physical space should all be removed, including the current.
				// Since that likely means the sector is on both sides of the wall thus the floor is contiguous.
				if (pendingWalls.Skip(i + 1).Where(x =>
					(x.LeftVertex == wall.RightVertex && x.RightVertex == wall.LeftVertex) ||
					(x.LeftVertex == wall.LeftVertex && x.RightVertex == wall.RightVertex)
				).Any()) {
					pendingWalls.RemoveAll(x =>
						(x.LeftVertex == wall.RightVertex && x.RightVertex == wall.LeftVertex) ||
						(x.LeftVertex == wall.LeftVertex && x.RightVertex == wall.RightVertex)
					);
					i--;
				}
			}

			// Now we can try and make shapes.
			while (pendingWalls.Count > 0) {
				List<Wall> currentShape = new();
				shapes.Add(currentShape);

				Wall wall = pendingWalls.FirstOrDefault();
				pendingWalls.Remove(wall);
				currentShape.Add(wall);

				while (currentShape[0].LeftVertex != currentShape[currentShape.Count - 1].RightVertex) {
					Wall nextWall = pendingWalls.FirstOrDefault(x => x.LeftVertex == wall.RightVertex);
					if (nextWall == null) {
						//Debug.LogWarning($"Sector {this.lev.Sectors.IndexOf(sector)} is not completely enclosed!");
						shapes.Remove(currentShape);
						break;
					}
					pendingWalls.Remove(nextWall);
					currentShape.Add(nextWall);
					wall = nextWall;
				}
			}

			// Weird sector (no volume?), can't make floor for it!
			if (shapes.Count < 1) {
				return Array.Empty<int>();
			}

			// Old way, check the shape wall length, the idea is the outer wall has the longest length.
			// But that's not true.
			/*List<Wall> outer = shapes
				.Select(x => (x, x.Sum(x => Vector2.Distance(
					x.LeftVertex.Position.ToUnity(),
					x.RightVertex.Position.ToUnity()
				))))
				.OrderByDescending(x => x.Item2).First().x;*/
			// This way we check the shape with the largest bounding box area.
			List<Wall> outer = shapes
				.Select(x => (x,
					(x.Max(x => x.LeftVertex.Position.X) - x.Min(x => x.LeftVertex.Position.X)) *
					(x.Max(x => x.LeftVertex.Position.Y) - x.Min(x => x.LeftVertex.Position.Y))
				))
				.OrderByDescending(x => x.Item2).First().x;
			shapes.Remove(outer);

			// We assume there's only ONE shape that encompasses all other inner shapes.
			// I think some community levels don't follow this. But all the built-in levels do.

			// TODO Improve this to allow for multiple outer shapes.
			// This could be done with a different way to detect outer shapes:
			// Find the vertex with a min X position, then the one with a max X, then min Z, then max Z.
			// If in the shape vertex array they are in the order of: min X, max Z, max X, min Z, or some variant, it's an outer shape.
			// Otherwise it's an inner shape.
			// Inner shape's bounding boxes should fit inside of their outer shape's.
			// With these bits of data it's possible to run the code below for each outer shape and its respective inner shapes.
			
			// We only need vertices now.
			List<Vertex> outerShape = outer.Select(x => x.LeftVertex).ToList();

			// Assume shapes that morph won't have anything inside of them so we can generate the floor through that space..
			// This would make morphing them easier since we don't need to regenerate the floor every frame.
			for (int i = 0; i < shapes.Count; i++) {
				List<Wall> shape = shapes[i];
				if (shape.All(x => x.TextureAndMapFlags.HasFlag(WallTextureAndMapFlags.WallMorphsWithElevator))) {
					shapes.Remove(shape);
					i--;
				}
			}

			List<List<Vertex>> innerShapes = shapes.Select(x => x.Select(x => x.LeftVertex).ToList()).ToList();

			/*foreach (List<Vertex> shape in innerShapes.Prepend(outerShape)) {
				for (int i = 0; i < shape.Count; i++) {
					if (shape[i].Position == shape[(i + 2) % shape.Count].Position) {
						shape.RemoveAt((i + 2) % shape.Count);
						shape.RemoveAt((i + 1) % shape.Count);
					}
				}
			}*/

			List<int> tris = new();

			// Process inner shapes first, to get a single contiguous outer shape.
			// This makes the algorithm for processing that shape simpler.
			while (innerShapes.Count > 0) {
				List<Vertex> innerShape = innerShapes[0];
				bool merged = false;
				// Try each inner shape vertex.
				for (int j = 0; j < innerShape.Count; j++) {
					// We want to try to join this shape with a different one, to reduce the number of total shapes.
					// Try the outer shape first.
					foreach (List<Vertex> shape in innerShapes.Skip(1).Prepend(outerShape)) {
						// Try each shape vertex as a candidate for joining.
						for (int k = 0; k < shape.Count; k++) {
							// Create a triangle using the candidate shape vertex and two vertices from the inner shape.
							// If the triangle is entirely inside the sector we can join the two shapes together using
							// the border of the triangle.into a single shape.
							Vertex vertex = shape[k % shape.Count];
							Vertex[] candidate = new[] { innerShape[j], innerShape[(j + 1 + innerShape.Count) % innerShape.Count], vertex };
							(bool pass, float angle) = TestCandidateVertices2(innerShapes.Prepend(outerShape),
								innerShape[(j + innerShape.Count - 1) % innerShape.Count],
								candidate,
								innerShape[(j + 2) % innerShape.Count],
								shape[(k + shape.Count - 1) % shape.Count],
								shape[(k + 1) % shape.Count]);
							if (!pass) {
								continue;
							}

							// angles of 0/180 indicate no volume thus no tri needed.
							if (angle < 0 || angle > 180) {
								tris.Add(map[candidate[0]]);
								tris.Add(map[candidate[1]]);
								tris.Add(map[candidate[2]]);
							}

							// Join the two shapes together around the tri.
							k %= shape.Count;
							Vertex[] innerVertices = innerShape.Concat(innerShape).Skip(j + 1).Take(innerShape.Count).ToArray();
							shape.Insert(k, shape[k]);
							shape.InsertRange(k + 1, innerVertices);
							innerShapes.RemoveAt(0);
							merged = true;
							break;
						}
						if (merged) {
							break;
						}
					}
					if (merged) {
						break;
					}
				}

				// If we can't merge a shape with any other shape, probably the geometry is screwed up.
				if (!merged) {
					ResourceCache.Instance.AddWarning($"{LevelLoader.Instance.CurrentLevelName}.LEV",
						$"Sector {sector.Name ?? LevelLoader.Instance.Level.Sectors.IndexOf(sector).ToString()} failed to draw floor and ceiling (probablty invalid geometry).");
					return Array.Empty<int>();
				}
			}

			// Now try candidate triangles to see if they are inside the sector, if so remove that volume from
			// consideration, repeat until all sector area is included in the polygons.

			// Any commented out code or references to innerShapes is old code which lacked the above
			// merging of outer and inner shapes and tried to do it here.
			int pos = 0;
			int lastFound = 0;
			//int innerShapePos = 0;
			//int innerPos = 0;
			//bool mergeInnerShape = outerShape.Count == 3 && innerShapes.Count > 0;
			while (outerShape.Count > 3 || innerShapes.Count > 0) {
				Vertex[] candidate;
				bool pass;
				float angle;
				/*if (mergeInnerShape) {
					candidate = outerShape.Concat(outerShape.Take(2)).Skip(pos).Take(2).Append(innerShapes[innerShapePos][innerPos]).ToArray();
					(pass, angle) = this.TestCandidateVertices2(innerShapes.Prepend(outerShape),
						outerShape[(pos + outerShape.Count - 1) % outerShape.Count],
						candidate,
						outerShape[(pos + 2) % outerShape.Count]);
				} else {*/

				// Take three consecutive vertices and see if the polygon is entirely inside the sector or not.
				candidate = outerShape.Concat(outerShape.Take(2)).Skip(pos).Take(3).ToArray();
				(pass, angle) = TestCandidateVertices(innerShapes.Prepend(outerShape),
					outerShape[(pos + outerShape.Count - 1) % outerShape.Count],
					candidate,
					outerShape[(pos + 3) % outerShape.Count]);
				//}

				if (!pass) {
					//if (!mergeInnerShape) {
					pos++;
					if (pos >= outerShape.Count) {
						pos = 0;
					}
					if (pos == lastFound) {
						/*if (innerShapes.Count > 0) {
							lastFound = 0;
							pos = 0;
							innerShapePos = 0;
							innerPos = 0;
							mergeInnerShape = true;
							continue;
						}*/

						ResourceCache.Instance.AddWarning($"{LevelLoader.Instance.CurrentLevelName}.LEV",
							$"Sector {sector.Name ?? LevelLoader.Instance.Level.Sectors.IndexOf(sector).ToString()} failed to draw floor and ceiling (probablty invalid geometry).");
						break;
					}
					/*} else {
						innerPos++;
						if (innerPos >= innerShapes[innerShapePos].Count) {
							innerShapePos++;
							innerPos = 0;
							if (innerShapePos >= innerShapes.Count) {
								pos++;
								innerPos = 0;
								innerShapePos = 0;
								if (pos >= outerShape.Count) {
									Debug.Log($"Sector {num} failed to draw floor and ceiling!");
									break;
								}
							}
						}
					}*/
					continue;
				}

				// If this is false there's no volume to the triangle so don't bother adding it.
				if (angle < 0 || angle > 180) {
					tris.Add(map[candidate[0]]);
					tris.Add(map[candidate[1]]);
					tris.Add(map[candidate[2]]);
				}

				/*if (mergeInnerShape) {
					Vertex[] innerVertices = innerShapes[innerShapePos].Concat(innerShapes[innerShapePos]).Skip(innerPos).Take(innerShapes[innerShapePos].Count + 1).ToArray();
					outerShape.InsertRange(pos + 1, innerVertices);
					innerShapes.RemoveAt(innerShapePos);
				} else {*/
				if (pos + 1 == outerShape.Count) {
					outerShape.RemoveAt(0);
					pos--;
				} else {
					outerShape.RemoveAt(pos + 1);
				}
				//}

				/*mergeInnerShape = outerShape.Count == 3 && innerShapes.Count > 0;
				if (mergeInnerShape) {
					lastFound = 0;
					pos = 0;
					innerShapePos = 0;
					innerPos = 0;
				} else {*/
				lastFound = pos;
				//}
			}

			// If there's only three left, it must be a valid polygon.
			if (outerShape.Count == 3) {
				Vector2[] vectors = outerShape.Select(x => x.Position.ToUnity()).ToArray();
				Vector2 wall1dir = vectors[1] - vectors[0];
				Vector2 wall2dir = vectors[2] - vectors[1];
				float angle = Vector2.SignedAngle(wall1dir, wall2dir);
				if (angle < 0 || angle > 180) {
					tris.Add(map[outerShape[0]]);
					tris.Add(map[outerShape[1]]);
					tris.Add(map[outerShape[2]]);
				}
			}

			return tris.ToArray();
		}

		// Below is an attempt at a better way of doing this, but it turned out to not be possible so I scrapped it.

		/*private class VertexLink {
			public int Index;
			public Vertex Vertex;
			public Vector2 Vector;
			public List<(Vertex prev, Vertex next)> WallPairs;
		}

		private Vector2 VertexToVector2(Vertex vertex) {
			return new Vector2((float)vertex.Position.X, (float)vertex.Position.Y);
		}

		// Won't work, needs to check for intersection

		public int[] NewSplitIntoFloorTris(Sector sector, int num) {
			List<Wall> pendingWalls = sector.Walls.ToList();
			for (int i = 0; i < pendingWalls.Count; i++) {
				Wall wall = pendingWalls[i];
				if (wall.LeftVertex == wall.RightVertex) {
					pendingWalls.Remove(wall);
					i--;
					continue;
				}

				if (pendingWalls.Skip(i + 1).Where(x => (x.LeftVertex == wall.RightVertex && x.RightVertex == wall.LeftVertex) ||
					(x.LeftVertex == wall.LeftVertex && x.RightVertex == wall.RightVertex)).Any()) {

					pendingWalls.RemoveAll(x => (x.LeftVertex == wall.RightVertex && x.RightVertex == wall.LeftVertex) ||
						(x.LeftVertex == wall.LeftVertex && x.RightVertex == wall.RightVertex));
					i--;
				}
			}

			List<List<Wall>> shapes = new List<List<Wall>>();
			while (pendingWalls.Count > 0) {
				List<Wall> currentShape = new List<Wall>();
				shapes.Add(currentShape);

				Wall wall = pendingWalls.FirstOrDefault();
				pendingWalls.Remove(wall);
				currentShape.Add(wall);

				while (currentShape[0].LeftVertex != currentShape[currentShape.Count - 1].RightVertex) {
					Wall nextWall = pendingWalls.FirstOrDefault(x => x.LeftVertex == wall.RightVertex);
					if (nextWall == null) {
						//Debug.LogWarning($"Sector {this.lev.Sectors.IndexOf(sector)} is not completely enclosed!");
						shapes.Remove(currentShape);
						break;
					}
					pendingWalls.Remove(nextWall);
					currentShape.Add(nextWall);
					wall = nextWall;
				}
			}

			pendingWalls = shapes.SelectMany(x => x).ToList();

			Dictionary<Vertex, Wall> rightMap = pendingWalls.ToDictionary(x => x.RightVertex);
			List<VertexLink> vertices = pendingWalls.Select(x => new VertexLink() {
				Index = sector.Walls.IndexOf(x),
				Vertex = x.LeftVertex,
				Vector = this.VertexToVector2(x.LeftVertex),
				WallPairs = new List<(Vertex prev, Vertex next)>() {
					(rightMap[x.LeftVertex].LeftVertex, x.RightVertex)
				}
			}).ToList();

			bool log = false;
			if (num == 5) {
				log = true;
			} else if (num > 5) {
				return new int[] { };
			}

			List<int> tris = new List<int>((vertices.Count - 2) * 3);
			List<int[]> added = new List<int[]>();
			int pos = 0;
			int failCount = 0;
			while (vertices.Count > 3) {
				VertexLink candidate = vertices[pos];

				VertexLink[] candidates = vertices.Where(x => x != candidate)
					.OrderBy(x => (x.Vector - candidate.Vector).magnitude).Take(2).Prepend(candidate).ToArray();
				if (log) {
					Debug.Log($"Checking vertices {string.Join(" ", candidates.Select(x => x.Index))}");
				}
				int[] pending = candidates.Select(x => x.Index).OrderBy(x => x).ToArray();
				if (added.Any(x => pending.SequenceEqual(x))) {
					if (log) {
						Debug.Log($"{candidate.Index}'s closest neighbors are already part of a tri!");
					}

					pos++;
					if (pos >= vertices.Count) {
						pos = 0;
					}
					if (failCount >= vertices.Count) {
						Debug.Log($"Sector {num} failed to draw floor and ceiling!");
						break;
					}
					failCount++;
					continue;
				}

				if (Vector2.SignedAngle(candidates[1].Vector - candidates[0].Vector, candidates[2].Vector - candidates[0].Vector) < 0) {
					candidates = new[] {
						candidates[0],
						candidates[2],
						candidates[1]
					};
				}

				int[] wallPairs = new[] { -1, -1, -1 };
				for (int i = 0; i < 3; i++) {
					candidate = candidates[i];
					Vector2 neighbor1 = candidates[i == 0 ? 1 : 0].Vector - candidate.Vector;
					Vector2 neighbor2 = candidates[i == 2 ? 1 : 2].Vector - candidate.Vector;
					for (int j = 0; j < candidate.WallPairs.Count; j++) {
						Vector2 baseline = this.VertexToVector2(candidate.WallPairs[j].prev) - candidate.Vector;
						float maxAngle = Vector2.SignedAngle(baseline,
							this.VertexToVector2(candidate.WallPairs[j].next) - candidate.Vector);
						if (maxAngle < 0) {
							maxAngle += 360;
						}
						float angle = Vector2.SignedAngle(baseline, neighbor1);
						if (angle < 0) {
							angle += 360;
						}
						if (angle > maxAngle) {
							continue;
						}
						angle = Vector2.SignedAngle(baseline, neighbor2);
						if (angle < 0) {
							angle += 360;
						}
						if (angle > maxAngle) {
							continue;
						}
						wallPairs[i] = j;
						break;
					}

					if (wallPairs[i] < 0) {
						break;
					}
				}

				if (wallPairs.Any(x => x < 0)) {
					if (log) {
						Debug.Log($"{candidates[0].Index} tri leaves sector, skipping...");
					}

					pos++;
					if (pos >= vertices.Count) {
						pos = 0;
					}
					if (failCount >= vertices.Count) {
						Debug.Log($"Sector {num} failed to draw floor and ceiling!");

						// before failing out, find any free tris and join them.

						break;
					}
					failCount++;
					continue;
				}

				if (log) {
					Debug.Log($"{candidates[0].Index} added to tri.");
				}

				tris.Add(candidates[0].Index);
				tris.Add(candidates[2].Index);
				tris.Add(candidates[1].Index);
				added.Add(new[] { candidates[0].Index, candidates[1].Index, candidates[2].Index }.OrderBy(x => x).ToArray());

				for (int i = 0; i < 3; i++) {
					(Vertex left, Vertex right) = candidates[i].WallPairs[wallPairs[i]];
					Vertex addedRight = candidates[(i + 1) % 3].Vertex;
					Vertex addedLeft = candidates[(i + 2) % 3].Vertex;
					candidates[i].WallPairs.RemoveAt(wallPairs[i]);
					if (left != addedRight) {
						candidates[i].WallPairs.Add((left, addedRight));
					}
					if (right != addedLeft) {
						candidates[i].WallPairs.Add((addedLeft, right));
					}
					if (candidates[i].WallPairs.Count == 0) {
						if (log) {
							Debug.Log($"{candidates[i].Index} is orphaned, removing from consideration.");
						}

						vertices.Remove(candidates[i]);
					}
				}
				failCount = 0;
				pos++;
				if (pos >= vertices.Count) {
					pos = 0;
				}
			}

			if (vertices.Count == 3) {
				VertexLink[] candidates = vertices.ToArray();
				if (Vector2.SignedAngle(candidates[1].Vector - candidates[0].Vector, candidates[2].Vector - candidates[0].Vector) < 0) {
					candidates = new[] {
						candidates[0],
						candidates[2],
						candidates[1]
					};
				}
				tris.Add(candidates[0].Index);
				tris.Add(candidates[2].Index);
				tris.Add(candidates[1].Index);
			}

			return tris.ToArray();
		}*/

		/// <summary>
		/// Creates floor and ceiling geometry for a sector.
		/// </summary>
		/// <param name="sector">The sector</param>
		public async Task RenderAsync(Sector sector) {
			int[] tris = SplitIntoFloorTris(sector);

			// Figure out which vertices are used and renumber only using the used ones.
			int[] used = tris.Distinct().OrderBy(x => x).ToArray();

			tris = tris.Select(x => Array.IndexOf(used, x)).ToArray();

			foreach (HorizontalSurface surface in new[] { sector.Floor, sector.Ceiling }) {
				DfBitmap bm = null;
				if (!string.IsNullOrEmpty(surface.TextureFile)) {
					bm = await ResourceCache.Instance.GetBitmapAsync(surface.TextureFile);
				} else {
					bm = await ResourceCache.Instance.GetBitmapAsync("DEFAULT.BM");
				}

				Shader shader;
				// DF Y axis is negative Unity's.
				float y = -surface.Y;
				bool isPlane;
				bool adjoinAdjacentPlanes;
				int lightLevel;
				if (surface == sector.Ceiling && (sector.Flags & SectorFlags.CeilingIsSky) > 0) {
					shader = ResourceCache.Instance.PlaneShader;
					// DF places the actual collider 100 units out for sky/pit.
					y += 100;
					isPlane = true;
					adjoinAdjacentPlanes = sector.Flags.HasFlag(SectorFlags.AdjoinAdjacentSkies);
					lightLevel = 31;
				} else if (surface == sector.Floor && (sector.Flags & SectorFlags.FloorIsPit) > 0) {
					shader = ResourceCache.Instance.PlaneShader;
					y -= 100;
					isPlane = true;
					adjoinAdjacentPlanes = sector.Flags.HasFlag(SectorFlags.AdjoinAdjacentPits);
					lightLevel = 31;
				} else {
					shader = ResourceCache.Instance.SimpleShader;
					isPlane = false;
					adjoinAdjacentPlanes = false;
					lightLevel = sector.LightLevel;
				}

				Material material = bm != null ? ResourceCache.Instance.GetMaterial(
					ResourceCache.Instance.ImportBitmap(bm.Pages[0], LevelLoader.Instance.Palette,
						lightLevel >= 31 ? null : LevelLoader.Instance.ColorMap, lightLevel),
					shader) : null;
				if (isPlane && material != null) {
					// Ensure parallaxing is done on sky/pit.
					Parallaxer.Instance.AddMaterial(material);
				}

				// Determine vertices for mesh. Mesh should be positioned at world coordinates 0, <floor/ceiling level>, 0
				// So the local coordinates of vertices are actual X, 0, actual Z.
				Vector3[] vertices = used.Select(x => sector.Walls[x].LeftVertex)
					.Select(x => new Vector3(
						x.Position.X * LevelGeometryGenerator.GEOMETRY_SCALE,
						0,
						x.Position.Y * LevelGeometryGenerator.GEOMETRY_SCALE)
					).ToArray();

				GameObject obj = new() {
					name = surface == sector.Ceiling ? "Ceiling" : "Floor",
					layer = LayerMask.NameToLayer("Geometry")
				};

				obj.transform.SetParent(this.transform, false);

				obj.transform.position = new Vector3(
					0,
					y * LevelGeometryGenerator.GEOMETRY_SCALE,
					0
				);

				Mesh mesh = new() {
					vertices = vertices
				};
				if (surface == sector.Ceiling) {
					// Invert the list to have the normals facing the other way (down).
					mesh.triangles = tris.Reverse().ToArray();
				} else {
					mesh.triangles = tris;
				}
				if (material != null) {
					Vector2 textureSize = new(material.mainTexture.width, material.mainTexture.height);
					if (textureSize.y != 64) {
						textureSize.x *= textureSize.y / 64; // TODO what is this logic really?
					}
					Vector2 offset = new(
						surface.TextureOffset.X / textureSize.x / LevelGeometryGenerator.TEXTURE_SCALE,
						surface.TextureOffset.Y / textureSize.y / LevelGeometryGenerator.TEXTURE_SCALE
					);
					mesh.uv = vertices.Select(x => new Vector2(
						offset.x - x.x / LevelGeometryGenerator.GEOMETRY_SCALE / LevelGeometryGenerator.TEXTURE_SCALE / textureSize.x,
						-offset.y + x.z / LevelGeometryGenerator.GEOMETRY_SCALE / LevelGeometryGenerator.TEXTURE_SCALE / textureSize.y
					)).ToArray();
				}

				mesh.Optimize();
				mesh.RecalculateNormals();

				MeshFilter filter = obj.AddComponent<MeshFilter>();
				filter.sharedMesh = mesh;

				MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
				renderer.sharedMaterial = material;

				obj.AddComponent<MeshCollider>();

				// If we are a sky/pit we also need to generate walls to line the sector to make up for the 100 unit change in Y.
				if (isPlane) {
					foreach (Wall wall in sector.Walls) {
						float minY = surface.Y;
						if (wall.Adjoined != null && adjoinAdjacentPlanes) {
							if (surface == sector.Ceiling && wall.Adjoined.Sector.Flags.HasFlag(SectorFlags.CeilingIsSky)) {
								minY = wall.Adjoined.Sector.Ceiling.Y - 100;
							}
							if (surface == sector.Floor && wall.Adjoined.Sector.Flags.HasFlag(SectorFlags.FloorIsPit)) {
								minY = wall.Adjoined.Sector.Floor.Y + 100;
							}
						}
						float maxY = -y;
						if (surface == sector.Floor) {
							maxY = minY;
							minY = -y;
						}
						if (maxY >= minY) {
							continue;
						}

						obj = await WallRenderer.CreateMeshAsync(minY, maxY, surface, wall, true);
						obj.transform.SetParent(this.transform, true);
						obj.name = $"Sky/Pit Wall";
					}
				}
			}
		}
	}
}
