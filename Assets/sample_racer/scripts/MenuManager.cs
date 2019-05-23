using UnityEngine;
using System.Collections;

public class MenuManager : MonoBehaviour {

	public enum Menus 
	{
		MainMenu,
		CustomizeSelection,
		TireCustom,
		PaintCustom,
		SusCustom,
		Options,
		LevelSelect,
		NetworkSingle,
		Network_,
		Modes,
		Mode
	}

	Menus currentMenu;

	[System.Serializable]
	public class UIElements
	{
		public GameObject back;
		public GameObject bottomBar;
		public GameObject arrowRight;
		public GameObject arrowLeft;
		public GameObject tirePanel;
		public GameObject paintPanel;
		public GameObject susPanel;
		public GameObject customTypes;
		public GameObject options;
		public GameObject levelSelect;
		public GameObject networkSingle;
		public GameObject network;
		public GameObject modes;
		public GameObject mode;
	}

	// uie Stands for (User Interface Elements).
	public UIElements uie;

	[HideInInspector]
	public string levelName;

    /*
	void Start ()
	{
		StartCoroutine (Init ());
	}
	IEnumerator Init () 
	{
		yield return new WaitForSeconds (.5f);
		iTween.MoveTo(uie.back, iTween.Hash ("path", iTweenPath.GetPath ("TopLeft_Trans"), "easetype", "linear", "time", .1f));
		iTween.MoveTo(uie.bottomBar, iTween.Hash("path", iTweenPath.GetPath("BottomRight_Trans"), "easetype", "linear", "time", .1f));
		iTween.MoveTo (uie.arrowLeft, iTween.Hash("path", iTweenPath.GetPath("Arrow_Left"), "easetype", "linear", "time", .1f));
		iTween.MoveTo (uie.arrowRight, iTween.Hash("path", iTweenPath.GetPath("Arrow_Right"), "easetype", "linear", "time", .1f));
		currentMenu = Menus.MainMenu;
	}

	public void BackButton () 
	{
		switch (currentMenu) 
		{
		case Menus.CustomizeSelection:
			iTween.MoveTo (uie.arrowLeft, iTween.Hash("path", iTweenPath.GetPath("Arrow_Left"), "easetype", "linear", "time", .1f));
			iTween.MoveTo (uie.arrowRight, iTween.Hash("path", iTweenPath.GetPath("Arrow_Right"), "easetype", "linear", "time", .1f));
			iTween.MoveTo(uie.bottomBar, iTween.Hash("path", iTweenPath.GetPath("BottomRight_Trans"), "easetype", "linear", "time", .1f));
			iTween.MoveTo (uie.customTypes, iTween.Hash("path", iTweenPath.GetPath("CustomTypes_R"), "easetype", "linear", "time", .1f));
			currentMenu = Menus.MainMenu;
			break;
		case Menus.TireCustom:
			iTween.MoveTo (uie.tirePanel, iTween.Hash("path", iTweenPath.GetPath("BottomLeft_Trans_R"), "easetype", "linear", "time", .1f));
			iTween.MoveTo (uie.customTypes, iTween.Hash("path", iTweenPath.GetPath("CustomTypes"), "easetype", "linear", "time", .1f));
			currentMenu = Menus.CustomizeSelection;
			break;
		case Menus.PaintCustom:
			iTween.MoveTo (uie.paintPanel, iTween.Hash("path", iTweenPath.GetPath("BottomLeft_Trans_R"), "easetype", "linear", "time", .1f));
			iTween.MoveTo (uie.customTypes, iTween.Hash("path", iTweenPath.GetPath("CustomTypes"), "easetype", "linear", "time", .1f));
			currentMenu = Menus.CustomizeSelection;
			break;
		case Menus.SusCustom:
			iTween.MoveTo (uie.susPanel, iTween.Hash("path", iTweenPath.GetPath("BottomLeft_Trans_R"), "easetype", "linear", "time", .1f));
			iTween.MoveTo (uie.customTypes, iTween.Hash("path", iTweenPath.GetPath("CustomTypes"), "easetype", "linear", "time", .1f));
			currentMenu = Menus.CustomizeSelection;
			break;
		case Menus.Options:
			iTween.MoveTo (uie.arrowLeft, iTween.Hash("path", iTweenPath.GetPath("Arrow_Left"), "easetype", "linear", "time", .1f));
			iTween.MoveTo (uie.arrowRight, iTween.Hash("path", iTweenPath.GetPath("Arrow_Right"), "easetype", "linear", "time", .1f));
			iTween.MoveTo(uie.bottomBar, iTween.Hash("path", iTweenPath.GetPath("BottomRight_Trans"), "easetype", "linear", "time", .1f));
			iTween.MoveTo (uie.options, iTween.Hash("path", iTweenPath.GetPath("Center_Trans_R"), "easetype", "linear", "time", .1f));
			currentMenu = Menus.MainMenu;
			break;
		case Menus.LevelSelect:
			iTween.MoveTo(uie.back, iTween.Hash ("path", iTweenPath.GetPath ("TopLeft_Trans"), "easetype", "linear", "time", .1f));
			iTween.MoveTo (uie.arrowLeft, iTween.Hash("path", iTweenPath.GetPath("Arrow_Left"), "easetype", "linear", "time", .1f));
			iTween.MoveTo (uie.arrowRight, iTween.Hash("path", iTweenPath.GetPath("Arrow_Right"), "easetype", "linear", "time", .1f));
			iTween.MoveTo(uie.bottomBar, iTween.Hash("path", iTweenPath.GetPath("BottomRight_Trans"), "easetype", "linear", "time", .1f));
			iTween.MoveTo (uie.levelSelect, iTween.Hash("path", iTweenPath.GetPath("Center_Trans_R"), "easetype", "linear", "time", .1f));
			currentMenu = Menus.MainMenu;
			break;
		case Menus.NetworkSingle:
			iTween.MoveTo (uie.levelSelect, iTween.Hash("path", iTweenPath.GetPath("Center_Trans"), "easetype", "linear", "time", .1f));
			iTween.MoveTo (uie.networkSingle, iTween.Hash("path", iTweenPath.GetPath("Center_Trans_R"), "easetype", "linear", "time", .1f));
			currentMenu = Menus.LevelSelect;
			levelName = null;
			break;
		case Menus.Network_:
			iTween.MoveTo (uie.levelSelect, iTween.Hash("path", iTweenPath.GetPath("Center_Trans"), "easetype", "linear", "time", .1f));
			iTween.MoveTo (uie.network, iTween.Hash("path", iTweenPath.GetPath("Center_Trans_R"), "easetype", "linear", "time", .1f));
			currentMenu = Menus.LevelSelect;
			levelName = null;
			break;
		case Menus.Modes:
			iTween.MoveTo (uie.networkSingle, iTween.Hash("path", iTweenPath.GetPath("Center_Trans"), "easetype", "linear", "time", .1f));
			iTween.MoveTo (uie.modes, iTween.Hash("path", iTweenPath.GetPath("Center_Trans_R"), "easetype", "linear", "time", .1f));
			currentMenu = Menus.NetworkSingle;
			break;
		case Menus.Mode:
			iTween.MoveTo (uie.network, iTween.Hash("path", iTweenPath.GetPath("Center_Trans"), "easetype", "linear", "time", .1f));
			iTween.MoveTo (uie.mode, iTween.Hash("path", iTweenPath.GetPath("Center_Trans_R"), "easetype", "linear", "time", .1f));
			currentMenu = Menus.Network_;
			break;
		}
	}

	public void CustomizeButton ()
	{
		iTween.MoveTo (uie.arrowLeft, iTween.Hash("path", iTweenPath.GetPath("Arrow_Left_R"), "easetype", "linear", "time", .1f));
		iTween.MoveTo (uie.arrowRight, iTween.Hash("path", iTweenPath.GetPath("Arrow_Right_R"), "easetype", "linear", "time", .1f));
		iTween.MoveTo(uie.bottomBar, iTween.Hash("path", iTweenPath.GetPath("BottomRight_Trans_R"), "easetype", "linear", "time", .1f));
		iTween.MoveTo (uie.customTypes, iTween.Hash("path", iTweenPath.GetPath("CustomTypes"), "easetype", "linear", "time", .1f));
		currentMenu = Menus.CustomizeSelection;
	}

	public void TirePanelButton ()
	{
		iTween.MoveTo (uie.customTypes, iTween.Hash("path", iTweenPath.GetPath("CustomTypes_R"), "easetype", "linear", "time", .1f));
		iTween.MoveTo (uie.tirePanel, iTween.Hash("path", iTweenPath.GetPath("BottomLeft_Trans"), "easetype", "linear", "time", .1f));
		currentMenu = Menus.TireCustom;
	}

	public void PaintPanelButton ()
	{
		iTween.MoveTo (uie.customTypes, iTween.Hash("path", iTweenPath.GetPath("CustomTypes_R"), "easetype", "linear", "time", .1f));
		iTween.MoveTo (uie.paintPanel, iTween.Hash("path", iTweenPath.GetPath("BottomLeft_Trans"), "easetype", "linear", "time", .1f));
		currentMenu = Menus.PaintCustom;
	}

	public void SusPanelButton ()
	{
		iTween.MoveTo (uie.customTypes, iTween.Hash("path", iTweenPath.GetPath("CustomTypes_R"), "easetype", "linear", "time", .1f));
		iTween.MoveTo (uie.susPanel, iTween.Hash("path", iTweenPath.GetPath("BottomLeft_Trans"), "easetype", "linear", "time", .1f));
		currentMenu = Menus.SusCustom;
	}

	public void Options ()
	{
		iTween.MoveTo (uie.arrowLeft, iTween.Hash("path", iTweenPath.GetPath("Arrow_Left_R"), "easetype", "linear", "time", .1f));
		iTween.MoveTo (uie.arrowRight, iTween.Hash("path", iTweenPath.GetPath("Arrow_Right_R"), "easetype", "linear", "time", .1f));
		iTween.MoveTo(uie.bottomBar, iTween.Hash("path", iTweenPath.GetPath("BottomRight_Trans_R"), "easetype", "linear", "time", .1f));
		iTween.MoveTo (uie.options, iTween.Hash("path", iTweenPath.GetPath("Center_Trans"), "easetype", "linear", "time", .1f));
		currentMenu = Menus.Options;
	}

	public void LevelSelect ()
	{
		iTween.MoveTo(uie.back, iTween.Hash ("path", iTweenPath.GetPath ("TopLeft_Trans_R"), "easetype", "linear", "time", .1f));
		iTween.MoveTo (uie.arrowLeft, iTween.Hash("path", iTweenPath.GetPath("Arrow_Left_R"), "easetype", "linear", "time", .1f));
		iTween.MoveTo (uie.arrowRight, iTween.Hash("path", iTweenPath.GetPath("Arrow_Right_R"), "easetype", "linear", "time", .1f));
		iTween.MoveTo(uie.bottomBar, iTween.Hash("path", iTweenPath.GetPath("BottomRight_Trans_R"), "easetype", "linear", "time", .1f));
		iTween.MoveTo (uie.levelSelect, iTween.Hash("path", iTweenPath.GetPath("Center_Trans"), "easetype", "linear", "time", .1f));
		currentMenu = Menus.LevelSelect;
	}

	public void Multiplayer ()
	{
		switch (currentMenu) {
		case Menus.Network_:
			iTween.MoveTo (uie.network, iTween.Hash("path", iTweenPath.GetPath("Center_Trans_R"), "easetype", "linear", "time", .1f));
			iTween.MoveTo (uie.mode, iTween.Hash("path", iTweenPath.GetPath("Center_Trans"), "easetype", "linear", "time", .1f));
			currentMenu = Menus.Mode;
			break;
		default:
			iTween.MoveTo (uie.networkSingle, iTween.Hash("path", iTweenPath.GetPath("Center_Trans_R"), "easetype", "linear", "time", .1f));
			iTween.MoveTo (uie.modes, iTween.Hash("path", iTweenPath.GetPath("Center_Trans"), "easetype", "linear", "time", .1f));
			currentMenu = Menus.Modes;
			break;
		}
	}

	public void Singleplayer ()
	{
		iTween.MoveTo (uie.networkSingle, iTween.Hash("path", iTweenPath.GetPath("Center_Trans_R"), "easetype", "linear", "time", .1f));
		iTween.MoveTo (uie.modes, iTween.Hash("path", iTweenPath.GetPath("Center_Trans"), "easetype", "linear", "time", .1f));
		currentMenu = Menus.Modes;
	}

	public void LevelLoad (string lvl)
	{
		levelName = lvl;
		switch (levelName) 
		{
		case "staduim":
			iTween.MoveTo (uie.levelSelect, iTween.Hash("path", iTweenPath.GetPath("Center_Trans_R"), "easetype", "linear", "time", .1f));
			iTween.MoveTo (uie.network, iTween.Hash("path", iTweenPath.GetPath("Center_Trans"), "easetype", "linear", "time", .1f));
			currentMenu = Menus.Network_;
			break;
		default:
			iTween.MoveTo (uie.levelSelect, iTween.Hash("path", iTweenPath.GetPath("Center_Trans_R"), "easetype", "linear", "time", .1f));
			iTween.MoveTo (uie.networkSingle, iTween.Hash("path", iTweenPath.GetPath("Center_Trans"), "easetype", "linear", "time", .1f));
			currentMenu = Menus.NetworkSingle;
			break;
		}
	}*/
}
