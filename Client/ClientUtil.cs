/*/////////////////////////////////////////////////////////////////////////////////////////////////////////
*ClientUtil.cs-----Handles commands send  by GUI and interacts with the repository
 * Author: ------ Akash Bhosale, aabhosla@syr.edu
  Source: Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2017 
  Course:CSE 681 Software Modelling and Analysis
*--------------------------------------------------------------------------------------------------------*/
/*
 * Added references to:
 * - MessagePassingComm
 *
 * This package provides:
 * ----------------------
 * Functions to implement commands received by the GUI
 * This package acts as an intermediatory between GUI and other process spawned by the GUI(Repository and Mother Builder).
 *
 * * Interfaces
 * ------------------------
 * 1. public static bool startMotherBuilder(int ChldBuilderNum)          -> function to start Mother Builder process depending on the number of child builders 
 *                                                                         passed in arguments
 * 2.public static bool startRepository(int numChld)                     ->function to start repository 
 * 3.public static void CommHandler()                                    ->function to handle wpf communication while sending files
 * 4.public static void InitializeReceiver()                             ->function to initialize receiver of client    
 * 5.public static void InitializeSender()                               ->function to initialize sender of client
 * 6.public static void SendBuildFiles()                                 ->function to send a 'Send selected build requests' message to repository
 * 7.public static  void CreateBuildRequest()                            ->function to handle Create Build Request command on UI
 * 8.public static void Quit_ButtonHandler()                             ->function to handle quit button on UI
 * 9.public static  bool saveXml(string path)                            ->function to save created XML
 * 10.public static void MakeBuildRequest()                              ->function to create build Request
 * 11.public static void ListUpdater(List<string>List1,List<string>List2)->function to update test files and driver files selected on UI
 * 12.public static void ListUpdater(List<string>List1)                  ->overloaded function to update build requests list
 * 13.public static void startUpSetup()                                  ->function to set up during initial start up 
 * Required Files:
 * ---------------
 * IMPCommService.cs         :Service interface and Message definition
 * MPCommService.cs          :Message passing implementation
 * MainWindow.xaml           :Send commands to client
 * Repository.cs             :Store created build requests. 
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml.Linq;
using System.Windows.Forms;
using MessagePassingComm;
namespace Client
{
    public class ClientUtil
    {
        private static List<string> testedFiles = new List<string>();
        private static List<string> driverFiles = new List<string>();
        private static List<string> buildRequests = new List<string>();
        private static List<string> UiTestFiles = new List<string>();
        private static XDocument doc;
        private static string XML_savePath = "../../../Repository/BuildRequests";
        private static Receiver rcvr;
        private static Sender sndr;
        private static CommMessage sndMsg;
        private static string baseAddress = "http://localhost";
        private static int port = 8079;
        
       
        //function to start MotherBuilder
        public static bool startMotherBuilder(int ChldBuilderNum)
        {
            Process proc = new Process();
            string fileName = "..\\..\\..\\Builder\\bin\\debug\\Builder.exe";
            string absFileSpec = Path.GetFullPath(fileName);
            Console.Write("\n  attempting to start {0}", absFileSpec);
            string cmdArgs = ChldBuilderNum.ToString();
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
        //function to start Repository process
        public static bool startRepository(int numChld)
        {
            Process proc = new Process();
            string fileName = "..\\..\\..\\Repo\\bin\\Debug\\Repo.exe";
            string absFileSpec = Path.GetFullPath(fileName);
            Console.Write("\n  attempting to start {0}", absFileSpec);
            try
            {
                Process.Start(fileName, numChld.ToString());
            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}", ex.Message);
                return false;
            }
            return true;

        }

        //function to start test harness
        public static bool startTestHarness(int numChld)
        {
            Process proc = new Process();
            string fileName = "..\\..\\..\\TestHarness\\bin\\Debug\\TestHarness.exe";
            string absFileSpec = Path.GetFullPath(fileName);
            Console.Write("\n  attempting to start {0}", absFileSpec);
            try
            {
                Process.Start(fileName, numChld.ToString());
            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}", ex.Message);
                return false;
            }
            return true;
        }

        //Send fileNames to MotherBuilder
        public static void CommHandler()
        {
            SendBuildFiles();
        }

        //Initialize receiver
        public static void InitializeReceiver()
        {
            rcvr = new Receiver();
            rcvr.start(baseAddress, port);
        }
        //Initialize sender
        public static void InitializeSender()
        {
            sndr = new Sender(baseAddress, port + 1);
        }

        //send files to MotherBuilder
        public static void SendBuildFiles()
        {
            sndr = new Sender(baseAddress, port + 1);
            sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "show";
            sndMsg.author = "Akash Bhosale";
            string toAddress = baseAddress + ":" + (port + 1) + "/IpluggableComm";
            string fromAddress = baseAddress + ":" + "8079" + "/IpluggableComm";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            foreach (string fileName in buildRequests)
            {
                sndMsg.arguments.Add(fileName);
            }
            sndr.postMessage(sndMsg);
            buildRequests.Clear(); 
        }

        //Function handling create Build Request operation
        public static void CreateBuildRequest()
        {
            foreach (string file in testedFiles)
            {
                System.IO.File.Copy(System.IO.Path.Combine(("../../../Repository/TestFiles"), file), System.IO.Path.Combine(("../../../Repository/RepoSendStore"), file), true);
            }
            foreach (string file in driverFiles)
            {
                System.IO.File.Copy(System.IO.Path.Combine(("../../../Repository/TestDrivers"), file), System.IO.Path.Combine(("../../../Repository/RepoSendStore"), file), true);
            }

            MakeBuildRequest();
            saveXml(XML_savePath);
        }

        //Quit Button Handler
        public static void Quit_ButtonHandler()
        {
             
            sndr = new Sender(baseAddress, port - 1);
            sndMsg = new CommMessage(CommMessage.MessageType.reply);
            sndMsg.command = "quit";
            sndMsg.author = "Akash Bhosale";
            string toAddress = baseAddress + ":" + (port - 1) + "/IpluggableComm"; 
            string fromAddress = baseAddress + ":" + "8079" + "/IpluggableComm";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            sndr.postMessage(sndMsg);

            sndr = new Sender(baseAddress, port + 1);
            sndMsg = new CommMessage(CommMessage.MessageType.reply);
            sndMsg.command = "quit";
            sndMsg.author = "Akash Bhosale";
            toAddress = baseAddress + ":" + (port + 1) + "/IpluggableComm"; 
            fromAddress = baseAddress + ":" + "8079" + "/IpluggableComm";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            sndr.postMessage(sndMsg);


            
            sndr = new Sender(baseAddress, port + 1);
            sndMsg = new CommMessage(CommMessage.MessageType.reply);
            sndMsg.command = "quit";
            sndMsg.author = "Akash Bhosale";
            toAddress = baseAddress + ":" + (port -2) + "/IpluggableComm"; 
            fromAddress = baseAddress + ":" + "8079" + "/IpluggableComm";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            sndr.postMessage(sndMsg);
        }
        //function to save created XML
        public static bool saveXml(string path)
        {
            string fileName = "BuildRequest" + DateTime.Now.ToString("HHmmss") + ".xml";
            string savePath = System.IO.Path.Combine(path, fileName);
            string clientFileStore = System.IO.Path.Combine("../../../Repository/RepoSendStore", fileName);
            try
            {
                doc.Save(savePath);
                System.IO.File.Copy(savePath, clientFileStore);
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--\n", ex.Message);
                return false;
            }
            
        }

        //Function to create build Request
        public static void MakeBuildRequest()
        {
            doc = new XDocument();
            XElement testRequestElem = new XElement("testRequest");
            doc.Add(testRequestElem);

            XElement TestRequestElem = new XElement("TestRequest");
            TestRequestElem.Add("Test1");
            testRequestElem.Add(TestRequestElem);

            XElement authorElem = new XElement("author");
            authorElem.Add("Akash Bhosale");
            testRequestElem.Add(authorElem);

            XElement dateTimeElem = new XElement("dateTime");
            dateTimeElem.Add(DateTime.Now.ToString());
            testRequestElem.Add(dateTimeElem);

            XElement testElem = new XElement("test");
            testRequestElem.Add(testElem);


            foreach (string driver_file in driverFiles)
            {
                XElement driverElem = new XElement("testDriver");
                driverElem.Add(driver_file);
                testElem.Add(driverElem);
            }

            foreach (string file in testedFiles)
            {
                XElement testedElem = new XElement("tested");
                testedElem.Add(file);
                testElem.Add(testedElem);
            }
        }
        //helper function for updating lists
        public static void ListUpdater(List<string> List1, List<string> List2)
        {
            testedFiles = List1;
            driverFiles = List2;

        }

        //overloaded function to update list
        public static void ListUpdater(List<string> List1)
        {
            buildRequests = List1;
        }

        //function which setsup initial start up 
        public static void startUpSetup()
        {

            string buildRequest1 = "BuildRequest213414.xml";
            string buildRequest2 = "BuildRequest213507.xml";
            string buildRequest3 = "BuildRequest214216.xml";
            string buildRequest4 = "BuildRequest215522.xml";
            buildRequests.Add(buildRequest1);
            buildRequests.Add(buildRequest2);
            buildRequests.Add(buildRequest3);
            buildRequests.Add(buildRequest4);
            
        }
        //function to clear build request once send to the Mother Builder
        public static void buildRequestClear()
        {
            buildRequests.Clear();
            
        }

        //function to get files from repository
        public static void getTestFiles()
        {
            sndr = new Sender(baseAddress, 8078);
            sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "getTestFiles";
            sndMsg.author = "Akash Bhosale";
            string toAddress = baseAddress + ":" + (port-1) + "/IpluggableComm";
            string fromAddress = baseAddress + ":" + "8079" + "/IpluggableComm";
            sndMsg.to = toAddress;
            sndMsg.from = fromAddress;
            sndr.postMessage(sndMsg);
        }

        
        
    }
}







