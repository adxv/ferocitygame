using UnityEngine;

public class IncomingSoundDetector : MonoBehaviour
{
    private Enemy enemyController;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemyController = GetComponentInParent<Enemy>();
        
        if (enemyController == null)
        {
            Debug.LogError("IncomingSoundDetector requires a parent with Enemy component");
        }
    }

    public void DetectSound()
    {
        if (enemyController != null)
        {
            // Use method to set enemy to pursue state
            enemyController.HearSound();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
