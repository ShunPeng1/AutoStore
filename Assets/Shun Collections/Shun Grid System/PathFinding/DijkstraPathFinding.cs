using System;
using System.Collections.Generic;

namespace Shun_Grid_System
{
    public class DijkstraPathFinding<TGrid, TCell, TItem> : Pathfinding<TGrid, TCell, TItem>
        where TGrid : BaseGrid2D<TCell, TItem>
        where TCell : BaseGridCell2D<TItem>
    {
        private IPathFindingDistanceCost _distanceCostFunction;
        private IPathFindingAdjacentCellSelection<TCell, TItem> _adjacentCellSelectionFunction;

        private readonly Dictionary<TCell, double> _gValues = new();
        private readonly Priority_Queue.SimplePriorityQueue<TCell, double> _openCells = new();


        public DijkstraPathFinding(TGrid gridXZ,
            IPathFindingAdjacentCellSelection<TCell, TItem> adjacentCellSelectionFunction = null,
            PathFindingCostFunction costFunctionType = PathFindingCostFunction.Manhattan) : base(gridXZ)
        {
            _adjacentCellSelectionFunction =
                adjacentCellSelectionFunction ?? new PathFindingAllAdjacentCellAccept<TCell, TItem>();
            _distanceCostFunction = costFunctionType switch
            {
                PathFindingCostFunction.Manhattan => new ManhattanDistanceCost(),
                PathFindingCostFunction.Euclidean => new EuclideanDistanceCost(),
                PathFindingCostFunction.Octile => new OctileDistanceCost(),
                PathFindingCostFunction.Chebyshev => new ChebyshevDistanceCost(),
                _ => throw new ArgumentOutOfRangeException(nameof(costFunctionType), costFunctionType, null)
            };
        }

        public DijkstraPathFinding(TGrid gridXZ, IPathFindingDistanceCost pathFindingDistanceCost) : base(gridXZ)
        {
            _distanceCostFunction = pathFindingDistanceCost;
        }


        public override LinkedList<TCell> FirstTimeFindPath(TCell startCell, TCell endCell,
            double maxCost = Double.PositiveInfinity)
        {
            throw new NotImplementedException();
        }

        public override LinkedList<TCell> UpdatePathWithDynamicObstacle(TCell currentStartNode,
            List<TCell> foundDynamicObstacles,
            double maxCost = Double.PositiveInfinity)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<TCell, double> FindAllCellsSmallerThanCost(TCell currentStartNode,
            double maxCost = Double.PositiveInfinity)
        {
            throw new NotImplementedException();
        }

        public List<TCell> LowestCostCellWithWeightMap(
            TCell currentStartCell,
            Dictionary<TCell, double> weightCellToCosts, 
            HashSet<TCell> obstacles = null)
        {

            List<TCell> lowestCostCells = new List<TCell>();
            double lowestWeightCost = GetWeightCost(currentStartCell);

            _gValues[currentStartCell] = 0;
            _openCells.Enqueue(currentStartCell, 0);

            // Dijkstra's algorithm to find the lowest weight cost cell from the start cell
            while (_openCells.Count > 0)
            {
                TCell currentCell = _openCells.Dequeue();

                var gValue = GetDijkstraGValue(currentCell);
                var cellCost = gValue + GetWeightCost(currentCell);
                if (lowestWeightCost > cellCost)
                {
                    lowestWeightCost = cellCost;
                    lowestCostCells.Clear();
                    lowestCostCells.Add(currentCell);
                }
                else if (lowestWeightCost == cellCost)
                {
                    lowestCostCells.Add(currentCell);
                }

                if (gValue > lowestWeightCost) break;

                // Check all adjacent cells
                foreach (var baseGridCell2D in currentCell.AdjacentCells)
                {
                    var neighbor = (TCell)baseGridCell2D;
                    
                    if (neighbor is not { IsObstacle: false } ||
                        !_adjacentCellSelectionFunction.CheckMovableCell(currentCell, neighbor)
                        || obstacles?.Contains(neighbor) != false) continue;
                    
                    double tentativeCost = GetDijkstraGValue(currentCell) + GetDistanceCost(currentCell, neighbor);

                    if (tentativeCost < GetDijkstraGValue(neighbor))
                    {
                        _gValues[neighbor] = tentativeCost;

                        if (_openCells.Contains(neighbor))
                            _openCells.TryRemove(neighbor);

                        _openCells.Enqueue(neighbor, tentativeCost);

                    }
                }
            }

            return lowestCostCells;


            double GetWeightCost(TCell cell)
            {
                return weightCellToCosts.TryGetValue(cell, out double value) ? value : 0;
            }

        }


        double GetDijkstraGValue(TCell cell)
        {
            return _gValues.TryGetValue(cell, out double value) ? value : double.PositiveInfinity;
        }

        protected virtual double GetDistanceCost(TCell start, TCell end)
        {
            var indexDifferenceAbsolute = Grid.GetIndexDifferenceAbsolute(start, end);

            return _distanceCostFunction.GetDistanceCost(indexDifferenceAbsolute.x, indexDifferenceAbsolute.y) +
                   start.GetAdditionalAdjacentCellCost(end);
        }

    }
}