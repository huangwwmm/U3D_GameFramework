using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[Serializable]
public class LevelEntity
{
    public int ID;

    public int Chapter;
    public int Level;

    public int Row, Colomn;
    public List<string> UIResources = new List<string>();

    public List<Vector2> OriginCubeRowAndColomns = new List<Vector2>();
    public List<Vector2> TargetCubeRowAndColomns = new List<Vector2>();

}

[Serializable]
public class ChapterEntity
{
    public List<LevelEntity> LevelEntities = new List<LevelEntity>();
}