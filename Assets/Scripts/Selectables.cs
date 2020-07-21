using UnityEngine;

public class Selectables : MonoBehaviour
{
    public enum SelectableType { Tile, Crate, Cube }
    public SelectableType selectableType;

    public RuntimeAnimatorController animHighlight;

    void Start()
    {

    }

    void Update()
    {

    }

    private void StartClickAnim()
    {
        gameObject.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
        gameObject.GetComponent<Animator>().runtimeAnimatorController = animHighlight;
        gameObject.GetComponent<Animator>().Play("Highlight", -1, 0f);
    }

    public void OnClickAnimFinished()
    {
        gameObject.GetComponent<Renderer>().material.DisableKeyword("_EMISSION");
        gameObject.GetComponent<Animator>().runtimeAnimatorController = null;
    }

#if UNITY_STANDALONE
    void OnMouseDown() // LMB
    {
        /*
        This is for selecting objects
        -clicking on objects (tile, cube, crate) will trigger the animation for selection (correct/incorrect anims)
        -on select, the player will go to the nearest adjacent edge and take push/pull motion
        */

        // click animation
        StartClickAnim();

        // push//pulling motion
        if (selectableType == SelectableType.Crate || selectableType == SelectableType.Cube)
        {

        }
    }

    void OnMouseOver() // RMB
    {
        /*
        This is for moving the player
        -clicking on objects (tile, cube, crate) will trigger the animation for selection (correct/incorrect anims)
        -on select, the player will move to the closet adjacent edge/block
        */

        if (Input.GetMouseButtonDown(1))
        {
            // click animation
            StartClickAnim();

            // events depending on selectable type
            switch (selectableType)
            {
                case SelectableType.Tile:

                    break;
                case SelectableType.Crate:

                    break;
                case SelectableType.Cube:

                    break;
                default:

                    break;
            }
        }
    }
#else
    /*
    void OnMouseDown() // LMB
    {
        
        This is for moving the player and selecting the object
        

        // click animation

        // push//pulling motion
        if (selectableType == SelectableType.Crate || selectableType == SelectableType.Cube)
        {
        
        }
    }
    */
    void OnMouseDown() // LMB
    {
        /*
        This is for selecting objects
        -clicking on objects (tile, cube, crate) will trigger the animation for selection (correct/incorrect anims)
        -on select, the player will go to the nearest adjacent edge and take push/pull motion
        */

        // click animation
        StartClickAnim();

        // push//pulling motion
        if (selectableType == SelectableType.Crate || selectableType == SelectableType.Cube)
        {

        }
    }

    void OnMouseOver() // RMB
    {
        /*
        This is for moving the player
        -clicking on objects (tile, cube, crate) will trigger the animation for selection (correct/incorrect anims)
        -on select, the player will move to the closet adjacent edge/block
        */

        if (Input.GetMouseButtonDown(1))
        {
            // click animation
            StartClickAnim();

            // events depending on selectable type
            switch (selectableType)
            {
                case SelectableType.Tile:

                    break;
                case SelectableType.Crate:

                    break;
                case SelectableType.Cube:

                    break;
                default:

                    break;
            }
        }
    }

#endif
}
