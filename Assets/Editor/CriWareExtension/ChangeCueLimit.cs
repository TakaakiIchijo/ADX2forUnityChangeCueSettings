using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Debug = UnityEngine.Debug;

public class ChangeCueLimit : EditorWindow
{
    private static string  atomCraftExe  = "CriAtomCraftC.exe";
    private const string pythonBaseScriptDirectoryName = "Editor/CriWareExtension/ADX2PythonScriptsBase";
    private const string changeCueLimitPythonBaseFileName = "ChangeCueLimit_ADX2PythonScriptBase.txt";
    private const string pythonScriptDirectoryName = "ADX2PythonScripts";
    private const string pythonScriptName = "ChangeCueLimit.py";
    private static bool hidden = false;

    [SerializeField]
    private string atomCraftDirectory,atomProjectPath;
    [SerializeField] 
    private string workUnitName = "WorkUnit_0";
    [SerializeField] 
    private string cueSheetFolderName = "WorkUnit_0";
    [SerializeField]
    private string cueSheetName, cueName; 
    
    private string currentCueLimit = "None";
    private int cueLimit;
    private enum CuePriorityType {FirstPriority, LastPriority}

    private CuePriorityType priorityType;//, currentPriorityType;
    
    [MenuItem("Window/CRIWARE/ChangeCueLimitWindow", false, 1)]
    static void ShowWindow()
    {
        GetWindow<ChangeCueLimit>();
    }

    void OnGUI()
    {
        atomCraftDirectory = EditorGUILayout.TextField("Atom Craft Directory Path", atomCraftDirectory); 
        atomProjectPath = EditorGUILayout.TextField(".atmcproject Path", atomProjectPath); 
        workUnitName = EditorGUILayout.TextField("Work Unit Name", workUnitName); 
        cueSheetFolderName = EditorGUILayout.TextField("Work Unit Name", cueSheetFolderName); 
        cueSheetName = EditorGUILayout.TextField("Cue Sheet Name", cueSheetName); 
        cueName = EditorGUILayout.TextField("Cue Name", cueName);

        EditorGUILayout.BeginHorizontal ();
        if( GUILayout.Button( "Check current settings", GUILayout.Width(150), GUILayout.Height(60) ) )
        {
            var cueInfo = AcbSearch(cueSheetName, cueName);

            currentCueLimit = "None";
            if (!string.IsNullOrEmpty(cueInfo.name))
            {
                currentCueLimit = cueInfo.numLimits.ToString();
            }
        }
        EditorGUILayout.LabelField("CueLimit: "+currentCueLimit);
        
        EditorGUILayout.EndHorizontal ();
        
        cueLimit = EditorGUILayout.IntField ("Cue Limit", cueLimit);
        priorityType = (CuePriorityType)EditorGUILayout.EnumPopup("Cue Priority Type", priorityType);
        
        if( GUILayout.Button( "Set and Build", GUILayout.Height(60) ) )
        {
            PythonGen();
            AttachAndBuild();
        }
    }
    
    private void AttachAndBuild()
    {
        string arg = atomProjectPath + " -script " + Directory.GetParent(Application.dataPath)+"/"+pythonScriptDirectoryName+"/"+pythonScriptName + " -workunitall";

        ProcessStartInfo info = new ProcessStartInfo
        {
            FileName = atomCraftExe,
            WorkingDirectory = atomCraftDirectory,
            Arguments = arg,
            WindowStyle = hidden
                ? ProcessWindowStyle.Hidden
                : ProcessWindowStyle.Normal,
        };

        try
        {
            Process p = Process.Start(info);
            p.WaitForExit();
        }
        catch( System.ComponentModel.Win32Exception i_exception )
        {
            UnityEngine.Debug.Assert( false, i_exception );
        }
    }

    private void PythonGen()
    {
        var path = Directory.GetParent(Application.dataPath) +"/"+pythonScriptDirectoryName;

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory (path);
        }

        var pythonScriptPath = path + "/" + pythonScriptName;
        
        if (File.Exists(pythonScriptPath))
        {
            File.Delete(pythonScriptPath);
        }
        
        using (StreamReader changeCueLimitBaseText = new StreamReader(Application.dataPath+"/"+pythonBaseScriptDirectoryName+"/"+changeCueLimitPythonBaseFileName))
        using (StreamWriter sw = File.CreateText(pythonScriptPath))
        {
            sw.WriteLine("WORK_UNIT_NAME = \""+workUnitName+"\"");
            sw.WriteLine("CUE_SHEET_FOLDER_NAME = \""+cueSheetFolderName+"\"");
            sw.WriteLine("CUE_SHEET_NAME = \""+cueSheetName+"\"");
            sw.WriteLine("CUE_NAME = \""+cueName+"\"");
            sw.WriteLine("CUE_LIMIT_NUM = "+cueLimit);
            sw.WriteLine("CUE_PRIORITY_TYPE = \""+priorityType.ToString()+"\"");
            sw.Write(changeCueLimitBaseText.ReadToEnd());
        }
    }

    private CriAtomEx.CueInfo AcbSearch(string cueSheetName, string cueName)
    {
        var files = Directory.GetFiles(Application.streamingAssetsPath, "*.acb", SearchOption.AllDirectories);

        var cueSheetFilePath = files.ToList().FirstOrDefault(file => Path.GetFileName(file).Replace(".acb", "") == cueSheetName);

        if (cueSheetFilePath != null)
        {
            CriAtomExAcb acb = CriAtomExAcb.LoadAcbFile(null, cueSheetFilePath.Replace("\\", "/"), "");
            if (acb != null)
            {
                List<CriAtomEx.CueInfo> cueInfoList = acb.GetCueInfoList().ToList();
                var cue = cueInfoList.FirstOrDefault(cueInfo => cueInfo.name == cueName);
                acb.Dispose();

                if (!string.IsNullOrEmpty(cue.name))
                {
                    return cue;
                }
            }
        }
            
        Debug.Log("Failed to load ACB file: " + cueSheetFilePath);
        return new CriAtomEx.CueInfo();
    }
}
