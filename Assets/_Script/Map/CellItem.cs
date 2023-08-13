
using System.Collections.Generic;

public class CellItem
{
    public readonly Crate[] CratesStack;
    public int CurrentCrateCount = 0; 
    public CellItem(int stackSize)
    {
        CratesStack = new Crate[stackSize];
        
    }

    public void AddCrateToStack(Crate crate)
    {
        CratesStack[CurrentCrateCount] = crate;
        CurrentCrateCount++;
        
    }
    
    
    
}
