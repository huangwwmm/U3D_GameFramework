using GF.Common.Math;
using GF.Common.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace GFTests.Common.Math
{
    public class TEST_PoissonDiskSampling2D : MonoBehaviour
    {
        public Vector2 MapSize;
        public float MinDistance;
        public int SampleCountPrePoint;

        [ContextMenu("Execute")]
        private void Execute()
        {
            ObjectUtility.DestroyAllChildern(transform);

            transform.localPosition = new Vector3(MapSize.x * -0.5f, 0, MapSize.y * -0.5f);

            List<Vector2> points = new List<Vector2>();
            new PoissonDiskSampling2D()
                .Execute(points, MapSize, MinDistance, SampleCountPrePoint);

            Vector3 localSize = Vector3.one * MinDistance * 0.2f;
            for (int iPoint = 0; iPoint < points.Count; iPoint++)
            {
                Transform child = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                child.SetParent(transform, false);
                child.localPosition = new Vector3(points[iPoint].x, 0, points[iPoint].y);
                child.localScale = localSize;
            }
        }
    }
}