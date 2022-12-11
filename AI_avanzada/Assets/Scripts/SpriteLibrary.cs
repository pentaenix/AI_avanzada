using UnityEngine;
using System.Collections.Generic;
/*
* ZiroDev Copyright(c)
*
*/
public class SpriteLibrary : MonoBehaviour {

    #region Variables
    public static SpriteLibrary instance;
    public static List<int> AvailableIndex = new List<int>{0,1,2,3,4,5,6,7,8,9,10,11,12 };
    public static List<int> TakenIndex = new List<int>();
    public Sprite deadHuman;

    public static Sprite[][] humanSprites;
    public Sprite[] classA;
    public Sprite[] classB;
    public Sprite[] classC;
    public Sprite[] classD;
    public Sprite[] classE;
    public Sprite[] classF;
    public Sprite[] classG;
    public Sprite[] classH;
    public Sprite[] classI;
    public Sprite[] classJ;
    public Sprite[] classK;
    public Sprite[] classL;
    public Sprite[] classM;

    public Sprite[] predatorSprites;
    public Sprite[] preySprites;

    public static Sprite GetDeadSprite {
        get { return instance.deadHuman; }
	}

    public static Sprite[] GetPredatorsSprites {
        get { return instance.predatorSprites; }
    }

    public static Sprite[] GetPreySprites {
        get { return instance.preySprites; }
    }

    #endregion

    #region Unity Methods

    void Awake() {
        AvailableIndex = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        TakenIndex = new List<int>();
        instance = this;
        humanSprites = new Sprite[13][];
        humanSprites[0] = classA;
        humanSprites[1] = classB;
        humanSprites[2] = classC;
        humanSprites[3] = classD;
        humanSprites[4] = classE;
        humanSprites[5] = classF;
        humanSprites[6] = classG;
        humanSprites[7] = classH;
        humanSprites[8] = classI;
        humanSprites[9] = classJ;
        humanSprites[10] = classK;
        humanSprites[11] = classL;
        humanSprites[12] = classM;
    }

    void Update() {
        
    }

    #endregion
}
