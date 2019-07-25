/*Repository.cs-----Handles sending files and receiving files including sending of build request
 * Author: ------ Akash Bhosale,aabhosal@syr.edu
  Source: Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2017 
  Course:CSE 681 Software Modelling and Analysis
  Environment: C# Console
*--------------------------------------------------------------------------------------------------------*/
/*
 * Added references to:
 * -MessagePassingComm
 *- MPMessagePassingCommService
 * This package provides:
 * ----------------------
 * Functionality of sending files stored in the repository to the child process and receiving build logs 
 * from the builder and receiving test logs from the test harness as and when the repository receives message from the child process.
 *  
 *  Interfaces:
 *  ----------------------
 *  1.public static void InitializeReceiver()                                                 ->function to initialize receiver of repository
 *  2.public static void ParseBuildRequest(string fileName)                                   ->function to parse requested build request  
 *  3.public static bool loadXML(string path)                                                 ->function to load build request
 *  4.public static List<string> parseList(string propertyName)                               ->helper function to parse created build requests
 *  5.public static void connectChildBuilder(string i,string j,MessagePassingComm.Sender[] x) ->function to connect to childBuilder 
 *  6.public static void sendRequestedFile(string i, string j, MessagePassingComm.Sender[] x) ->function to send Requested files
 *  7.public static void AcceptBuildLogs()                                                     ->function to carry out some necessary activities such as copying received files in its appropriate 
 *                                                                                               build log store folder
 *  8.public static void SendBuildLogsToBuilder(CommMessage rcvMsg, Sender[] cs)               ->function to send message to child builder acknowledging receive request of build logs from builder
 *  9.public static void SendFiles(CommMessage rcvMsg, Sender[] cs)                            ->function to send files to builder
 *  10. public static void repoSetup()                                                         ->performs initial operations such as creating folder for build logs and
 *                                                                                               setting client environment and service environment paths

 * Required Files:
 * ---------------
 * IMPCommService.cs         : Service interface and Message definition
 * MPCommService.cs 
 * Maintenance History:
 * --------------------
 *  * ver 2.0 : 6 December 2017
 * - second release
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePassingComm;
using System.Threading;
using System.Xml.Linq;
using System.Diagnostics;

namespace Repo
{
    public class Repository
    {
        private static XDocument doc;
        private static List<string> testedFiles = new List<string>();
        private static List<string> driverFiles = new List<string>();
        private static List<string> sendFileNames = new List<string>();
        private static Receiver rcvr;
        private static Sender sndr;
        private static string baseAddress = "http://localhost";
        private static int port = 8078;
        private static List<string> BuildRequests = new List<string>();
        private static readonly object ConsoleWriterLock = new object();
        private static string buildLogsPath = "../../../Repository/BuildLogs";
        private static string testLogsPath = "../../../Repository/TestLogs";
        private static string testFilesPath = "../../../Repository/TestFiles";
        private static string testDriversPath = "../../../Repository/TestDrivers";


        static void Main(string[] args)
        {
            repoSetup();
            if (args.Count() != 0)
            {
                CommMessage rcvMsg;
                Sender[] cs = new Sender[Int32.Parse(args[0]) + 100];
                for (int i = 0; i <= Int32.Parse(args[0]); i++)
                {
                    cs[i] = new Sender("http://localhost", 8081 + i);
                }
                InitializeReceiver();
                while (true)
                {
                    rcvMsg = rcvr.getMessage();
                    rcvMsg.show();
                    if (rcvMsg.command == "connectRepo")
                    {
                        connectRepo(rcvMsg, cs);
                    }
                    else if (rcvMsg.command == "sendFiles")
                        SendFiles(rcvMsg, cs);
                    else if (rcvMsg.command == "quit")
                    {
                        Thread.Sleep(1000);
                        Process.GetCurrentProcess().Kill();
                    }
                    else if (rcvMsg.command == "GetReadyToReceiveBuildLogs")
                        SendMsgToChildBuilder(rcvMsg, cs);
                    else if (rcvMsg.command == "AcknowledgementAccepted")
                        SendBuildLogsToBuilder(rcvMsg, cs);
                    else if (rcvMsg.command == "BuildLogsSent")
                        AcceptBuildLogs(rcvMsg);
                    else if (rcvMsg.command == "AcknowledgementAcceptedFromTh")
                        SendTestLogsToTh(rcvMsg);
                    else if (rcvMsg.command == "GetReadyToReceiveTestLogs")
                        SendMsgToTh(rcvMsg);
                    else if (rcvMsg.command == "TestLogsSent")
                        AcceptTestLogs(rcvMsg);
                    else if (rcvMsg.command == "getTestFiles")
                        getFiles(rcvMsg);
                }
            }
        }

        //send connected msg to child builder 
        public static void connectRepo(CommMessage rcvMsg, Sender[] cs)
        {
            int count = rcvMsg.arguments.Count<string>();
            int chldPort = Int32.Parse(rcvMsg.arguments[count - 1]);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "connectedToRepo";
            sndMsg.author = "Akash Bhosale";
            sndMsg.to = baseAddress + ":" + "808" + chldPort + "/IpluggableComm";
            sndMsg.from = baseAddress + ":" + "8078" + "/IpluggableComm"; sndMsg.show();
            cs[chldPort].postMessage(sndMsg);
        }

        /*function to carry out some processing activity for accepting build logs
         * This function consolidates build logs created by different builders into one folder
         * in the repository
         */
        public static void AcceptBuildLogs(CommMessage rcvMsg)
        {
            var directories = System.IO.Directory.GetDirectories("../../../Repository/RepoReceiveStore");
            foreach (string dir in directories)
            {
              
                var subdirectories = System.IO.Directory.GetDirectories(dir);
                foreach (string subdirectory in subdirectories)
                {
                    string[] buildLogs = System.IO.Directory.GetFiles(subdirectory);
                    foreach (string file in buildLogs)
                    {
                        if (System.IO.File.Exists(System.IO.Path.Combine(buildLogsPath, System.IO.Path.GetFileName(file))))
                        {
                             System.IO.File.Delete(System.IO.Path.Combine(buildLogsPath, System.IO.Path.GetFileName(file)));
                        }
                        try
                        {
                            System.IO.File.Move(file, System.IO.Path.Combine(buildLogsPath, System.IO.Path.GetFileName(file)));
                        }
                        catch(Exception ex)
                        {
                            ex.ToString();
                        }
                        Console.Write("\n\n===========================================================================\n\n");
                        Console.Write("Build Logs found in {0}", System.IO.Path.GetFullPath(buildLogsPath));
                        Console.Write("\n\n===========================================================================\n\n");
                    }
                }
            }
            foreach (string dir in directories)
            {
                var subdirectories = System.IO.Directory.GetDirectories(dir);
                foreach (string subdirectory in subdirectories)
                {
                    var files1 = System.IO.Directory.GetFiles(subdirectory);
                    if (files1.Length == 0)
                        System.IO.Directory.Delete(subdirectory);

                    var subdirectories1 = System.IO.Directory.GetDirectories(dir);
                    if (subdirectories.Length == 0)
                        System.IO.Directory.Delete(dir);
                }
            }
        }
        /*function to carry out some processing activity for accepting test logs
         * This function consolidates test logs created by different builders into one folder
         * in the repository
         */
        public static void AcceptTestLogs(CommMessage rcvMsg)
        {
            var directories = System.IO.Directory.GetDirectories("../../../Repository/RepoReceiveStore");
            foreach (string dir in directories)
            {
                var subdirectories = System.IO.Directory.GetDirectories(dir);
                foreach (string subdirectory in subdirectories)
                {
                    string[] testLogs = System.IO.Directory.GetFiles(subdirectory);
                    foreach (string file in testLogs)
                    {
                        if (System.IO.File.Exists(System.IO.Path.Combine(testLogsPath, System.IO.Path.GetFileName(file))))
                        {
                            try
                            {
                                System.IO.File.Delete(System.IO.Path.Combine(testLogsPath, System.IO.Path.GetFileName(file)));
                            }
                            catch(Exception ex)
                            {
                                ex.ToString();
                            }
                        }
                        try
                        {
                            System.IO.File.Move(file, System.IO.Path.Combine(testLogsPath, System.IO.Path.GetFileName(file)));
                        }
                        catch(Exception ex)
                        {
                            ex.ToString();
                        }
                        Console.Write("\n\n===================================================================================\n\n");
                        Console.Write("Test logs stored in {0}", System.IO.Path.GetFullPath(testLogsPath));
                        Console.Write("\n\n===================================================================================\n\n");
                    }
                }
            }
        }

        //function to notify builder for sending buildLogs
        public static void SendBuildLogsToBuilder(CommMessage rcvMsg, Sender[] cs)
        {
            int count = rcvMsg.arguments.Count<string>();
            int chldPort = Int32.Parse(rcvMsg.arguments[count - 1]);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "SendBuildLogs";
            sndMsg.author = "Akash Bhosale";
            sndMsg.to = baseAddress + ":" + "808" + chldPort + "/IpluggableComm";
            sndMsg.from = baseAddress + ":" + "8078" + "/IpluggableComm"; sndMsg.show();
            sndMsg.arguments.Add(rcvMsg.arguments[0]);
            cs[chldPort].postMessage(sndMsg);
        }

        //Send ready message to Child Builder
        public static void SendMsgToChildBuilder(CommMessage rcvMsg,Sender[]cs)
        {
            int count = rcvMsg.arguments.Count<string>();
            int chldPort = Int32.Parse(rcvMsg.arguments[count - 1]);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "ReadyToReceiveLogs";
            sndMsg.author = "Akash Bhosale";
            sndMsg.to = baseAddress + ":" + "808" + chldPort + "/IpluggableComm";
            sndMsg.from = baseAddress + ":" + "8078" + "/IpluggableComm";
            sndMsg.arguments.Add(rcvMsg.arguments[0]);
            cs[chldPort].postMessage(sndMsg);
        }

        //send message to test harness
        public static void SendMsgToTh(CommMessage rcvMsg)
        {
            sndr = new Sender(baseAddress, 8077);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "ReadyToReceiveLogs";
            sndMsg.author = "Akash Bhosale";
            sndMsg.to = baseAddress + ":" + "8077"+ "/IpluggableComm";
            sndMsg.from = baseAddress + ":" + "8078" + "/IpluggableComm";
            sndMsg.arguments.Add(rcvMsg.arguments[0]);
            sndr.postMessage(sndMsg);
                        
        }
        //function to notify test harness for sending test logs
        public static void SendTestLogsToTh(CommMessage rcvMsg)

        {
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "SendTestLogs";
            sndMsg.author = "Akash Bhosale";
            sndMsg.to = baseAddress + ":" + "8077" + "/IpluggableComm";
            sndMsg.from = baseAddress + ":" + "8078" + "/IpluggableComm"; sndMsg.show();
            sndMsg.arguments.Add(rcvMsg.arguments[0]);
            sndr.postMessage(sndMsg);
        }

        //function to send Requested files
        public static void SendFiles(CommMessage rcvMsg, Sender[] cs)
        {
            string buildRequest = "";
            Console.Write("\nSending requested files");
            int count = rcvMsg.arguments.Count<string>();
            int chldPort = Int32.Parse(rcvMsg.arguments[count - 1]);
            rcvMsg.arguments.RemoveAt(count - 1);
            Console.Write("\n chldPort is {0}", "808" + chldPort);
            try
            {
                buildRequest = rcvMsg.arguments[0];
                ParseBuildRequest(buildRequest);
            }
            catch(Exception ex)
            {
                ex.ToString();
            }
            Thread.Sleep(2000);
            testedFiles.Add(buildRequest);
            foreach (string file in testedFiles)
            { sendFileNames.Add(file); }
            foreach (string file in driverFiles)
            { sendFileNames.Add(file); }
            Console.Write("\n\n===================================================================================\n\n");
            foreach (string file in sendFileNames)
            {
                Console.Write("\nSending files {0}:", file);
                bool transfersuccess = cs[chldPort].postFile(file, chldPort, System.IO.Path.GetFileNameWithoutExtension(buildRequest));

            }
            Console.Write("\n\n===================================================================================\n\n");
            sendFileNames.Clear();
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "fileSent";
            sndMsg.author = "Akash Bhosale";
            sndMsg.to = baseAddress + ":" + "808" + chldPort + "/IpluggableComm";
            sndMsg.from = baseAddress + ":" + "8078" + "/IpluggableComm";
            sndMsg.arguments.Add(System.IO.Path.GetFileNameWithoutExtension(buildRequest));
            cs[chldPort].postMessage(sndMsg);

        }
        
        //Initialize receiver of repository
        public static void InitializeReceiver()
        {
            rcvr = new Receiver();
            rcvr.start(baseAddress, port);
        }
        //Build Request Parser   
        public static void ParseBuildRequest(string fileName)
        {
            loadXML(System.IO.Path.Combine(("../../../Repository/BuildRequests"), fileName));
            parseList("tested");
            parseList("testDriver");

        }

        //load Build request
        public static bool loadXML(string path)
        {
            try
            {
                doc = XDocument.Load(path);
                Console.Write("\n\n===================================================================================\n\n");
                Console.Write("\nLoaded XML is \n{0}", doc.ToString());
                Console.Write("\n\n===================================================================================\n\n");
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--\n", ex.Message);
                return false;
            }

        }
        //Parse Build Request
        public static List<string> parseList(string propertyName)
        {
            List<string> values = new List<string>();
            IEnumerable<XElement> parseElems = doc.Descendants(propertyName);
            if (parseElems.Count() > 0)
            {
                switch (propertyName)
                {
                    case "tested":
                        foreach (XElement elem in parseElems)
                        {
                            values.Add(elem.Value);
                        }
                        testedFiles = values;
                        break;
                    case "testDriver":
                        foreach (XElement elem in parseElems)
                        {
                            values.Add(elem.Value);

                        }
                        driverFiles = values;
                        break;
                    default:
                        break;
                }
            }
            return values;
        }

        //function to send connected message to childBuilder 
        public static void connectChildBuilder(string i, string j, MessagePassingComm.Sender[] x)
        {
            int chldPort = Int32.Parse(j);
            Console.Write("chldPort is {0}", j);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "connectedToRepo";
            sndMsg.author = "Akash Bhosale";
            sndMsg.to = baseAddress + ":" + "808" + j + "/IpluggableComm";
            sndMsg.from = baseAddress + ":" + "8078" + "/IpluggableComm";
            lock (ConsoleWriterLock)
            {
                sndMsg.show();
            }

            x[chldPort].postMessage(sndMsg);
        }

        //function to create initial directories and set necessary paths
        public static void repoSetup()
        {
            Console.Title = "Repository";
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write("Starting Repository");
            System.IO.Directory.CreateDirectory(buildLogsPath);
            System.IO.Directory.CreateDirectory(testLogsPath);
            ClientEnvironment.fileStorage = "../../../Repository/RepoSendStore";
            ServiceEnvironment.fileStorage = "../../../Repository/RepoReceiveStore";
        }

        //function to fetch files from storage
        public static void getFiles(CommMessage rcvMsg)
        {
            string[] files = System.IO.Directory.GetFiles(testFilesPath);
            string []testDriver= System.IO.Directory.GetFiles(testDriversPath);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "TestFiles";
            sndMsg.author = "Akash Bhosale";
            sndMsg.to = baseAddress + ":" + "8079" + "/IpluggableComm";
            sndMsg.from = baseAddress + ":" + "8078" + "/IpluggableComm";
            Console.Write("===============================================================================\n");
            Console.Write("Sending file to client:");
            foreach(string file in files)
            {
                Console.Write("\n {0}", System.IO.Path.GetFileNameWithoutExtension(file));
                sndMsg.arguments.Add(file);

            }
            foreach (string file in testDriver)
            {
                Console.Write("\n{0}", System.IO.Path.GetFileNameWithoutExtension(file));
                sndMsg.arguments.Add(file);

            }
            Console.Write("\n===============================================================================\n");

        }
    }

}
