using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class demo : MonoBehaviour {

	public NativeEditBox testNativeEdit;
	public Canvas mainCanvas;
	private RectTransform rectTrans;

	// Use this for initialization
	void Start () {
		rectTrans = testNativeEdit.transform.Find("Text").GetComponent<RectTransform>();
	}

	public void OnEditValueChanged(string str)
	{
		Text txt = this.GetComponent<Text>();
		txt.text = string.Format("val changed {0}", str);
	}

	public void OnEditEnded(string str)
	{
		Text txt = this.GetComponent<Text>();
		txt.text = string.Format("edit ended {0}", str);
	}

	public void OnReturnPressed(NativeEditBox editBox)
	{
		//hide keyboard
		editBox.SetFocus(false);
	}
}
