using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GF.UI
{
    public class ConstData
    {
        //ui 资源存放路径
        public static string UIDataPath = Application.dataPath + "/UI_Test/Res/Fgui";
    
        //统计资源后文件存放位置
        private static string _windowInformation = "WindowInformation.json";
        public static string UIConfigPathBase = Application.dataPath + "/../Build/";
        public static string UIConfigPath = UIConfigPathBase + _windowInformation;
        
        //资源包的前缀
        public static string ResPackagePrefix = "Res";
    }

}
