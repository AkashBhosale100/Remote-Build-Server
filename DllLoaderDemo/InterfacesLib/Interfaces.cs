///////////////////////////////////////////////////////////////////////////
// Interfaces.cs - Interfaces for DLL Loader Demonstration               //
// ver 2 - changed test return to bool                                   //
// Author: Akash Bhosale, aabhosal@syr.edu                               //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2017     
//Environment: C# Console
///////////////////////////////////////////////////////////////////////////
/*
 *This package defines the interface for test driver and tested code 
 * 
 */
 
using System;

namespace DllLoaderDemo
{
  public interface ITest      
  {
    void say();
    bool test();
  }

  public interface ITested    
  {
    void say();
  }
}
