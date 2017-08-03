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
	
	// Update is called once per frame
	void Update () {

	
	}

	private string GetCurObjName()
	{
		string strObjName = "";
		GameObject objSel = EventSystem.current.currentSelectedGameObject;
		if (objSel != null && objSel.transform.parent != null)
		{
			strObjName = objSel.transform.parent.name;
		}

		return strObjName;
	}

	public void OnEditValugChanged(string str)
	{
		Text txt = this.GetComponent<Text>();
		txt.text = string.Format("[{0}] val changed {1}", this.GetCurObjName(), str);
	}

	public void OnEditEnded(string str)
	{
		Text txt = this.GetComponent<Text>();
		txt.text = string.Format("[{0}] edit ended {1}", this.GetCurObjName(), str);
	}
}
