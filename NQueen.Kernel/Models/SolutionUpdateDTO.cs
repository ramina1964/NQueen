﻿using NQueen.Kernel.Enums;

namespace NQueen.Kernel.Models;

public class SolutionUpdateDTO
{
    public sbyte BoardSize { get; set; }

    public SolutionMode SolutionMode { get; set; }

    public sbyte[] QueenPositions { get; set; }

    public HashSet<sbyte[]> Solutions { get; set; }

    public int NoOfSolution => Solutions.Count;
}