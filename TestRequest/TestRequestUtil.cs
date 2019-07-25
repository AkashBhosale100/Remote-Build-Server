
///////////////////////////////////////////////////////////////////////////////////////
//  TestRequest -   Package for creating, saving and parsing the test request created//
//                                                                                //
//  Version: version1                                                            //
//  Author:Akash Bhosale                                                       //
//  Programming Language:C#                                                    //
//  IDE:Visual Studio 2017                                                    //
//  Subject: CSE681 - Software Modeling and Analysis                         //                                
//  Sem:Fall 2017                                                           //
/////////////////////////////////////////////////////////////////////////////
/*
 * This package provides: Package for creating and saving the test request created 
 * ----------------------
 * TestRequestUtil - TestRequest class is required to create Xml requests and parse the xml request.
*makeRequest()- method of this class creates xml
*loadXml()- loads the xml
*SaveXml()-saves the xml to the specified path
*BuildRequestCreate()--invoke makeRequest and SaveXml
*processMessage()- used to process message and pass it to different packages
*parse()--parse the test request created
*parseList()--parse the list element of test request.
 *
 * Required Files:
 * ---------------
 * IMPCommService.cs
 * MPCommService.cs
 * BlockingQueue.cs
 *  
 * Maintenance History:
 * --------------------
 * ver 1.0 : 27th October 2017
 *  - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;

namespace TestRequest
{
    public class TestRequestUtil
    {
        private string path = "..\\..\\..\\ClientFileStore";
        private string author { get; set; } = "";
        private string dateTime { get; set; } = "";
        private List<string> testDriver { get; set; } = new List<string>();
        private List<string> testedFiles { get; set; } = new List<string>();
        private string file;
        private XDocument doc { get; set; } = new XDocument();

        public void BuildRequestCreate()
        {
            makeRequest();

        }

        public void makeRequest()
        {

            XElement testRequestElem = new XElement("testRequest");
            doc.Add(testRequestElem);

            XElement authorElem = new XElement("author");
            authorElem.Add("Shweta Sinha");
            testRequestElem.Add(authorElem);

            XElement dateTimeElem = new XElement("dateTime");
            dateTimeElem.Add(DateTime.Now.ToString());
            testRequestElem.Add(dateTimeElem);

            XElement testElem = new XElement("test");
            testRequestElem.Add(testElem);

            foreach (string file in testDriver)
            {
                XElement driverElem = new XElement("testDriver");
                driverElem.Add(testDriver);
                testElem.Add(driverElem);
            }

            foreach (string file in testedFiles)
            {
                XElement testedElem = new XElement("tested");
                testedElem.Add(System.IO.Path.GetFileName(file));
                testElem.Add(testedElem);
            }
            saveXml(path);

        }

        public bool saveXml(string path)
        {
            string fileName = "TestRequest" + DateTime.Now.ToString("HHmmssfff");
            file = fileName + ".xml";
            string savePath = System.IO.Path.Combine(path, file);
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                doc.Save(System.IO.Path.Combine(path, file));
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--\n", ex.Message);
                return false;
            }

        }

        /*----< load TestRequest from XML file >-----------------------*/

        public bool loadXml(string path)
        {
            //path = Path.GetFullPath(path);
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

        /*----< parse document for property value >--------------------*/

        public string parse(string propertyName)
        {

            string parseStr = doc.Descendants(propertyName).First().Value;
            if (parseStr.Length > 0)
            {
                switch (propertyName)
                {
                    case "author":
                        author = parseStr;
                        break;
                    case "dateTime":
                        dateTime = parseStr;
                        break;

                    default:
                        break;
                }
                return parseStr;
            }
            return "";
        }


        public List<string> parseList(string propertyName)
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
                        testDriver = values;
                        break;

                    default:
                        break;
                }
            }
            return values;
        }

    }
#if (TEST_X)
class Test_TestRequest
{
    static void Main(string[] args)
    {
            Console.Write("\n  Testing TestRequest");
            Console.Write("\n =====================");
            XDocument doc = new XDocument();
            TestRequestUtil tr = new TestRequestUtil();
            tr.author = "Shweta Sinha";
            tr.testDriver.Add("td1.cs");
            tr.testedFiles.Add("tf1.cs");
            tr.testedFiles.Add("tf2.cs");
            tr.testedFiles.Add("tf3.cs"); 
            tr.makeRequest();
            Console.Write("\n{0}",doc.ToString());

            tr.saveXml("..\\..\\..\\ClientFileStore");

            TestRequestUtil tr1 = new TestRequestUtil();
            tr1.loadXml("..\\..\\..\\ClientFileStore");

            Console.Write("\n{0}", doc.ToString());
            Console.Write("\n");

            tr1.parse("author");
            Console.Write("\n  author is \"{0}\"", tr1.author);

            tr1.parse("dateTime");
           Console.Write("\n  dateTime is \"{0}\"", tr1.dateTime);

            tr1.parseList("testDriver");
            foreach (string file in tr1.testDriver)
            {
                Console.Write("\n    \"{0}\"", file);
            }

            tr1.parseList("tested");
            Console.Write("\n  testedFiles are:");

            foreach (string file in tr1.testedFiles)
            {
                Console.Write("\n    \"{0}\"", file);
            }
            Console.Write("\n\n");
        }
    }
#endif
}
