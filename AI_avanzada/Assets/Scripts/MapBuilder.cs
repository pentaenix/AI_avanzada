using UnityEngine;
//using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
/*
* ZiroDev Copyright(c)
*
*/
public class MapBuilder : MonoBehaviour {

	#region Variables
	public Agent agent;
	public List<Agent> Animals = new List<Agent>();
	public GameObject FirePlacePrefab;
	public GameObject animalPrefab;
	public GameObject predatorPrefab;

	public static MapBuilder instance;

	public bool MakeMap = false;
	public int loops = 1;

	public bool fullLineCoast = false;
	public bool fullLineContinent = false;

	//public GameObject pixel;
	[Range(0f, 1f)]
	public float startProbability;
	public bool seedCenter = false;
	public int SeededLandmasses = 2;
	public int SeededOceans = 2;
	public int ContinentSize = 7;
	public int OceanSize = 7;

	public int minToDie = 2;
	public int maxToDie = 6;

	public int minToBirth = 2;
	public int maxToBirth = 6;

	public Vector2 size;
	public Color[][] colors;
	private Node[][] Map;
	private bool[][] MapState;
	private List<Node> iterratingList = new List<Node>();
	private List<Node> Nodes = new List<Node>();

	public int MaxSize;

	private int[] dirx = new int[24] { -1, 1, 0, 0, 1, 1, -1, -1, -2, 2, 0, 0, 2, 2, -2, -2, -1, -2, 2, 1, -1, -2, 1, 2 };
	private int[] diry = new int[24] { 0, 0, -1, 1, -1, 1, 1, -1, 0, 0, -2, 2, -2, 2, -2, 2, 2, 1, 1, 2, -2, -1, -2, -1 };

	public Color[] colorMapGround;
	public Color[] colorMapWater;

	public int[] stepsPerLevelLand;
	public int[] stepsPerLevelWater;

	[HideInInspector]
	public Node[] livingNodes;
	public int maxIndexLN = 0;

	#endregion

	#region Unity Methods

	void Start() {
		PlanLibrary.AvailableActions = new Dictionary<string, Behavior>();
		Behavior.InitializeBehavoiurs();
		MaxSize = (int)(size.x * size.y);
		livingNodes = new Node[MaxSize];


		instance = this;
		colors = new Color[(int)size.x][];
		Map = new Node[(int)size.x][];
		MapState = new bool[(int)size.x][];

		for (int x = 0; x < size.x; x++) {
			Map[x] = new Node[(int)size.y];
			colors[x] = new Color[(int)size.y];
			MapState[x] = new bool[(int)size.y];
			for (int y = 0; y < size.y; y++) {
				Node tmpNode = new Node();
				iterratingList.Add(tmpNode);
				tmpNode.pos = new intVector(x, y);
				Map[x][y] = tmpNode;
			}
		}

		StartMap();
	}
	private intVector[] livingNodeSeedSize;
	private intVector[] deadNodeSeedSize;
	void StartMap() {

		livingNodeSeedSize = new intVector[SeededLandmasses];
		deadNodeSeedSize = new intVector[SeededOceans];
		for (int i = 0; i < livingNodeSeedSize.Length; i++) {
			livingNodeSeedSize[i].x = Random.Range(0, ContinentSize);
			livingNodeSeedSize[i].y = Random.Range(0, ContinentSize);
		}

		for (int i = 0; i < deadNodeSeedSize.Length; i++) {
			deadNodeSeedSize[i].x = Random.Range(0, OceanSize);
			deadNodeSeedSize[i].y = Random.Range(0, OceanSize);
		}


		for (int x = 0; x < Map.Length; x++) {
			for (int y = 0; y < Map[0].Length; y++) {
				bool state = Random.Range(0, 1f) < startProbability;
				if (seedCenter) {
					for (int i = 0; i < deadNodeSeedSize.Length; i++) {
						state = KillRandomArea(x, y, deadNodeSeedSize[i], state);
					}
					for (int i = 0; i < livingNodeSeedSize.Length; i++) {
						state = SeedRandomArea(x, y, livingNodeSeedSize[i], state);
					}
				}
				MapState[x][y] = state;
				Map[x][y].SetState(state);

			}
		}
		DrawMap();
	}

	int distFromWalls = 3;

	bool SeedRandomArea(int x, int y, intVector p, bool state) {
		float limitXInferior = size.x / ContinentSize * p.x;
		float limitXSuperior = size.x / ContinentSize * (p.x + 1);
		float limitYInferior = size.y / ContinentSize * p.y;
		float limitYSuperior = size.y / ContinentSize * (p.y + 1);

		if (x > limitXInferior && x < limitXSuperior && y > limitYInferior && y < limitYSuperior) {

			if (x < limitXInferior + distFromWalls + 1 || x > limitXSuperior - distFromWalls - 1 || y < limitYInferior + distFromWalls + 1 || y > limitYSuperior - distFromWalls) {
				return Random.Range(0, 100) > 40;
			} else
				return Random.Range(0, 100) > 20;
		}
		return state;
	}

	bool KillRandomArea(int x, int y, intVector p, bool state) {

		float limitXInferior = size.x / OceanSize * p.x;
		float limitXSuperior = size.x / OceanSize * (p.x + 1);
		float limitYInferior = size.y / OceanSize * p.y;
		float limitYSuperior = size.y / OceanSize * (p.y + 1);

		if (x > limitXInferior && x < limitXSuperior && y > limitYInferior && y < limitYSuperior) {

			if (x < limitXInferior + distFromWalls + 1 || x > limitXSuperior - distFromWalls - 1 || y < limitYInferior + distFromWalls + 1 || y > limitYSuperior - distFromWalls) {
				return Random.Range(0, 100) < 10;
			} else
				return false;
		}
		return state;
	}

	int countNeighbours(bool[][] map, int _x, int _y, bool state) {
		int cnt = 0;
		for (int i = 0; i < 8; i++) {
			int valuex = Mathf.Clamp(_x + dirx[i], 0, Map.Length - 1);
			int valuey = Mathf.Clamp(_y + diry[i], 0, Map[0].Length - 1);
			if (map[valuex][valuey] == state) {
				cnt++;
			}
		}
		return cnt;
	}

	int setNeighbours(int _x, int _y, bool state) {
		int cnt = 0;
		for (int i = 0; i < 8; i++) {
			int valuex = Mathf.Clamp(_x + dirx[i], 0, Map.Length - 1);
			int valuey = Mathf.Clamp(_y + diry[i], 0, Map[0].Length - 1);
			Map[valuex][valuey].SetState(state);
			MapState[valuex][valuey] = state;
		}
		return cnt;
	}

	public bool nextToNode(intVector p, bool stateofOtherNode) {
		for (int i = 0; i < 8; i++) {
			int valuex = Mathf.Clamp(p.x + dirx[i], 0, Map.Length - 1);
			int valuey = Mathf.Clamp(p.y + diry[i], 0, Map[0].Length - 1);
			if (MapState[valuex][valuey] == stateofOtherNode) {
				return true;
			}
		}
		return false;
	}

	bool NextToNeighbour(intVector p, int height) {
		for (int i = 0; i < (fullLineContinent ? 8 : 4); i++) {
			int valuex = Mathf.Clamp(p.x + dirx[i], 0, Map.Length - 1);
			int valuey = Mathf.Clamp(p.y + diry[i], 0, Map[0].Length - 1);
			if (Map[valuex][valuey].height == height && !takenPositions.Contains(new intVector(valuex, valuey))) {
				return true;
			}
		}
		return false;
	}



	void stepWorld() {
		maxIndexLN = 0;
		foreach (Node n in iterratingList) n.height = -1;
		bool[][] tmpCopy = new bool[(int)size.x][];

		for (int x = 0; x < size.x; x++) {
			tmpCopy[x] = new bool[(int)size.y];
			for (int y = 0; y < size.y; y++) {
				tmpCopy[x][y] = MapState[x][y];
			}
		}

		for (int x = 0; x < size.x; x++) {
			for (int y = 0; y < size.y; y++) {
				int neigbours = countNeighbours(tmpCopy, x, y, true);
				bool state;
				if (tmpCopy[x][y]) {
					if (neigbours < minToDie || neigbours > maxToDie) {
						state = false;
					} else {
						state = true;
					}
				} else {
					if (neigbours > minToBirth && neigbours < maxToBirth) {
						state = true;
					} else {
						state = false;
					}
				}
				MapState[x][y] = state;
				Map[x][y].SetState(state);
				Map[x][y].neighbours = neigbours;
				if (state) livingNodes[maxIndexLN++] = Map[x][y];

			}
		}
		DrawMap();
	}


	public static Node GetNode(intVector point) {
		return instance.Map[Mathf.Clamp(point.x, 0, instance.Map.Length - 1)][Mathf.Clamp(point.y, 0, instance.Map[0].Length - 1)];
	}

	public List<Node> GetNeighbours(Node n) {
		intVector p = n.pos;
		List<Node> neighbours = new List<Node>();

		for (int i = 0; i < 8; i++) {
			int valuex = Mathf.Clamp(p.x + dirx[i], 0, Map.Length - 1);
			int valuey = Mathf.Clamp(p.y + diry[i], 0, Map[0].Length - 1);
			neighbours.Add(Map[valuex][valuey]);
		}
		return neighbours;
	}


	int currentStepLand = 0;

	int prevStepLand = 0;
	int stepIndexLand = 0;

	int currentStepWater = 0;

	int prevStepWater = 0;
	int stepIndexWater = 0;

	bool coloring = false;
	public bool printWhileDrawing = false;
	bool RandomTierOneDots = false;
	bool SettedPointsThisLayer = false;
	public bool AddRandomDots = true;
	public float RandomDotsProbability = 0.001f;
	void colorMap() {
		if (Nodes.Count > 0) {

			takenPositions.Clear();

			if (stepIndexLand == 1 && !SettedPointsThisLayer) {
				SettedPointsThisLayer = true;
				RandomTierOneDots = true;
			}

			for (int i = Nodes.Count - 1; i > 0; i--) {

				if ((Nodes[i].isAlive && nextToNode(Nodes[i].pos, false)) || (!Nodes[i].isAlive && nextToNode(Nodes[i].pos, true)) || (NextToNeighbour(Nodes[i].pos, Nodes[i].isAlive ? prevStepLand : prevStepWater))) {
					takenPositions.Add(new intVector(Nodes[i].pos.x, Nodes[i].pos.y));
					Nodes[i].SetColor(Nodes[i].isAlive ? stepIndexLand : stepIndexWater);
					Nodes.RemoveAt(i);

				} else if (AddRandomDots && !NextToNeighbour(Nodes[i].pos, prevStepLand) && !NextToNeighbour(Nodes[i].pos, prevStepWater) && Random.Range(0, 5000) < RandomDotsProbability && RandomTierOneDots) {
					SetRandomPoints(i);
				}

				if (Nodes.Count == 0) break;
			}

			RandomTierOneDots = false;
			prevStepLand = stepIndexLand;
			prevStepWater = stepIndexWater;

			if (Nodes.Count < 10) {
				foreach (Node n in Nodes) {
					n.SetColor(n.isAlive ? stepIndexLand : stepIndexWater);
				}
				Nodes.Clear();
			}

			if (++currentStepLand >= stepsPerLevelLand[stepIndexLand]) {
				if (stepIndexLand < stepsPerLevelLand.Length - 1) stepIndexLand++;
				currentStepLand = 0;
			}

			if (++currentStepWater >= stepsPerLevelWater[stepIndexWater]) {
				if (stepIndexWater < stepsPerLevelWater.Length - 1) stepIndexWater++;
				currentStepWater = 0;
			}
			if (printWhileDrawing) DrawMap();

		} else if (coloring) {
			RandomTierOneDots = true;
			SettedPointsThisLayer = false;
			coloring = false;

			currentStepLand = 0;
			prevStepLand = 0;
			stepIndexLand = 0;

			currentStepWater = 0;
			prevStepWater = 0;
			stepIndexWater = 0;
			if(spawnEntities)ObjectLibrary.instance.SpawnDecor(5f);
			if (firstSpawn && spawnEntities) {
				SpawnBases();
				SpawnNpc();
				firstSpawn = false;
			} else {
				AdvanceGeneration();
			}
			MapDone = true;
			StartCoroutine(DragCamera.instance.FocusOn(1f, WorldPos(PlayerManager.instance.playerHome.pos)));
			UI_LoadingActivator.Deactivate();
			if (!printWhileDrawing) DrawMap();
		}
	}

	public bool spawnEntities = true;
	bool firstSpawn = true;

	void SetRandomPoints(int indx) {
		Nodes[indx].SetColor(Nodes[indx].isAlive ? 1 : 1);
		takenPositions.Add(Nodes[indx].pos);
		intVector p = Nodes[indx].pos;
		Nodes.RemoveAt(indx);
		for (int i = 0; i < (Random.Range(0, 10) < 5 ? 8 : 4); i++) {
			int valuex = Mathf.Clamp(p.x + dirx[i], 0, Map.Length - 1);
			int valuey = Mathf.Clamp(p.y + diry[i], 0, Map[0].Length - 1);
			if (Nodes.Contains(Map[valuex][valuey])) {
				Map[valuex][valuey].SetColor(Map[valuex][valuey].isAlive ? 1 : 1);
				Nodes.Remove(Map[valuex][valuey]);
				takenPositions.Add(Map[valuex][valuey].pos);
			}
		}

	}

	void lightColorer() {
		while (Nodes.Count > 0) {
			if ((Nodes[0].isAlive && nextToNode(Nodes[0].pos, false)) || (!Nodes[0].isAlive && nextToNode(Nodes[0].pos, true))) {
				Nodes[0].SetColor(0);
				Nodes.RemoveAt(0);
			} else {
				Nodes[0].SetColor(1);
				Nodes.RemoveAt(0);
			}
		}
		DrawMap();
	}

	public bool LightColorer = false;
	float roundTime = 0;
	void DrawMap() {
		Texture2D texture = new Texture2D((int)size.x, (int)size.y);
		texture.filterMode = FilterMode.Point;
		GetComponent<RawImage>().texture = texture;

		for (int y = 0; y < texture.height; y++) {
			for (int x = 0; x < texture.width; x++) {
				texture.SetPixel(x, y, colors[x][y]);
			}
		}
		texture.Apply();
	}

	List<intVector> takenPositions = new List<intVector>();
	public static bool MapDone = false;

	void Update() {
		if (instance == null) instance = this;

		if (!LightColorer) colorMap();
		/*//DEBUG TOOLS
		if (Input.GetKeyDown(KeyCode.Space)) {
			stepWorld();
		} else if (Input.GetKeyDown(KeyCode.T)) {
			StartMap();
		} else if (Input.GetKeyDown(KeyCode.C)) {
			colorMapHeights();
		} else if (Input.GetKeyDown(KeyCode.A)) {
			agent.SetPos(10, 10);
			PathManager.RequestPathToPoint(new intVector(10, 10), new intVector(180, 130), agent);
		} else if (Input.GetKeyDown(KeyCode.Q)) {
			foreach (Node a in livingNodes) {
				if (a.resource != ObjectLibrary.Resource.none) print(a.resource);
			}
		} else if (Input.GetKeyDown(KeyCode.S)) {
			ObjectLibrary.instance.SpawnDecor(3f);
		} else*/
		if (Input.GetKeyDown(KeyCode.K) || roundTime > 500) {
			PlayerManager.instance.ShowMenu();
		}

		if (MakeMap || Input.GetKeyDown(KeyCode.M)) {
			roundTime = 0;
			if (!UI_LoadingActivator.IsRunning) {
				StartCoroutine(UI_LoadingActivator.Activate());
				StartCoroutine(DragCamera.instance.ResetCamera());
			}
			MapDone = false;
			ObjectLibrary.instance.killDecor();
			MakeMap = false;
			StartMap();
			for (int i = 0; i < loops; i++) {
				stepWorld();
			}
			colorMapHeights();
		}

		if (MapDone) {
			roundTime += Time.deltaTime;
		}

		if (MapDone == true && ObjectLibrary.instance.objects.Count > 0 && ObjectLibrary.instance.objects.Count < ObjectLibrary.instance.middPoint) {
			ObjectLibrary.instance.SpawnDecor(2.5f);
		}
	}

	public void AdvanceGeneration() {

		Node tmpPlayerNode = livingNodes[Random.Range(0, maxIndexLN - 1)];
		PlayerManager.instance.playerHome.transform.position = WorldPos(tmpPlayerNode.pos);
		PlayerManager.instance.playerHome.pos = tmpPlayerNode.pos;
		PlayerManager.instance.playerHome.GenerationPassed(true, PlayerManager.choosenIndex);

		for (int i = 0; i < homes.Length; i++) {
			Node tmp = livingNodes[Random.Range(0, maxIndexLN - 1)];
			homes[i].transform.position = WorldPos(tmp.pos);
			homes[i].pos = tmp.pos;
			homes[i].GenerationPassed();
		}
		GeneticEvolution.EvolveAgents(preyGroup);
		GeneticEvolution.EvolveAgents(predatorGroup);
		for(int i = 0; i < preyGroup.Count; i++) {
			Node tmp = livingNodes[Random.Range(0, maxIndexLN - 1)];
			preyGroup[i].transform.position = WorldPos(tmp.pos);
			preyGroup[i].SetUpAgent();
		}

		for (int i = 0; i < predatorGroup.Count; i++) {
			Node tmp = livingNodes[Random.Range(0, maxIndexLN - 1)];
			predatorGroup[i].transform.position = WorldPos(tmp.pos);
			predatorGroup[i].SetUpAgent();
		}
	}
	HomeBase[] homes = new HomeBase[10];
	List<Agent> preyGroup = new List<Agent>();
	List<Agent> predatorGroup = new List<Agent>();

	public void SpawnBases() {
		
		for (int i = 0; i < 11; i++) {
			Node tmp = livingNodes[Random.Range(0, maxIndexLN - 1)];
			if (i == 10) {
				PlayerManager.instance.playerHome = Instantiate(FirePlacePrefab, WorldPos(tmp.pos), Quaternion.identity).GetComponent<HomeBase>();
				PlayerManager.instance.playerHome.PlayerSetUp();
				PlayerManager.instance.playerHome.pos = tmp.pos;

			} else {
				HomeBase home = Instantiate(FirePlacePrefab, WorldPos(tmp.pos), Quaternion.identity).GetComponent<HomeBase>();
				home.pos = tmp.pos;
				home.SetUp();
				homes[i] = home;
			}

		}
	}

	public void SpawnNpc() {
		for (int i = 0; i < 100; i++) {
			Node tmp = livingNodes[Random.Range(0, maxIndexLN - 1)];
			Agent animal = Instantiate(i < 40 ? predatorPrefab : animalPrefab, WorldPos(tmp.pos), Quaternion.identity).GetComponent<Agent>();
			if (i < 40) {
				predatorGroup.Add(animal);
			} else {
				preyGroup.Add(animal);
			}
			Animals.Add(animal);
		}
	}

	public void colorMapHeights() {
		coloring = true;
		Nodes.Clear();
		for (int x = 0; x < size.x; x++) {
			for (int y = 0; y < size.y; y++) {
				Nodes.Add(Map[x][y]);
			}
		}
		if (LightColorer)
			lightColorer();
	}

	public static intVector GridPos(Vector2 position) {
		return new intVector(position.x * 15, position.y * 15);
	}

	public static Vector2 WorldPos(intVector position) {
		return new Vector2(position.x / 15f, position.y / 15f);
	}

	#endregion
}




public struct intVector {
	public int x;
	public int y;
	public intVector(int _x, int _y) { x = _x; y = _y; }
	public intVector(float _x, float _y) { x = (int)_x; y = (int)_y; }

	public Vector2 Vector2() {
		return new Vector2(x, y);
	}

	public void print() {
		Debug.Log("" + (x + "," + y));
	}

	public static float Distance(intVector a, intVector b) {
		return Mathf.Sqrt(Mathf.Pow((b.x - a.x), 2) + Mathf.Pow((b.y - a.y), 2));
	}
}
