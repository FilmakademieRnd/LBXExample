#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

//helper to change the playmode tint to the MinoPlayer color (better debugging!)
public static class SettingsHelper{
    static bool m_Initialized = false;
    static FieldInfo m_PrefsField = null;
    static FieldInfo m_PrefColorField = null;
    static MethodInfo m_SetColorPref = null;
    static SortedList<string, object> GetList()
    {
        return (SortedList<string, object>)m_PrefsField.GetValue(null);
    }
    static object GetPref(string aName)
    {
        return GetList()[aName];
    }
    static System.Type GetEditorType(string aName)
    {
        return typeof(Editor).Assembly.GetTypes().Where((a) => a.Name == aName).FirstOrDefault();
    }

    static SettingsHelper()
    {
        var settingsType = GetEditorType("PrefSettings");   //Settings
        var prefColorType = GetEditorType("PrefColor");
        if (settingsType == null || prefColorType == null)
            throw new System.Exception("Something has changed in Unity and the SettingsHelper class is no longer supported");
        m_PrefsField = settingsType.GetField("m_Prefs", BindingFlags.Static | BindingFlags.NonPublic);
        m_PrefColorField = prefColorType.GetField("m_Color", BindingFlags.Instance | BindingFlags.NonPublic);
        m_SetColorPref = prefColorType.GetMethod("ToUniqueString", BindingFlags.Instance | BindingFlags.Public);
        if (m_PrefsField == null || m_PrefColorField == null || m_SetColorPref == null)
            throw new System.Exception("Something has changed in Unity and the SettingsHelper class is no longer supported");
        m_Initialized = true;
    }

    public static Color PlaymodeTint
    {
        get
        {
            if (!m_Initialized)
                return Color.black;
            var p = GetPref("Playmode tint");
            return (Color)m_PrefColorField.GetValue(p);
        }
        set
        {
            if (!m_Initialized)
                return;
            var p = GetPref("Playmode tint");
            m_PrefColorField.SetValue(p, value);
            string data = (string)m_SetColorPref.Invoke(p, null);
            EditorPrefs.SetString("Playmode tint", data);
        }
    }
}

#endif