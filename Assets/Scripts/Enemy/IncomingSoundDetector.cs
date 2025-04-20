using UnityEngine;

public class IncomingSoundDetector : MonoBehaviour
{
    private Enemy enemyController;
    
    void Start()
    {
        enemyController = GetComponentInParent<Enemy>();
        
        if (enemyController == null)
        {
            Debug.LogError("IncomingSoundDetector requires a parent with enemy component");
        }
    }
    public void DetectSound()
    {
        if (enemyController != null)
        {
            // set enemy to pursue state
            enemyController.HearSound();
        }
    }
}
