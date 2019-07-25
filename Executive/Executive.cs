/*Executive.cs-----Invokes all processes and supplies necessary inputs
 * Author: ------ Akash Bhosale, aabhosal@syr.edu
  Source: Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2017 
  Course:CSE 681 Software Modelling and Analysis
  Application: CSE 681 Project 4- Executive.cs
  Environment: C# Console
  *--------------------------------------------------------------------------------------------------------*/
/*
 * Added references to:
 * - MessagePassingComm
 *
 * This package :
 * ----------------------
 *  Invokes different processes(child, mother builder and test )
 *  
 * Interfaces
 * ---------------------
 * 1.public static void startUpSetup()                          ->start Up Setup
 * 2.public static bool startUI()                               ->function to start UI 
        
   
 * Required Files:
 ** ---------------
 * IMPCommService.cs         : Service interface and Message definition
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
using Client;

namespace Executive
{
    class Executive
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.Title="Executive";
            startUpSetup();

        }

        //start Up Setup
        public static void startUpSetup()
        {
            startUI();
            int numChldBldr = 2;
            MainWindow UI = new MainWindow();
            UI.StartupHandler(numChldBldr);
            ClientUtil.startMotherBuilder(numChldBldr);
            ClientUtil.startRepository(numChldBldr);
            ClientUtil.startTestHarness(numChldBldr);
            ClientUtil.startUpSetup();
             ClientUtil.CommHandler();
            ClientUtil.buildRequestClear();            
        }

        //function to start UI 
        public static bool startUI()
        {

            Process proc = new Process();
            string fileName = "..\\..\\..\\Client\\bin\\Debug\\Client.exe";
            string absFileSpec = Path.GetFullPath(fileName);
            Console.Write("\n  attempting to start {0}", absFileSpec);
            try
            {
                Process.Start(fileName);
            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}", ex.Message);
                return false;
            }
            return true;
        }
    }
}
