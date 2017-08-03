using System.Collections.Generic;
using System;
using MiniJSON_Min;

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


/// <summary>
///  JsonObject provide safe and type-compatible wrapper to access Json Element
/// Safe means, even if a certain Json doesn't contain 'key', it doesn't crash.
/// </summary>
public class JsonObject : UnityEngine.Object
{
	/// <summary>
	/// Supported object type : double, bool, string, List, Array!
	/// </summary>
	public Dictionary<string, object> objectDict;

	public JsonObject()
	{
		objectDict = new Dictionary<string, object>();
	}

	public JsonObject(string strJson)
	{
		objectDict = Json.Deserialize(strJson) as Dictionary<string, object>;
	}

	public JsonObject(Dictionary<string, object> dict)
	{
		objectDict = dict;
	}

	public object this[string key]
	{
		get { return objectDict[key]; }
		set { objectDict[key] = value; }
	}

	public string getCmd()
	{
		return (string) objectDict["cmd"];
	}

	public string Serialize()
	{
		var strData = Json.Serialize(objectDict);
		return strData;
	}

	//// Deserialize helper

	private object GetDictValue(string key)
	{
		object obj;
		if (objectDict.TryGetValue(key, out obj))
			return obj ?? "";
		return "";
	}

	public bool KeyExist(string key)
	{
		object obj;
		if (objectDict.TryGetValue(key, out obj))
			return true;
		return false;
	}

	public Dictionary<string, object> GetJsonDict(string key)
	{
		object obj;
		if (objectDict.TryGetValue(key, out obj))
			return (Dictionary<string, object>) obj;
		return new Dictionary<string, object>();
	}

	public JsonObject GetJsonObject(string key)
	{
		var dict = GetJsonDict(key);
		return new JsonObject(dict);
	}

	public bool GetBool(string key)
	{
		bool val;
		if (bool.TryParse(GetDictValue(key).ToString(), out val)) return val;
		return false;
	}

	public int GetInt(string key)
	{
		int val;
		if (int.TryParse(GetDictValue(key).ToString(), out val)) return val;
		return 0;
	}

	public float GetFloat(string key)
	{
		float val;
		if (float.TryParse(GetDictValue(key).ToString(), out val)) return val;
		return 0.0f;
	}

	public string GetString(string key)
	{
		return GetDictValue(key).ToString();
	}

	public object GetEnum(Type type, string key)
	{
		var obj = (string) GetDictValue(key);
		if (obj.Length > 0)
			return Enum.Parse(type, obj);
		return 0;
	}
	
	public List<JsonObject> GetListJsonObject(string key)
	{
		object obj;
		var retList = new List<JsonObject>();
		if (objectDict.TryGetValue(key, out obj))
			foreach (var elem in (List<object>) obj)
				retList.Add(new JsonObject((Dictionary<string, object>) elem));
		return retList;
	}
}
