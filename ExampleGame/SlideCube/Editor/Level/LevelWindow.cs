using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GF.Common.Debug;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class LevelWindow : EditorWindow
{
    private string m_FilePath = "";
    private string m_UiFilePath = "";

    private List<string> m_SelectedChapters;//目前已有章节选项
    private List<string> m_SelectedLevels;//目前已有关卡选项
    private Dictionary<int, List<LevelEntity>> m_ChapterDic;

    private int m_TargetChapter;//当前选择的章节
    private int m_TargetLevel;//当前选择的关卡

    private string m_InputRow;
    private string m_InputColomn;

    private bool m_IsConfirm;

    private List<Texture2D> m_OriginUISources;
    private List<Texture2D> m_TargetUISources;
    int m_Row,m_Colomn = 0;
    private Vector2 m_MousePosition;

    private LevelEntity m_CurrentLevelEntity;
    private ChapterEntity m_CurrentChapterEntity;
    
    // 储存获取到的图片  
    List<Texture2D> allTex2d = new List<Texture2D>();
    private void OnEnable()
    {
        m_SelectedChapters = new List<string>();
        m_SelectedLevels = new List<string>();
        m_ChapterDic = new Dictionary<int, List<LevelEntity>>();
        m_TargetChapter = 0;
        m_TargetLevel = 0;
        m_InputRow = "0";
        m_InputColomn = "0";
        m_IsConfirm = false;
        m_OriginUISources = new List<Texture2D>();
        m_TargetUISources = new List<Texture2D>();
        m_Row=m_Colomn = 0;
        m_CurrentLevelEntity = new LevelEntity();
        m_CurrentChapterEntity = new ChapterEntity();
        
        
         //filePath = Application.dataPath + "/../../ExampleGame/SlideCube/Editor/Level/Data/LevelInfos.json";
         m_FilePath = Application.dataPath + "/../../ExampleGame/SlideCube/Editor/Level/Data/";
         m_UiFilePath = Application.dataPath + "/../../ExampleGame/SlideCube";
        //todo 读取数据表
        // if (filePath!=null)
        // {
        //     string jsonData= System.IO.File.ReadAllText(filePath);
        //     m_CurrentChapterEntity=JsonUtility.FromJson(jsonData,typeof(ChapterEntity)) as ChapterEntity;
        // }
        // LoadTexture();
    }

    private void OnBecameVisible()
    {
        if (m_CurrentChapterEntity==null||m_CurrentChapterEntity.LevelEntities.Count==0)
        {
            return;
        }
        //解析数据
        List<LevelEntity> currentLevelEntitys = m_CurrentChapterEntity.LevelEntities;
        for (int i = 0; i < currentLevelEntitys.Count; i++)
        {
            if (!m_ChapterDic.ContainsKey(currentLevelEntitys[i].Chapter))
            {
                List<LevelEntity> levelEntitys = new List<LevelEntity>();
                levelEntitys.Add(currentLevelEntitys[i]);
                m_ChapterDic[currentLevelEntitys[i].Chapter] = levelEntitys;
            }
            else
            {
                m_ChapterDic[currentLevelEntitys[i].Chapter].Add(currentLevelEntitys[i]);
            }
        }

        foreach (var chapter in m_ChapterDic)
        {
            m_SelectedChapters.Add($"第{chapter.Key.ToString()}章");
        }
        
        //===================================默认加载================================================
        //默认加载第一章节关卡
        for (int i = 0; i < m_ChapterDic[1].Count; i++)
        {
            m_SelectedLevels.Add($"关卡{m_ChapterDic[1][i].Level.ToString()}");
        }
        //第一章第一关卡
        m_Row = m_ChapterDic[1][0].Row;
        m_Colomn = m_ChapterDic[1][0].Colomn;
        m_InputRow = m_ChapterDic[1][0].Row.ToString();
        m_InputColomn = m_ChapterDic[1][0].Colomn.ToString();

        
        OnCreateConfirm();
    }

    private Texture2D GetTargetTexture(string textureName)
    {
        for (int i = 0; i < allTex2d.Count; i++)
        {
            if (allTex2d[i].name==(textureName+".png"))
            {
                return allTex2d[i];
            }
        }
        return null;
    }
    

    void LoadTexture()
    {
        List<string> filePaths = new List<string>();
        string imgtype = "*.BMP|*.JPG|*.GIF|*.PNG";
        string[] ImageType = imgtype.Split('|');
        for (int i = 0; i < ImageType.Length; i++)
        {
            //获取Application.dataPath文件夹下所有的图片路径  
            string[] dirs = Directory.GetFiles(m_UiFilePath, ImageType[i]);
            for (int j = 0; j < dirs.Length; j++)
            {
                filePaths.Add(dirs[j]);
            }
        }

        for (int i = 0; i < filePaths.Count; i++)
        {
            Texture2D tx = new Texture2D(64, 64);
            tx.LoadImage(GetImageByte(filePaths[i]));
            allTex2d.Add(tx);
            tx.name = filePaths[i].Split('\\')[1];
        }
    }
    
    /// <summary>  
    /// 根据图片路径返回图片的字节流byte[]  
    /// </summary>  
    /// <param name="imagePath">图片路径</param>  
    /// <returns>返回的字节流</returns>  
    private static byte[] GetImageByte(string imagePath)
    {
        FileStream files = new FileStream(imagePath, FileMode.Open);
        byte[] imgByte = new byte[files.Length];
        files.Read(imgByte, 0, imgByte.Length);
        files.Close();
        return imgByte;
    }

    private void OnGUI()
    {
        #region 按钮行

        GUILayout.BeginHorizontal("box");
        if (GUILayout.Button("创建章节",GUILayout.Width(100)))
        {
            EditorApplication.delayCall = OnChapterCreateCallBack;
        }
        if (GUILayout.Button("删除章节",GUILayout.Width(100)))
        {
            EditorApplication.delayCall = OnTargetChapterDeleteCallBack;
        }
        if (GUILayout.Button("创建关卡",GUILayout.Width(100)))
        {
            EditorApplication.delayCall = OnLevelCreateCallBack;
        }
        if (GUILayout.Button("删除关卡",GUILayout.Width(100)))
        {
            EditorApplication.delayCall = OnTargetLevelDeleteCallBack;
        }
        EditorGUILayout.Space();
        GUILayout.EndHorizontal();

        #endregion

        #region 当前选中章节关卡

        GUILayout.BeginHorizontal("box");
        GUILayout.Label("当前章节",GUILayout.Width(200));
        GUILayout.Label("当前关卡",GUILayout.Width(200));
        GUILayout.EndHorizontal();
            
        GUILayout.BeginHorizontal("box");
        m_TargetChapter=EditorGUILayout.Popup(m_TargetChapter, m_SelectedChapters.ToArray(), GUILayout.Width(200));
        m_TargetLevel=EditorGUILayout.Popup(m_TargetLevel, m_SelectedLevels.ToArray(), GUILayout.Width(200));
        GUILayout.EndHorizontal();

        #endregion
        
        GUILayout.BeginHorizontal("box");
        GUILayout.Label("当前关卡行数",GUILayout.Width(100));
        GUILayout.Label("当前关卡列数",GUILayout.Width(100));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal("box");
        m_InputRow = EditorGUILayout.TextField(m_InputRow, GUILayout.Width(100));
        m_InputColomn=EditorGUILayout.TextField(m_InputColomn, GUILayout.Width(100));
        if (GUILayout.Button("确定",GUILayout.Width(50)))
        {
            EditorApplication.delayCall= OnCreateConfirm;
        }
        GUILayout.EndHorizontal();
        

        if (m_IsConfirm)
        {
            m_MousePosition = EditorGUILayout.BeginScrollView(m_MousePosition);
            #region origin
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("当前关卡初始布局",GUILayout.Width(100));
            GUILayout.BeginVertical("box");
            
            if (m_Row<2|| m_Colomn<2)
            {
                return;
            }

            int index = 0;
            for (int i = 0; i < m_Row; i++)
            {
                GUILayout.BeginHorizontal();
                for (int j = 0; j < m_Colomn; j++)
                {
                    m_OriginUISources[index]= EditorGUILayout.ObjectField("",m_OriginUISources[index],typeof(Texture2D),GUILayout.Width(100)) as Texture2D;
                    index++;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            #endregion

            #region target

            GUILayout.BeginHorizontal("box");
            GUILayout.Label("当前关卡目标布局",GUILayout.Width(100));
            GUILayout.BeginVertical("box");
            index = 0;
            for (int i = 0; i < m_Row; i++)
            {
                GUILayout.BeginHorizontal();
                for (int j = 0; j < m_Colomn; j++)
                {
                    m_TargetUISources[index]= EditorGUILayout.ObjectField("",m_TargetUISources[index],typeof(Texture2D),GUILayout.Width(100)) as Texture2D;
                    index++;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        
            GUILayout.BeginHorizontal("box");
            if (GUILayout.Button("保存",GUILayout.Width(100)))
            {
                EditorApplication.delayCall = OnSaveLevelData;
            }
            GUILayout.EndHorizontal();

            #endregion
            //m_IsConfirm = false;
            EditorGUILayout.EndScrollView();
        }
    }

    private void OnSaveLevelData()
    {

        if (m_OriginUISources.Count!=m_TargetUISources.Count)
        {
            return;
        }

        LevelEntity levelEntity = new LevelEntity();
        levelEntity.ID = 0;
        levelEntity.Chapter = m_TargetChapter+1;
        levelEntity.Level = m_TargetLevel+1;
        levelEntity.Row = m_Row;
        levelEntity.Colomn = m_Colomn;
        for (int i = 0; i < m_OriginUISources.Count; i++)
        {
            levelEntity.UIResources.Add(m_OriginUISources[i].ToString().Split(' ')[0]);
        }

        int index = 0;
        for (int i = 0; i < m_Row; i++)
        {
            for (int j = 0; j < m_Colomn; j++)
            {
                levelEntity.OriginCubeRowAndColomns.Add(new Vector2(i,j));
                levelEntity.TargetCubeRowAndColomns.Add(GetTargetRowAndColomn(m_OriginUISources[index]));
                index++;
            }
        }
        string jsonData = JsonUtility.ToJson(levelEntity);
        System.IO.File.WriteAllText(m_FilePath+$"Chapter_{levelEntity.Chapter}_Level_{levelEntity.Level}.json", jsonData);
        MDebug.Log("保存数据：","保存完毕");
    }

    private Vector2 GetTargetRowAndColomn(Texture texture)
    {
        Vector2 retRowAndColomn = new Vector2(-1, -1);
        int index = 0;
        for (int i = 0; i < m_Row; i++)
        {
            for (int j = 0; j < m_Colomn; j++)
            {
                if (m_TargetUISources[index]==texture)
                {
                    retRowAndColomn = new Vector2(i, j);
                    return retRowAndColomn;
                }

                index++;
            }
        }
        MDebug.Log("错误：",$"{texture}图片不匹配！");
        return retRowAndColomn;
    }

    private void OnTargetLevelDeleteCallBack()
    {
        if (m_SelectedLevels.Count-1 == m_TargetLevel&&m_SelectedLevels.Count>0)
        {
            MDebug.Log("删除关卡：",$"关卡{m_SelectedLevels.Count}");
            
            if (m_ChapterDic.ContainsKey(m_TargetChapter))
            {
                LevelEntity levelEntity= m_ChapterDic[m_TargetChapter][m_SelectedLevels.Count];
                m_ChapterDic[m_TargetChapter].Remove(levelEntity);
            }
            
            m_SelectedLevels.Remove(m_SelectedLevels[m_SelectedLevels.Count-1]);
            m_TargetLevel = m_TargetLevel - 1 < 0 ? 0 : m_TargetLevel - 1;
        }
    }

    private void OnLevelCreateCallBack()
    {
        if (m_SelectedChapters.Count>0)
        {
            MDebug.Log("创建关卡：",$"关卡{m_SelectedLevels.Count+1}");
            
            LevelEntity levelEntity = new LevelEntity();
            
            m_ChapterDic[m_SelectedChapters.Count].Add(levelEntity);
            
            m_SelectedLevels.Add($"关卡{m_SelectedLevels.Count+1}");
        }
    }

    private void OnTargetChapterDeleteCallBack()
    {
        if (m_SelectedChapters.Count-1==m_TargetChapter&&m_SelectedChapters.Count>0)
        {
            MDebug.Log("删除章节：",$"第{m_SelectedChapters.Count}章");

            if (m_ChapterDic.ContainsKey(m_SelectedChapters.Count))
            {
                List<LevelEntity> levelEntitys = m_ChapterDic[m_SelectedChapters.Count];
                levelEntitys.Clear();
                m_ChapterDic.Remove(m_SelectedChapters.Count);
            }
            
            m_SelectedChapters.Remove(m_SelectedChapters[m_SelectedChapters.Count-1]);
            m_TargetChapter = m_TargetChapter - 1 < 0 ? 0 : m_TargetChapter - 1;
        }
    }

    private void OnChapterCreateCallBack()
    {
        MDebug.Log("创建章节：",$"第{m_SelectedChapters.Count+1}章");

        m_SelectedChapters.Add($"第{m_SelectedChapters.Count+1}章");
        if (!m_ChapterDic.ContainsKey(m_SelectedChapters.Count))
        {
            List<LevelEntity> levelEntitys = new List<LevelEntity>();
            m_ChapterDic[m_SelectedChapters.Count] = levelEntitys;
        }
    }

    /// <summary>
    /// 点击确认
    /// </summary>
    private void OnCreateConfirm()
    {
        
        int.TryParse(m_InputRow, out m_Row);
        int.TryParse(m_InputColomn, out m_Colomn);

        if (m_Row<2||m_Colomn<2)
        {
            return;
        }
        
        m_IsConfirm = true;

        m_OriginUISources.Clear();
        m_TargetUISources.Clear();
        for (int i = 0; i < m_Row; i++)
        {
            for (int j = 0; j < m_Colomn; j++)
            {
                Texture2D source = null;
                m_OriginUISources.Add(source);
                m_TargetUISources.Add(source);
            }
        }
    }

    private void OnBecameInvisible()
    {
        m_FilePath = null;
        m_UiFilePath = null;
        m_SelectedChapters.Clear();
        m_SelectedLevels.Clear();
        m_ChapterDic.Clear();
        m_InputRow = null;
        m_InputColomn = null;
        m_IsConfirm = false;
        m_OriginUISources.Clear();
        m_TargetUISources.Clear();
        m_CurrentLevelEntity = null;
    }
}
