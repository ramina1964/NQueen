namespace NQueen.Kernel.Solvers.Engines;

internal sealed partial class BitmaskParallelEngine
{
    public void RunUnique(UniqueRequest request)
    {
        int N=request.BoardSize;request.ReportProgress(0.0);int totalRoots=(N+1)/2;int rootsCompleted=0;var tasks=new List<Task<HashSet<UInt128>>>();
        for(int firstRow=0;firstRow<totalRoots;firstRow++){int fr=firstRow;tasks.Add(Task.Run(()=>{var localUnique=new HashSet<UInt128>();var scratchBuf=new int[SymmetryHelper.GetScratchBufferSize(N)];var rowsArr=new int[N];Array.Fill(rowsArr,-1);rowsArr[0]=fr;ulong bitFirst=1UL<<fr;ulong cols=bitFirst;ulong d1=bitFirst<<1;ulong d2=bitFirst>>1;ulong mask=(N==64)?ulong.MaxValue:((1UL<<N)-1UL);ulong[] stackCols=new ulong[N];ulong[] stackD1=new ulong[N];ulong[] stackD2=new ulong[N];ulong[] stackRemaining=new ulong[N];int col=1;ulong remaining=ComputeAvail(col);while(true){if(col==N){SymmetryHelper.AddIfUniquePacked(rowsArr,localUnique,scratchBuf,out _,out _);col--;if(col<=0)break;Restore(col,out remaining);continue;} if(remaining==0){col--;if(col<=0)break;Restore(col,out remaining);continue;} ulong bit=remaining & (ulong)-(long)remaining;remaining^=bit;int row=BitOperations.TrailingZeroCount(bit);rowsArr[col]=row;stackCols[col]=cols;stackD1[col]=d1;stackD2[col]=d2;stackRemaining[col]=remaining;cols|=bit;d1=(d1|bit)<<1;d2=(d2|bit)>>1;col++;if(col==N)continue;remaining=ComputeAvail(col);} if(request.EnableEvents){int done=Interlocked.Increment(ref rootsCompleted);double pct=Math.Min(100.0,(double)done/totalRoots*100.0);request.ReportProgress(pct);} return localUnique; ulong ComputeAvail(int c){ulong avail=~(cols|d1|d2)&mask;int maxRow=SymmetryHelper.MaxRowExclusiveForColumn(N,c,rowsArr); if(maxRow<N) avail&=(1UL<<maxRow)-1UL; return avail;} void Restore(int c,out ulong rem){rem=stackRemaining[c];cols=stackCols[c];d1=stackD1[c];d2=stackD2[c];}}));}
        Task.WaitAll(tasks.ToArray());var globalUnique=new HashSet<UInt128>();foreach(var t in tasks){foreach(var key in t.Result){if(globalUnique.Add(key)){var rows=UnpackKeyToArray(key,N);request.OnUniqueSolution(rows);}}}request.ReportProgress(100.0);
    }
    private static int[] UnpackKeyToArray(UInt128 key,int n){var rows=new int[n];for(int i=n-1;i>=0;i--){rows[i]=(int)(key & 0x1F);key>>=5;}return rows;}
}
