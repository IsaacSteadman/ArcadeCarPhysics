using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif


//this one provide the capability to inject library into ios build, those library are normally not typical library which won't be built to unity
public class BuildPostProcessor
{

	[PostProcessBuildAttribute(1)]
	public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToBuiltProject)
	{
       
		if (buildTarget == BuildTarget.iOS)
		{

#if UNITY_IOS
			var projectPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";
			PBXProject pbxProject = new PBXProject();
			pbxProject.ReadFromFile(projectPath);
            
#if UNITY_2017 || UNITY_2018 || UNITY_2019_1 || UNITY_2019_2
            string targetGuid = pbxProject.TargetGuidByName("Unity-iPhone");
            pbxProject.AddFrameworkToProject(targetGuid, "CoreTelephony.framework", false);
            pbxProject.AddFrameworkToProject(targetGuid, "CoreMotion.framework", false);
            Debug.Log("build framework on unity version 2017");
#else
			string targetGuid = pbxProject.GetUnityFrameworkTargetGuid(); //this API is support after UNITY 2019.3
            pbxProject.AddFrameworkToProject(targetGuid, "CoreTelephony.framework", false);
			pbxProject.AddFrameworkToProject(targetGuid, "CoreMotion.framework", false);
            Debug.Log("build framework on unity version 2017 later version");
#endif

            File.WriteAllText(projectPath, pbxProject.WriteToString());
#endif
        }
    }

}