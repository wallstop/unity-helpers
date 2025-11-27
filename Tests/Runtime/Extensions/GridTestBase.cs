namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    /// <summary>
    /// Shared helpers for grid-based extension tests.
    /// </summary>
    public abstract class GridTestBase : CommonTestBase
    {
        protected Grid CreateGrid(out GameObject owner)
        {
            owner = Track(new GameObject("Grid", typeof(Grid)));
            Grid grid = owner.GetComponent<Grid>();
            grid.cellSize = Vector3.one;
            return grid;
        }
    }
}
