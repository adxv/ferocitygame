using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FloorManager : MonoBehaviour
{
    public string floorName = "Floor 1";
    public int floorIndex = 0;
    public Color gizmoColor = Color.cyan;
    public Vector2 floorBounds = new Vector2(30f, 20f);
    
    //public List<FloorAccessController> stairwaysOnFloor = new List<FloorAccessController>(); //unused

    private void OnDrawGizmos()
    {
        // draw floor boundaries in editor
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(transform.position, new Vector3(floorBounds.x, floorBounds.y, 1f));
    }
    
    // get a list of all enemies on this floor
    public Enemy[] GetEnemiesOnFloor()
    {
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
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
    
    // check if all enemies on this floor are dead
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