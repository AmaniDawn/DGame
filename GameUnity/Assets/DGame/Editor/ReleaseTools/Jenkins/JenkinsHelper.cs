using System;
using System.Collections;
using System.Collections.Generic;
using DGame;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

public static class JenkinsHelper
{
    public static void JenkinsBuildPC()
    {
        PlayerSettings.bundleVersion = GetJenkinsParameter("Version");
        // PlayerSettings.iOS.buildNumber = GetJenkinsParameter("BuildNumber");
        PlayerSettings.applicationIdentifier = GetJenkinsParameter("BundleName");
        ReleaseTools.AutoBuildWindow();
    }

    /// <summary>
    /// 获取Jenkins传输的参数
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string GetJenkinsParameter(string name)
    {
        foreach (var arg in Environment.GetCommandLineArgs())
        {
            if (arg.StartsWith(name))
            {
                return arg.Split("-"[0])[1];
            }
        }

        return null;
    }
}