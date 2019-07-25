///////////////////////////////////////////////////////////////////////////
// DllLoader.cs - Demonstrate Robust loading and dynamic invocation of   //
//                Dynamic Link Libraries found in specified location     //
// ver 2 - tests now return bool for pass or fail                        //
// Author: Akash Bhosale, aabhosal@syr.edu                               //
// Source:Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2017//
//Environment:C# Console                                                //
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
 * Added references to:
 * - MessagePassingComm
 *
 * This package :
 * ----------------------
 *   Loads the specified dll and performs tests as mentioned in the test request
 *   
 * Interfaces
 * ---------------------
 * 1.static Assembly LoadFromComponentLibFolder(object sender, ResolveEventArgs args)       ->function to load assemblies
 * 2.string loadAndExerciseTesters()                                                        ->function to load and perform tests on them
 * 3.bool runSimulatedTest(Type t, Assembly asm, string LogFile)                            ->function to run tester t from assembly asm
 * 4.string GuessTestersParentDir()                                                         ->function to find tester parent directory
 * 5.public static void InitiateTest()                                                      ->funcion to initiate tests
 * 6.public static void UpdateDllLoader(string buildPath, string LogFileName)               ->function to upate tester location and log file name
 * 7.public static void CleanPath()                                                         ->function to delete files from build path
 * 
 * Required Files:
 ** ---------------
 * TestHarness.cs       : sets necessary paths and provides dll name depending on test request sent by builder
 * 
 * Maintenance History:
 * --------------------
  *  * ver 1.0 : 27 October 2017
 * - first release
 * 
 *  ver 2.0 6th December 2017
 *      -second release
 */


using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DllLoaderDemo
{
    public class DllLoaderExec
    {
        private static string testersLocation = "";
        private static string LogFile;
        private static System.IO.StreamWriter log;
        private static string LogFilePath = "../../../TestHarnessStore/TestLogs";

        //function to load assemblies
        static Assembly LoadFromComponentLibFolder(object sender, ResolveEventArgs args)
        {
            Console.Write("\n  called binding error event handler");
            string folderPath = testersLocation;
            string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath)) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }
        //----< load assemblies from testersLocation and run their tests >-----

        string loadAndExerciseTesters()
        {
            Console.Write("Testers location is {0}", testersLocation);
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromComponentLibFolder);
            try
            {
                DllLoaderExec loader = new DllLoaderExec();
                string[] files = Directory.GetFiles(testersLocation, "*.dll");
                foreach (string file in files)
                {
                    Console.Write("Loading file in test harness");
                    Assembly asm = Assembly.LoadFile(file);
                    string fileName = Path.GetFileName(file);
                    Console.Write("\n  loaded {0}", fileName);
                    Type[] types = asm.GetTypes();
                    foreach (Type t in types)
                    {
                        if (t.GetInterface("DllLoaderDemo.ITest", true) != null)
                        {
                            if (!loader.runSimulatedTest(t, asm, LogFile))
                            {
                                Console.Write("\n  test {0} failed to run", t.ToString());
                                using (log = new System.IO.StreamWriter(System.IO.Path.Combine(LogFilePath, "TestLogs_" + LogFile + ".txt"), true))
                                {
                                    log.WriteLine("\n  test {0} failed to run", t.ToString());
                                }
                            }
                            
                        }
                        else
                            using (log = new System.IO.StreamWriter(System.IO.Path.Combine(LogFilePath, "TestLogs_" + LogFile + ".txt"), true))
                            {
                                log.WriteLine("\n  test {0} failed", t.ToString());
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return "Simulated Testing completed";
        }

        //function to run tester t from assembly asm
        bool runSimulatedTest(Type t, Assembly asm, string LogFile)
        {
            try
            {
                Console.Write(
                  "\n  attempting to create instance of {0}", t.ToString()
                  );
                object obj = asm.CreateInstance(t.ToString());
                MethodInfo method = t.GetMethod("say");
                if (method != null)
                    method.Invoke(obj, new object[0]);
                bool status = false;
                method = t.GetMethod("test");
                if (method != null)
                    status = (bool)method.Invoke(obj, new object[0]);
                Func<bool, string> act = (bool pass) =>
                {
                    if (pass)
                    {
                        using (log = new System.IO.StreamWriter(System.IO.Path.Combine(LogFilePath, "TestLogs_" + LogFile + ".txt"), true))
                        {
                            log.WriteLine(Environment.NewLine + "passed");
                        }
                        return "passed";
                    }
                    else
                    {
                        using (log = new System.IO.StreamWriter(System.IO.Path.Combine(LogFilePath, "TestLogs_" + LogFile + ".txt"), true))
                        {
                            log.WriteLine(Environment.NewLine + "failed");
                        }
                        return "failed";
                    }
                };
                Console.Write("\n  test {0}", act(status));
                using (log = new System.IO.StreamWriter(System.IO.Path.Combine(LogFilePath, "TestLogs_" + LogFile + ".txt"), true))
                {
                    log.WriteLine( act(status));
                }
            }
            catch (Exception ex)
            {
                Console.Write("\n  test failed with message \"{0}\"", ex.Message);
                using (log = new System.IO.StreamWriter(System.IO.Path.Combine(LogFilePath, "TestLogs_" + LogFile + ".txt"), true))
                {
                    log.WriteLine("Failed");
                }
                return false;
            }
            return true;
        }


        //function to find tester parent directory
        string GuessTestersParentDir()
        {
            string dir = Directory.GetCurrentDirectory();
            int pos = dir.LastIndexOf(Path.DirectorySeparatorChar);
            string name = dir.Remove(0, pos + 1).ToLower();
            if (name == "debug")
                return "../..";
            else
                return ".";
        }


        //funcion to initiate tests
        public static void InitiateTest()
        {
            System.IO.Directory.CreateDirectory("../../../TestHarnessStore/TestLogs");
            using (log = new System.IO.StreamWriter(System.IO.Path.Combine(LogFilePath, "TestLogs_" + LogFile + ".txt"), true))
            {
                log.Write("Testing started at: " + DateTime.Now.ToString());
                foreach (System.Collections.DictionaryEntry env in Environment.GetEnvironmentVariables())
                {
                    log.WriteLine((string)env.Key + "=" + (string)env.Value);
                }
                log.WriteLine(Environment.NewLine + "Loading:" + Environment.NewLine);
            }
            Console.Write("\n  Demonstrating Robust Test Loader");
            DllLoaderExec loader = new DllLoaderExec();
            DllLoaderExec.testersLocation = Path.GetFullPath(DllLoaderExec.testersLocation);
            Console.Write("\n  Loading Test Modules from:\n    {0}\n", DllLoaderExec.testersLocation);
            string result = loader.loadAndExerciseTesters();
            Console.Write("\n===============================================================\n");
            Console.Write(result);
            using (log = new System.IO.StreamWriter(System.IO.Path.Combine(LogFilePath, "TestLogs_" + LogFile + ".txt"), true))
            {
                log.WriteLine(Environment.NewLine+result);
            }
            try
            {
                System.IO.File.Copy(System.IO.Path.Combine(LogFilePath, "TestLogs_" + LogFile + ".txt"), System.IO.Path.Combine("../../../TestHarnessStore/TestHarnessSendStore", "TestLogs_" + LogFile + ".txt"), true);
            }
            catch (Exception ex)
            { ex.ToString();
            }
            try
            {
                System.IO.File.Delete(System.IO.Path.Combine(LogFilePath, "TestLogs_" + LogFile + ".txt"));
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
            Console.WriteLine("\nTest logs generated at {0} ", System.IO.Path.GetFullPath(System.IO.Path.Combine("../../../TestHarnessStore/TestLogs")));
            CleanPath();
        }

        //function to upate tester location and log file name
        public static void UpdateDllLoader(string buildPath, string LogFileName)
        {
            testersLocation = buildPath;
            LogFile = LogFileName;
        }

        //function to delete files from build path
        public static void CleanPath()
        {
           
            string[] files = Directory.GetFiles("../../../TestHarnessStore/Builds", " *.dll");
            foreach(string file in files )
            {
                Console.Write("Deleting file {0}", file);
                try
                {
                    System.IO.File.Delete(file);
                }
                catch(Exception ex)
                {
                    ex.ToString();
                }                
            }

        }
    }
}
