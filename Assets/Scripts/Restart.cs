using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class Restart : MonoBehaviour
{

    public static bool isHolding = false;
    float heldAtTime = 0f;
    public float holdTime = 0.75f;
    
    // ADDED: References
    private UIManager uiManager;
    private AmmoDisplay ammoDisplay;

    void Start()
    {
        //GameObject CanvasFade = GameObject.Find("CanvasFade");
        //CanvasFade.SetActive(true);
        
        // ADDED: Get references
        uiManager = UIManager.Instance; 
        ammoDisplay = FindObjectOfType<AmmoDisplay>();
        
        if (uiManager == null) Debug.LogWarning("Restart script could not find UIManager.", this);
        if (ammoDisplay == null) Debug.LogWarning("Restart script could not find AmmoDisplay.", this);
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
            heldAtTime = 0f;

            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}
