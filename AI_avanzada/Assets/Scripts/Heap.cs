using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
/*
* ZiroDev Copyright(c)
*
*/
public class Heap<T> where T : IHeapNode<T>{
	T[] nodes;
	int nodeCount;

	public int Count {
		get {
			return nodeCount;
		}
	}

	public Heap(int p_maxSize) {
		nodes = new T[p_maxSize];
	}

	public void Push(T p_node) {
		if(nodeCount >= nodes.Length) {
			Array.Resize(ref nodes,nodes.Length+100);
		}
		p_node.HeapIndex = nodeCount;
		nodes[nodeCount] = p_node;
		SortUp(p_node);
		nodeCount++;
	}
	public T Pop() {
		T R_node = nodes[0];
		nodeCount--;
		nodes[0] = nodes[nodeCount];
		nodes[0].HeapIndex = 0;
		SortDown(nodes[0]);

		return R_node;
	}

	public void UpdateValue(T p_node) {
		SortUp(p_node);
	}


	public bool Contains(T p_node) {
		return Equals(nodes[p_node.HeapIndex],p_node);
	}

	void SortDown(T p_node) {
		while (true) {
			int childIndex_Left = p_node.HeapIndex * 2 + 1;
			int childIndex_Right = p_node.HeapIndex * 2 + 2;
			int SwapIndex;

			if(childIndex_Left < nodeCount) {
				SwapIndex = childIndex_Left;
				if (childIndex_Right < nodeCount && nodes[childIndex_Left].CompareTo(nodes[childIndex_Right]) < 0) {
					SwapIndex = childIndex_Right;
				}
				if (p_node.CompareTo(nodes[SwapIndex]) < 0) {
					SwapNodes(p_node, nodes[SwapIndex]);
				} else {
					return;
				}
			} else {
				return;
			}
		}
	}


	void SortUp(T p_node) {
		int parentIndex = (p_node.HeapIndex - 1) / 2;
		while (true) {
			T parentNode = nodes[parentIndex];
			if (p_node.CompareTo(parentNode) > 0) {
				SwapNodes(p_node,parentNode);
			} else {
				break;
			}

			parentIndex = (p_node.HeapIndex - 1) / 2;
		}
	}

	void SwapNodes(T p_nodeA, T p_nodeB) {
		nodes[p_nodeA.HeapIndex] = p_nodeB;
		nodes[p_nodeB.HeapIndex] = p_nodeA;

		(p_nodeA.HeapIndex, p_nodeB.HeapIndex) = (p_nodeB.HeapIndex, p_nodeA.HeapIndex);
	}
}

public interface IHeapNode<T> : IComparable<T>{
	int HeapIndex {
		get; set; 
	}
}