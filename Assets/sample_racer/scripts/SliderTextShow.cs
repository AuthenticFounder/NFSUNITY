using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SliderTextShow : MonoBehaviour {

	Text txt;

	public string add;

	void Start () 
	{
		txt = GetComponent<Text> ();
	}

	public void ShowValue (float value)
	{
		txt.text = value.ToString ()+ add;
	}
}
