using UnityEngine;
using System.Collections.Generic;
/*
* ZiroDev Copyright(c)
*
*/
public class HomeBase : MonoBehaviour {

    #region Variables
    public Transform parent;
    public float fuel = 60;
    public List<Agent> tribe = new List<Agent> ();
    public GameObject agentPrefab;
    public intVector pos;
    public int classIndex;
    public GameObject image;
    public Node place;

    public bool ownedByPlayer = false;
    #endregion

    #region Unity Methods
    
    void Start() {
        if (transform.parent == null) {
            parent = GameObject.Find("GameScene").transform;
            transform.parent = parent;
        }
    }

    public void SetUp() {
        do {
            classIndex = SpriteLibrary.AvailableIndex[Random.Range(0, SpriteLibrary.AvailableIndex.Count)];
        } while (SpriteLibrary.TakenIndex.Contains(classIndex));
        SpriteLibrary.TakenIndex.Add(classIndex);

        for (int i = 0; i < 5; i++) {
            Vector2 tmpPosition = new Vector2(transform.position.x + Random.Range(-0.5f, 0.5f), transform.position.y + Random.Range(-0.5f, 0.5f));
            Agent tmp = Instantiate(agentPrefab, tmpPosition, Quaternion.identity).GetComponent<Agent>();
            if (ownedByPlayer) tmp.ControlledByPlayer = true;
            int rnd = Random.Range(0, 4);
            tmp.GetComponentInChildren<SpriteRenderer>().sprite = SpriteLibrary.humanSprites[classIndex][rnd];
            tmp.prevSprite = SpriteLibrary.humanSprites[classIndex][rnd];
            tribe.Add(tmp);
            tmp.home = this;
        }

        Node tmpNode = MapBuilder.GetNode(pos);
        tmpNode.resource = ObjectLibrary.Resource.Fireplace;
        pos = tmpNode.pos;
        tmpNode.firePlace = this;
        place = tmpNode;

    }

    public void PlayerSetUp() {
        ownedByPlayer = true;
        classIndex = PlayerManager.index;
        SpriteLibrary.TakenIndex.Add(classIndex);

        for (int i = 0; i < 5; i++) {
            Vector2 tmpPosition = new Vector2(transform.position.x + Random.Range(-0.5f, 0.5f), transform.position.y + Random.Range(-0.5f, 0.5f));
            Agent tmp = Instantiate(agentPrefab, tmpPosition, Quaternion.identity).GetComponent<Agent>();
            tmp.ControlledByPlayer = true;
            tmp.GetComponentInChildren<SpriteRenderer>().sprite = SpriteLibrary.humanSprites[classIndex][Random.Range(0, 4)];
            tribe.Add(tmp);
            tmp.home = this;
        }

        Node tmpNode = MapBuilder.GetNode(pos);
        tmpNode.resource = ObjectLibrary.Resource.Fireplace;
        tmpNode.firePlace = this;
        pos = tmpNode.pos;
        place = tmpNode;
    }

    public void GenerationPassed(bool player = false,int index = 0) {
        gameOverThisTurn = false;
        //Gen and respawn in new world
        if (place != null) {
            place.resource = ObjectLibrary.Resource.none;
            place.firePlace = null;
            place = null;
        }
        fuel = 60;
        if (player) GeneticEvolution.EvolveAgents(tribe, player, index);
        else GeneticEvolution.EvolveAgents(tribe);
        foreach (Agent a in tribe) {
            Vector2 tmpPosition = new Vector2(transform.position.x + Random.Range(-0.5f, 0.5f), transform.position.y + Random.Range(-0.5f, 0.5f));
            a.transform.position = tmpPosition;
            a.GetComponentInChildren<SpriteRenderer>().sprite = SpriteLibrary.humanSprites[classIndex][Random.Range(0, 4)];
            a.SetUpAgent();
		}

	}
    bool gameOverThisTurn = false;
    void Update() {
        fuel -= Time.deltaTime;
  

        image.SetActive(fuel > 0);

		if (ownedByPlayer) {
            int counter = 0;
            foreach(Agent a in tribe) {
                if (a.Alive) break;
                else counter++;
			}
            if(counter >= 5 && !gameOverThisTurn) {
                //GAME OVER
                gameOverThisTurn = true;
                PlayerManager.instance.ShowMenu(true);
                

            }
		}
    }

    #endregion
}
