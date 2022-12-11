using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
/*
* ZiroDev Copyright(c)
*
*/
public class PathManager : MonoBehaviour {


	static Node.Type[] PriorityNodes = {Node.Type.Grass,Node.Type.Grass2,Node.Type.Beach,Node.Type.Mountain,Node.Type.Water };
	static Queue<PathRequest> PathRequestQueue = new Queue<PathRequest>();
	static PathRequest currentPathRequest;
	static bool IsProcessingPath = false;
	private void Awake() {
		PriorityNodes = new Node.Type[] { Node.Type.Grass,Node.Type.Grass2,Node.Type.Beach,Node.Type.Mountain,Node.Type.Water };
		PathRequestQueue = new Queue<PathRequest>();
	}

	public static void RequestPathToPoint(intVector start, intVector end, Agent agent) {
		PathRequest newRequest = new PathRequest(start, end, agent);
		PathRequestQueue.Enqueue(newRequest);
		TryProcessNext();
	}

	public static void RequestSearch(intVector start, Node.Type type, Agent agent) {
		PathRequest newRequest = new PathRequest(start, type, agent);
		PathRequestQueue.Enqueue(newRequest);
		TryProcessNext();
	}

	public static void RequestSearch(intVector start, ObjectLibrary.Resource resource, Agent agent) {
		PathRequest newRequest = new PathRequest(start, resource, agent);
		PathRequestQueue.Enqueue(newRequest);
		TryProcessNext();
	}

	public static void RequestSearchOfRandomTileOfType(intVector start,int priorityIndex ,Agent agent, float radius) {
		List<intVector> posibleNodes = new List<intVector>();

		int top = (int)Mathf.Ceil(start.y - radius),
			bottom = (int)Mathf.Floor(start.y + radius),
			left = (int)Mathf.Ceil(start.x - radius),
			right = (int)Mathf.Floor(start.x + radius);

		for (int y = top; y <= bottom; y++) {
			for (int x = left; x <= right; x++) {
				intVector tmpVector = new intVector(x, y);
				if (inside_circle(start, tmpVector, radius) && MapBuilder.GetNode(tmpVector).type == PriorityNodes[priorityIndex]) {
					posibleNodes.Add(tmpVector);
				}
			}
		}
		if (posibleNodes.Count == 0) {
			if (priorityIndex < PriorityNodes.Length - 1) {
				RequestSearchOfRandomTileOfType(start, priorityIndex + 1, agent, radius);
			} else return;
		} else {
			int randomNode = UnityEngine.Random.Range(0, posibleNodes.Count);
			RequestPathToPoint(start, posibleNodes[randomNode], agent);
		}

	}

	public static void RequestFurtherFrom(intVector start, intVector enemy, Agent agent, float radius) {
		int MaxDistance = 0;
		intVector node = start;

		int top = (int)Mathf.Ceil(start.y - radius),
			bottom = (int)Mathf.Floor(start.y + radius),
			left = (int)Mathf.Ceil(start.x - radius),
			right = (int)Mathf.Floor(start.x + radius);

		for (int y = top; y <= bottom; y++) {
			for (int x = left; x <= right; x++) {
				intVector tmpVector = new intVector(x, y);
				if (inside_circle(start, tmpVector, radius)) {
					int dist = PathFinder.instance.GetDistance(tmpVector, enemy);
					if (dist >= MaxDistance) {
						MaxDistance = dist;
						node = tmpVector;
					}
				}
			}
		}

		RequestPathToPoint(start, node, agent);

	}

	private static void TryProcessNext() {
		if (!IsProcessingPath && PathRequestQueue.Count > 0) {
			currentPathRequest = PathRequestQueue.Dequeue();
			IsProcessingPath = true;
			if (currentPathRequest.type != Node.Type.None) {
				PathFinder.instance.StartSearch(currentPathRequest.start, currentPathRequest.type, currentPathRequest.agent);
			}else if (currentPathRequest.resource != ObjectLibrary.Resource.none) {
				PathFinder.instance.StartSearch(currentPathRequest.start, currentPathRequest.resource, currentPathRequest.agent);
			} else {
				PathFinder.instance.StartPathFind(currentPathRequest.start, currentPathRequest.end, currentPathRequest.agent);
				
			}
		}
	}

	static bool inside_circle(intVector center, intVector tile, float radius) {
		float dx = center.x - tile.x,
			  dy = center.y - tile.y;
		float distance_squared = dx * dx + dy * dy;
		return distance_squared <= radius * radius;
	}

	static public float UnSquaredDistance(intVector A, intVector B) {
		float dx = A.x - B.x,
			  dy = A.y - B.y;
		float distance_squared = dx * dx + dy * dy;
		return distance_squared;
	}

	static public void FinishedProcessingPath(Agent agent) {
		agent.WaitingForPath = false;
		IsProcessingPath = false;
		TryProcessNext();
	}

	struct PathRequest {
		public intVector start;
		public intVector end;
		public Node.Type type;
		public ObjectLibrary.Resource resource;
		public Agent agent;

		public PathRequest(intVector p_start, intVector p_end, Agent p_Agent) {
			start = p_start; end = p_end; agent = p_Agent; type = Node.Type.None; resource = ObjectLibrary.Resource.none;
		}

		public PathRequest(intVector p_start, Node.Type p_type, Agent p_Agent) {
			start = p_start; end = p_start; agent = p_Agent; type = p_type; resource = ObjectLibrary.Resource.none;
		}

		public PathRequest(intVector p_start, ObjectLibrary.Resource p_resource, Agent p_Agent) {
			start = p_start; end = p_start; agent = p_Agent; type = Node.Type.None; resource = p_resource;
		}
	}
}

public class PathFinder : MonoBehaviour {
	public static PathFinder instance;
	private void Awake() {
		instance = this;
	}

	public void StartPathFind(intVector A, intVector B, Agent agent) {
		StartCoroutine(FindPath(A, B, agent));
	}

	public void StartSearch(intVector A, Node.Type obj, Agent agent) {
		StartCoroutine(FindPathToNodeOfType(A, obj, agent));
	}

	public void StartSearch(intVector A, ObjectLibrary.Resource resourse, Agent agent) {
		StartCoroutine(FindPathToNodeWithResourse(A,  resourse, agent));
	}

	IEnumerator FindPath(intVector A, intVector B, Agent agent) {


		Node startNode = MapBuilder.GetNode(A);
		Node endNode = MapBuilder.GetNode(B);

		Heap<Node> OpenSet = new Heap<Node>(MapBuilder.instance.MaxSize);
		HashSet<Node> ClosedSet = new HashSet<Node>();


		OpenSet.Push(startNode);
		while (OpenSet.Count > 0) {

			Node current = OpenSet.Pop();
			ClosedSet.Add(current);

			if (current == endNode) {
				agent.currentPath.Clear();
				agent.currentPath = GetPath(startNode, endNode);
				break;
			}

			foreach (Node neighbour in MapBuilder.instance.GetNeighbours(current)) {
				if (!neighbour.walkable || ClosedSet.Contains(neighbour)) continue;

				int neighbourPathCost = current.gCost + GetDistance(current, neighbour);
				if (neighbourPathCost < neighbour.gCost || !OpenSet.Contains(neighbour)) {
					neighbour.gCost = neighbourPathCost;
					neighbour.hCost = GetDistance(neighbour, endNode);
					neighbour.Parent = current;
					if (!OpenSet.Contains(neighbour)) {
						OpenSet.Push(neighbour);
					}
				}

			}
		}
		yield return null;
		PathManager.FinishedProcessingPath(agent);
	}

	IEnumerator FindPathToNodeOfType(intVector A, Node.Type type, Agent agent) {

		Node startNode = MapBuilder.GetNode(A);

		Heap<Node> OpenSet = new Heap<Node>(MapBuilder.instance.MaxSize);
		HashSet<Node> ClosedSet = new HashSet<Node>();

		OpenSet.Push(startNode);
		while (OpenSet.Count > 0) {

			Node current = OpenSet.Pop();
			ClosedSet.Add(current);

			if (current.type == type) {
				agent.currentPath.Clear();
				agent.currentPath = GetPath(startNode, current);
				break;
			}

			foreach (Node neighbour in MapBuilder.instance.GetNeighbours(current)) {
				if (!neighbour.walkable || ClosedSet.Contains(neighbour)) continue;

				int neighbourPathCost = current.gCost + GetDistance(current, neighbour);
				if (neighbourPathCost < neighbour.gCost || !OpenSet.Contains(neighbour)) {
					neighbour.gCost = neighbourPathCost;
					neighbour.hCost = 0;
					neighbour.Parent = current;
					if (!OpenSet.Contains(neighbour)) {
						OpenSet.Push(neighbour);
					}
				}
			}
		}
		yield return null;
		PathManager.FinishedProcessingPath(agent);
	}

	IEnumerator FindPathToNodeWithResourse(intVector A,  ObjectLibrary.Resource resourse, Agent agent) {

		Node startNode = MapBuilder.GetNode(A);

		Heap<Node> OpenSet = new Heap<Node>(MapBuilder.instance.MaxSize);
		HashSet<Node> ClosedSet = new HashSet<Node>();

		OpenSet.Push(startNode);
		while (OpenSet.Count > 0) {

			Node current = OpenSet.Pop();
			ClosedSet.Add(current);

			if ((current.resource == resourse && resourse != ObjectLibrary.Resource.Fireplace) || 
				(resourse == ObjectLibrary.Resource.Fireplace && current.firePlace != null && current.firePlace != agent.home)) {
				if (resourse == ObjectLibrary.Resource.Fireplace) agent.enemyBase = current.firePlace;
				agent.currentPath.Clear();
				agent.currentPath = GetPath(startNode, current);
				break;
			}

			foreach (Node neighbour in MapBuilder.instance.GetNeighbours(current)) {
				if (!neighbour.walkable || ClosedSet.Contains(neighbour)) continue;

				int neighbourPathCost = current.gCost + GetDistance(current, neighbour);
				if (neighbourPathCost < neighbour.gCost || !OpenSet.Contains(neighbour)) {
					neighbour.gCost = neighbourPathCost;
					neighbour.hCost = 0;
					neighbour.Parent = current;
					if (!OpenSet.Contains(neighbour)) {
						OpenSet.Push(neighbour);
					}
				}

			}
		}
		yield return null;
		PathManager.FinishedProcessingPath(agent);
	}

	List<Node> GetPath(Node start, Node end) {
		List<Node> path = new List<Node>();
		Node current = end;

		while (current != start) {
			path.Add(current);
			current = current.Parent;
		}
		path.Reverse();

		return path;
	}

	public int GetDistance(Node A, Node B) {
		int x = Mathf.Abs(A.pos.x - B.pos.x);
		int y = Mathf.Abs(A.pos.y - B.pos.y);
		if (x > y) return 14 * y + 10 * (x - y);
		else return 14 * x + 10 * (y - x);
	}

	public int GetDistance(intVector A, intVector B) {
		int x = Mathf.Abs(A.x - B.x);
		int y = Mathf.Abs(A.y - B.y);
		if (x > y) return 14 * y + 10 * (x - y);
		else return 14 * x + 10 * (y - x);
	}
}


static class SteeringBehaviors {
	static public Vector2 Seek(Vector2 targetPosition, Vector2 agentPosition, Rigidbody2D agentRb2d, float maxSpeed, float maxForce) {
		//obtener la velocidad deseada 
		Vector2 desiredVelocity = targetPosition - agentPosition;
		//normalizar la veldeseada para que no salte de golpe
		desiredVelocity.Normalize();
		desiredVelocity *= maxSpeed;
		Vector2 steer = desiredVelocity - agentRb2d.velocity;
		steer = Vector2.ClampMagnitude(steer, maxForce);
		steer /= agentRb2d.mass;
		steer = Vector2.ClampMagnitude(agentRb2d.velocity + steer, maxSpeed);
		return steer;
	}

	static public Vector2 Flee(Vector2 targetPosition, Vector2 agentPosition, Rigidbody2D agentRb2d, float maxSpeed, float maxForce) {
		return Seek(agentPosition,targetPosition,agentRb2d,maxSpeed,maxForce);
	}

}