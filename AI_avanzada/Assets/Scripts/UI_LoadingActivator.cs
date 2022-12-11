using UnityEngine;
using System.Collections;
/*
* ZiroDev Copyright(c)
*
*/
public class UI_LoadingActivator : MonoBehaviour {

    #region Variables
    public static bool IsRunning = false;
    public GameObject UI;
    public static UI_LoadingActivator instance;
    #endregion

    #region Unity Methods
    
    void Awake() {
        IsRunning = false;
        instance = this;
    }

    void Update() {
        
    }

    public static IEnumerator Activate() {
        IsRunning = true;
        instance.UI.SetActive(true);
        yield return null;
	}

    public static void Deactivate() {
        instance.UI.SetActive(false);
        IsRunning = false;
    }

    #endregion
}
