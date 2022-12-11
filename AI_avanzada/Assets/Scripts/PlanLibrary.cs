using UnityEngine;
using System.Collections.Generic;
/*
* ZiroDev Copyright(c)
*
*/
public static class PlanLibrary {

	static public Dictionary<string, Behavior> AvailableActions = new Dictionary<string, Behavior>();

	public static void SearchForPlanThatResultsIn(Agent agent, (Behavior.State, bool) p_worldState) {
		foreach (var key in AvailableActions.Keys) {
			if (AvailableActions[key].Result == p_worldState) {
				if (agent.BehaviourStack.Count > 0) {
					agent.BehaviourStack.Peek().stage = Behavior.Stage.Inactive;
				}
				agent.BehaviourStack.Push(Behavior.Clone(AvailableActions[key]));
				break;
			}
		}
	}

	public static void DoAction(Behavior.Action toDo, Agent agent) {
		if (agent.BehaviourStack.Count > 0) {
			agent.BehaviourStack.Peek().stage = Behavior.Stage.Executing;

		}
		switch (toDo) {
			case Behavior.Action.Eat:
				if (agent.inventory.Food > 0)
					Eat(agent);
				break;
			case Behavior.Action.Drink:
				if (CloseToWater(agent) || agent.inventory.Water > 0)
					Drink(agent);
				break;
			case Behavior.Action.CollectFood:
				SearchForFoodSource(agent);
				break;
			case Behavior.Action.CollectWater:
				GetResource(agent, ObjectLibrary.Resource.Water);

				break;
			case Behavior.Action.CollectLogs:
				GetResource(agent, ObjectLibrary.Resource.Log);

				break;
			case Behavior.Action.CollectRocks:
				GetResource(agent, ObjectLibrary.Resource.Rock);

				break;
			case Behavior.Action.CareFor:
				if (agent.inventory.Logs > 0) {
					GoHome(agent);
				}
				break;
			case Behavior.Action.Flee:
				if (AgentSeesAnotherAgent(agent)) {
					Flee(agent);
				}
				break;
			case Behavior.Action.Pursue:
				if (AgentSeesAnotherAgent(agent)) {
					Seek(agent);
				}
				break;
			case Behavior.Action.Drown:
				if (AgentSeesAnotherAgent(agent)) {
					DrownBase(agent);
				}
				break;
			case Behavior.Action.FindBase:
				PathManager.RequestSearch(agent.pos, ObjectLibrary.Resource.Fireplace, agent);
				break;
			default:
				Wander(agent);
				break;

		}
		agent.currentAction = toDo;
	}

	static public void SearchForFoodSource(Agent agent) {
		if (agent.type == Agent.Type.Predator) Hunt(agent);
		else GetResource(agent, ObjectLibrary.Resource.Food);
	}

	static public void CheckPlanValidity(Agent agent) {
		agent.CheckState();
		if (agent.BehaviourStack.Count > 0) {
			Behavior tmp = agent.BehaviourStack.Peek();
			if (agent.AgentState[tmp.Result.Item1] == tmp.Result.Item2) {
				tmp.stage = Behavior.Stage.Completed;
			} else {
				if (agent.AgentState[tmp.Condition.Item1] == tmp.Condition.Item2) {
					DoAction(tmp.action, agent);
				} else {
					SearchForPlanThatResultsIn(agent, tmp.Condition);
				}
			}
		}
		//if valid, do action again, if not mark as completed, if failed mark as failed
	}

	static public void CheckIfEnoughtResources(Agent agent, ObjectLibrary.Resource res) {
		agent.CheckState();
		if (agent.BehaviourStack.Count > 0) {
			Behavior tmp = agent.BehaviourStack.Peek();
			if (agent.inventory.Peek(res) >= 5) {
				agent.currentJob = Agent.State.None;
				tmp.stage = Behavior.Stage.Completed;
			} else {
				DoAction(tmp.action, agent);
			}
		}
		//if valid, do action again, if not, mark as completed, if failed mark as failed
	}

	static void Drink(Agent agent) {
		if (!CloseToWater(agent) && agent.inventory.Water > 0) {
			agent.inventory.Take(ObjectLibrary.Resource.Water);

		}
		agent.thirst = agent.GeneticValues[Agent.Gen.maxWater];

		if (agent.BehaviourStack.Count > 0)
			agent.BehaviourStack.Peek().stage = Behavior.Stage.Completed;
	}

	static void Hunt(Agent agent) {
		agent.AgentOfInterest = agent.FindClosestAgent(Agent.Type.any);
		if (agent.AgentOfInterest != null) {
			agent.currentJob = Agent.State.Hunting;
			Seek(agent.AgentOfInterest);
		}

	}

	static void DrownBase(Agent agent) {
		if (agent.enemyBase != null && agent.inventory.Water > 0 && intVector.Distance(agent.pos, agent.enemyBase.pos) < 0.5f) {
			agent.inventory.Take(ObjectLibrary.Resource.Water);
			agent.enemyBase.fuel -= 10;
			agent.damageDealth += 20;
			agent.enemyBase = null;
			agent.BehaviourStack.Peek().stage = Behavior.Stage.Completed;
		}
	}

	static void Eat(Agent agent) {
		agent.timesEaten++;
		agent.inventory.Take(ObjectLibrary.Resource.Food);
		agent.hunger = agent.GeneticValues[Agent.Gen.maxHunger];
		if (agent.BehaviourStack.Count > 0)
			agent.BehaviourStack.Peek().stage = Behavior.Stage.Completed;
	}

	static public bool CloseToWater(Agent agent) {
		return MapBuilder.instance.nextToNode(agent.pos, false);
	}

	static bool AgentSeesAnotherAgent(Agent agent) {
		return agent.AgentOfInterest != null;
	}

	static public void GoHome(Agent agent) {
		agent.WaitingForPath = true;
		PathManager.RequestPathToPoint(agent.pos, agent.home.pos, agent);
	}
	static public void RefuelHome(Agent agent) {
		agent.home.fuel = 60;
		agent.inventory.Take(ObjectLibrary.Resource.Log);
		CheckPlanValidity(agent);
	}
	static void GetResource(Agent agent, ObjectLibrary.Resource res) {
		agent.currentJob = Agent.State.Gathering;
		agent.NeedResource = res;
		agent.WaitingForPath = true;
		if (res == ObjectLibrary.Resource.Water) {
			PathManager.RequestSearch(agent.pos, Node.Type.Water, agent);
		} else {
			PathManager.RequestSearch(agent.pos, res, agent);
		}
	}
	static void Wander(Agent agent) {
		if (agent.currentPath.Count == 0 && agent.cooldown <= 0 && agent.currentAction == Behavior.Action.Wander)
			agent.WaitingForPath = false;
		if (agent.currentPath.Count == 0 && agent.cooldown <= 0 && !agent.WaitingForPath) {
			agent.WaitingForPath = true;
			PathManager.RequestSearchOfRandomTileOfType(agent.pos, 0, agent, agent.WanderRadius);
			agent.cooldown = agent.seekNewNode;
		}
	}

	static public void Flee(Agent agent) {
		if (agent.AgentOfInterest != null) {
			agent.WaitingForPath = true;
			PathManager.RequestFurtherFrom(agent.pos, agent.AgentOfInterest.pos, agent, 10);
		}
	}

	static public void ReactiveFlee(Agent agent, intVector point) {
		agent.WaitingForPath = true;
		PathManager.RequestFurtherFrom(agent.pos, point, agent, 10);
	}

	static public void Seek(Agent agent) {
		if (agent.AgentOfInterest != null) {
			agent.WaitingForPath = true;
			PathManager.RequestPathToPoint(agent.pos, agent.AgentOfInterest.pos, agent);
		}
	}

	static public float RateUtility(Agent agent, Behavior.State state) {
		switch (state) {
			case Behavior.State.Hungry:
				return (1f - (agent.hunger / 5f)) * agent.GeneticValues[Agent.Gen.genw1] / 10f;
			case Behavior.State.Thirsty:
				return (1f - (agent.thirst / 5f)) * agent.GeneticValues[Agent.Gen.genw2] / 10f;
			case Behavior.State.HomeRisk:
				return (agent.enemyCount + (1f - (agent.home.fuel / 10))) * agent.GeneticValues[Agent.Gen.genw3] / 10f;
			case Behavior.State.InDanger:
				return (agent.enemyCount + (1f - (agent.health / 10))) * agent.GeneticValues[Agent.Gen.genw4] / 10f;
			case Behavior.State.HasWater:
				return (1f - (agent.thirst / 10f)) * agent.GeneticValues[Agent.Gen.genw5] / 10f;
			case Behavior.State.HasFood:
				return (1f - (agent.hunger / 10f)) * agent.GeneticValues[Agent.Gen.genw6] / 10f;
			case Behavior.State.HasLogs:
				return (1f - (agent.home != null ? agent.home.fuel : 0 / 10f)) * agent.GeneticValues[Agent.Gen.genw7] / 10f;
			case Behavior.State.HasRocks:
				return (agent.enemyCount + 0.1f) * agent.GeneticValues[Agent.Gen.genw8] / 10f;
			default:
				return 1f;
		}
	}
}



public class Behavior {

	public enum State {
		HasWater,
		HasFood,
		HasLogs,
		HasRocks,
		Hungry,
		Thirsty,
		InDanger,
		HomeRisk,
		EnemyClose,
		PreyClose,
		EnemyBaseClose,
		EnemyBaseAlive,
		None
	}

	public enum Stage {
		Executing,
		Completed,
		Failed,
		Inactive
	}

	public enum Action {
		Eat,
		Drink,
		CollectFood,
		CollectLogs,
		CollectWater,
		CollectRocks,
		CareFor,
		Flee,
		Pursue,
		Attack,
		Locate,
		Wander,
		Drown,
		FindBase
	}

	public (State, bool) Condition;
	public (State, bool) Result;
	public Action action;

	public Stage stage = Stage.Inactive;
	//public float cost;
	public Behavior((State, bool) p_condition, Action p_action, (State, bool) p_result) {
		action = p_action; Condition = p_condition; Result = p_result;
	}

	public static Behavior Clone(Behavior other) {
		return new Behavior(other.Condition, other.action, other.Result);
	}

	static public void InitializeBehavoiurs() {
		PlanLibrary.AvailableActions.Add("Eat", new Behavior(
				(State.HasFood, true),
				Action.Eat,
				(State.Hungry, false))
		);

		PlanLibrary.AvailableActions.Add("Drink", new Behavior(
				(State.HasWater, true),
				Action.Drink,
				(State.Thirsty, false))
		);

		PlanLibrary.AvailableActions.Add("GetFood", new Behavior(
				(State.None, true),
				Action.CollectFood,
				(State.HasFood, true))
		);

		PlanLibrary.AvailableActions.Add("GetWater", new Behavior(
				(State.None, true),
				Action.CollectWater,
				(State.HasWater, true))
		);

		PlanLibrary.AvailableActions.Add("GetLogs", new Behavior(
				(State.None, true),
				Action.CollectLogs,
				(State.HasLogs, true))
		);

		PlanLibrary.AvailableActions.Add("GetRocks", new Behavior(
				(State.None, true),
				Action.CollectRocks,
				(State.HasRocks, true))
		);

		PlanLibrary.AvailableActions.Add("FuelHome", new Behavior(
				(State.HasLogs, true),
				Action.CareFor,
				(State.HomeRisk, false))
		);

		PlanLibrary.AvailableActions.Add("Run Away", new Behavior(
				(State.None, true),
				Action.Flee,
				(State.EnemyClose, false))
		);

		PlanLibrary.AvailableActions.Add("Follow", new Behavior(
				(State.None, true),
				Action.Pursue,
				(State.PreyClose, true))
		);

		PlanLibrary.AvailableActions.Add("Hunt", new Behavior(
				(State.HasRocks, true),
				Action.Attack,
				(State.HasFood, true))

		);

		PlanLibrary.AvailableActions.Add("Fight", new Behavior(
				(State.HasRocks, true),
				Action.Attack,
				(State.EnemyClose, false))
		);

		PlanLibrary.AvailableActions.Add("Destroy enemy base", new Behavior(
				(State.EnemyBaseClose, true),
				Action.Drown,
				(State.EnemyBaseAlive, false))
		);

		PlanLibrary.AvailableActions.Add("Find enemy base", new Behavior(
				(State.HasWater, true),
				Action.FindBase,
				(State.EnemyBaseClose, true))
		);
	}
}

