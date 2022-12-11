using UnityEngine;
/*
* ZiroDev Copyright(c)
*
*/
public class Node : IHeapNode<Node> {
	public enum Type {
		Water,
		Beach,
		Grass,
		Grass2,
		Mountain,
		None
	}


	public bool isAlive = false;
	public bool walkable = true;
	public int height = -100;
	public int neighbours = 0;
	public float speed = 1;
	public intVector pos;

	public int gCost;
	public int hCost;

	int heapIndex;

	public Type type = Type.None;

	public ObjectLibrary.Resource resource = ObjectLibrary.Resource.none;
	public Object NaturalResource;
	public HomeBase firePlace;

	public Node Parent;

	public int fCost {
		get { return gCost + hCost * Weight; }
		private set { }
	}

	public int Weight {
		get { return isAlive ? Mathf.Abs(height - 1) * 2 : 150 * (height + 1); }
		private set { }
	}


	public void SetState(bool state) {
		isAlive = state;
		resource = ObjectLibrary.Resource.none;
		MapBuilder.instance.colors[pos.x][pos.y] = isAlive ? MapBuilder.instance.colorMapGround[MapBuilder.instance.colorMapGround.Length - 1] : MapBuilder.instance.colorMapWater[MapBuilder.instance.colorMapWater.Length - 1];
	}

	public void SetColor(int index) {
		height = index;
		speed = 1 - Mathf.Abs(height - 1) / 10;
		if (!isAlive) speed /= 2;
		if (isAlive) {
			if (height == 1) {
				type = Type.Grass;
			}else if(height == 2) {
				type = Type.Grass2;
			
			} else if (height == 0) {
				type = Type.Beach;
			} else {
				type = Type.Mountain;
			}
		} else {
			type = Type.Water;
		}

		MapBuilder.instance.colors[pos.x][pos.y] = isAlive ? MapBuilder.instance.colorMapGround[height] : MapBuilder.instance.colorMapWater[height];
	}


	public void SetWalkable(bool _walkable) {
		walkable = _walkable;
	}

	public Vector2 WorldCoordinates() {
		return MapBuilder.WorldPos(pos);
	}

	public int HeapIndex {
		get { return heapIndex; }
		set { heapIndex = value; }
	}

	public int CompareTo(Node n) {
		int compare = (int)fCost.CompareTo(n.fCost);
		if (compare == 0) {
			compare = hCost.CompareTo(n.hCost);
		}
		return -compare;
	}

}