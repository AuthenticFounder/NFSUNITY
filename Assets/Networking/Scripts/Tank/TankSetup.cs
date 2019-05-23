using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

//Purpose of that class is syncing data between server - client
public class TankSetup : NetworkBehaviour 
{
    [Header("UI")]
    public Text m_NameText;
    public GameObject m_Crown;
    public Material mainMaterial;
    [Header("Network")]
    [Space]
    [SyncVar]
    public Color m_Color;

    [SyncVar]
    public string m_PlayerName;

    //this is the player number in all of the players
    [SyncVar]
    public int m_PlayerNumber;

    //This is the local ID when more than 1 player per client
    [SyncVar]
    public int m_LocalID;

    [SyncVar]
    public bool m_IsReady = false;

    //This allow to know if the crown must be displayed or not
    protected bool m_isLeader = false;
    
    CarController m_CarController;
    bool m_HasCarController;
    public GameObject m_TankRenderers;
    public Transform currentSpawnPoint;
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!isServer) //if not hosting, we had the tank to the gamemanger for easy access!
            GameManager.AddTank(gameObject, m_PlayerNumber, m_Color, m_PlayerName, m_LocalID);

        //m_TankRenderers = transform.Find("TankRenderers").gameObject;

        // Get all of the renderers of the tank.
        Renderer[] renderers = m_TankRenderers.GetComponentsInChildren<Renderer>();

        // Go through all the renderers...
        for (int i = 0; i < renderers.Length; i++)
        {
            // ... set their material color to the color specific to this tank.
            if(renderers[i].material.name.ToLower() == mainMaterial.name.ToLower() + " (instance)")
                renderers[i].material.color = m_Color;
        }

        //if (m_TankRenderers)
        //    m_TankRenderers.SetActive(false);

        m_NameText.text = "<color=#" + ColorUtility.ToHtmlStringRGB(m_Color) + ">"+m_PlayerName+"</color>";
        m_Crown.SetActive(false);

        m_CarController = GetComponent<CarController>();
        if (m_CarController && isLocalPlayer)
            m_CarController.enabled = true;
    }

    private void GetCarController()
    {
        if (!isLocalPlayer) return;
        if (!m_HasCarController)
        {
            m_CarController = GetComponent<CarController>();
            m_CarController.enabled = true;
            m_HasCarController = true;
        }
    }

    public void DisableInput()
    {
        if (m_CarController) m_CarController.enabled = false; else Debug.LogError("Cant disable input, CarController not found!");
    }
    public void EnableInput()
    {
        if (m_CarController) m_CarController.enabled = true; else Debug.LogError("Cant enable input, CarController not found!");
    }

    // This function is called at the start of each round to make sure each tank is set up correctly.
    public void SetDefaults()
    {
        if (!m_CarController) return;
        Rigidbody m_Rigidbody = m_CarController.GetComponent<Rigidbody>();
        m_Rigidbody.velocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;

        m_CarController.throttleInput = 0f;
        m_CarController.steering = 0;        
    }

    void UpdateCar()
    {

        if (!m_CarController) return;

        if (isLocalPlayer)
        {
            CarCamera cc = Camera.main.GetComponent<CarCamera>();
            if (cc)
            {
                cc.target = transform;
            }
            
            CarUI cu = GameObject.FindObjectOfType<CarUI>();
            if (cu)
            {
                cu.car = GetComponent<Drivetrain>();
            }
        }
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer)
            return;
        
        UpdateCar();
    }

    [ClientCallback]
    public void Update()
    {
        if (GameManager.s_Instance.m_GameIsFinished && !m_IsReady)
        {
            if(Input.GetButtonDown("Fire"+(m_LocalID + 1)))
            {
                //CmdSetReady();
            }
        }
    }

    public void SetLeader(bool leader)
    {
        RpcSetLeader(leader);
    }

    [ClientRpc]
    public void RpcSetLeader(bool leader)
    {
        m_isLeader = leader;
    }

    [Command]
    public void CmdSetReady()
    {
        m_IsReady = true;
    }

    public void ActivateCrown(bool active)
    {//if we try to show (not hide) the crown, we only show it we are the current leader
        m_Crown.SetActive(active ? m_isLeader : false);
        m_NameText.gameObject.SetActive(active);
    }

    public override void OnNetworkDestroy()
    {
        GameManager.s_Instance.RemoveTank(gameObject);
    }
}
