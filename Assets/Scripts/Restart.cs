using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class Restart : MonoBehaviour
{

    public static bool isHolding = false;
    float heldAtTime = 0f;
    public float holdTime = 0.75f;
    void Start()
    {
        //GameObject CanvasFade = GameObject.Find("CanvasFade");
        //CanvasFade.SetActive(true);
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            isHolding = true;
            heldAtTime = Time.time;
        }
        else if(Input.GetKeyUp(KeyCode.R))
        {
            isHolding = false;
            heldAtTime = 0f;
        }
        if(isHolding && Time.time - heldAtTime > holdTime)
        {
            isHolding = false;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            heldAtTime = 0f;
        }
    }
}
