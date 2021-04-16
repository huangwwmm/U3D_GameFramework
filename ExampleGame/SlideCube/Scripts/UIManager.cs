using System.Collections;
using System.Collections.Generic;
using GF.Core.Behaviour;
using UnityEngine;

namespace GF.ExampleGames.SlideCube
{
    public class UIManager:BaseBehaviour
    {
        public UIManager()
            : base("UIManager", (int)BehaviourPriority.GF_Start, BehaviourGroup.Default.ToString())
        {
            
        }
    }
}
