/*CommunicationInterface.cs-----A communication interface for messages
 * Author: ------ Akash Bhosale, aabhosal@syr.edu
  Source: Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2017 
  Course:CSE 681 Software Modelling and Analysis
  Application: CSE 681 Project 4- CommunicationInterface.cs
  Environment: C# Console
  *--------------------------------------------------------------------------------------------------------*/
/*
 * Interfaces
 * ---------------------
 * 1.public virtual void IncomingMessageProcessor(Message msg) { }          ->function to handle incoming message
 * 2.public void Updateui(string msg)                                       ->function to update  UI 
 *   
 *
 *This package provides:
 * ---------------------
 * Interfaces for the communicator base
 * Required Files:
 ** ---------------
 * IMPCommService.cs         : Service interface and Message definition
 * Program.cs                 :Child Builders to be started by Mother Builder
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

namespace CommunicationInterface
{
    public delegate void SomeEventHandler(String msg); 
    public delegate void MsgProcessor(Message msg); 
    

    public interface ICommunicator
    {
        MsgProcessor MessageReceiver();      
        
    }

    public abstract class CommunicatorBase_Delegates : ICommunicator
    {
        public CommunicatorBase_Delegates() { }

        public MsgProcessor MessageReceiver()
        {
            return ReceiverDelegate;
        }

       
        //function to update  UI 
        public void Updateui(string msg)
        {
            
            lock (thisLock)
            {
                Console.WriteLine(msg);
            }
        }

        //function to handle incoming message
        public virtual void IncomingMessageProcessor(Message msg) { }

        static protected CommunicationEnvironment_Delegates environ;
        protected MsgProcessor ReceiverDelegate = null;
        public Object thisLock = new Object();    
    }

    public struct CommunicationEnvironment_Delegates
    {
        public ICommunicator client { get; set; }
        public ICommunicator repo { get; set; }
        public ICommunicator builder { get; set; }
        public ICommunicator testHarness { get; set; }
        public ICommunicator console { get; set; }
    }

    

    public class Message
    {
        public string type { get; set; } = "";
        public string to { get; set; } = "";
        public string from { get; set; } = "";
        public string body { get; set; } = "";
        public static Message makeMsg(string type, string to, string from, string body)
        {
            Message msg = new Message();
            msg.type = type;
            msg.to = to;
            msg.from = from;
            msg.body = body;
            return msg;
        }

        //function to overrid string function
        public override string ToString()
        {
            string outStr = "Message - " +
              string.Format("type: {0}, ", type) +
              string.Format("from: {0}, ", from) +
              string.Format("to: {0}, ", to) +
              string.Format("body: {0}, ", body);
            return outStr;
        }
    }

}
