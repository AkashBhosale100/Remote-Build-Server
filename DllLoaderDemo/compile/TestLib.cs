///////////////////////////////////////////////////////////////////////////
// TestLib.cs - Simulates testing production modules                     //
//                                                                       //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2004       //
///////////////////////////////////////////////////////////////////////////
/*
 * Note:
 * Since both Tests and the production code they test are application
 * specific, tester classes will know the names and locations of the
 * tested classes, so there is no need to use dynamic invocation here.
 *
 * The project for this code simply makes a reference to the tested
 * Library and calls new on the relevent classes and invokes the
 * resulting instances methods directly.
 * 
 */
using System;
using System.Reflection;
using System.IO;

namespace DllLoaderDemo
{
  public class Test1 : ITest
  {
    public Test1()
    {
      Console.Write("\n  constructing instance of Test1");
    }
    public virtual void say()
    {
      Console.Write("\n  Test #1:");
    }
    private ITested getTested()
    {
      Tested tested = new Tested();
      return tested;
    }
    public virtual void test()
    {
      ITested tested = getTested();
      tested.say();
    }
  }

  public class Test2 : ITest
  {
    public Test2()
    {
      Console.Write("\n  constructing instance of Test2");
    }
    public virtual void say()
    {
      Console.Write("\n  Test #2:");
    }
    private ITested getTested()
    {
      Tested tested = new Tested();
      return tested;
    }
    public virtual void test()
    {
      ITested tested = getTested();
      tested.say();
    }
  }
}
