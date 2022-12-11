using UnityEngine;
using System.Collections.Generic;
/*
* ZiroDev Copyright(c)
*
*/
public class ObjectLibrary : MonoBehaviour {

	public static ObjectLibrary instance;
	public GameObject prefab;
	public Sprite[] sprites;
	public List<GameObject> objects;
	public Transform Container;
	public int middPoint = 0;
	private void Awake() {
		instance = this;
	}

	public Sprite GetSprite(Type t) {
		return sprites[(int)t];
	}
	public enum Type {
		Pine,
		Oak,
		Palm,
		Tree_1,
		Tree_2,
		Orange_1,
		Orange_2,
		Leaf,
		Cave_1,
		Cave_2,
		Cave_3,
		Mountain,
		rock,
		none
	}

	public enum Resource {
		Rock,
		Food,
		Log,
		Water,
		Fireplace,
		none
	}

	public Resource SetResource(Type t) {
		if (t == Type.Pine || t == Type.Oak || t == Type.Palm || t == Type.Tree_1 || t == Type.Tree_2) return Resource.Log;
		if (t == Type.Cave_1 || t == Type.Cave_2 || t == Type.Cave_3 || t == Type.Mountain || t == Type.rock) return Resource.Rock;
		if (t == Type.Orange_1 || t == Type.Orange_2 || t == Type.Leaf) return Resource.Food;
		else return Resource.none;
	}

	Type TypeFromTile(Node.Type nType) {
		float RandomNumber = Random.Range(0f, 10f);
		switch (nType) {
			case Node.Type.Beach:
				if (RandomNumber < 5) {
					return Type.Palm;
				} else {
					return Type.Leaf;
				}

			case Node.Type.Grass:
				if (RandomNumber < 3) {
					return Type.Oak;
				} else if (RandomNumber < 5) {
					return Type.Pine;
				} else if (RandomNumber < 6) {
					return Type.rock;
				} else {
					return Type.Leaf;
				}

			case Node.Type.Grass2:
				if (RandomNumber < 1) {
					return Type.Orange_1;
				} else if (RandomNumber < 2) {
					return Type.Orange_2;
				} else if (RandomNumber < 5) {
					return Type.Tree_1;
				} else if (RandomNumber < 6) {
					return Type.Tree_2;
				} else if (RandomNumber < 7) {
					return Type.rock;
				} else {
					return Type.Leaf;
				}

			case Node.Type.Mountain:
				if (RandomNumber < 0.01f) {
					return Type.Mountain;
				} else if (RandomNumber < 4) {
					return Type.Cave_1;
				} else if (RandomNumber < 7) {
					return Type.Cave_2;
				} else if (RandomNumber < 9.5f) {
					return Type.rock;
				} else  {
					return Type.Cave_3;
				} 
			default:
				return Type.Leaf;
		}
	}

	public int GetHP(Type t) {
		if (t == Type.Cave_1 || t == Type.Orange_1 || t == Type.Orange_2) return 1;
		if (t == Type.Cave_2) return 1;
		if (t == Type.Cave_3) return 1;
		if (t == Type.Mountain) return 1;
		else return 1;
	}

	public void SpawnDecor(float probability) {
		MapBuilder map = MapBuilder.instance;
		for (int i = 0; i < map.maxIndexLN; i++) {
			if (Random.Range(0, 100) < probability) {
				Node node = map.livingNodes[i];
				GameObject tmp = Instantiate(prefab, MapBuilder.WorldPos(node.pos), Quaternion.identity);
				objects.Add(tmp);
				tmp.GetComponent<Object>().SetUp(TypeFromTile(node.type),node.pos);
				node.resource = tmp.GetComponent<Object>().resource;
				tmp.transform.parent = Container;
			}
		}
		middPoint = objects.Count / 2;
	}

	public void RemoveFromList(GameObject obj,Object script) {
		MapBuilder.GetNode(script.nodeIndex).resource = Resource.none;
		MapBuilder.GetNode(script.nodeIndex).NaturalResource = null;
		objects.Remove(obj);
		Destroy(obj);
	}

	public void killDecor() {
		foreach(GameObject d in objects) {
			MapBuilder.GetNode(d.GetComponent<Object>().nodeIndex).resource = Resource.none;
			Destroy(d, 0.1f);
		}
		objects.Clear();
	}


}
