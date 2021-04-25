﻿using System.Collections;
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
        public static string UIConfigPath = Application.streamingAssetsPath + "/" + _windowInformation;
    }

}
