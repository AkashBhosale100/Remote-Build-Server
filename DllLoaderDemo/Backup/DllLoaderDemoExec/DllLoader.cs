///////////////////////////////////////////////////////////////////////////
// DllLoader.cs - Demonstrate Robust loading and dynamic invocation of   //
//                Dynamic Link Libraries found in specified location     //
//                                                                       //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2004       //
///////////////////////////////////////////////////////////////////////////
/*
 * If user has entered args on command line then DllLoader assumes that the
 * first parameter is the path to a directory with testers to run.
 * 
 * Otherwise DllLoader checks if it is running from a debug directory.
 * 1.  If so, it assumes the testers directory is "../../Testers"
 * 2.  If not, it assumes the testers directory is "./testers"
 * 
 * If none of these are the case, then DllLoader emits an error message and
 * quits.
 */
 
using System;
using System.Reflection;
using System.IO;

namespace DllLoaderDemo
{
  class DllLoaderExec
  {
    //----< load assemblies from testersLocation and run their tests >-----

    string loadAndExerciseTesters(string testersLocation)
    {
      try
      {
        DllLoaderExec loader = new DllLoaderExec();

        // load each assembly found in testersLocation

        string[] files = Directory.GetFiles(testersLocation,"*.dll");
        foreach(string file in files)
        {
          Assembly asm = Assembly.LoadFrom(file);
          string fileName = Path.GetFileName(file);
          Console.Write("\n  loaded {0}",fileName);

          // exercise each tester found in assembly

          Type[] types = asm.GetTypes();
          foreach(Type t in types)
          {
            // if type supports ITest interface then run test

            if(t.GetInterface("DllLoaderDemo.ITest",true) != null)
              if(!loader.runSimulatedTest(t,asm))
                Console.Write("\n  test {0} failed to run",t.ToString());
          }
        }
      }
      catch(Exception ex)
      {
        return ex.Message;
      }
      return "Simulated Testing completed";
    }
    //
    //----< run tester t from assembly asm >-------------------------------

    bool runSimulatedTest(Type t, Assembly asm)
    {
      try
      {
        Console.Write(
          "\n  attempting to create instance of {0}",t.ToString()
          );
        object obj = asm.CreateInstance(t.ToString());

        // announce test

        MethodInfo method = t.GetMethod("say");
        if(method != null)
          method.Invoke(obj,new object[0]);
        
        // run test

        method = t.GetMethod("test");
        if(method != null)
          method.Invoke(obj,new object[0]);
      }
      catch(Exception)
      {
        Console.Write("\n  failed to create {0}",t.ToString());
        return false;
      }
            
      ///////////////////////////////////////////////////////////////////
      //  You would think that the code below should work, but it fails
      //  with invalidcast exception, even though the types are correct.
      //
      //    DllLoaderDemo.ITest tester = (DllLoaderDemo.ITest)obj;
      //    tester.say();
      //    tester.test();
      //
      //  This is a design feature of the .Net loader.  If code is loaded 
      //  from two different sources, then it is considered incompatible
      //  and typecasts fail, even thought types are Liskov substitutable.
      //
     return true;
    }
    //
    //----< extract name of current directory without its parents ---------

    string GuessTestersParentDir()
    {
      string dir = Directory.GetCurrentDirectory();
      int pos = dir.LastIndexOf(Path.DirectorySeparatorChar);
      string name = dir.Remove(0,pos+1).ToLower();
      if(name == "debug")
        return "../..";
      else
        return ".";
    }
    //----< run demonstration >--------------------------------------------

    [STAThread]
    static void Main(string[] args)
    {
      Console.Write("\n  Demonstrating Robust Test Loader");
      Console.Write("\n ==================================\n");

      DllLoaderExec loader = new DllLoaderExec();
      
      string testersLocation;
      if(args.Length > 0)
        testersLocation = args[0];
      else
        testersLocation = loader.GuessTestersParentDir() + "/Testers";

      // convert testers relative path to absolute path

      string absPath;
      absPath = Path.GetFullPath(testersLocation);
      Console.Write("\n  Loading Test Modules from:\n    {0}\n",absPath);

      // run load and tests

      string result = loader.loadAndExerciseTesters(absPath);
      
      Console.Write("\n\n  {0}",result);
      Console.Write("\n\n");
    }
  }
}
