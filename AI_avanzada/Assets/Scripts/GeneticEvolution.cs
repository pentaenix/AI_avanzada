using UnityEngine;
using System.Collections.Generic;
/*
* ZiroDev Copyright(c)
*
*/
public class GeneticEvolution : MonoBehaviour {

	public static void EvolveAgents(List<Agent> group, bool playerInput = false, int indexToimprove = 0) {
		(Agent.Gen, float)[][] GroupGeneticValues = new (Agent.Gen, float)[group.Count][];

		//FITNESS EVALUATION
		float[] fitnessValues = new float[group.Count];
		float totalFitness = 0;
		for (int i = 0; i < group.Count; i++) {

			fitnessValues[i] = 1 / 1 + group[i].Fitness();

			totalFitness += fitnessValues[i];
		}
		if (playerInput) {
			float tmpfloat = fitnessValues[indexToimprove];
			fitnessValues[indexToimprove] = totalFitness / 2f;
			totalFitness -= tmpfloat;
			totalFitness += fitnessValues[indexToimprove];
		}
		float[] probabilityOfSurvival = new float[group.Count];
		float[] RouletteValues = new float[group.Count];
		float combinedRoulette = 0;
		float[] RandomValues = new float[group.Count];

		//SELECTION
		for (int i = 0; i < group.Count; i++) {
			probabilityOfSurvival[i] = fitnessValues[i] / totalFitness;
			RouletteValues[i] = probabilityOfSurvival[i] + combinedRoulette;
			combinedRoulette += probabilityOfSurvival[i];
			RandomValues[i] = Random.Range(0f, 1f);
		}

		for (int i = 0; i < RandomValues.Length; i++) {
			for (int j = 0; j < group.Count - 1; j++) {
				if (RandomValues[i] > RouletteValues[j] && RandomValues[i] <= probabilityOfSurvival[j + 1]) {
					GroupGeneticValues[i] = Agent.ExtractGenData(group[j + 1].GeneticValues);
					break;
				} else if (RandomValues[i] <= RouletteValues[0]) {
					GroupGeneticValues[i] = Agent.ExtractGenData(group[0].GeneticValues);
					break;
				}
			}
			if (GroupGeneticValues[i] == null)
				GroupGeneticValues[i] = Agent.ExtractGenData(group[i].GeneticValues);
		}

		//CROSSOVER
		float crossoverRate = 0.33f;
		int index = 0;
		List<int> mattingAgents = new List<int>();
		while (index < group.Count) {
			if (Random.Range(0f, 1f) < crossoverRate) {
				mattingAgents.Add(index);
			}
			index++;
		}
		int cutArea = Random.Range(1, group[0].GeneticValues.Count - 2);

		for (int i = 0; i < mattingAgents.Count; i++) {
			for (int j = 0; j < mattingAgents.Count; j++) {
				if (i != j) {
					for (int cut = 0; cut < cutArea; cut++) {
						(GroupGeneticValues[mattingAgents[i]][cut].Item2, GroupGeneticValues[mattingAgents[j]][cut].Item2) =
						(GroupGeneticValues[mattingAgents[j]][cut].Item2, GroupGeneticValues[mattingAgents[i]][cut].Item2);
					}
				}
			}
		}

		//MUTATION
		float totalGenomeLength = group.Count * group[0].GeneticValues.Count;
		float mutationRate = 0.10f;
		int numberOFMutations = (int)(totalGenomeLength * mutationRate);

		for (int i = 0; i < numberOFMutations; i++) {
			int tmpMutatorIndex = Random.Range(0, (int)totalGenomeLength);
			int GenomeToMutate = tmpMutatorIndex / GroupGeneticValues[0].Length;
			int GenToMutate = tmpMutatorIndex % GroupGeneticValues[0].Length;

			GroupGeneticValues[GenomeToMutate][GenToMutate].Item2 = Random.Range(0, 50);
		}

		//ASSIGNATION
		for (int agent = 0; agent < group.Count; agent++) {
			for (int i = 0; i < group[0].GeneticValues.Count; i++) {
				group[agent].GeneticValues[GroupGeneticValues[agent][i].Item1] = GroupGeneticValues[agent][i].Item2;
			}
		}
	}
}
