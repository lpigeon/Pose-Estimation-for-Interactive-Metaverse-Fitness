using UnityEngine;
using System.Collections.Generic;

namespace UnityEngine.Polybrush
{
	public struct PolyEdge : System.IEquatable<PolyEdge>
	{
		// tri indices
		internal int x;
		internal int y;

		internal PolyEdge(int _x, int _y)
		{
			this.x = _x;
			this.y = _y;
		}

		public bool Equals(PolyEdge p)
		{
			return (p.x == x && p.y == y) || (p.x == y && p.y == x);
		}

		public override bool Equals(System.Object b)
		{
			return b is PolyEdge && this.Equals((PolyEdge)b);
		}

		public static bool operator ==(PolyEdge a, PolyEdge b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(PolyEdge a, PolyEdge b)
		{
			return !a.Equals(b);
		}

		public override int GetHashCode()
		{
			// http://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode/263416#263416
			int a, b, hash = 17;

			a = (x < y ? x : y).GetHashCode();
			b = (x < y ? y : x).GetHashCode();
	
			unchecked
			{
				hash = hash * 29 + a.GetHashCode();
				hash = hash * 29 + b.GetHashCode();
			}

			return hash;
		}

		public override string ToString()
		{
			return string.Format("{{{{{0},{1}}}}}", x, y);
		}

        /// <summary>
        /// Returns a new list of indices by selecting the x,y of each edge.
        /// </summary>
        /// <param name="edges"></param>
        /// <returns></returns>
		internal static List<int> ToList(IEnumerable<PolyEdge> edges)
		{
			List<int> list = new List<int>();

			foreach(PolyEdge e in edges)
			{
				list.Add(e.x);
				list.Add(e.y);
			}

			return list;
		}
        /// <summary>
        /// Returns a new hashset of indices by selecting the x,y of each edge.
        /// </summary>
        /// <param name="edges"></param>
        /// <returns></returns>
		internal static HashSet<int> ToHashSet(IEnumerable<PolyEdge> edges)
		{
			HashSet<int> hash = new HashSet<int>();

			foreach(PolyEdge e in edges)
			{
				hash.Add(e.x);
				hash.Add(e.y);
			}

			return hash;
		}
	}
}
