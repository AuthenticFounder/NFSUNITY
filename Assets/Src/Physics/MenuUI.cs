using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[System.Serializable]
public class CarHUD
{
    public Transform panel;
    public Text speed;
    public Text gear;
    public Text rpm;

    public Dial tacho;
    
}

[System.Serializable]
public class Dial
{
    public Transform needle;
    public float minAngle = -125;
    public float maxAngle = 125;
    public float range = 8000;

    public bool smooth = true;
    public float dampFactor = 0.1f;

    public bool flip;
    [HideInInspector]
    public float fraction;
    [HideInInspector]
    public float smoothFraction;
}

public enum GameMenuMode
{
    MainMenu,
    Settings,
    RaceSetup,
    RaceSetupMP,
    Garage,
    Game,
    Replay,
    Paused,
    Loading
}

public enum UIState
{
    MainMenu,
    HUD
}

public class MenuUI : MonoBehaviour {

    public GameMenuMode menuMode = GameMenuMode.MainMenu;
    public CarHUD hud;
    public Text status;
    public string menuScene = "Menu";
    public string gameScene = "Practice";
    public string multiplayerScene = "Network";
    public Transform rootPanel;
    public Transform settingsPanel;
    public Transform hudPanel;
    public Transform replayPanel;
    public Transform raceSetupPanel;
    public Transform garagePanel;
    public Transform loadingPanel;
    public Transform pausePanel;
    public List<Transform> panel;
    public bool settingsInFront;

    private int currentSettingsId;
    private Drivetrain car;

    private bool hasTarget;
    private GameMenuMode prevMenuMode;
    private AsyncOperation asyncOperation;
    public static MenuUI instance;

    bool showPauseMenu;
    bool sceneIsDetected;
    bool isRecording;

    void Start()
    {
        /*
        DontDestroyOnLoad(this.gameObject);
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            DestroyObject(instance.gameObject);
        }
        //Call the LoadButton() function when the user clicks this Button
        //m_Button.onClick.AddListener(LoadButton);
        */
        int num = FindObjectsOfType<MenuUI>().Length;
        if (num != 1)
        {
            Destroy(this.gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    void Awake2()
    {
        instance = this;
        //LoadValues(); // Do we have something to load from previos scene or any other user stored values?
    }

    void OnLevelWasLoaded()
    {
        DynamicGI.UpdateEnvironment();
    }

    void GetTarget()
    {
        if (!hasTarget)
        {
            if (CarCamera.instance) car = CarCamera.instance.target.GetComponent<Drivetrain>();
            hasTarget = true;
        }
    }

    void UpdateTacho(Drivetrain car)
    {
        float value = car.rpm;
        hud.tacho.fraction = Mathf.Lerp(hud.tacho.minAngle, hud.tacho.maxAngle, value / hud.tacho.range);

        if (hud.tacho.smooth)
        {
            hud.tacho.smoothFraction = Mathf.Lerp(hud.tacho.smoothFraction, hud.tacho.fraction, Time.deltaTime / hud.tacho.dampFactor);
            hud.tacho.fraction = hud.tacho.smoothFraction;
        }

        hud.tacho.needle.rotation = Quaternion.Euler((hud.tacho.fraction * (hud.tacho.flip ? -1 : 1)) * Vector3.forward);
    }

    void Update()
    {
        //DetectScene(); // may cause problems
        DetectGame();
        UpdateUI();
    }


    void UpdateReplay()
    {
        if (!isRecording)
        {
            Drivetrain[] c = GameObject.FindObjectsOfType<Drivetrain>();
            ReplayManager.instance.GetRacersAndStartRecording(c);
            isRecording = true;
        }
    }

    void UpdateHUD()
    {
        // hud
        GetTarget();

        if (!car) return;

        if (hud.panel) hud.panel.gameObject.SetActive(menuMode == GameMenuMode.Game);
        UpdateTacho(car);
        if (hud.speed) hud.speed.text = "Velocity: " + car.Speed.ToString("0") + " km/h";
        if (hud.gear) hud.gear.text = "Gear: " + car.Gear.ToString("0");
        if (hud.rpm) hud.rpm.text = "RPM: " + car.rpm.ToString("0");
    }

    public void PauseResume()
    {
        
        if (menuMode == GameMenuMode.Paused)
        {
            //Handle un-pausing
            menuMode = GameMenuMode.Game;
            Time.timeScale = 1.0f;
            AudioListener.volume = 1.0f;
            //SmoothFollow.instance.ChaseCameraClose = true;
        }
        else if (menuMode == GameMenuMode.Replay)
        {
            // hande replay
            menuMode = GameMenuMode.Paused;
            Time.timeScale = 0.0f;
            AudioListener.volume = 0.0f;
            QuitReplay();
        }
        else
        {
            //Handle pausing
            menuMode = GameMenuMode.Paused;
            Time.timeScale = 0.0f;
            AudioListener.volume = 0.0f;
            //SmoothFollow.instance.MenuCamera = true;
        }
    }

    void UpdateUI()
    {
        switch(menuMode)
        {
            case GameMenuMode.MainMenu:
                UpdateMainMenuUI();
                break;
            case GameMenuMode.Settings:
                UpdateSettingsUI();
                break;
            case GameMenuMode.RaceSetup:
                UpdateRaceSetupUI();
                break;
            case GameMenuMode.Garage:
                UpdateGarageUI();
                break;
            case GameMenuMode.Game:
                UpdateReplay();
                UpdateHUD();
                UpdateGameUI(); // not to be confused with UpdateHUD() !
                break;
            case GameMenuMode.Replay:
                UpdateReplayUI();
                break;
            case GameMenuMode.Loading:
                UpdateLoadingUI();
                break;
        }
    }

    void UpdateMainMenuUI()
    {
        if (pausePanel) pausePanel.gameObject.SetActive(false);
        if (rootPanel) rootPanel.gameObject.SetActive(true);
        //if (hudPanel) hudPanel.gameObject.SetActive(false);
        if (hud.panel) hud.panel.gameObject.SetActive(false);
        if (settingsPanel) settingsPanel.gameObject.SetActive(false);
        if (replayPanel) replayPanel.gameObject.SetActive(false);
        if (raceSetupPanel) raceSetupPanel.gameObject.SetActive(false);
        if (garagePanel) garagePanel.gameObject.SetActive(false);
        if (loadingPanel) loadingPanel.gameObject.SetActive(false);
    }
    void UpdateGameUI()
    {
        // temporary pausing solution, should be rewritten! (works for multiplayer)
        if (Input.GetKeyDown(KeyCode.Escape)) showPauseMenu = !showPauseMenu;
        if (rootPanel) rootPanel.gameObject.SetActive(false);
        if (pausePanel) pausePanel.gameObject.SetActive(showPauseMenu);
        //if (hudPanel) hudPanel.gameObject.SetActive(!showPauseMenu);
        if (hudPanel) hudPanel.gameObject.SetActive(!showPauseMenu);
        if (settingsPanel) settingsPanel.gameObject.SetActive(false);
        if (replayPanel) replayPanel.gameObject.SetActive(false);
        if (raceSetupPanel) raceSetupPanel.gameObject.SetActive(false);
        if (garagePanel) garagePanel.gameObject.SetActive(false);
        if (loadingPanel) loadingPanel.gameObject.SetActive(false);
    }
    void UpdateSettingsUI()
    {
        if (pausePanel) pausePanel.gameObject.SetActive(false);
        if (rootPanel) rootPanel.gameObject.SetActive(false);
        //if (hudPanel) hudPanel.gameObject.SetActive(false);
        if (hud.panel) hud.panel.gameObject.SetActive(false);
        if (settingsPanel) settingsPanel.gameObject.SetActive(true);
        if (replayPanel) replayPanel.gameObject.SetActive(false);
        if (raceSetupPanel) raceSetupPanel.gameObject.SetActive(false);
        if (garagePanel) garagePanel.gameObject.SetActive(false);
        if (loadingPanel) loadingPanel.gameObject.SetActive(false);
    }
    void UpdateReplayUI()
    {
        if (pausePanel) pausePanel.gameObject.SetActive(false);
        if (rootPanel) rootPanel.gameObject.SetActive(false);
        //if (hudPanel) hudPanel.gameObject.SetActive(false);
        if (hud.panel) hud.panel.gameObject.SetActive(false);
        if (settingsPanel) settingsPanel.gameObject.SetActive(false);
        if (replayPanel) replayPanel.gameObject.SetActive(true);
        if (raceSetupPanel) raceSetupPanel.gameObject.SetActive(false);
        if (garagePanel) garagePanel.gameObject.SetActive(false);
        if (loadingPanel) loadingPanel.gameObject.SetActive(false);
    }
    void UpdateRaceSetupUI()
    {
        if (pausePanel) pausePanel.gameObject.SetActive(false);
        if (rootPanel) rootPanel.gameObject.SetActive(false);
        //if (hudPanel) hudPanel.gameObject.SetActive(false);
        if (hud.panel) hud.panel.gameObject.SetActive(false);
        if (settingsPanel) settingsPanel.gameObject.SetActive(false);
        if (replayPanel) replayPanel.gameObject.SetActive(false);
        if (raceSetupPanel) raceSetupPanel.gameObject.SetActive(true);
        if (garagePanel) garagePanel.gameObject.SetActive(false);
        if (loadingPanel) loadingPanel.gameObject.SetActive(false);
    }
    void UpdateGarageUI()
    {
        if (pausePanel) pausePanel.gameObject.SetActive(false);
        if (rootPanel) rootPanel.gameObject.SetActive(false);
        //if (hudPanel) hudPanel.gameObject.SetActive(false);
        if (hud.panel) hud.panel.gameObject.SetActive(false);
        if (settingsPanel) settingsPanel.gameObject.SetActive(false);
        if (replayPanel) replayPanel.gameObject.SetActive(false);
        if (raceSetupPanel) raceSetupPanel.gameObject.SetActive(false);
        if (garagePanel) garagePanel.gameObject.SetActive(true);
        if (loadingPanel) loadingPanel.gameObject.SetActive(false);
    }
    void UpdateLoadingUI()
    {
        if (pausePanel) pausePanel.gameObject.SetActive(false);
        if (rootPanel) rootPanel.gameObject.SetActive(false);
        //if (hudPanel) hudPanel.gameObject.SetActive(false);
        if (hud.panel) hud.panel.gameObject.SetActive(false);
        if (settingsPanel) settingsPanel.gameObject.SetActive(false);
        if (replayPanel) replayPanel.gameObject.SetActive(false);
        if (raceSetupPanel) raceSetupPanel.gameObject.SetActive(false);
        if (garagePanel) garagePanel.gameObject.SetActive(false);
        if (loadingPanel) loadingPanel.gameObject.SetActive(true);
    }

    public void Back()
    {        
        switch (menuMode)
        {
            // Return to main menu
            case GameMenuMode.Paused:
                /*
                if (fadeOnExit && screenFade)
                {
                    StartCoroutine(ScreenFadeOut(fadeSpeed * 2, true, menuScene));
                }
                else
                {
                    StartCoroutine(LoadScene(menuScene));
                }*/
                LoadScene(menuScene);
                break;
            case GameMenuMode.Game:
                menuMode = prevMenuMode;
                break;
            case GameMenuMode.MainMenu:
                menuMode = prevMenuMode;
                break;
            case GameMenuMode.Settings:
                menuMode = prevMenuMode;
                break;
            case GameMenuMode.RaceSetup:
                menuMode = prevMenuMode;
                break;
            case GameMenuMode.RaceSetupMP:
                menuMode = prevMenuMode;
                break;
            case GameMenuMode.Garage:
                menuMode = prevMenuMode;
                break;
            case GameMenuMode.Loading:
                menuMode = prevMenuMode;
                break;
            case GameMenuMode.Replay:
                QuitReplay();
                //if (ReplayManager.instance) { ReplayManager.instance.StopRecording(); }
                menuMode = prevMenuMode;
                //if(menuMode == GameMenuMode.Game) showPauseMenu = false;
                break;
        }
        UpdateUI();
    }

    public void ExitGame()
    {
        
        if (menuMode == GameMenuMode.Paused)
        {
            PauseResume();
        }
        // save any game data here
        #if UNITY_EDITOR
        // Application.Quit() does not work in the editor so
        // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
        UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    void DetectGame()
    {
        if (!sceneIsDetected)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name == gameScene)
            {
                menuMode = GameMenuMode.Game;
            }
            else if(scene.name == menuScene)
            {
                menuMode = GameMenuMode.MainMenu;
            }
            sceneIsDetected = true;
        }
    }
    void DetectScene()
    {
        if(!sceneIsDetected)
        {
            if (status) status.gameObject.SetActive(false);
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name == gameScene)
            {
                menuMode = GameMenuMode.Game;
            }
            else if (scene.name == menuScene)
            {
                menuMode = GameMenuMode.MainMenu;
            }
            sceneIsDetected = true;
        }
    }

    bool IsSettingsActive()
    {
        bool result = false;

        if (panel.Count <= 0) return false;

        for (int i = 0; i < panel.Count; i++)
        {
            if (panel[i].gameObject.activeSelf)
                result = true;
        }
        return result;
    }
    /*
    public void ShowRootPanel()
    {
        currentSettingsId = 0;
        HidePanels();
        
        //if(IsSettingsActive())
        if (settingsPanel) settingsPanel.gameObject.SetActive(false); //
        //if (panel[currentSettingsId]) panel[currentSettingsId].parent.gameObject.SetActive(false);

        if (rootPanel)
            rootPanel.gameObject.SetActive(true);
    }*/

    public void LoadNextScene()
    {
        //Start loading the Scene asynchronously and output the progress bar
        StartCoroutine(LoadAsyncGameSP());
    }

    public void Restart()
    {
        StartCoroutine(LoadScene(gameScene));
        showPauseMenu = false;
    }

    IEnumerator LoadScene(string scene)
    {
        asyncOperation = SceneManager.LoadSceneAsync(scene);
        while (!asyncOperation.isDone)
        {
            //if (loadingProgress) loadingProgress.fillAmount = asyncOperation.progress;
            yield return null;
        }
    }

    IEnumerator LoadAsyncGameSP()
    {
        yield return null;

        //Begin to load the Scene you specify
        //AsyncOperation asyncOperation = null;
        
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name == gameScene)
        {
            asyncOperation = SceneManager.LoadSceneAsync(menuScene);
            menuMode = GameMenuMode.Game;
        }
        else if (scene.name == menuScene)
        {
            asyncOperation = SceneManager.LoadSceneAsync(gameScene);
            menuMode = GameMenuMode.MainMenu;
            menuMode = prevMenuMode;
        }

        ShowLoading();
        if (status) status.text = string.Format("Loading {0}",asyncOperation.progress.ToString("%"));

        //Don't let the Scene activate until you allow it to
        asyncOperation.allowSceneActivation = false;
        Debug.Log("Menu UI - Load Scene : " + gameScene);
        //When the load is still in progress, output the Text and progress bar
        while (!asyncOperation.isDone)
        {
            if (status)
            {
                status.gameObject.SetActive(true);
                //Output the current progress
                status.text = string.Format("{0} - Loading progress: {0}", gameScene, asyncOperation.progress.ToString("%"));
            }
            // Raise a flag to identify that the scene needs to be updated

            // also tell the replaymanager to stop recording
            if (ReplayManager.instance) { ReplayManager.instance.StopRecording(); }

            //if (loadingProgress) loadingProgress.fillAmount = asyncOperation.progress;
            // Check if the load has finished
            if (asyncOperation.progress >= 0.9f)
            {
                //Change the Text to show the Scene is ready
                status.text = "Press any key to continue...";
                //Wait to you press the space key to activate the Scene
                if (Input.anyKeyDown)
                { 
                    //Activate the Scene
                    asyncOperation.allowSceneActivation = true;

                    sceneIsDetected = false;
                }
            }

            yield return null;
        }
    }

    IEnumerator LoadAsyncGameMP()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(multiplayerScene);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    public void ShowLoading()
    {
        prevMenuMode = menuMode;
        menuMode = GameMenuMode.Loading;

        Debug.Log("MenuUI - Loading");
    }
    public void ShowRaceSetup()
    {
        prevMenuMode = menuMode;
        menuMode = GameMenuMode.RaceSetup;

        Debug.Log("MenuUI - Race Setup");
    }

    public void ShowGarage()
    {
        prevMenuMode = menuMode;
        menuMode = GameMenuMode.Garage;

        Debug.Log("MenuUI - Garage");
    }

    public void ShowSettings(int i)
    {
        prevMenuMode = menuMode;
        menuMode = GameMenuMode.Settings;

        //if (rootPanel)
        //    rootPanel.gameObject.SetActive(false);

        //if (settingsPanel) settingsPanel.gameObject.SetActive(true);

        currentSettingsId = i;
        if (panel.Count<=0) return;
        HideSettings();
        Debug.Log("MenuUI - Show Settings "+ i);

        if (panel[i])
            panel[i].gameObject.SetActive(true);
    }

    void HideSettings()
    {
        if (panel.Count <= 0) return;
        for (int i = 0; i < panel.Count; i++)
        {
            panel[i].gameObject.SetActive(false);
        }
    }

    public void ViewReplay()
    {
        AudioListener.volume = 1.0f;
        prevMenuMode = menuMode;
        menuMode = GameMenuMode.Replay;
        //CarCamera.instance.spectatorView = true;
        if (!CarCamera.instance) return;
        CarCamera.instance.trackMode = true;

        //CarCamera.instance.enabled = false;
        //Animator anim = CarCamera.instance.gameObject.GetComponent<Animator>();
        //if (anim) anim.enabled = true;

        //SmoothFollow.instance.spectatorMode = true;
        //SmoothFollow.instance.SpectatorCamera = true;
        //CarController.instance.disableInput = true;
        if (!ReplayManager.instance) return;
        ReplayManager.instance.StopRecording();
        ReplayManager.instance.SetPlaybackSpeed(1);
        if (ReplayManager.instance.TotalFrames <= 0) return;
        if (ReplayManager.instance.CurrentFrame <= 2) ReplayManager.instance.ResetScene();
        ReplayManager.instance.replayState = ReplayManager.ReplayState.Playing;
        Debug.Log("MenuUI - View Replay");
    }

    public void QuitReplay()
    {
        //prevMenuMode = menuMode;
        //menuMode = prevMenuMode;
        if (!ReplayManager.instance) return;
        //CarCamera.instance.spectatorView = false;
        if (!CarCamera.instance) return;
        CarCamera.instance.trackMode = false;

        //CarCamera.instance.enabled = true;
        //Animator anim = CarCamera.instance.gameObject.GetComponent<Animator>();
        //if (anim) anim.enabled = false;

        if (prevMenuMode == GameMenuMode.Paused || showPauseMenu)
        {
            ReplayManager.instance.SetLastFrame();
            ReplayManager.instance.replayState = ReplayManager.ReplayState.Recording;
            //SmoothFollow.instance.spectatorMode = false;
            //SmoothFollow.instance.ChaseCameraClose = true;
            //CarController.instance.disableInput = false;
            PauseResume();
        }
        Debug.Log("MenuUI - Quit Replay");
    }
}
