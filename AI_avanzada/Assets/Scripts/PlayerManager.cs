using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
/*
* ZiroDev Copyright(c)
*
*/
public class PlayerManager : MonoBehaviour {

	#region Variables
	public static PlayerManager instance;
	int DeadCounter;
	public GameObject gameScene;
	public GameObject menuscene;
	public GameObject gameoverScene;
	public HomeBase playerHome;
	public bool SetCamera = false;
	static public int index = 0;

	public GameObject buttonsMenu;
	public Image[] icons;


	#endregion

	#region Unity Methods

	void Start() {
		instance = this;
	}

	void Update() {
	
	}
	public static int choosenIndex  = 0;
	public void ButtonPressed(int i) {
		choosenIndex = i;
		
		
		Time.timeScale = 1;
		MapBuilder.instance.MakeMap = true;
		buttonsMenu.SetActive(false);
	}

	public void ShowMenu(bool playerDied = false) {
		if(playerDied)DeadCounter++;
		if (DeadCounter >= 3) {
			gameoverScene.SetActive(true);
			gameScene.SetActive(false);
		} else {
			for (int i = 0; i < icons.Length; i++) {
				icons[i].sprite = playerHome.tribe[i].sprite.sprite;
			}
			buttonsMenu.SetActive(true);
			Time.timeScale = 0;
		}
	}

	public void onPlayButton(int indx) {
		SetCamera = true;
		index = indx;
		gameScene.SetActive(true);
		menuscene.SetActive(false);
	}

	public void KillScene() {
		//SceneManager.ResetStaticVariables();
		// Restart the current scene to apply the changes
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}

	#endregion
}
