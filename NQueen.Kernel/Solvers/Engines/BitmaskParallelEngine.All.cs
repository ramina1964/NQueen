namespace NQueen.Kernel.Solvers.Engines;

using NQueen.Kernel.Solvers.Heuristics;

internal sealed partial class BitmaskParallelEngine
{
    public void RunAll(AllRequest request)
    {
        int N=request.BoardSize;int splitDepth=request.RootSplitDepth<1?1:request.RootSplitDepth;if(splitDepth>N)splitDepth=N; if(request.RootSplitDepth==-1) splitDepth=ParallelSplitDepthHeuristic.GetOptimalSplitDepth(N);
        request.ReportProgress(0.0);var tasks=new List<Task>();var rootStack=new Stack<RootFrame>();rootStack.Push(new RootFrame(0,0UL,0UL,0UL,new int[N]));ulong mask=(N==64)?ulong.MaxValue:((1UL<<N)-1UL);var rootList=new List<RootFrame>(N*N);
        while(rootStack.Count>0){var frame=rootStack.Pop();if(frame.Col==splitDepth){rootList.Add(frame);continue;}ulong avail=~(frame.Cols|frame.D1|frame.D2)&mask;while(avail!=0){ulong bit=avail & (ulong)-(long)avail;avail^=bit;int row=BitOperations.TrailingZeroCount(bit);var rowsCopy=(int[])frame.Rows.Clone();rowsCopy[frame.Col]=row;ulong cols=frame.Cols|bit;ulong d1=(frame.D1|bit)<<1;ulong d2=(frame.D2|bit)>>1;rootStack.Push(new RootFrame(frame.Col+1,cols,d1,d2,rowsCopy));}}
        int totalRoots=rootList.Count;int rootsCompleted=0;int lastPercentReported=-1;bool throttle=N>=SimulationSettings.LargeBoardProgressThrottleThreshold;int bucketSize=SimulationSettings.ProgressThresholdPct;if(bucketSize<1)bucketSize=1;
        foreach(var root in rootList){tasks.Add(Task.Run(()=>{var rowsArr=root.Rows;int startCol=root.Col;for(int i=startCol;i<N;i++) if(rowsArr[i]==0) rowsArr[i]=-1;ulong cols=root.Cols;ulong d1=root.D1;ulong d2=root.D2;ulong[] stackCols=new ulong[N];ulong[] stackD1=new ulong[N];ulong[] stackD2=new ulong[N];ulong[] stackRemaining=new ulong[N];int col=startCol;ulong remaining=ComputeAvail();while(true){if(col==N){request.OnSolution((int[])rowsArr.Clone());col--;if(col<startCol)break;Restore(col,out remaining);continue;} if(remaining==0){col--;if(col<startCol)break;Restore(col,out remaining);continue;} ulong bit=remaining & (ulong)-(long)remaining;remaining^=bit;int row=BitOperations.TrailingZeroCount(bit);rowsArr[col]=row;stackCols[col]=cols;stackD1[col]=d1;stackD2[col]=d2;stackRemaining[col]=remaining;cols|=bit;d1=(d1|bit)<<1;d2=(d2|bit)>>1;col++;if(col==N)continue;remaining=ComputeAvail();}
            if(request.EnableEvents){int done=Interlocked.Increment(ref rootsCompleted);ReportRootProgress(done,totalRoots,throttle,bucketSize,ref lastPercentReported,request.ReportProgress);} ulong ComputeAvail()=>~(cols|d1|d2)&mask; void Restore(int c,out ulong rem){rem=stackRemaining[c];cols=stackCols[c];d1=stackD1[c];d2=stackD2[c];}}));}
        Task.WaitAll(tasks.ToArray());request.ReportProgress(100.0);
    }
}
