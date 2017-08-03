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

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using System.IO;
using AOT;

public class PluginMsgHandler : MonoBehaviour 
{
	private static PluginMsgHandler _instance = null;

	private int	_curReceiverIndex = 0;
	private Dictionary<int, PluginMsgReceiver> _receiverDict;

	public delegate void ShowKeyboardDelegate(bool bKeyboardShow, int nKeyHeight);
	public ShowKeyboardDelegate onShowKeyboard = null; 

	private static string MSG_SHOW_KEYBOARD = "ShowKeyboard";
	private static string DEFAULT_NAME = "NativeEditPluginHandler";

	private bool isEditor
	{
		get 
		{
			#if UNITY_EDITOR
			return true;
			#else
			return false;
			#endif
		}
	}

	private bool isStandalone
	{
		get 
		{			
			#if UNITY_STANDALONE
			return true;
			#else
			return false;
			#endif
		}
	}

	public static PluginMsgHandler GetInstanceForReceiver(PluginMsgReceiver receiver)
	{
		if (_instance == null) 
		{
			GameObject handlerObject = new GameObject(DEFAULT_NAME);
			handlerObject.transform.SetParent(receiver.gameObject.transform);
			_instance = handlerObject.AddComponent<PluginMsgHandler>();
		}
		return _instance;
	}

	void Awake()
	{
		this._receiverDict = new Dictionary<int, PluginMsgReceiver>();
		this.InitializeHandler();
	}

	void OnDestroy()
	{
		this.FinalizeHandler();
		_instance = null;
	}

	public int RegisterAndGetReceiverId(PluginMsgReceiver receiver)
	{
		int index = _curReceiverIndex;
		_curReceiverIndex++;

		_receiverDict[index] = receiver;
		return index;
	}

	public void RemoveReceiver(int nReceiverId)
	{
		_receiverDict.Remove(nReceiverId);
	}
	
	public PluginMsgReceiver GetReceiver(int nSenderId)
	{
		return _receiverDict[nSenderId];
	}
	
	private void OnMsgFromPlugin(string jsonPluginMsg)
	{
		if (jsonPluginMsg == null) return;

		JsonObject jsonMsg = new JsonObject(jsonPluginMsg);

		string msg = jsonMsg.GetString("msg");

		if (msg.Equals(MSG_SHOW_KEYBOARD))
		{
			bool bShow = jsonMsg.GetBool("show");
			int nKeyHeight = (int)( jsonMsg.GetFloat("keyheight") * (float) Screen.height);
			if (onShowKeyboard != null) 
			{
				onShowKeyboard(bShow, nKeyHeight);
			}
		}
		else
		{
			int nSenderId = jsonMsg.GetInt("senderId");

			// In some cases the receiver might be already removed, for example if a button is pressed
			// that will destoy the receiver while the input field is focused an end editing message
			// will be sent from the plugin after the receiver is already destroyed on Unity side.
			if (_receiverDict.ContainsKey(nSenderId))
			{
				PluginMsgReceiver receiver = GetReceiver(nSenderId);
				receiver.OnPluginMsgDirect(jsonMsg);
			}
		}
	}
	
	#if UNITY_IPHONE  
	[DllImport ("__Internal")]
	private static extern void _iOS_InitPluginMsgHandler(string unityName);
	[DllImport ("__Internal")]
	private static extern string _iOS_SendUnityMsgToPlugin(int nSenderId, string strMsg);
	[DllImport ("__Internal")]
	private static extern void _iOS_ClosePluginMsgHandler();	

	public void InitializeHandler()
	{		
		if (!isEditor) _iOS_InitPluginMsgHandler(this.name);
	}
	
	public void FinalizeHandler()
	{
		if (!isEditor)
			_iOS_ClosePluginMsgHandler();
		
	}

	#elif UNITY_ANDROID 

	private static AndroidJavaClass smAndroid;
	public void InitializeHandler()
	{	
		if (isEditor) return;

		// Reinitialization was made possible on Android to be able to use as a workaround in an issue where the
		// NativeEditBox text would be hidden after using Unity's Handheld.PlayFullScreenMovie().
		if (smAndroid == null)
			smAndroid = new AndroidJavaClass("com.bkmin.android.NativeEditPlugin");
		smAndroid.CallStatic("InitPluginMsgHandler", this.name);
	}
	
	public void FinalizeHandler()
	{	
		if (!isEditor)
			smAndroid.CallStatic("ClosePluginMsgHandler");
	}

	#else
	public void InitializeHandler()
	{
	}
	public void FinalizeHandler()
	{
	}

	#endif

	
	public JsonObject SendMsgToPlugin(int nSenderId, JsonObject jsonMsg)
	{	
		#if UNITY_EDITOR || UNITY_STANDALONE
			return new JsonObject();
		#else
			jsonMsg["senderId"] = nSenderId;
			string strJson = jsonMsg.Serialize();

			string strRet = "";
			#if UNITY_IPHONE
			strRet = _iOS_SendUnityMsgToPlugin(nSenderId, strJson);
			#elif UNITY_ANDROID 
			strRet = smAndroid.CallStatic<string>("SendUnityMsgToPlugin", nSenderId, strJson);
			#endif

			JsonObject jsonRet = new JsonObject(strRet);
			return jsonRet;
		#endif
	}
}
