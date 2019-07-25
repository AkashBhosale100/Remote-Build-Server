///////////////////////////////////////////////////////////////////////////
// TestedLIb.cs - Simulates operation of a tested module                 //
//                                                                       //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2004       //
///////////////////////////////////////////////////////////////////////////

using System;

namespace DllLoaderDemo
{
  public class Tested : ITested
  {
    public Tested()
    {
      Console.Write("\n    constructing instance of Tested");
    }
    public void say()
    {
      Console.Write("\n    Production code - TestedLib");
    }
  }
}
