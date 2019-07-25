///////////////////////////////////////////////////////////////////////////
// Interfaces.cs - Interfaces for DLL Loader Demonstration               //
//                                                                       //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2004       //
///////////////////////////////////////////////////////////////////////////

using System;

namespace DllLoaderDemo
{
  public interface ITest
  {
    void say();
    void test();
  }

  public interface ITested
  {
    void say();
  }
}
