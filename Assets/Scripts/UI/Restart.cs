using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Restart : MonoBehaviour
{
    public static bool isHolding = false;
    float heldAtTime = 0f;
    public float holdTime = 0.75f;
    
    // References
    private UIManager uiManager;
    private ScoreManager scoreManager;
    private AmmoDisplay ammoDisplay;

    // Static method to reset all necessary static variables
    public static void ResetStaticVariables()
    {
        // Reset FloorAccessController variables
        FloorAccessController.isLevelComplete = false;
    }

    void Start()
    {
        //GameObject CanvasFade = GameObject.Find("CanvasFade");
        //CanvasFade.SetActive(true);
        
        // Get references
        uiManager = UIManager.Instance; 
        scoreManager = ScoreManager.Instance;
        ammoDisplay = FindObjectOfType<AmmoDisplay>();
        
        if (uiManager == null) Debug.LogWarning("Restart script could not find UIManager.", this);
        if (ammoDisplay == null) Debug.LogWarning("Restart script could not find AmmoDisplay.", this);
        
        // Reset static variables when scene starts
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

            // Reset static variables before loading the scene
            ResetStaticVariables();
            
            // Load scene - the ScoreManager's OnSceneLoaded handler will reset its state
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
