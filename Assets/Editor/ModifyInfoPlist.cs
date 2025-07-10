using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public class ModifyInfoPlist
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
    {
        if (buildTarget != BuildTarget.iOS) return;

        var plistPath = Path.Combine(path, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        var root = plist.root;
        var urlTypes = root.CreateArray("CFBundleURLTypes");
        var urlDict = urlTypes.AddDict();
        var schemes = urlDict.CreateArray("CFBundleURLSchemes");

        // Set your scheme here.
        // Must match your Product Name from Project Settings -> Player
        schemes.AddString("Pong");

        plist.WriteToFile(plistPath);
    }
}