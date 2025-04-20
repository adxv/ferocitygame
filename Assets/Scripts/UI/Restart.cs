using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Restart : MonoBehaviour
{
    public static bool isHolding = false;
    float heldAtTime = 0f;
    public float holdTime = 0.75f; // 0.75 seconds
    
    private UIManager uiManager;
    private ScoreManager scoreManager;
    private AmmoDisplay ammoDisplay;

    public static void ResetStaticVariables()
    {
        FloorAccessController.isLevelComplete = false;
    }

    void Start()
    {
        //GameObject CanvasFade = GameObject.Find("CanvasFade");
        //CanvasFade.SetActive(true);
        
        uiManager = UIManager.Instance; 
        scoreManager = ScoreManager.Instance;
        ammoDisplay = FindFirstObjectByType<AmmoDisplay>();
        
        if (uiManager == null) Debug.LogWarning("could not find UIManager.", this);
        if (ammoDisplay == null) Debug.LogWarning("could not find AmmoDisplay.", this);
        
        ResetStaticVariables();
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

            ResetStaticVariables();
            
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
