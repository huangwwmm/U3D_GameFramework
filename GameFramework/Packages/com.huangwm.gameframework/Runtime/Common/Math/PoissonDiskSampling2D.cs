using GF.Common.Debug;
using System.Collections.Generic;
using UnityEngine;

namespace GF.Common.Math
{
    /// <summary>
    /// 在平面内采样一批分布均匀的点
    /// <see cref="http://devmag.org.za/2009/05/03/poisson-disk-sampling/"/>
    /// </summary>
    public class PoissonDiskSampling2D
    {
        private static readonly Vector2 NOTSET_POINT = Vector2.one * float.MaxValue;
        /// <summary>
        /// 采样区域的大小
        /// </summary>
        private Vector2 m_MapSize;
        /// <summary>
        /// 两个采样点之间的最小距离
        /// </summary>
        private float m_MinDistance;
        
        /// <summary>
        /// <see cref="m_MinDistance"/>的平方，为了性能
        /// </summary>
        private float m_MinDistanceSqrt;
        /// <summary>
        /// 单元格的边长
        /// </summary>
        private float m_CellSize;
        /// <summary>
        /// 横向有多少个格子
        /// </summary>
        private int m_XCellCount;
        /// <summary>
        /// 纵向有多少个格子
        /// </summary>
        private int m_YCellCount;

        /// <param name="result">采样的结果，应该传进来空列表</param>
        /// <param name="mapSize"><see cref="m_MapSize"/></param>
        /// <param name="minDistance"><see cref="m_MinDistance"/></param>
        /// <param name="sampleCountPrePoint">
        /// 每个点附近的尝试采样的次数
        /// 这个数值越大，随机出来的效果越好，但是性能也更废
        /// </param>
        public void Execute(ICollection<Vector2> result
            , Vector2 mapSize
            , float minDistance
            , int sampleCountPrePoint)
        {
            m_MapSize = mapSize;
            m_MinDistance = minDistance;

            m_MinDistanceSqrt = m_MinDistance * m_MinDistance;

            m_CellSize = m_MinDistance / Mathf.Sqrt(2);
            m_XCellCount = Mathf.CeilToInt(m_MapSize.x / m_CellSize);
            m_YCellCount = Mathf.CeilToInt(m_MapSize.y / m_CellSize);
            int cellCount = m_XCellCount * m_YCellCount;
            Vector2[] grid = new Vector2[cellCount];
            for (int iPoint = 0; iPoint < cellCount; iPoint++)
            {
                grid[iPoint] = NOTSET_POINT;
            }

            MDebug.Log("Sampling"
                , $"PoissonDiskSampling2D Sample: MapSize({m_MapSize}) MinDistance({m_MinDistance}) CellSize({m_CellSize}) XCellCount({m_XCellCount}) YCellCount({m_YCellCount})");

            Stack<Vector2> processPoints = new Stack<Vector2>();

            Vector2 firstPoint = new Vector2(Random.Range(0, m_MapSize.x), Random.Range(0, m_MapSize.y));
            processPoints.Push(firstPoint);
            result.Add(firstPoint);
            grid[PointToGridIndex(firstPoint)] = firstPoint;

            while (processPoints.Count > 0)
            {
                Vector2 handlePoint = processPoints.Pop();
                for (int iPoint = 0; iPoint < sampleCountPrePoint; iPoint++)
                {
                    Vector2 newPoint = GenerateRandomPointAround(handlePoint);
                    if (InRectangle(newPoint)
                        && !HasNeighbourhood(grid, newPoint))
                    {
                        processPoints.Push(newPoint);
                        result.Add(newPoint);
                        grid[PointToGridIndex(newPoint)] = newPoint;

                    }
                }
            }
        }

        private bool InRectangle(Vector2 point)
        {
            return point.x >= 0
                && point.x < m_MapSize.x
                && point.y >= 0
                && point.y < m_MapSize.y;
        }

        private int PointToGridIndex(Vector2 point)
        {
            int indexX = (int)(point.x / m_CellSize);
            int indexY = (int)(point.y / m_CellSize);

            MDebug.LogVerbose("Sampling", $"PointToGridIndex({point}) indexX({indexX}) indexY({indexY})");
            return indexX + indexY * m_YCellCount;
        }

        private void PointToGridIndex(Vector2 point, out int xIndex, out int yIndex)
        {
            xIndex = (int)(point.x / m_CellSize);
            yIndex = (int)(point.y / m_CellSize);
        }

        /// <summary>
        /// 在到point距离高于<see cref="m_MinDistance"/>低于2倍<see cref="m_MinDistance"/>的圆环内随机一个点
        /// </summary>
        private Vector2 GenerateRandomPointAround(Vector2 point)
        {
            float radius = m_MinDistance * (1 + Random.value);
            float angle = 2 * Mathf.PI * Random.value;
            return new Vector2(radius * Mathf.Cos(angle)
                    , radius * Mathf.Sin(angle))
                + point;
        }

        /// <summary>
        /// 是否有临近（距离小于<see cref="m_MinDistance"/>）的点
        /// </summary>
        private bool HasNeighbourhood(Vector2[] grid, Vector2 point)
        {
            PointToGridIndex(point, out int pointXIndex, out int pointYIndex);
            for (int iXOffset = -2; iXOffset < 5; iXOffset++)
            {
                for (int iYOffset = -2; iYOffset < 5; iYOffset++)
                {
                    int iterPointXIndex = pointXIndex + iXOffset;
                    int iterPointYIndex = pointYIndex + iYOffset;
                    if (iterPointXIndex >= 0
                        && iterPointXIndex < m_XCellCount
                        && iterPointYIndex >= 0
                        && iterPointYIndex < m_YCellCount)
                    {
                        int iterPointIndex = (pointYIndex + iYOffset) * m_YCellCount + (pointXIndex + iXOffset);
                        Vector2 neighPoint = grid[iterPointIndex];
                        if (neighPoint != NOTSET_POINT
                            && (neighPoint - point).sqrMagnitude < m_MinDistanceSqrt)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}