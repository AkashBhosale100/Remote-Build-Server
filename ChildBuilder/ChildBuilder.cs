/*ChildBuilder.cs-----Builds and sends logs to the repository
 * Author: ------ Akash Bhosale, aabhosal@syr.edu
  Source: Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2017 
  Course:CSE 681 Software Modelling and Analysis
  Environment:C# Console
  Application: CSE 681 Project 4- ChildBuilder.cs
*--------------------------------------------------------------------------------------------------------*/
/*
 * Added references to:
 * - MessagePassingComm
 *
 * This package provides:
 * ----------------------
 * Accepting files from repository depending on the test/s mentioned in the build request
 * Building files (creating .dll's/.exe's) send by the repository on command by client
 * Sending build to the test harness
 * Sending build logs to the repository
 * 
 * Interface
 * -----------------------
 * 1.public static void InitializeSender(List<string> commandlineArgs)                                       ->Initialize sender of the child process to send msg's to the mother builder and repository.
 * 2.public static void InitializeReceiver()                                                                 ->function to initialize receiver of child builder.
 * 3.public static void getMessage()                                                                          ->thread function to keep dequeing messages from the receive queue
 * 4.public static void requestFilesFromRepo(CommMessage msg)                                                 ->function to request files from repository
 * 5.public static void connectRepo(CommMessage msg)                                                          ->function to connect to repository
 * 6.public static void startBuild(CommMessage msg, string buildPath, int index)                              ->code to select appropriate toolchain for build process
 * 7.public static void executeBuild(string compiler, string buildPath, int index)                            ->function to execute build process depending on the toolchain
 * 8.public static void cSharpCompiler(string compiler, string buildPath, int index)                          ->function to carry out compilation using csc compiler
 * 9.public static void generateCSharpLogs(Process BuildProcess, int exitCode, string buildPath, int index)    ->function to generate build Logs
 * 10.public static void cSharpCompilerHelper(string buildPath)                                               ->helper function for the CSharp compilation operation
 * 11.public static void cscSetParameters(Process BuildProcess, string buildPath, int index)                  ->function to set necessary parameters to launch csc process
 * 12.public static void MsgtoMotherBuilder()                                                                 ->function to send message to Mother Builder after build process is over
 * 13.public static void SendMsgToRepo(int index)                                                             ->function to send message to repo to accept build Logs
 * 14.public static void errorCalculator(string buildPath, int index)                                         ->function to calculate the number of errors generated
 * 15.public static void SendAckMsgToRepo(CommMessage rcvMsg)                                                 ->function to send acknowledge message to the repository
 * 16.public static void SendBuildLogsToRepo(CommMessage rcvMsg)                                              ->function to send Build Logs to the repository
 * 17.public static void testHarnessHandler(string buildOutput)                                               ->function to handle testHarness operations
 * 18.public static void sendAckMsgToTh(CommMessage rcvMsg)                                                   ->send acknowledgement msg to test harness 
 * 19.public static void sendBuildToTh(CommMessage rcvMsg)                                                    ->function to send build to test harness
 * 20.private static void InitialSetup()                                                                      ->initial setup function
 * 21.public static void generateCPlusPlusLogs(Process BuildProcess,int exitCode,string buildPath)            ->function to generate Cplusplus logs 
 * 22.public static void cPlusPlusSetParameters(Process BuildProcess,string buildPath,int index)              ->funtion to set parameters for cplusplus compiling
 * 23.public static void cPlusPlusCompilerHelper(string buildPath)                                            ->cPlusPlusCompilerHelper
 * 24.public static bool saveXml()                                                                            ->function to save Xml
 * 25.public static bool loadXml(string path)                                                                 ->function to load Xml
 * 26.public static void CreateTestRequest()                                                                   ->function to create new test request
 
 * Required Files
 * * ---------------
 * IMPCommService.cs         : Service interface 
 *  MPCommService.cs         : Message definition
 *  MotherBuilder.cs         : Child Builder manager
 *  
 * Maintenance History:
 * --------------------
 * * ver 1.0 : 27 October 2017
 * - first release
 * 
 *  ver 2.0 6th December 2017
 *      -second release
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using MessagePassingComm;
using System.Threading;
using System.Windows;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Repo;
using TestRequest;
namespace ChildProc
{
    class ChildProc
    {
        private static int pId;
        private static int portNumber;
        private static string baseAddress;
        private static Sender sndr;
        private static Receiver rcvr;
        private static CommMessage rcvMsg;
        private static string toAddress;
        private static string fromAddress;
        private static List<string> commandLineArgs = new List<string>();
        private static List<string> Files = new List<string>();
        private static readonly object ConsoleWriterLock = new object();
        private static string chldPath = "";
        private static string testFilesPath = "";
        private static string storePath = "";
        private static List<string> buildFiles = new List<string>();
        private static string buildRequest = "";
        private static string buildLogsPath = "";
        private static System.IO.StreamWriter log;
        private static XDocument doc;
        private static string buildOutput = "";
        private static string buildRequestName = "";
        private static int index { get; set; } = -1;
        private static string buildName = "";
        static void Main(string[] args)
        {
            InitialSetup();

            foreach (string command in args)
            {
                commandLineArgs.Add(command);
            }
            pId = Int32.Parse(commandLineArgs[0]);
            chldPath = System.IO.Path.GetFullPath("../../../BuilderStore/BuilderReceiveStore/" + "Builder" + pId);
            System.IO.Directory.CreateDirectory(chldPath);
            Console.Title = "ChildProc" + args[0];
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write("\n  Demo Child Process");
            Console.Write("\n ====================\n");
            if (args.Count() == 0)
            {
                Console.Write("\n  please enter integer value on command line");
                return;
            }
            else
            {
                foreach (string command in args)
                {
                    commandLineArgs.Add(command);
                }
                InitializeSender(commandLineArgs);
                Thread cBuilderMsgQ = new Thread(new ThreadStart(getMessage));
                Console.Write("\nStarting getMessage thread to check incoming messages in Child Builder");
                Console.Write("\n=========================================================\n");
                cBuilderMsgQ.Start();
            }
            Console.Read();
            Console.Write("\n  Press key to exit");
            Console.ReadKey();
            Console.Write("\n  ");
        }

        //Initialize sender of the child process to send msg's to the mother builder and repository.
        public static void InitializeSender(List<string> commandlineArgs)
        {

            pId = Int32.Parse(commandlineArgs[0]);
            portNumber = Int32.Parse(commandlineArgs[2]);
            baseAddress = commandlineArgs[1];
            Console.Write("\n  Hello from child #{0}\n\n", pId);
            toAddress = baseAddress + ":" + portNumber + "/IpluggableComm";
            fromAddress = baseAddress + ":" + (portNumber + pId) + "/IpluggableComm";
            sndr = new Sender(baseAddress, portNumber);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "ready";
            sndMsg.author = "Akash Bhosale";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            sndMsg.arguments.Add(pId.ToString());
            sndr.postMessage(sndMsg);
            sndMsg.show();
        }
        //Initialize receiver of child process
        public static void InitializeReceiver()
        {
            rcvr = new Receiver();
            rcvr.start(baseAddress, (portNumber + pId));
        }
        //thread function to keep dequeing messages from the receive queue
        public static void getMessage()
        {
            InitializeReceiver();
            while (true)
            {
                rcvMsg = rcvr.getMessage();
                lock (ConsoleWriterLock)
                {
                    rcvMsg.show();
                }
                if (rcvMsg.type == CommMessage.MessageType.request)
                {
                    if (rcvMsg.command == "processBuildRequest")
                        connectRepo(rcvMsg);
                    else if (rcvMsg.command == "connectedToRepo")
                    {
                        requestFilesFromRepo(rcvMsg);
                    }
                    else if (rcvMsg.command == "fileSent")
                    {
                        lock (ConsoleWriterLock)
                        {
                            Console.Write("Files Received");
                            Console.Write("\n\nFiles Received in: {0} ", System.IO.Path.GetFullPath(System.IO.Path.Combine(("../../../BuilderStore/BuilderReceiveStore/Builder" + pId), rcvMsg.arguments[0])));
                            AcceptFiles(rcvMsg);
                        }
                    }
                    else if (rcvMsg.command == "ReadyToReceiveLogs")
                        SendAckMsgToRepo(rcvMsg);
                    else if (rcvMsg.command == "SendBuildLogs")
                        SendBuildLogsToRepo(rcvMsg);
                    else if (rcvMsg.command == "SendBuild")
                        sendBuildToTh(rcvMsg);
                    else if (rcvMsg.command == "ReadyToReceiveTestRequest")
                        SendAckMsgTrMsgtoRepo(rcvMsg);
                    else if (rcvMsg.command == "SendTestRequest")
                        sendTestRequest(rcvMsg);
                }
                if (rcvMsg.command == "quit")
                {
                    Console.Write("Quitting......");
                    Thread.Sleep(1000);
                    Process.GetCurrentProcess().Kill();
                }
            }
        }
        //function to request files from repository
        public static void requestFilesFromRepo(CommMessage msg)
        {
            lock (ConsoleWriterLock)
            {
                Console.Write("Requesting files from Repo");
            }
            toAddress = baseAddress + ":" + "8078" + "/IpluggableComm";
            fromAddress = baseAddress + ":" + (portNumber + pId) + "/IpluggableComm";
            sndr = new Sender(baseAddress, 8078);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "sendFiles";
            sndMsg.author = "Akash Bhosale";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            foreach (string file in Files)
            {
                sndMsg.arguments.Add(file);
            }
            Files.Clear();
            sndMsg.arguments.Add(pId.ToString());
            sndr.postMessage(sndMsg);
            sndMsg.show();
        }
        //function to connect to repository
        public static void connectRepo(CommMessage msg)
        {
            toAddress = baseAddress + ":" + "8078" + "/IpluggableComm";
            fromAddress = baseAddress + ":" + (portNumber + pId) + "/IpluggableComm";
            sndr = new Sender(baseAddress, 8078);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "connectRepo";
            sndMsg.author = "Akash Bhosale";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            foreach (string file in msg.arguments)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(chldPath, System.IO.Path.GetFileNameWithoutExtension(file)));
                Files.Add(file);
                sndMsg.arguments.Add(file);
            }
            sndMsg.arguments.Add(pId.ToString());
            sndr.postMessage(sndMsg);
            sndMsg.show();
        }

        //function to accept received files from repository
        public static void AcceptFiles(CommMessage rcvMsg)
        {
            index = -1;
            testFilesPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(("../../../BuilderStore/BuilderReceiveStore/Builder" + pId), rcvMsg.arguments[0]));
            string xml = null;
            if (rcvMsg.arguments.Count() > 0)
            {
                xml = rcvMsg.arguments[0];
                if (xml != null)
                {
                    string buildPath = "../../../BuilderStore/Builds";
                    string xmlFilePath = System.IO.Path.Combine(testFilesPath, xml + ".xml");
                    List<string[]> tests = ParseBuildRequest(xmlFilePath, testFilesPath);
                    foreach (string[] test in tests)
                    {
                        foreach (string file in test)
                        {
                            System.IO.File.Copy(file, System.IO.Path.Combine(buildPath, System.IO.Path.GetFileName(file)), true);
                        }
                        index++;
                        startBuild(rcvMsg, buildPath, index);
                        cleanPath(buildPath);
                    }
                }
            }
        }

        //Function to parse build request content
        public static List<string[]> ParseBuildRequest(string path_of_xml, string savePath)
        {
            
            TestRequestUtil obj = new TestRequestUtil();
            Console.WriteLine(path_of_xml);
            loadXml(path_of_xml);
            var items = doc.Descendants("test");
            List<string[]> finallist = new List<string[]>();
            List<string> arr = new List<string>();
            string[] one;
            foreach (var item in items)
            {
                if (item.Descendants("testDriver").First().Value != null)
                {
                    try
                    {
                        var childs = item.Descendants("testDriver").First().Value;
                        childs = System.IO.Path.Combine(savePath, childs);
                        childs = System.IO.Path.GetFullPath(childs);
                        Console.WriteLine();
                        arr.Add(childs);
                        IEnumerable<XElement> parseElems = item.Descendants("tested");
                        if (parseElems.Count() > 0)
                        {
                            foreach (XElement elem in parseElems)
                            {
                                var x = System.IO.Path.Combine(savePath, elem.Value);
                                x = System.IO.Path.GetFullPath(x);
                                arr.Add(x);
                            }
                        }
                        one = arr.ToArray();
                        foreach (var x in one)
                        {
                        }
                        finallist.Add(one);
                        arr.Clear();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Invalid Test Request", e.Message);
                    }
                }
            }
            return finallist;
        }

        //function to handle build process
        public static void startBuild(CommMessage msg, string buildPath, int index)
        {
            String compiler = "abc";
            string fileType = "";
            var files = System.IO.Directory.GetFiles(buildPath).Where(name => !name.EndsWith(".xml"));
            buildRequest = System.IO.Path.GetFileNameWithoutExtension(msg.arguments[0]);
            buildRequestName = buildRequest;
            foreach (var file in files)
            {
                buildFiles.Add(file);
            }

            fileType = System.IO.Path.GetExtension(buildFiles[0]);

            if (fileType == ".cs")
            {
                Console.Write("\nInitiating process for Csharp compiler \n");
                compiler = "CSharp";
            }
            else if (fileType == ".cpp")
            {
                Console.Write("\nInitiating process for CPlusPlus compiler \n");
                compiler = "CPlusPlus";
                Console.Write("Compiler is {0}", compiler);
            }
            executeBuild(compiler, buildPath, index);
        }

        //function to execute build process
        public static void executeBuild(string compiler, string buildPath, int index)
        {
            
            storePath = System.IO.Path.Combine(("../../../BuilderStore/BuilderSendStore"));
            buildLogsPath = "../../../BuilderStore/BuilderSendStore";
            Console.Write("\n\nStarting build process.......... \n");
            Console.Write("\nCompiling........\n\n");
            
            if (compiler == "CSharp")
            {
                Console.Write("\nCalling CSharp compiler");

                cSharpCompiler(compiler, buildPath, index);
            }
            else if (compiler == "CPlusPlus")
            {

                Console.Write("\nCalling CPlusPLus compiler");
                Console.Write("\nCreating build using supplied files");
                cPlusPlusCompiler(compiler, buildPath, index);
            }
        }
        /*CSharp compiler function.
         * Compiles code using csc compiler
         */
        public static void cSharpCompiler(string compiler, string buildPath, int index)
        {
            AutoResetEvent outputWaitHandle; AutoResetEvent errorWaitHandle;
            compiler = "";
            int exitCode = -1;
            cSharpCompilerHelper(buildPath);
            using (Process BuildProcess = new Process())
            {
                StringBuilder data = new StringBuilder();
                StringBuilder error = new StringBuilder();
                cscSetParameters(BuildProcess, buildPath, index);
                using (outputWaitHandle = new AutoResetEvent(false))
                using (errorWaitHandle = new AutoResetEvent(false))
                {
                    BuildProcess.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                            try
                            { outputWaitHandle.Set(); }
                            catch (Exception ex)
                            { ex.ToString(); }
                        else
                            data.AppendLine(e.Data);
                    };
                    BuildProcess.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                            try
                            { errorWaitHandle.Set(); }
                            catch (Exception ex)
                            { ex.ToString(); }
                        else
                            error.AppendLine(e.Data);
                    };
                    BuildProcess.Start();
                    BuildProcess.BeginOutputReadLine();
                    BuildProcess.BeginErrorReadLine();
                    if (BuildProcess.WaitForExit(1000) && outputWaitHandle.WaitOne(1000) && errorWaitHandle.WaitOne(1000))
                        exitCode = BuildProcess.ExitCode;
                    generateCSharpLogs(BuildProcess, exitCode, buildPath, index);
                    using (log = new System.IO.StreamWriter(System.IO.Path.Combine(buildLogsPath, "BuildLog_" + buildRequest + index + ".txt"), true))
                    {
                        log.WriteLine(data.ToString() + Environment.NewLine);
                        log.WriteLine(error.ToString() + Environment.NewLine);
                        outputWaitHandle = null; errorWaitHandle = null;
                        log.Close();
                        errorCalculator(buildPath, index);
                    }
                }
            }
        }

        //function to generate logs 
        public static void generateCSharpLogs(Process BuildProcess, int exitCode, string buildPath, int index)
        {
            List<string> logOutput = new List<string>();
            string[] buildFiles = System.IO.Directory.GetFiles(buildPath, "*.cs*");
            using (log = new System.IO.StreamWriter(System.IO.Path.Combine(buildLogsPath, "BuildLog_" + buildRequest + index + ".txt")))
            {
                log.Write("Build Started at: " + DateTime.Now.ToString() + Environment.NewLine);
                foreach (System.Collections.DictionaryEntry env in Environment.GetEnvironmentVariables())
                {
                    log.WriteLine((string)env.Key + "=" + (string)env.Value);
                }
                log.WriteLine(Environment.NewLine + "Compiling:" + Environment.NewLine);
                foreach (string file in buildFiles)
                {
                    log.WriteLine(System.IO.Path.GetFileName(file) + Environment.NewLine);
                }
                if (exitCode == 0)
                {
                    Console.Write("\n\n===============================================================================================");
                    Console.Write("\nBuild Succeeded.");
                    Console.Write("\n\n===============================================================================================");
                    log.WriteLine("\nBuild Succeeded.");
                    Console.Write("\nCheck log file generatead at {0}", System.IO.Path.GetFullPath((System.IO.Path.Combine(buildLogsPath, buildRequest + ".txt"))), "for more details");
                    Console.Write("\nBuild output generated at {0}", System.IO.Path.GetFullPath(storePath));
                    exitCode = -1;
                }
                else if (exitCode == 1)
                {
                    Console.Write("\n\n===============================================================================================");
                    Console.Write("\nBuild Failed.");
                    Console.Write("\n\n===============================================================================================");
                    log.WriteLine("\n\nBuild Failed.");
                    Console.Write("\nCheck log file generated at {0} for more details", System.IO.Path.GetFullPath((System.IO.Path.Combine(buildLogsPath, buildRequest + ".txt"))));
                    exitCode = -1;
                }
            }
            log.Close();
        }

        //cSharpCompiler helper function
        public static void cSharpCompilerHelper(string buildPath)
        {
            string[] files = System.IO.Directory.GetFiles(buildPath, "*.cs*", System.IO.SearchOption.AllDirectories);
            foreach (string file in files)
            {
                Console.Write(System.IO.Path.GetFileName(file));
                Console.Write("\n");
            }
        }

        //cscParameterSet
        public static void cscSetParameters(Process BuildProcess, string buildPath, int index)
        {
            
            string datetime = DateTime.Now.ToString();
            var frameworkPath = RuntimeEnvironment.GetRuntimeDirectory();
            var cscPath = System.IO.Path.Combine(frameworkPath, "csc.exe");
            BuildProcess.StartInfo.CreateNoWindow = false;
            BuildProcess.StartInfo.UseShellExecute = false;
            BuildProcess.StartInfo.FileName = cscPath;
            BuildProcess.StartInfo.WorkingDirectory = System.IO.Path.Combine(buildPath);
            BuildProcess.StartInfo.Arguments = "/target:" + "library" + " /out:" + (buildRequest + index + ".dll") + " /pdb:false  /debug *.cs";
            BuildProcess.StartInfo.RedirectStandardOutput = true;
            BuildProcess.StartInfo.RedirectStandardError = true;
        }

        //send msg to mother builder after build process is over
        public static void MsgtoMotherBuilder()
        {
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "ready";
            sndMsg.author = "Akash Bhosale";
            toAddress = baseAddress + ":" + "8080" + "/IpluggableComm";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            sndMsg.arguments.Add(pId.ToString());
            sndMsg.show();
            sndr.postMessage(sndMsg);
        }

        //send msg to repo
        public static void SendMsgToRepo(int index)
        {
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "GetReadyToReceiveBuildLogs";
            sndMsg.author = "Akash Bhosale";
            toAddress = baseAddress + ":" + "8078" + "/IpluggableComm";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            sndMsg.arguments.Add("BuildLog_" + buildRequest + index);
            sndMsg.arguments.Add(pId.ToString());
            sndr.postMessage(sndMsg);
        }

        //function to find the number of errors generated in the build log
        public static void errorCalculator(string buildPath, int index)
        {
            int errorCount = 0;
            errorCount = System.IO.File.ReadLines(System.IO.Path.Combine(buildLogsPath, "BuildLog_" + buildRequest + index + ".txt")).Select(line => Regex.Matches(line, "error").Count).Sum();
            using (log = new System.IO.StreamWriter(System.IO.Path.Combine(buildLogsPath, "BuildLog_" + buildRequest + index + ".txt"), true))
            {
                log.WriteLine("Errors: {0}", errorCount);
            }
            errorCount = 0;
            log.Close();

            if (System.IO.File.Exists(System.IO.Path.Combine(buildPath, "BuildLog_" + buildRequest + index + ".txt")))
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(ClientEnvironment.fileStorage, "BuildLog_" + buildRequest + index + ".txt")))
                {
                    System.IO.File.Delete(System.IO.Path.Combine(ClientEnvironment.fileStorage, "BuildLog_" + buildRequest + index + ".txt"));
                }
                System.IO.File.Move(System.IO.Path.Combine(buildPath, "BuildLog_" + buildRequest + index + ".txt"), System.IO.Path.Combine(ClientEnvironment.fileStorage, "BuildLog_" + buildRequest + index + ".txt"));
            }

            if (System.IO.File.Exists(System.IO.Path.Combine(buildPath, buildRequest + index + ".dll")))
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(ClientEnvironment.fileStorage, buildRequest + index + ".dll")))
                {
                    System.IO.File.Delete(System.IO.Path.Combine(ClientEnvironment.fileStorage, buildRequest + index + ".dll"));
                }
                System.IO.File.Move(System.IO.Path.Combine(buildPath, buildRequest + index + ".dll"), System.IO.Path.Combine(ClientEnvironment.fileStorage, buildRequest + index + ".dll"));
            }
            buildName = buildRequest + index + ".dll";
            SendMsgToRepo(index);
        }

        //function to send acknowledgement message to repo
        public static void SendAckMsgToRepo(CommMessage rcvMsg)
        {
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "AcknowledgementAccepted";
            sndMsg.author = "Akash Bhosale";
            toAddress = baseAddress + ":" + "8078" + "/IpluggableComm";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            sndMsg.arguments.Add(rcvMsg.arguments[0]);
            sndMsg.arguments.Add(pId.ToString());
            sndr.postMessage(sndMsg);
        }

        //function to send Build logs to the repository
        public static void SendBuildLogsToRepo(CommMessage rcvMsg)
        {
            Console.Write("\n\n===================================================================================\n\n");
            Console.Write("Sending build logs from {0}", System.IO.Path.GetFullPath(ClientEnvironment.fileStorage));
            Console.Write("\n\n===================================================================================\n\n");
            buildRequest = rcvMsg.arguments[0];
            Console.Write("Sending file {0}", rcvMsg.arguments[0] + ".txt");
            Thread.Sleep(5000);
            sndr.postFile(buildRequest + ".txt", pId, buildRequest);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "BuildLogsSent";
            sndMsg.author = "Akash Bhosale";
            toAddress = baseAddress + ":" + "8078" + "/IpluggableComm";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            sndMsg.arguments.Add(rcvMsg.arguments[0]);
            sndMsg.arguments.Add(pId.ToString());
            sndr.postMessage(sndMsg);
            buildOutput = rcvMsg.arguments[0];
            testHarnessHandler(buildOutput);
        }

        //function to handle testHarness operations
        public static void testHarnessHandler(string buildOutput)
        {
            CreateSendTestRequest();
        }


        //function to send acknowledge msg to test harness
        public static void SendAckMsgToTh(CommMessage rcvMsg)
        {
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "AcknowledgementAccepted";
            sndMsg.author = "Akash Bhosale";
            toAddress = baseAddress + ":" + "8077" + "/IpluggableComm";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            sndMsg.arguments.Add(rcvMsg.arguments[0]);
            sndMsg.arguments.Add(pId.ToString());
            sndr.postMessage(sndMsg);
        }

        //function to send build to test harness
        public static void sendBuildToTh(CommMessage rcvMsg)
        {
            string[] buildFiles = System.IO.Directory.GetFiles("../../../BuilderStore/BuilderSendStore", "*.dll");

            foreach (string buildFile in buildFiles)
            {
                buildName = System.IO.Path.GetFileName(buildFile);
                Console.Write("\n\n===================================================================================\n\n");
                Console.Write("Sending build {0}", buildName);
                Console.Write("\n\n===================================================================================\n\n");
                sndr.postFile(buildName, pId, buildRequest);
            }
            Thread.Sleep(3000);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "BuildSent";
            sndMsg.author = "Akash Bhosale";
            toAddress = baseAddress + ":" + "8077" + "/IpluggableComm";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            sndMsg.arguments.Add(buildName);
            sndr.postMessage(sndMsg);
            MsgtoMotherBuilder();

        }
        //function to create and send test request to test harness
        public static void CreateSendTestRequest()
        {
            CreateTestRequest();
        }

        //function to create test request
        public static void CreateTestRequest()
        {
            doc = new XDocument();
            XElement testRequestElem = new XElement("testRequest");
            doc.Add(testRequestElem);

            XElement TestElem = new XElement("Test");
            TestElem.Add("Test");
            testRequestElem.Add(TestElem);

            XElement authorElem = new XElement("author");
            authorElem.Add("Akash Bhosale");
            testRequestElem.Add(authorElem);

            XElement dateTimeElem = new XElement("dateTime");
            dateTimeElem.Add(DateTime.Now.ToString());
            testRequestElem.Add(dateTimeElem);

            XElement testElem = new XElement("test");
            testRequestElem.Add(testElem);

            XElement testFileElem = new XElement("testFile");
            testFileElem.Add(buildName);
            testElem.Add(testFileElem);
            saveXml();
        }

        //function to saveXml
        public static bool saveXml()
        {
            string fileName = "TestRequest" + pId + DateTime.Now.ToString("HHmmss") + ".xml";
            string savePath = System.IO.Path.Combine(ClientEnvironment.fileStorage, fileName);
            try
            {
                doc.Save(savePath);
                sendTestRequestMsgToTh(fileName);
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--\n", ex.Message);
                return false;
            }
        }

        //function to send test request msg to test harness
        public static void sendTestRequestMsgToTh(string TestRequest)
        {
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "GetReadyToReceiveTestRequest";
            sndMsg.author = "Akash Bhosale";
            toAddress = baseAddress + ":" + "8077" + "/IpluggableComm";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            sndMsg.arguments.Add(TestRequest);
            sndMsg.arguments.Add(pId.ToString());
            sndr.postMessage(sndMsg);
        }
        //function to accept acknowledgement message
        public static void SendAckMsgTrMsgtoRepo(CommMessage rcvMsg)
        {
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "AcknowledgementAcceptedForTh";
            sndMsg.author = "Akash Bhosale";
            toAddress = baseAddress + ":" + "8077" + "/IpluggableComm";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            sndMsg.arguments.Add(rcvMsg.arguments[0]);
            sndMsg.arguments.Add(pId.ToString());
            sndr.postMessage(sndMsg);
        }

        //function to send test request
        public static void sendTestRequest(CommMessage rcvMsg)
        {
            
            string testRequest = rcvMsg.arguments[0];
            Console.Write("\n\n===================================================================================\n\n");
            Console.Write("Sending test request generated : {0}", testRequest);
            Console.Write("\n\n===================================================================================\n\n");
            sndr.postFile(testRequest, pId, buildRequest);
            Thread.Sleep(3000);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "TestRequestSent";
            sndMsg.author = "Akash Bhosale";
            toAddress = baseAddress + ":" + "8077" + "/IpluggableComm";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            sndMsg.arguments.Add(testRequest);
            sndMsg.arguments.Add(pId.ToString());
            sndr.postMessage(sndMsg);
            sendMsgtoTh();

        }
        //function to send message to test harness
        public static void sendMsgtoTh()
        {
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "GetReadyToReceiveBuild";
            sndMsg.author = "Akash Bhosale";
            toAddress = baseAddress + ":" + "8077" + "/IpluggableComm";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            sndMsg.arguments.Add(rcvMsg.arguments[0]);
            sndMsg.arguments.Add(pId.ToString());
            sndr.postMessage(sndMsg);

        }

        /*----< load TestRequest from XML file >-----------------------*/

        public static bool loadXml(string path)
        {
            
            try
            {
                doc = XDocument.Load(path);
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--\n", ex.Message);
                return false;
            }
        }

        //initial setup function
        private static void InitialSetup()
        {
            string ClientfileStorage = "../../../BuilderStore/BuilderSendStore";
            string ServicefileStorage = "../../../BuilderStore/BuilderReceiveStore";
            ClientEnvironment.fileStorage = ClientfileStorage;
            ServiceEnvironment.fileStorage = ServicefileStorage;
        }

        //function to clean build path
        public static void cleanPath(String buildPath)
        {
            
            
            foreach (string file in System.IO.Directory.GetFiles(buildPath))
            {
                try
                {
                    System.IO.File.Delete(file);
                }
                catch (Exception ex)
                { ex.ToString(); }
            }

        }

        //CPlusPlusCompiler code
        public static void cPlusPlusCompiler(string compiler, string buildPath, int index)
        {
            compiler = "";
            int exitCode = -1;
            cPlusPlusCompilerHelper(buildPath);
            using (Process BuildProcess = new Process())
            {
                StringBuilder data = new StringBuilder();
                StringBuilder error = new StringBuilder();
                cPlusPlusSetParameters(BuildProcess,buildPath,index);
                using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                {
                    BuildProcess.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            outputWaitHandle.Set();
                        }
                        else
                        {
                            data.AppendLine(e.Data);
                        }
                    };
                    BuildProcess.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                            errorWaitHandle.Set();
                        else
                            error.AppendLine(e.Data);
                    };
                    Console.Write("\n Starting cl.exe");
                    BuildProcess.Start();
                    BuildProcess.BeginOutputReadLine();
                    BuildProcess.BeginErrorReadLine();
                    if (BuildProcess.WaitForExit(1000) && outputWaitHandle.WaitOne(1000) && errorWaitHandle.WaitOne(1000))
                    {
                        exitCode = BuildProcess.ExitCode;
                    }
                    generateCPlusPlusLogs(BuildProcess, exitCode,buildPath);
                    using (log = new System.IO.StreamWriter(System.IO.Path.Combine(buildLogsPath, buildRequest + ".txt"), true))
                    {
                        log.WriteLine(data.ToString() + Environment.NewLine);
                        log.WriteLine(error.ToString() + Environment.NewLine);
                    }
                    Thread.Sleep(10000);
                    MsgtoMotherBuilder();
                }
            }
        }


        //cPlusPlusCompilerHelper
        public static void cPlusPlusCompilerHelper(string buildPath)
        {
            string[] files = System.IO.Directory.GetFiles(buildPath, "*.cpp*");
            foreach (string file in files)
            {
                Console.Write(System.IO.Path.GetFileName(file));
                Console.Write("\n");
            }
        }
          //funtion to set parameters for cplusplus compiling
        public static void cPlusPlusSetParameters(Process BuildProcess,string buildPath,int index)
        {
            var framework = "";
            String[] cPlusPlusPath;
            if (8 == IntPtr.Size || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                framework = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }
            else
            {
                framework = Environment.GetEnvironmentVariable("ProgramFiles");
            }

            var batFilePath = System.IO.Directory.GetFiles(framework, "cl.exe");

            Process BuildProcess1 = new Process();
            BuildProcess1.StartInfo.CreateNoWindow = false;
            BuildProcess1.StartInfo.UseShellExecute = false;
            BuildProcess1.StartInfo.WorkingDirectory = System.IO.Path.Combine(buildPath);
               
            cPlusPlusPath = System.IO.Directory.GetFiles(framework, "cl.exe", System.IO.SearchOption.AllDirectories);

            BuildProcess.StartInfo.CreateNoWindow = false;
            BuildProcess.StartInfo.EnvironmentVariables.Add("tempPath",framework);

            BuildProcess.StartInfo.CreateNoWindow = false;
            BuildProcess.StartInfo.UseShellExecute = false;
            BuildProcess.StartInfo.FileName = cPlusPlusPath[0];
            BuildProcess.StartInfo.WorkingDirectory = buildPath;
            BuildProcess.StartInfo.Arguments = "/LDd" + " *.cpp* " + " /link " + "/LIBPATH:" + @"""C:/Program Files (x86)/*.lib*""" + " /implib:CodeToTest1.dll " + " /out:test1.dll";
            BuildProcess.StartInfo.RedirectStandardOutput = true;
            BuildProcess.StartInfo.RedirectStandardError = true;
            Console.Write(BuildProcess.StartInfo.FileName);
            Console.Write("\n");
            Console.Write(BuildProcess.StartInfo.Arguments);
            
        }

        //function to generate Cplusplus logs 
        public static void generateCPlusPlusLogs(Process BuildProcess,int exitCode,string buildPath)
        {

            List<string> logOutput = new List<string>();
            string[] buildFiles = System.IO.Directory.GetFiles(buildPath, "*.cs*");
            using (log = new System.IO.StreamWriter(System.IO.Path.Combine(buildLogsPath, "BuildLog_" + buildRequest + index + ".txt")))
            {
                log.Write("Build Started at: " + DateTime.Now.ToString() + Environment.NewLine);
                foreach (System.Collections.DictionaryEntry env in Environment.GetEnvironmentVariables())
                {
                    log.WriteLine((string)env.Key + "=" + (string)env.Value);
                }
                log.WriteLine(Environment.NewLine + "Compiling:" + Environment.NewLine);
                foreach (string file in buildFiles)
                {
                    log.WriteLine(System.IO.Path.GetFileName(file) + Environment.NewLine);
                }
                if (exitCode == 0)
                {
                    Console.Write("\n\n===============================================================================================");
                    Console.Write("\nBuild Succeeded.");
                    Console.Write("\n\n===============================================================================================");
                    log.WriteLine("\nBuild Succeeded.");
                    Console.Write("\nCheck log file generatead at {0}", System.IO.Path.GetFullPath((System.IO.Path.Combine(buildLogsPath, buildRequest + ".txt"))), "for more details");
                    Console.Write("\nBuild output generated at {0}", System.IO.Path.GetFullPath(storePath));
                    exitCode = -1;
                }
                else if (exitCode == 1)
                {
                    Console.Write("\n\n===============================================================================================");
                    Console.Write("\nBuild Failed.");
                    Console.Write("\n\n===============================================================================================");
                    log.WriteLine("\n\nBuild Failed.");
                    Console.Write("\nCheck log file generated at {0} for more details", System.IO.Path.GetFullPath((System.IO.Path.Combine(buildLogsPath, buildRequest + ".txt"))));

                    exitCode = -1;
                }
            }
            log.Close();
            
        }


    }
}

