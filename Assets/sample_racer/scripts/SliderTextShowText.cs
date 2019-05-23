using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SliderTextShowText : MonoBehaviour {

	public enum Type 
	{
		GearType,
		DrivingType
	}

	public Type type;

	Text txt;

	void Start () 
	{
		txt = GetComponent<Text> ();
	}

	public void ShowValue (float value)
	{
		int val = Mathf.FloorToInt(value);
		switch (val) 
		{
		case 1:
			switch (type)
			{
			case Type.GearType:
				txt.text = "AUTO";
				break;
			case Type.DrivingType:
				txt.text = "FWD";
				break;
			}
			break;
		case 2:
			switch (type)
			{
			case Type.GearType:
				txt.text = "MANUAL";
				break;
			case Type.DrivingType:
				txt.text = "RWD";
				break;
			}
			break;
		case 3:
			switch (type)
			{
			case Type.DrivingType:
				txt.text = "AWD";
				break;
			}
			break;
		}

	}
}
