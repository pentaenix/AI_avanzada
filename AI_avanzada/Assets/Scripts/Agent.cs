using UnityEngine;
using System.Collections.Generic;
/*
* ZiroDev Copyright(c)
*
*/
public class Agent : MonoBehaviour {

	public enum Type {
		Player,
		Human,
		Prey,
		Predator,
		any,
		none
	}

	public enum Gen {
		speed,
		maxHealth,
		strength,
		maxHunger,
		aggresivness,
		offensiveness,
		maxWater,
		maxStamina,
		homeRiskThreshHold,
		genw1,
		genw2,
		genw3,
		genw4,
		genw5,
		genw6,
		genw7,
		genw8
	}

	public enum State {
		Hunting,
		Gathering,
		None
	}




	#region Variables
	public Transform parent;
	[HideInInspector]
	public List<Node> currentPath = new List<Node>();
	public SpriteRenderer sprite;
	public Transform spriteTransform;
	public GameObject highlight;
	public bool ControlledByPlayer = false;

	public float seekNewNode = 3;
	public Type type;
	public Behavior.Action currentAction;

	public HomeBase home;
	public bool Alive = true;
	public Rigidbody2D RB2D;

	public float cooldown;
	public float hunger = 5;
	public float thirst = 5;
	public float stamina = 5;
	public float WanderRadius = 3;
	public float health;
	private float perception = 3;
	public State currentJob = State.None;

	public float TimeAlive = 0;
	public int enemiesKilled = 0;
	public int damageDealth = 0;
	public int timesEaten = 0;
	public int damageReceibed = 0;

	public Inventory inventory = new Inventory();

	public ObjectLibrary.Resource NeedResource = ObjectLibrary.Resource.none;

	public Agent AgentOfInterest;

	public bool WaitingForPath = false;

	public int enemyCount = 0;
	public int PreyCount = 0;

	public List<(Behavior.State, bool)> BehavioursToDo = new List<(Behavior.State, bool)>();
	public Stack<Behavior> BehaviourStack = new Stack<Behavior>();
	public Dictionary<Behavior.State, bool> AgentState = new Dictionary<Behavior.State, bool>();
	public Dictionary<Behavior.State, float> AgentThreshold = new Dictionary<Behavior.State, float>();
	private List<Behavior.State> dealingWith = new List<Behavior.State>();

	[Space]
	[Header("Genetic Variables")]
	public Dictionary<Gen, float> GeneticValues = new Dictionary<Gen, float>();
	[Space]
	public bool deb_behav = false;
	public bool ClearStack = false;
	public bool printInventory = false;
	public bool printStack = false;

	public bool debugState = true;
	public Behavior.State toCheck;
	public HomeBase enemyBase;
	public Sprite prevSprite;
	public intVector pos {
		get { return MapBuilder.GridPos(transform.position); }
	}


	public void SetPos(float x, float y) {
		transform.position = MapBuilder.WorldPos(new intVector(x, y));
	}
	public float GetSpeed(Node tile) {
		return tile.speed * 1 - Mathf.Min(0, (1 / (GeneticValues[Gen.speed] + 1)) + (tile.isAlive ? 0 : 0.5f));
	}

	#endregion

	#region Unity Methods

	void Start() {
		highlight.SetActive(ControlledByPlayer);
		agressivnessCoolDown = Random.Range(0f, 6f);
		ScannCoolDown = Random.Range(0f, 6f);
		if (transform.parent == null) {
			parent = GameObject.Find("GameScene").transform;
			transform.parent = parent;
		}
		if (type == Type.Predator) {
			sprite.sprite = SpriteLibrary.GetPredatorsSprites[Random.Range(0, 3)];
		}
		if (type == Type.Prey) {
			sprite.sprite = SpriteLibrary.GetPreySprites[Random.Range(0, 3)];
		}
		InitializeGeneticVariables();
		RB2D = GetComponent<Rigidbody2D>();
		foreach (int i in System.Enum.GetValues(typeof(Behavior.State))) {
			if (!AgentState.ContainsKey((Behavior.State)i)) AgentState.Add((Behavior.State)i, false);
			if (!AgentThreshold.ContainsKey((Behavior.State)i)) AgentThreshold.Add((Behavior.State)i, 1);
		}
	}

	void Update() {
		if (MapBuilder.MapDone && Alive) {
			if (BehaviourStack.Count > 0) {
				Behavior currentBehaviour = BehaviourStack.Peek();

				if (currentBehaviour.action == Behavior.Action.CareFor && intVector.Distance(pos, home.pos) <= 1.5f) PlanLibrary.RefuelHome(this);
				if (currentBehaviour.action == Behavior.Action.CollectWater && PlanLibrary.CloseToWater(this)) {
					inventory.Give(ObjectLibrary.Resource.Water);
					PlanLibrary.CheckPlanValidity(this);
				}

				if (currentBehaviour.stage == Behavior.Stage.Inactive) {
					if (currentBehaviour.Condition.Item1 != Behavior.State.None && AgentState[currentBehaviour.Condition.Item1] != currentBehaviour.Condition.Item2) {
						PlanLibrary.SearchForPlanThatResultsIn(this, currentBehaviour.Condition);
					} else {
						PlanLibrary.DoAction(currentBehaviour.action, this);
					}
				} else if (currentBehaviour.stage == Behavior.Stage.Completed || currentBehaviour.stage == Behavior.Stage.Failed) {
					if (dealingWith.Contains(currentBehaviour.Result.Item1)) {
						dealingWith.Remove(currentBehaviour.Result.Item1);
					}
					BehaviourStack.Pop();
				}
			} else {
				PlanLibrary.DoAction(Behavior.Action.Wander, this);
			}

			if (GeneticValues[Gen.aggresivness] > 40f && type == Type.Human) {
				if (agressivnessCoolDown > 6) {
					agressivnessCoolDown = 0;
					FindEnemyFirePlace();
				} else {
					agressivnessCoolDown += Time.deltaTime;
				}
			}

			if (ScannCoolDown > 6) {
				ScannCoolDown = 0;
				if(type != Type.Human) {
					findFirePlaces();
				} else {
					FindClosestAgent();
				}
			} else {
				ScannCoolDown += Time.deltaTime;
			}


			AgentStateUpdater();

			DebugFunctions();

			if (stamina < GeneticValues[Gen.maxStamina]) stamina += Time.deltaTime;
			if (cooldown > 0) cooldown -= Time.deltaTime;

			if (currentPath.Count != 0 || WaitingForPath) FollowPath();

			TimeAlive += Time.deltaTime;
			if (hunger < -1 || health <= 0) Alive = false;
			if (home != null && home.fuel < -1) Alive = false;

		}
		if (!Alive) {
			//prevSprite = sprite.sprite;
			sprite.sprite = SpriteLibrary.GetDeadSprite;
			RB2D.velocity = Vector2.zero;
		}
	}

	float agressivnessCoolDown = 0;
	float ScannCoolDown = 0;
	void DebugFunctions() {
		if (deb_behav) {
			deb_behav = false;
			PlanLibrary.SearchForPlanThatResultsIn(this, (toCheck, debugState));
		}

		if (ClearStack) {
			ClearStack = false;
			print(BehaviourStack.Count);
			dealingWith.Clear();
			BehaviourStack.Clear();
		}

		if (printInventory) {
			printInventory = false;
			print("Water: " + inventory.Water);
			print("Food:  " + inventory.Food);
			print("Logs:  " + inventory.Logs);
			print("Rocks: " + inventory.Rocks);
		}

		if (printStack) {
			printStack = false;

			string StackValues = "STACK: \n";
			Behavior[] tmp = new Behavior[BehaviourStack.Count];
			BehaviourStack.CopyTo(tmp, 0);
			for (int i = 0; i < tmp.Length; i++) {
				StackValues += tmp[i].action.ToString() + "\n";
			}

			print(StackValues);
		}
	}



	public void FollowPath() {
		if (currentPath.Count > 0) {
			WaitingForPath = false;
			if (stamina >= 1f) {
				RB2D.velocity = SteeringBehaviors.Seek(currentPath[0].WorldCoordinates(), transform.position, RB2D, GetSpeed(currentPath[0]), 1);
			}
			if (Vector2.Distance(transform.position, currentPath[0].WorldCoordinates()) < 0.01f) {
				stamina -= 1;
				if (currentPath.Count <= 1) {
					if (currentJob == State.Gathering) {
						if (NeedResource == ObjectLibrary.Resource.Water && PlanLibrary.CloseToWater(this)) {
							inventory.Give(ObjectLibrary.Resource.Water);
						} else if (currentPath[0].resource == NeedResource && currentPath[0].NaturalResource != null) {
							inventory.Give(currentPath[0].NaturalResource.resource);
							currentPath[0].NaturalResource.Gather();
						}
						PlanLibrary.CheckIfEnoughtResources(this, NeedResource);
					} else if (currentJob == State.Hunting) {
						if (Vector2.Distance(AgentOfInterest.transform.position, transform.position) < 0.1f) {
							Fight(AgentOfInterest);
						} else {
							if ((stamina < 2 || (type == Type.Human && inventory.Rocks <= 0)) && BehaviourStack.Count > 0) {
								BehaviourStack.Peek().stage = Behavior.Stage.Failed;
							} else {
								PlanLibrary.Seek(AgentOfInterest);
							}
						}
					}
				}

				if (spriteTransform.localRotation.z <= 0) {
					spriteTransform.localRotation = Quaternion.Euler(0, 0, 15);
				} else {
					spriteTransform.localRotation = Quaternion.Euler(0, 0, -15);
				}
				if (currentPath.Count > 0) {
					sprite.flipX = pos.x > currentPath[currentPath.Count - 1].pos.x;
					currentPath.RemoveAt(0);
				}
			}
			if (currentPath.Count == 0) {
				spriteTransform.localRotation = Quaternion.Euler(0, 0, 0);
			}
		}
	}

	public void Fight(Agent agent) {
		
		if (type == Type.Human && inventory.Rocks <= 0) {
			PlanLibrary.SearchForPlanThatResultsIn(this, (Behavior.State.HasRocks, true));
			currentJob = State.None;
			return;
		}
		if (type == Type.Human) inventory.Take(ObjectLibrary.Resource.Rock);
		agent.takeDamage(GeneticValues[Gen.strength]);
		damageDealth += (int)GeneticValues[Gen.strength];

		if (agent.type != Type.Prey || GeneticValues[Gen.offensiveness] < 20f) {
			agent.Counter(this);
		} else {
			AlertAgent(agent);
		}
		if (agent.health <= 0) {
			inventory.Give(ObjectLibrary.Resource.Food, 5);
			if (BehaviourStack.Count > 0) BehaviourStack.Peek().stage = Behavior.Stage.Completed;
			currentJob = State.None;
			AgentOfInterest = null;
		}
	}

	void Counter(Agent agent) {
		if (type == Type.Human && inventory.Rocks <= 0) {
			return;
		}
		if (type == Type.Human) inventory.Take(ObjectLibrary.Resource.Rock);
		agent.takeDamage(GeneticValues[Gen.strength]);
		damageDealth += (int)GeneticValues[Gen.strength];

		if (agent.health <= 0) {
			inventory.Give(ObjectLibrary.Resource.Food, 5);
		}
	}

	public void AlertAgent(Agent agent) {
		AgentOfInterest = agent;
		if (type == Type.Prey || GeneticValues[Gen.offensiveness] < 20f) {
			PlanLibrary.Flee(agent);
		}
	}

	public void takeDamage(float ammount) {
		damageReceibed += (int)ammount;
		health -= ammount;
	}

	public bool AgentCloseBy(Agent agent) {
		print(Vector2.Distance(agent.transform.position, transform.position));
		return Vector2.Distance(agent.transform.position, transform.position) < perception;
	}

	public Agent FindClosestAgent(Type t = Type.any) {
		enemyCount = 0;
		PreyCount = 0;
		Collider2D[] otherCollider = Physics2D.OverlapCircleAll(transform.position, perception);
		List<Agent> CloseAgents = new List<Agent>();
		if (otherCollider.Length > 0) {
			foreach (Collider2D col in otherCollider) {
				if (col != GetComponent<Collider2D>()) {
					Agent tmp = col.GetComponent<Agent>();
					if (tmp != null) {
						CloseAgents.Add(tmp);
					}
				}
			}
		}
		float closestDist = float.MaxValue;
		Agent objectiveAgent = null;
		foreach (Agent other in CloseAgents) {
			if (type != Type.Prey && other.type == Type.Prey) PreyCount++;
			if (type != Type.Predator && other.type == Type.Predator) enemyCount++;

			if (home != null && other.enemyBase == home || (type == Type.Human && GeneticValues[Gen.aggresivness] > 40 && (other.type != Type.Human || other.type == Type.Human && other.home != home))) {
				Fight(other);
			}

			float dist = PathManager.UnSquaredDistance(pos, other.pos);
			if ((other.type == t || (other.type != type && t == Type.any)) && dist < closestDist) {
				closestDist = dist;
				objectiveAgent = other;
			}
		}
		return objectiveAgent;
	}

	public LayerMask HomeMask;
	public void FindEnemyFirePlace() {
		if (enemyBase == null) {
			Collider2D[] otherCollider = Physics2D.OverlapCircleAll(transform.position, perception, HomeMask);
			List<HomeBase> enemyBases = new List<HomeBase>();
			if (otherCollider.Length > 0) {
				foreach (Collider2D col in otherCollider) {

					HomeBase tmp = col.GetComponent<HomeBase>();
					if (tmp != null && home != tmp) {
						PlanLibrary.SearchForPlanThatResultsIn(this, (Behavior.State.EnemyBaseAlive, false));
						break;
					}

				}
			}

		}
	}

	public void findFirePlaces() {
		Collider2D col = Physics2D.OverlapCircle(transform.position, perception, HomeMask);
		if (col != null) {
			HomeBase tmp = col.GetComponent<HomeBase>();
			if (tmp != null) {
				PlanLibrary.ReactiveFlee(this, tmp.pos);

			}
		}
	}

	void InitializeGeneticVariables() {

		foreach (int i in System.Enum.GetValues(typeof(Gen))) {
			if (!GeneticValues.ContainsKey((Gen)i)) GeneticValues.Add((Gen)i, Random.Range(0f, 50f));
		}
		SetUpAgent();



	}

	public void SetUpAgent() {
		Alive = true;
		BehaviourStack.Clear();
		dealingWith.Clear();
		inventory.Refill();

		if (type == Type.Predator) {
			sprite.sprite = SpriteLibrary.GetPredatorsSprites[Random.Range(0, 3)];
		}
		if (type == Type.Prey) {
			sprite.sprite = SpriteLibrary.GetPreySprites[Random.Range(0, 3)];
		}

		hunger = GeneticValues[Gen.maxHunger];
		thirst = GeneticValues[Gen.maxHunger];
		health = GeneticValues[Gen.maxHealth];
		stamina = GeneticValues[Gen.maxHunger];

		AgentThreshold[Behavior.State.Hungry] = GeneticValues[Gen.maxHunger] / 2f;
		AgentThreshold[Behavior.State.Thirsty] = GeneticValues[Gen.maxWater] / 2f;
		AgentThreshold[Behavior.State.InDanger] = GeneticValues[Gen.maxHealth] / 2f;
		AgentThreshold[Behavior.State.HomeRisk] = GeneticValues[Gen.homeRiskThreshHold];
	}

	public void AgentStateUpdater() {
		hunger -= 0.2f * Time.deltaTime;
		thirst -= 0.1f * Time.deltaTime;

		CheckState();

		AppendToToDoList(Behavior.State.Hungry, false);
		AppendToToDoList(Behavior.State.Thirsty, false);
		AppendToToDoList(Behavior.State.InDanger, false);
		AppendToToDoList(Behavior.State.HasWater, true);
		AppendToToDoList(Behavior.State.HasFood, true);

		if (type == Type.Human) {
			AppendToToDoList(Behavior.State.HomeRisk, false);
			AppendToToDoList(Behavior.State.HasLogs, true);
			AppendToToDoList(Behavior.State.HasRocks, true);
		}
		OrderTasks();

	}

	public void CheckState() {
		AgentState[Behavior.State.HasWater] = inventory.Water > 0;
		AgentState[Behavior.State.HasFood] = inventory.Food > 0;
		AgentState[Behavior.State.HasLogs] = inventory.Logs > 0;
		AgentState[Behavior.State.HasRocks] = inventory.Rocks > 0;
		AgentState[Behavior.State.EnemyClose] = enemyCount > 0;
		AgentState[Behavior.State.PreyClose] = PreyCount > 0;


		AgentState[Behavior.State.Hungry] = hunger < AgentThreshold[Behavior.State.Hungry];
		AgentState[Behavior.State.Thirsty] = thirst < AgentThreshold[Behavior.State.Thirsty];
		AgentState[Behavior.State.InDanger] = health < AgentThreshold[Behavior.State.InDanger];
		if (type == Type.Human || type == Type.Player)
			AgentState[Behavior.State.HomeRisk] = home.fuel < AgentThreshold[Behavior.State.HomeRisk];
	}

	void AppendToToDoList(Behavior.State state, bool expectedValue) {
		if (AgentState[state] != expectedValue && !dealingWith.Contains(state)) {
			BehavioursToDo.Add((state, expectedValue));
		}
	}

	void OrderTasks() {
		while (BehavioursToDo.Count > 0) {
			float LeastImportantTaskUtility = int.MaxValue;
			(Behavior.State, bool) tmp = (Behavior.State.None, false);

			foreach (var val in BehavioursToDo) {
				float currentUtility = PlanLibrary.RateUtility(this, val.Item1);
				if (currentUtility < LeastImportantTaskUtility) {
					LeastImportantTaskUtility = currentUtility;
					tmp = val;
				}
			}
			if (BehaviourStack.Count > 0 && LeastImportantTaskUtility < PlanLibrary.RateUtility(this, BehaviourStack.Peek().Result.Item1)) {
				Behavior mostImportantBehavior = BehaviourStack.Pop();
				PlanLibrary.SearchForPlanThatResultsIn(this, tmp);
				dealingWith.Add(tmp.Item1);
				BehavioursToDo.Remove(tmp);
				BehaviourStack.Push(mostImportantBehavior);
			} else {
				PlanLibrary.SearchForPlanThatResultsIn(this, tmp);
				dealingWith.Add(tmp.Item1);
				BehavioursToDo.Remove(tmp);
			}
		}
	}

	public float Fitness() {
		return TimeAlive + timesEaten * 10 + enemiesKilled * 30 + damageDealth * 10 - damageReceibed * 20;
	}

	public static (Gen, float)[] ExtractGenData(Dictionary<Gen, float> input) {
		(Gen, float)[] retArray = new (Gen, float)[input.Count];
		int indx = 0;
		foreach (var key in input.Keys) {
			retArray[indx] = (key, input[key]);
			indx++;
		}

		return retArray;
	}

	#endregion
}


public class Inventory {
	public int maxWater = 5;
	public int maxRocks = 5;
	public int maxLogs = 5;
	public int maxFood = 5;

	public int Water = 3;
	public int Rocks = 3;
	public int Logs = 3;
	public int Food = 3;

	public void Refill() {
		Water = Random.Range(0, 5);
		Rocks = Random.Range(0, 5);
		Logs = Random.Range(0, 5);
		Rocks = Random.Range(0, 5);
	}

	public void Take(ObjectLibrary.Resource resource, int ammount = 1) {
		switch (resource) {
			case ObjectLibrary.Resource.Water:
				if (Water > 0) Water -= ammount;
				break;
			case ObjectLibrary.Resource.Rock:
				if (Rocks > 0) Rocks -= ammount;
				break;
			case ObjectLibrary.Resource.Log:
				if (Logs > 0) Logs -= ammount;
				break;
			case ObjectLibrary.Resource.Food:
				if (Food > 0) Food -= ammount;
				break;
			default:
				break;
		}
	}

	public void Give(ObjectLibrary.Resource resource, int ammount = 1) {
		switch (resource) {
			case ObjectLibrary.Resource.Water:
				if (Water < maxWater) Water += ammount;
				break;
			case ObjectLibrary.Resource.Rock:
				if (Rocks < maxRocks) Rocks += ammount;
				break;
			case ObjectLibrary.Resource.Log:
				if (Logs < maxLogs) Logs += ammount;
				break;
			case ObjectLibrary.Resource.Food:
				if (Food < maxFood) Food += ammount;
				break;
			default:
				break;
		}
	}


	public int Peek(ObjectLibrary.Resource resource) {
		switch (resource) {
			case ObjectLibrary.Resource.Water: return Water;
			case ObjectLibrary.Resource.Rock: return Rocks;
			case ObjectLibrary.Resource.Log: return Logs;
			case ObjectLibrary.Resource.Food: return Food;
			default: return 0;
		}
	}

}