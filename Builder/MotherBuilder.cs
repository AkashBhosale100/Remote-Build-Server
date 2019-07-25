/*MotherBuilder.cs-----Simulates the operations of sending and receiving buildRequests and testFiles using WCF
 * Author: ------ Akash Bhosale, aabhosal@syr.edu
  Source: Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2017 
  Course:CSE 681 Software Modelling and Analysis
  Application: CSE 681 Project 4- MotherBuilder.cs
  Environment: C# Console
  *--------------------------------------------------------------------------------------------------------*/
/*
 * Added references to:
 * - MessagePassingComm
 *
 * This package provides:
 * ----------------------
 * Handles invoking and maintaining child process by maintaining a ready queue and build request queue
 * Handles files received from repository.
 * Handles shutting down of child processes when 'quit' message is received by GUI.
 *
 * Interfaces
 * ---------------------
 * 1.static bool createProcess(int id, string baseAddress, int port)     ->function to spawn child builders
 * 2.public static void InitializeReceiver()                             ->function to initialize receiver of mother builder    
 * 3.public static void InitializeSender()                               ->function to initialize sender of mother builder
 * 4.public static void getMessage()                                     ->thread function continuously checking for incoming messages
 * 5.public static void qManager()                                       ->function to manage ready queue
 * 6.public static void processPid(int pId)                              ->function to take necessary action on the process retrieved from the ready queue
 * 
 * Required Files:
 ** ---------------
 * IMPCommService.cs         : Service interface and Message definition
 * ChildBuilder.cs               :Child Builders to be started by Mother Builder
 * MPCommService.cs 
 * Maintenance History:
 * --------------------
  *  * ver 1.0 : 27 October 2017
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
using System.IO;
using MessagePassingComm;
using System.Threading;



namespace Builder
{
    class MotherBuilder
    {
        private static string baseAddress = "http://localhost";
        private static int port = 8080;
        private static Receiver rcvr;
        private static Sender sndr;
        private static CommMessage rcvMsg;
        private static SWTools.BlockingQueue<int> readyQ = new SWTools.BlockingQueue<int>();
        private static SWTools.BlockingQueue<string> buildRequestQ = new SWTools.BlockingQueue<string>();
        private static readonly object ConsoleWriterLock = new object();
        private static int childProcNum = 0;

        //function to spawn child builders
        static bool createProcess(int id, string baseAddress, int port)
        {
            string pId = id.ToString();
            string bAddress = baseAddress;
            string portNumber = port.ToString();
            const string argsSeparator = " ";
            var arr = new string[] { pId, bAddress, portNumber };
            string cmdArgs = string.Join(argsSeparator, arr);

            Process proc = new Process();
            string fileName = "..\\..\\..\\ChildBuilder\\bin\\debug\\ChildBuilder.exe";
            string absFileSpec = Path.GetFullPath(fileName);
            Console.Write("\n  attempting to start {0}", absFileSpec);

            try
            {
                Process.Start(fileName, cmdArgs);
            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}", ex.Message);
                return false;
            }
            return true;
        }
        static void Main(string[] args)
        {
            InitializeReceiver();
            Console.Title = "Mother Builder";
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write("\n Starting Mother Builder Process");
            Console.Write("\n===================================");
            Thread rcvMsg = new Thread(new ThreadStart(getMessage));
            Console.Write("\nStarting getMessage thread to check incoming messages in Mother Builder");
            Console.Write("\n=========================================================");
            rcvMsg.Start();
           Thread manageQ = new Thread(new ThreadStart(qManager));
            manageQ.Start();

            int count;
            if (args.Count() != 0)
            {
                count = Int32.Parse(args[0]);
                for (int id = 1; id <= count; ++id)
                {
                    if (createProcess(id, baseAddress, port))
                    {
                        Console.Write(" - succeeded");
                    }
                    else
                    {
                        Console.Write(" - failed");
                    }
                    childProcNum = id;
                }
            }
            Console.Read();
        }
        //Initialize receiver of Mother Builder at a specific address
        public static void InitializeReceiver()
        {
            rcvr = new Receiver();
            rcvr.start(baseAddress, port);
        }


        //thread function to get messages from the receiver queue
        public static void getMessage()
        {
            while (true)
            {
                rcvMsg = rcvr.getMessage();
                lock (ConsoleWriterLock)
                {
                    rcvMsg.show();
                }
                if (rcvMsg.from == baseAddress + ":" + "8079" + "/IpluggableComm")
                {
                    if (rcvMsg.type == CommMessage.MessageType.request)
                    {
                        foreach (string file in rcvMsg.arguments)
                        { buildRequestQ.enQ(file); }
                    }
                }
                else if (rcvMsg.command == "ready")
                {
                    if (rcvMsg.from != null)
                    {
                        Console.Write("\nEnqueing msg {0} into the ready queue", rcvMsg.arguments[0]);
                        readyQ.enQ(Int32.Parse(rcvMsg.arguments[0]));
                    }
                }
                if (rcvMsg.command == "quit")
                {
                        Console.Write("\nQuitting Mother Builder");
                    
                    for (int i = 1; i <= childProcNum; i++)
                    {
                        QuitMsg(i);
                    }
                    Thread.Sleep(2000);
                    Process.GetCurrentProcess().Kill();
                    
                }
            }
        }

        //function to manage ready queue
        public static void qManager()
        {
            while (true)
            {
                int pId;

                if (readyQ.size() != 0)
                {
                    pId = readyQ.deQ();
                    lock (ConsoleWriterLock)
                    {
                        Console.Write("\npId retrieved from readyQ is {0}", pId);
                    }
                    processPid(pId);
                }
            }

        }

        //function to take necessary action on the process retrieved from the ready queue
        public static void processPid(int pId)
        {
            
            lock (ConsoleWriterLock)
            {
                Console.Write("\nProcessing id {0}\n", pId);
            }
            string fileName = buildRequestQ.deQ();
            Console.Write("\nFile name retrieved from build queue is {0}\n", fileName);
            int processId = pId;
            sndr = new Sender(baseAddress, port + processId);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "processBuildRequest";
            sndMsg.author = "Akash Bhosale";
            string toAddress = baseAddress + ":" + (port + processId) + "/IpluggableComm";
            string fromAddress = baseAddress + ":" + port + "/IpluggableComm";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            sndMsg.arguments.Add(fileName);
            sndr.postMessage(sndMsg);

        }

        //function which sends quit message to child builders
        public static void QuitMsg(int i)
        {
            Sender sndr = new Sender(baseAddress, port + i);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "quit";
            string fromAddress = baseAddress + ":" + port + "/IpluggableComm";
            string toAddress = baseAddress + ":" + (port + i) + "/IpluggableComm";
            sndMsg.from = fromAddress;
            sndMsg.to = toAddress;
            sndr.postMessage(sndMsg);
        }

    }

}


