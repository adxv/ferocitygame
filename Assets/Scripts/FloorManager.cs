using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FloorManager : MonoBehaviour
{
    [Header("Floor Settings")]
    public string floorName = "Floor 1";
    public int floorIndex = 0; // 0-based index for floor identification
    public Color gizmoColor = Color.cyan;
    public Vector2 floorBounds = new Vector2(30f, 20f); // Width, Height of the floor area
    
    // Optional list of stairways on this floor
    public List<FloorAccessController> stairwaysOnFloor = new List<FloorAccessController>();

    private void OnDrawGizmos()
    {
        // Draw floor boundaries in the editor for visualization
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(transform.position, new Vector3(floorBounds.x, floorBounds.y, 1f));
        
        // Draw floor name text
        #if UNITY_EDITOR
        Handles.Label(transform.position, floorName);
        #endif
    }
    
    // Get a list of all enemies on this floor
    public Enemy[] GetEnemiesOnFloor()
    {
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();
        List<Enemy> enemiesOnFloor = new List<Enemy>();
        
        Bounds floorArea = new Bounds(transform.position, new Vector3(floorBounds.x, floorBounds.y, 10f));
        
        foreach (Enemy enemy in allEnemies)
        {
            if (floorArea.Contains(enemy.transform.position))
            {
                enemiesOnFloor.Add(enemy);
            }
        }
        
        return enemiesOnFloor.ToArray();
    }
    
    // Check if all enemies on this floor are dead
    public bool AreAllEnemiesDead()
    {
        Enemy[] enemiesOnFloor = GetEnemiesOnFloor();
        
        foreach (Enemy enemy in enemiesOnFloor)
        {
            if (!enemy.isDead)
            {
                return false;
            }
        }
        
        return true;
    }
} 