/*
 * Copyright (c) 2015 Kyungmin Bang
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */


/*
 *  NativeEditBox script should be attached to Unity UI InputField object 
 * 
 *  Limitation
 * 
 * 1. Screen auto rotation is not supported.
 */


using UnityEngine;
using System;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(InputField))]
public class NativeEditBox : PluginMsgReceiver
{
	private struct EditBoxConfig
	{
		public bool multiline;
		public Color textColor;
		public Color backColor;
		public string contentType;
		public string font;
		public float fontSize;
		public string align;
		public string placeHolder;
		public int characterLimit;
		public Color placeHolderColor;
	}

	public enum ReturnKeyType
	{
		Default,
		Next,
		Done,
		Send,
		Go
	}

	public bool withDoneButton = true;
	public ReturnKeyType returnKeyType;
	public bool useInputFieldFont;
	public float updateDeltaTime = 0.25f;

	public event Action returnPressed;
	public UnityEngine.Events.UnityEvent onReturnPressed;

	private bool _hasNativeEditCreated = false;

	private InputField _inputField;
	private Text _textComponent;
	private bool _focusOnCreate;
	private bool _visibleOnCreate = true;
	private float _fakeTimer = 0f;

	private const string MSG_CREATE = "CreateEdit";
	private const string MSG_REMOVE = "RemoveEdit";
	private const string MSG_SET_TEXT = "SetText";
	private const string MSG_SET_RECT = "SetRect";
	private const string MSG_SET_FOCUS = "SetFocus";
	private const string MSG_SET_VISIBLE = "SetVisible";
	private const string MSG_TEXT_CHANGE = "TextChange";
	private const string MSG_TEXT_END_EDIT = "TextEndEdit";
	// to fix bug Some keys 'back' & 'enter' are eaten by unity and never arrive at plugin
	private const string MSG_ANDROID_KEY_DOWN = "AndroidKeyDown";
	private const string MSG_RETURN_PRESSED = "ReturnPressed";
	private const string MSG_GET_TEXT = "GetText";

	public InputField inputField
	{
		get { return _inputField; }
	}

	public bool visible { get; private set; }

	public string text
	{
		get { return _inputField.text; }
		set
		{
			_inputField.text = value;
			if(_hasNativeEditCreated)
				SetTextNative(value);
		}
	}

	public static Rect GetScreenRectFromRectTransform(RectTransform rectTransform)
	{
		Vector3[] corners = new Vector3[4];

		rectTransform.GetWorldCorners(corners);

		float xMin = float.PositiveInfinity;
		float xMax = float.NegativeInfinity;
		float yMin = float.PositiveInfinity;
		float yMax = float.NegativeInfinity;

		for (int i = 0; i < 4; i++)
		{
			// For Canvas mode Screen Space - Overlay there is no Camera; best solution I've found
			// is to use RectTransformUtility.WorldToScreenPoint) with a null camera.
			Vector3 screenCoord = RectTransformUtility.WorldToScreenPoint(null, corners[i]);

			if (screenCoord.x < xMin)
				xMin = screenCoord.x;
			if (screenCoord.x > xMax)
				xMax = screenCoord.x;
			if (screenCoord.y < yMin)
				yMin = screenCoord.y;
			if (screenCoord.y > yMax)
				yMax = screenCoord.y;
		}
		Rect result = new Rect(xMin, Screen.height - yMax, xMax - xMin, yMax - yMin);
		return result;
	}

	private EditBoxConfig mConfig;

	private void Awake()
	{
		_inputField = this.GetComponent<InputField>();
		if (_inputField == null)
		{
			Debug.LogErrorFormat("No InputField found {0} NativeEditBox Error", this.name);
			throw new MissingComponentException();
		}

		_textComponent = _inputField.textComponent;
	}

	// Use this for initialization
	protected override void Start()
	{
		base.Start();

		// Wait until the end of frame before initializing to ensure that Unity UI layout has been built. We used to
		// initialize at Start, but that resulted in an invalid RectTransform position and size on the InputField if it
		// was instantiated at runtime instead of being built in to the scene.
		StartCoroutine(InitializeOnNextFrame());
	}

	private void OnEnable()
	{
		if (_hasNativeEditCreated)
			this.SetVisible(true);
	}

	private void OnDisable()
	{
		if (_hasNativeEditCreated)
			this.SetVisible(false);
	}

	protected override void OnDestroy()
	{
		if (!_hasNativeEditCreated)
			return;

		RemoveNative();
		base.OnDestroy();
	}

	private void OnApplicationPause(bool pause)
	{
		if (!_hasNativeEditCreated)
			return;

		this.SetVisible(!pause);
	}

	private IEnumerator InitializeOnNextFrame()
	{
		yield return null;

		this.PrepareNativeEdit();
#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
		this.CreateNativeEdit();
		this.SetTextNative(this._textComponent.text);

		_inputField.placeholder.gameObject.SetActive(false);
		_textComponent.enabled = false;
		_inputField.enabled = false;
#endif
	}

	private void Update()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		this.UpdateForceKeyeventForAndroid();

		//Plugin has to update rect continually otherwise we cannot see characters inputted just now 
		_fakeTimer += Time.deltaTime;
		if (_fakeTimer >= updateDeltaTime && this._inputField != null && _hasNativeEditCreated && this.visible)
		{
			SetRectNative(this._textComponent.rectTransform);
			_fakeTimer = 0f;
		}
#endif
	}

	private void PrepareNativeEdit()
	{
		var placeHolder = _inputField.placeholder.GetComponent<Text>();

		if (useInputFieldFont)
			mConfig.font = _textComponent.font.fontNames.Length > 0 ? _textComponent.font.fontNames[0] : "Arial";

		mConfig.placeHolder = placeHolder.text;
		mConfig.placeHolderColor = placeHolder.color;
		mConfig.characterLimit = _inputField.characterLimit;

		Rect rectScreen = GetScreenRectFromRectTransform(this._textComponent.rectTransform);
		float fHeightRatio = rectScreen.height/_textComponent.rectTransform.rect.height;
		mConfig.fontSize = ((float) _textComponent.fontSize)*fHeightRatio;

		mConfig.textColor = _textComponent.color;
		mConfig.align = _textComponent.alignment.ToString();
		mConfig.contentType = _inputField.contentType.ToString();
		mConfig.backColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
		mConfig.multiline = _inputField.lineType != InputField.LineType.SingleLine;
	}

	private void onTextChange(string newText)
	{
		// Avoid firing a delayed onValueChanged event if the text was changed from Unity with the text property in this
		// class.
		if (newText == this._inputField.text)
			return;

		this._inputField.text = newText;
		if (this._inputField.onValueChanged != null)
		this._inputField.onValueChanged.Invoke(newText);
	}

	private void onTextEditEnd(string newText)
	{
		this._inputField.text = newText;
		if (this._inputField.onEndEdit != null)
			this._inputField.onEndEdit.Invoke(newText);
	}

	public override void OnPluginMsgDirect(JsonObject jsonMsg)
	{
		PluginMsgHandler.GetInstanceForReceiver(this).StartCoroutine(PluginsMessageRoutine(jsonMsg));
	}

	private IEnumerator PluginsMessageRoutine(JsonObject jsonMsg)
	{
		// this is to avoid a deadlock for more info when trying to get data from two separate native plugins and handling them in Unity
		yield return null;

		string msg = jsonMsg.GetString("msg");
		if (msg.Equals(MSG_TEXT_CHANGE))
		{
			string text = jsonMsg.GetString("text");
			this.onTextChange(text);
		}
		else if (msg.Equals(MSG_TEXT_END_EDIT))
		{
			string text = jsonMsg.GetString("text");
			this.onTextEditEnd(text);
		}
		else if (msg.Equals(MSG_RETURN_PRESSED))
		{
			if (returnPressed != null)
				returnPressed();
			if (onReturnPressed != null)
				onReturnPressed.Invoke();
		}
	}

	private bool CheckErrorJsonRet(JsonObject jsonRet)
	{
		bool bError = jsonRet.GetBool("bError");
		string strError = jsonRet.GetString("strError");
		if (bError)
		{
			Debug.LogError(string.Format("NativeEditbox error {0}", strError));
		}
		return bError;
	}

	private void CreateNativeEdit()
	{
		Rect rectScreen = GetScreenRectFromRectTransform(this._textComponent.rectTransform);

		JsonObject jsonMsg = new JsonObject();

		jsonMsg["msg"] = MSG_CREATE;

		jsonMsg["x"] = rectScreen.x/Screen.width;
		jsonMsg["y"] = rectScreen.y/Screen.height;
		jsonMsg["width"] = rectScreen.width/Screen.width;
		jsonMsg["height"] = rectScreen.height/Screen.height;
		jsonMsg["characterLimit"] = mConfig.characterLimit;

		jsonMsg["textColor_r"] = mConfig.textColor.r;
		jsonMsg["textColor_g"] = mConfig.textColor.g;
		jsonMsg["textColor_b"] = mConfig.textColor.b;
		jsonMsg["textColor_a"] = mConfig.textColor.a;
		jsonMsg["backColor_r"] = mConfig.backColor.r;
		jsonMsg["backColor_g"] = mConfig.backColor.g;
		jsonMsg["backColor_b"] = mConfig.backColor.b;
		jsonMsg["backColor_a"] = mConfig.backColor.a;
		jsonMsg["font"] = mConfig.font;
		jsonMsg["fontSize"] = mConfig.fontSize;
		jsonMsg["contentType"] = mConfig.contentType;
		jsonMsg["align"] = mConfig.align;
		jsonMsg["withDoneButton"] = this.withDoneButton;
		jsonMsg["placeHolder"] = mConfig.placeHolder;
		jsonMsg["placeHolderColor_r"] = mConfig.placeHolderColor.r;
		jsonMsg["placeHolderColor_g"] = mConfig.placeHolderColor.g;
		jsonMsg["placeHolderColor_b"] = mConfig.placeHolderColor.b;
		jsonMsg["placeHolderColor_a"] = mConfig.placeHolderColor.a;
		jsonMsg["multiline"] = mConfig.multiline;

		switch (returnKeyType)
		{
			case ReturnKeyType.Next:
				jsonMsg["return_key_type"] = "Next";
				break;
			case ReturnKeyType.Done:
				jsonMsg["return_key_type"] = "Done";
				break;
			case ReturnKeyType.Send:
				jsonMsg["return_key_type"] = "Send";
				break;
			case ReturnKeyType.Go:
				jsonMsg["return_key_type"] = "Go";
				break;
			default:
				jsonMsg["return_key_type"] = "Default";
				break;
		}

		JsonObject jsonRet = this.SendPluginMsg(jsonMsg);
		_hasNativeEditCreated = !this.CheckErrorJsonRet(jsonRet);

		this.visible = _visibleOnCreate;
		if (!_visibleOnCreate)
			SetVisible(false);

		if (_focusOnCreate)
			SetFocus(true);
	}

	private void SetTextNative(string newText)
	{
		JsonObject jsonMsg = new JsonObject();

		jsonMsg["msg"] = MSG_SET_TEXT;
		jsonMsg["text"] = newText;

		this.SendPluginMsg(jsonMsg);
	}

	private void RemoveNative()
	{   
		JsonObject jsonMsg = new JsonObject();

		jsonMsg["msg"] = MSG_REMOVE;
		this.SendPluginMsg(jsonMsg);
	}

	public void SetRectNative(RectTransform rectTrans)
	{
		Rect rectScreen = GetScreenRectFromRectTransform(rectTrans);

		JsonObject jsonMsg = new JsonObject();

		jsonMsg["msg"] = MSG_SET_RECT;

		jsonMsg["x"] = rectScreen.x/Screen.width;
		jsonMsg["y"] = rectScreen.y/Screen.height;
		jsonMsg["width"] = rectScreen.width/Screen.width;
		jsonMsg["height"] = rectScreen.height/Screen.height;

		this.SendPluginMsg(jsonMsg);
	}

	public void SetFocus(bool bFocus)
	{
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
		if (!_hasNativeEditCreated)
		{
			_focusOnCreate = bFocus;
			return;
		}

		JsonObject jsonMsg = new JsonObject();

		jsonMsg["msg"] = MSG_SET_FOCUS;
		jsonMsg["isFocus"] = bFocus;

		this.SendPluginMsg(jsonMsg);
#else
		if (gameObject.activeInHierarchy)
		{
			if (bFocus)
				_inputField.ActivateInputField();
			else
				_inputField.DeactivateInputField();
		}
		else
			_focusOnCreate = bFocus;
#endif
	}

	public void SetVisible(bool bVisible)
	{
		if (!_hasNativeEditCreated)
		{
			_visibleOnCreate = bVisible;
			return;
		}

		JsonObject jsonMsg = new JsonObject();

		jsonMsg["msg"] = MSG_SET_VISIBLE;
		jsonMsg["isVisible"] = bVisible;

		this.SendPluginMsg(jsonMsg);

		this.visible = bVisible;
	}

#if UNITY_ANDROID && !UNITY_EDITOR
	private void ForceSendKeydown_Android(string key)
	{
		JsonObject jsonMsg = new JsonObject();

		jsonMsg["msg"] = MSG_ANDROID_KEY_DOWN;
		jsonMsg["key"] = key;
		this.SendPluginMsg(jsonMsg);
	}

	private void UpdateForceKeyeventForAndroid()
	{
		if (Input.anyKeyDown)
		{
			if (Input.GetKeyDown(KeyCode.Backspace))
			{
				this.ForceSendKeydown_Android("backspace");
			}
			else
			{
				foreach(char c in Input.inputString)
				{
					if (c == '\n')
					{
						this.ForceSendKeydown_Android("enter");
					}
					else
					{
						this.ForceSendKeydown_Android(Input.inputString);
					}
				}
			}
		}
	}
#endif
}
