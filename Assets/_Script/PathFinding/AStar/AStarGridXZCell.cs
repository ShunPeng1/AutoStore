namespace _Script.PathFinding.AStar
{
    public interface IAStarGridData
    {
        public GridXZCell ParentXZCell { get; set; }
        public int FCost => HCost+GCost;
        public int HCost { get; set; }
        public int GCost { get; set; }
        public float Weight { get; set; }
    }

    public class AStarGridXZCell : GridXZCell, IAStarGridData 
    {
        public GridXZCell ParentXZCell { get; set; } = null;
        public int HCost { get; set; }
        public int GCost { get; set; }
        public float Weight { get; set; }
        
        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public AStarGridXZCell(GridXZ<GridXZCell> grid, int x, int z, StackStorage stackStorage) : base(grid, x, z, stackStorage)
        {
        }

    }
}
