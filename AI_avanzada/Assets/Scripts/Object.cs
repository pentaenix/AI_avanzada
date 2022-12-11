using UnityEngine;
/*
* ZiroDev Copyright(c)
*
*/
public class Object : MonoBehaviour {

    #region Variables


    public SpriteRenderer sprite;
    public ObjectLibrary.Type type;
    public ObjectLibrary.Resource resource;
    public intVector nodeIndex;
    public int hp = 1;
    #endregion

    #region Unity Methods
    

    public ObjectLibrary.Resource Gather() {
        hp--;
        if(hp <= 0) {
            ObjectLibrary.instance.RemoveFromList(gameObject,this);
        }
        return resource;

	}

    public void SetUp(ObjectLibrary.Type t, intVector n) {
        MapBuilder.GetNode(n).NaturalResource = this;
        nodeIndex = n;
        type = t;
        resource = ObjectLibrary.instance.SetResource(t);
        hp = ObjectLibrary.instance.GetHP(t);
        sprite.sprite = ObjectLibrary.instance.GetSprite(type);
    }


    #endregion
}
