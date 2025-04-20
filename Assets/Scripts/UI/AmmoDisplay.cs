using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.UI;

public class AmmoDisplay : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI ammoText;
    //[SerializeField] public Image weaponIconImage; //unused
    [SerializeField] public PlayerController playerController;
    private PlayerEquipment playerEquipment;
    
    void Start()
    {
        ammoText.gameObject.SetActive(true);
        // Find player controller on start, need for restarting
        if (playerController == null)
        {
            playerController = FindObjectsByType<PlayerController>(FindObjectsSortMode.None).FirstOrDefault();
            if (playerController == null) 
            {
                Debug.LogError("could not find playercontroller!");
            }
        }
        
        if (playerController != null)
        {
            playerEquipment = playerController.GetComponent<PlayerEquipment>();
            
        
            if (playerEquipment != null)
            {
                // unsubscribe to prevent double-subscription on scene reload
                playerEquipment.OnWeaponChanged -= OnWeaponChanged; 
                playerEquipment.OnWeaponChanged += OnWeaponChanged;
                
                // first update
                UpdateAmmoDisplay();
            }
            else
            {
                Debug.LogError("ammodisplay missing PlayerEquipment");
            }
        }
        
        // Ensure we have a text component
        if (ammoText == null)
        {
            ammoText = GetComponent<TextMeshProUGUI>();
            
            if (ammoText == null)
            {
                Debug.LogError("ammodisplay missing text");
                enabled = false;
                return;
            }
        }
        
        UpdateAmmoDisplay();
    }
    
    void OnDestroy()
    {
        // unsubscribe from event when destroyed to prevent memory leaks
        if (playerEquipment != null)
        {
            playerEquipment.OnWeaponChanged -= OnWeaponChanged;
        }
    }
    
    private void OnWeaponChanged()
    {
        UpdateAmmoDisplay();
    }
    
    
    private void UpdateAmmoDisplay()
    {
        if (playerEquipment != null && playerEquipment.CurrentWeapon != null)
        {
            WeaponData weapon = playerEquipment.CurrentWeapon;
            
            // normal weapon
            if (weapon.canShoot && !weapon.isMelee)
            {
                ammoText.text = weapon.currentAmmo.ToString();
            }
            else
            {
                // show empty string instead of disabling
                ammoText.text = "";
                //Debug.Log("ammodisplay: empty string");
            }
        }
        else
        {
            //nothing equipped
            ammoText.text = "";
            ammoText.gameObject.SetActive(true);
            Debug.Log("AmmoDisplay: No weapon equipped, showing empty display");
        }
    }
    
    // called on restart
    public void ResetDisplay()
    {
        //show empty string instead of disabling
        if (ammoText != null) 
        {
            ammoText.text = "";
        }
        Debug.Log("ammodisplay: reset");
    }

    void Update()
    {
        if (playerEquipment != null && playerEquipment.CurrentWeapon != null)
        {
            WeaponData weapon = playerEquipment.CurrentWeapon;
            
            if (weapon.canShoot && !weapon.isMelee && ammoText.gameObject.activeSelf)
            {
                int currentAmmo = weapon.currentAmmo;

                ammoText.text = currentAmmo.ToString();
            }
        }
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        //reinint
        Start();
    }
}