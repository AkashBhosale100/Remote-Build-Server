/*TestHarness.cs-----Mangages all interactions between child builder and repository related to perform tests on build(.dll)
 * Author: ------ Akash Bhosale, aabhosal@syr.edu
  Source: Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2017 
  Course:CSE 681 Software Modelling and Analysis
  Application: CSE 681 Project 4-TestHarness.cs
  Environment: C# Console
  *--------------------------------------------------------------------------------------------------------*/
/*
 * Added references to:
 * - MessagePassingComm
 *
 * This package provides:
 * ----------------------
 * Handling of incoming messages from various processes and invokes the loader to perform load operation
 * It also sents test logs generated
 * 
 * Interfaces
 * ---------------------
 * 1.public static void InitializeReceiver()                                    ->function to initialize receiver
 * 2.public static void getMessage()                                            ->thread function to get messages from the receiver queue  
 * 3.public static void thSetup()                                               ->function to perform intial operations at startup
 * 4.public static void AcceptBuild()                                           ->function to perform operations after receiving build
 * 5.public static void SendMsgtoRepo(string LogFileName)                       ->Send message to repository for receiving test logs
 * 6.public static void SendAckMsgToRepo(CommMessage rcvMsg)                    ->function to send acknowledgement message to repo
 * 7.public static void AcceptTr(CommMessage rvcMsg)                            ->function to accept test request send by builder
 * 8.public static void buildSendMsgToBldr(CommMessage rcvMsg)                  ->function to send msg to builder requesting to send build
 * 9.public static bool loadXML(string path)                                     ->function to load test request
 * 10.public static string parse(string propertyName)                             ->function to parse test request
              
 * Required Files:
 ** ---------------
 * IMPCommService.cs         : Service interface and Message definition
 * MPCommService.cs 
 * Maintenance History:
 * --------------------
  *
 *  ver 1.0 6th December 2017
 *      -second release
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using MessagePassingComm;
using System.Threading;
using DllLoaderDemo;
namespace TestHarness
{

    class TestHarness
    {
        private static string baseAddress = "http://localhost";
        private static int port = 8077;
        private static Receiver rcvr;
        private static Sender sndr;
        private static CommMessage rcvMsg;
        private static Sender[] cs;
        private static readonly object ConsoleWriterLock = new object();
        private static string buildPath = "../../../TestHarnessStore/Builds";
        private static string testRequestPath = "../../../TestHarnessStore/TestRequests";
        private static string LogFileName = "";
        private static XDocument doc;
        private static string testFile = "";

        static void Main(string[] args)
        {
            thSetup();
            InitializeReceiver();
            Thread rcvMsg = new Thread(new ThreadStart(getMessage));
            rcvMsg.Start();
            if (args.Count() != 0)
            {
                cs = new Sender[Int32.Parse(args[0]) + 100];
                for (int i = 0; i <= Int32.Parse(args[0]); i++)
                {
                    cs[i] = new Sender("http://localhost", 8081 + i);
                }
            }
        }
        //function to initialize receiver
        public static void InitializeReceiver()
        {
            rcvr = new Receiver();
            rcvr.start(baseAddress, port);
        }
        //thread function to get messages from the receiver queue
        public static void getMessage()
        {
            Console.Write("\nStarting getMessage thread to check incoming messages in Test Harness");
            Console.Write("\n=========================================================");
            while (true)
            {
                rcvMsg = rcvr.getMessage();
                lock (ConsoleWriterLock)
                {
                    rcvMsg.show();
                }
                if (rcvMsg.command == "BuildSent")
                {
                    AcceptBuild();
                }
                else if (rcvMsg.command == "ReadyToReceiveLogs")
                    SendAckMsgToRepo(rcvMsg);
                else if (rcvMsg.command == "SendTestLogs")
                    SendTestLogsToRepo(rcvMsg);
                else if (rcvMsg.command == "GetReadyToReceiveTestRequest")
                    sendTestReadyMsg(rcvMsg);
                else if (rcvMsg.command == "AcknowledgementAcceptedForTh")
                    sendMsgtoBuilderTh(rcvMsg);
                else if (rcvMsg.command == "TestRequestSent")
                    AcceptTr(rcvMsg);
                else if (rcvMsg.command == "GetReadyToReceiveBuild")
                    buildSendMsgToBldr(rcvMsg);
                else if (rcvMsg.command == "quit")
                {
                    Console.Write("===Quitting===========");
                    Process.GetCurrentProcess().Kill();
                }
            }
        }

        //function to perform intial operations at startup
        public static void thSetup()
        {
            Console.Title = "Test Harness";
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write("Starting Test Harness");
            string ClientfileStorage = "../../../TestHarnessStore/TestHarnessSendStore";
            string ServicefileStorage = "../../../TestHarnessStore/TestHarnessReceiveStore";
            ClientEnvironment.fileStorage=ClientfileStorage;
            ServiceEnvironment.fileStorage=ServicefileStorage;
            System.IO.Directory.CreateDirectory(buildPath);
        }
        
        //function to perform operations after receiving builds
        public static void AcceptBuild()
        {
            var directories = System.IO.Directory.GetDirectories(ServiceEnvironment.fileStorage);
            foreach (string dir in directories)
            {
                var subdirectories = System.IO.Directory.GetDirectories(dir);
                foreach (string subdirectory in subdirectories)
                {
                    string[] buildFiles = System.IO.Directory.GetFiles(subdirectory);
                    foreach (string file in buildFiles)
                    {
                        if (System.IO.File.Exists(System.IO.Path.Combine(buildPath, System.IO.Path.GetFileName(file))))
                        {
                            try
                            {
                                System.IO.File.Delete(System.IO.Path.Combine(buildPath, System.IO.Path.GetFileName(file)));
                            }
                            catch(Exception ex) { ex.ToString(); }
                        }
                        try
                        {
                            System.IO.File.Move(file, System.IO.Path.Combine(buildPath, System.IO.Path.GetFileName(file)));
                        }
                        catch(Exception ex)
                        {
                            ex.ToString();
                        }
                        string[] buildFiles1 = System.IO.Directory.GetFiles(subdirectory);
                        if (buildFiles1.Length == 0)
                        {
                          System.IO.Directory.Delete(subdirectory);
                        }
                        var subdirectories1 = System.IO.Directory.GetDirectories(dir);
                        if (subdirectories1.Length == 0)
                        {
                            System.IO.Directory.Delete(dir);
                        }
                        LogFileName = System.IO.Path.GetFileNameWithoutExtension(file);
                        DllLoaderExec.UpdateDllLoader(buildPath, System.IO.Path.GetFileNameWithoutExtension(file));
                        DllLoaderExec.InitiateTest();
                        SendMsgtoRepo(LogFileName);
                    }
                }
            }

        }
        //Send message to repository for receiving test logs
        public static void SendMsgtoRepo(string LogFileName)
        {
            sndr = new Sender(baseAddress, 8078);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "GetReadyToReceiveTestLogs";
            sndMsg.author = "Akash Bhosale";
            string toAddress = baseAddress + ":" + "8078" + "/IpluggableComm";
            string fromAddress = baseAddress + ":" + "8077" + "/IpluggableComm";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            sndMsg.arguments.Add(LogFileName);
            sndr.postMessage(sndMsg);

        }

        //function to send acknowledgement message to repo
        public static void SendAckMsgToRepo(CommMessage rcvMsg)
        {
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "AcknowledgementAcceptedFromTh";
            sndMsg.author = "Akash Bhosale";
            string toAddress = baseAddress + ":" + "8078" + "/IpluggableComm";
            string fromAddress = baseAddress + ":" + "8077" + "/IpluggableComm";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            sndMsg.arguments.Add(rcvMsg.arguments[0]);
            sndr.postMessage(sndMsg);
        }

        //function to send test logs to repository
        public static void SendTestLogsToRepo(CommMessage rcvMsg)
        {
            Console.Write("\n\n===================================================================================\n\n");
            Console.Write("Sending test logs from {0}", System.IO.Path.GetFullPath(ClientEnvironment.fileStorage));
            Console.Write("\n\n===================================================================================\n\n");
            LogFileName = rcvMsg.arguments[0];
            Console.Write("\n\n===================================================================================\n\n");
            Console.Write("Log file Name is  {0}", "TestLogs_"+LogFileName);
            Console.Write("\n\n===================================================================================\n\n");
            Console.Write("Sending file {0}", "TestLogs_"+LogFileName);
            Console.Write("\n\n===================================================================================\n\n");
            sndr.postFile("TestLogs_"+LogFileName + ".txt", 0, LogFileName);
            Thread.Sleep(3000);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "TestLogsSent";
            sndMsg.author = "Akash Bhosale";
            string toAddress = baseAddress + ":" + "8078" + "/IpluggableComm";
            string fromAddress = baseAddress + ":" + "8077" + "/IpluggableComm";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            sndr.postMessage(sndMsg);
            DllLoaderExec.CleanPath();
        }
        //function to send ready msg to builder
        public static void sendTestReadyMsg(CommMessage rcvMsg)
        {
            int count = rcvMsg.arguments.Count<string>();
            int chldPort = Int32.Parse(rcvMsg.arguments[count - 1]);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "ReadyToReceiveTestRequest";
            sndMsg.author = "Akash Bhosale";
            sndMsg.to = baseAddress + ":" + "808" + chldPort + "/IpluggableComm";
            sndMsg.from = baseAddress + ":" + "8077" + "/IpluggableComm"; sndMsg.show();
            sndMsg.arguments.Add(rcvMsg.arguments[0]);
            cs[chldPort].postMessage(sndMsg);

        }

        //function to send message to the builder
        public static void sendMsgtoBuilderTh(CommMessage rcvMsg)
        {

            int count = rcvMsg.arguments.Count<string>();
            int chldPort = Int32.Parse(rcvMsg.arguments[count - 1]);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "SendTestRequest";
            sndMsg.author = "Akash Bhosale";
            sndMsg.to = baseAddress + ":" + "808" + chldPort + "/IpluggableComm";
            sndMsg.from = baseAddress + ":" + "8077" + "/IpluggableComm"; sndMsg.show();
            sndMsg.arguments.Add(rcvMsg.arguments[0]);
            cs[chldPort].postMessage(sndMsg);

        }

        //function to accept test request send by builder
        public static void AcceptTr(CommMessage rvcMsg)
        {
            var directories = System.IO.Directory.GetDirectories(ServiceEnvironment.fileStorage);
            foreach (string dir in directories)
            {
                var subdirectories = System.IO.Directory.GetDirectories(dir);
                foreach (string subdirectory in subdirectories)
                {
                    string[] testRequest = System.IO.Directory.GetFiles(subdirectory);
                    foreach (string file in testRequest)
                    {
                        if (System.IO.File.Exists(System.IO.Path.Combine(testRequestPath, System.IO.Path.GetFileName(file))))
                        {
                            System.IO.File.Delete(System.IO.Path.Combine(testRequestPath, System.IO.Path.GetFileName(file)));
                        }

                        try
                        {
                            System.IO.File.Move(file, System.IO.Path.Combine(testRequestPath, System.IO.Path.GetFileName(file)));
                        }
                        catch (Exception ex)
                        {
                            ex.ToString();
                        }
                        
                        Console.Write("\n=========================================================================\n\n");
                        Console.Write("Parsing received test request ");
                        Console.Write("\n=========================================================================\n\n");
                        ParseTestRequest(System.IO.Path.GetFileName(file));
                        string[] testRequest1 = System.IO.Directory.GetFiles(subdirectory);
                        if (testRequest1.Length == 0)
                        {
                            try
                            {
                                System.IO.Directory.Delete(subdirectory);
                            }
                            catch(Exception ex) { ex.ToString(); }
                        }
                        var subdirectories1 = System.IO.Directory.GetDirectories(dir);
                        if (subdirectories1.Length == 0)
                        {
                            System.IO.Directory.Delete(dir);
                        }
                    }
                }
            }
       }

        //function to send msg to the builder to send build
        public static void buildSendMsgToBldr(CommMessage rcvMsg)
        {
            CleanPath();
            int count = rcvMsg.arguments.Count<string>();
            int chldPort = Int32.Parse(rcvMsg.arguments[count - 1]);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "SendBuild";
            sndMsg.author = "Akash Bhosale";
            sndMsg.to = baseAddress + ":" + "808" + chldPort + "/IpluggableComm";
            sndMsg.from = baseAddress + ":" + "8077" + "/IpluggableComm"; sndMsg.show();
            sndMsg.arguments.Clear();
            sndMsg.arguments.Add(testFile);
            cs[chldPort].postMessage(sndMsg);
            
        }


        //function to clean path 
        public static void CleanPath()
        {
            string[] files = Directory.GetFiles("../../../TestHarnessStore/Builds");

            foreach (string file in files)
            {
                try
                {
                    System.IO.File.Delete(file);
                }
                catch(Exception ex) { ex.ToString(); }
            }

        }
        
        //function to parse test request
        public static void ParseTestRequest(string TestRequest)
        {
            loadXML(System.IO.Path.Combine(("../../../TestHarnessStore/TestRequests"), TestRequest));
            try
            {
                parse("testFile");
            }
            catch(Exception ex)
            {
                ex.ToString();
            }
        }
        //function to load test request 
        public static bool loadXML(string path)
        {
            try
            {
                doc = XDocument.Load(path);
                Console.Write("\n\n===================================================================================\n\n");
                Console.Write("\n\n===================================================================================\n\n");
                return true;
            }
            catch (Exception ex)
            {
                ex.ToString();
                return false;
            }

        }


        //function to parse test request
        public static string parse(string propertyName)
        {
            Console.Write("in parse function ");
            string parseStr = doc.Descendants(propertyName).First().Value;
            if (parseStr.Length > 0)
            {
                switch (propertyName)
                {
                    case "testFile":
                        testFile = parseStr;
                        break;
                    default:
                        break;
                }
                return parseStr;
            }
            return "";
        }
    }
}
