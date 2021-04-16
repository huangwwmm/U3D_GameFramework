using GF.Core.Event;
using UnityEngine;

namespace GF.ExampleGames.SlideCube
{
    public class SlideData : IUserData
    {
        public Transform transform;
        public Vector2 startPosition;
        public Vector2 endPosition;
    }
}