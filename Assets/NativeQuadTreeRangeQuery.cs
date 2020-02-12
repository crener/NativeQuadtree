using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace NativeQuadTree
{
	public unsafe partial struct NativeQuadTree<T> where T : unmanaged
	{
		struct QuadTreeRangeQuery<T> where T : unmanaged
		{
			NativeQuadTree<T> tree;
			NativeList<QuadElement<T>> results;
			AABB2D bounds;

			public void Query(NativeQuadTree<T> tree, AABB2D bounds, NativeList<QuadElement<T>> results)
			{
				this.tree = tree;
				this.bounds = bounds;
				this.results = results;
				RecursiveRangeQuery(tree.bounds, false, 0, 0);
			}

			public void RecursiveRangeQuery(AABB2D parentBounds, bool parentContained, int atNode, int depth)
			{
				var totalOffset = LookupTables.DepthSizeLookup[++depth];

				for (int l = 0; l < 4; l++)
				{
					var childBounds = GetChildBounds(parentBounds, l);

					var contained = parentContained;
					if(!contained)
					{
						if(bounds.Contains(childBounds))
						{
							contained = true;
						}
						else if(!bounds.Intersects(childBounds))
						{
							continue;
						}
					}

					var at = totalOffset + atNode + l;
					var elementCount = UnsafeUtility.ReadArrayElement<int>(tree.lookup->Ptr, at);

					if(elementCount > tree.maxLeafElements && depth < tree.maxDepth)
					{
						RecursiveRangeQuery(childBounds, contained, (atNode + l) * 4, depth);
					}
					else if(elementCount != 0)
					{
						var node = UnsafeUtility.ReadArrayElement<QuadNode>(tree.nodes->Ptr, at);

						if(contained)
						{
							var index = (void*) ((IntPtr) tree.elements->Ptr + node.firstChildIndex * UnsafeUtility.SizeOf<QuadElement<T>>());
							results.AddRange(index, node.count);
						}
						else
						{
							for (int k = 0; k < node.count; k++)
							{
								var element = UnsafeUtility.ReadArrayElement<QuadElement<T>>(tree.elements->Ptr, node.firstChildIndex + k);
								if(bounds.Contains(element.pos))
								{
									results.Add(element);
								}
							}
						}
					}
				}
			}

			static AABB2D GetChildBounds(AABB2D parentBounds, int childZIndex)
			{
				var half = parentBounds.Extents.x * .5f;

				switch (childZIndex)
				{
					case 0: return new AABB2D(new float2(parentBounds.Center.x - half, parentBounds.Center.y + half), half);
					case 1: return new AABB2D(new float2(parentBounds.Center.x + half, parentBounds.Center.y + half), half);
					case 2: return new AABB2D(new float2(parentBounds.Center.x - half, parentBounds.Center.y - half), half);
					case 3: return new AABB2D(new float2(parentBounds.Center.x + half, parentBounds.Center.y - half), half);
					default: throw new Exception();
				}
			}
		}

	}
}