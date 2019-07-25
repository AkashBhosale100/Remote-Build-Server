/*MainWindow.xaml.cs-----Simulates the operations of sending and receiving buildRequests and testFiles using WCF
 * Author: ------ Akash Bhosale, aabhosal@syr.edu
  Source: Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2017 
  Course:CSE 681 Software Modelling and Analysis
  Environment:C# Console
  Application: CSE 681- MainWindow.xaml.cs
*--------------------------------------------------------------------------------------------------------*/
/*
 * Added references to:
 * - MessagePassingComm
 *
 * This package provides:
 * ----------------------
 * Implements various operations to be performed as per GUI design
 * 
 * Interfaces
 * --------------------------------
 * 1.private void CommandButton_Click(object sender, RoutedEventArgs e)                                    ->function to handle Start Mother builder button on UI
 * 2.public void CreateBuildRequestButton_Click(object sender, RoutedEventArgs e)                          ->function to handle create build request button on UI
 * 3.private void SendBuildRequest_Click(object sender, RoutedEventArgs e)                                 ->function to send build request
 * 4.public void QuitButton_Click(object sender, RoutedEventArgs e)                                        ->function to handle quit button on UI
 * 5.void MainWindow_Closing(object sender, CancelEventArgs e)                                             ->function to handle (x)button on UI
 * 6.public void updateBuildRequestsOnUI()                                                                 ->thread function to update build requests list box
 * 7.private void ViewBuildRequest_Click(object sender, RoutedEventArgs e)                                 ->function to view generated build request
 * 8.public static bool saveXml(string path, string fileName)                                              ->functionality to save the xml document when adding new test to build request
 * 9. public static bool loadXml(string path)                                                                -> function to loadXml
 * 10.public static void addTestHandler(String testDriver, List<String> testedFiles, String testRequestList)->function to add multiple tests to one test request
 * 11.private void AddTestsBtn_Click(object sender, RoutedEventArgs e)                                      ->function to handle add tests button on ui
 * 12.private void TestLogslistBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)                  ->function to handle test logs list box mouse double click on ui
 * 13.private void GetTestLogsBtn_Click(object sender, RoutedEventArgs e)                                   ->function to handler get test logs button on ui
 * 14.startup handler function to specify number of child builders to be spawned at startup                 ->public void StartupHandler(int numChldBldr)
        
 * Required Files:
 * ---------------
 * MainWindow.xaml
 * 
 * Maintenance History:
 * --------------------
 *  * ver 1.0 : 25 October 2017
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.IO;
using MessagePassingComm;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Diagnostics;
using System.Xml.Linq;
using System.ComponentModel;
using TestRequest;

namespace Client
{
    
    public partial class MainWindow : Window
    {
        
        private string buildRequestPath = "..\\..\\..\\Repository\\BuildRequests";
        private string buildLogsPath = "..\\..\\..\\Repository\\BuildLogs";
        private string testLogsPath = "..\\..\\..\\Repository\\TestLogs";
        private ClientUtil client;
        private static List<string> testedFiles = new List<string>();
        private static List<string> driverFiles = new List<string>();
        private static List<string> BuildRequests = new List<string>();
        private static XDocument doc { get; set; } = new XDocument();


        //constructor
        public MainWindow()
        {
            InitializeComponent();
            loadList();
            client = new ClientUtil();
            Thread fileCheck = new Thread(new ThreadStart(updateBuildRequestsOnUI));
            fileCheck.Start();
            SendBuildRequests.IsEnabled = true;
            FileNameTextBox.Text = "2";
            FileNameTextBox.IsReadOnly = true;
        }

        //initialize setup
        //function to handle Start Mother builder button on UI
        private void CommandButton_Click(object sender, RoutedEventArgs e)
        {
            var h = FileNameTextBox.Text;
            int numOfChilds;
            if (!(Int32.TryParse(h, out numOfChilds)))
            {



            }

            else
            {
                int chldBuilderNum = 0;
                chldBuilderNum = Int32.Parse(FileNameTextBox.Text);
                SendBuildRequests.IsEnabled = true;
                ClientUtil.startMotherBuilder(chldBuilderNum);
                ClientUtil.startRepository(chldBuilderNum);
                ClientUtil.startTestHarness(chldBuilderNum);
                Thread.Sleep(500);
                //CommandButton.IsEnabled = false;
            }

        }
        //Function to handle create build request button on UI
        public void CreateBuildRequestButton_Click(object sender, RoutedEventArgs e)
        {

            foreach (var file in TestFileslistBox.SelectedItems)
            {

                testedFiles.Add(file.ToString());

            }

            foreach (var file in TestDriverslistBox.SelectedItems)
            {
                driverFiles.Add(file.ToString());

            }
            ClientUtil.ListUpdater(testedFiles, driverFiles);
            TestFileslistBox.SelectedIndex = -1;
            TestDriverslistBox.SelectedIndex = -1;
            ClientUtil.CreateBuildRequest();
            testedFiles.Clear();
            driverFiles.Clear();

        }
        //Function to send build request
        private void SendBuildRequest_Click(object sender, RoutedEventArgs e)
        {
            foreach (var file in BuildRequestlistBox.SelectedItems)
            {

                BuildRequests.Add(file.ToString());
            }
            ClientUtil.ListUpdater(BuildRequests);
            ClientUtil.CommHandler();
            BuildRequestlistBox.SelectedItems.Clear();
        }

        //Function to handle quit button on UI
        public void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            ClientUtil.Quit_ButtonHandler();
        }

        //function to handle quit button for entire application
        public void QuitEntireAppButton_Click(object sender, RoutedEventArgs e)
        {
            ClientUtil.Quit_ButtonHandler();
            Thread.Sleep(3000);
            Process.GetCurrentProcess().Kill();
        }
        //load created build request
        public void loadList()
        {

            ClientUtil.getTestFiles();
            string testFilesPath = "..\\..\\..\\Repository\\TestFiles";
            string driverFilesPath = "..\\..\\..\\Repository\\TestDrivers";
            DirectoryInfo directoryInfo = new DirectoryInfo(testFilesPath);
            FileInfo[] testfiles = directoryInfo.GetFiles("*.*");
            foreach (FileInfo file in testfiles)
            {
                if (!TestFileslistBox.Items.Contains(file.Name))
                {
                    TestFileslistBox.Items.Add(file.Name);
                }
            }
            DirectoryInfo directoryInfo1 = new DirectoryInfo(driverFilesPath);
            FileInfo[] driverFiles = directoryInfo1.GetFiles("*.*");
            foreach (FileInfo file in driverFiles)

                if (!TestDriverslistBox.Items.Contains(file.Name))
                {
                    TestDriverslistBox.Items.Add(file.Name);

                }
        }
        //thread function to update build requests list box
        public void updateBuildRequestsOnUI()
        {
            int length = 0;
            int length1 = 0;
            try
            {
                while (true)
                {
                    this.Dispatcher.Invoke(() =>
                    {


                        DirectoryInfo directoryInfo = new DirectoryInfo(buildRequestPath);
                        FileInfo[] buildRequests = directoryInfo.GetFiles("*.*");
                        length = buildRequests.Count<FileInfo>();
                        if (length1 != length)
                        {
                            BuildRequestlistBox.Items.Clear();

                            foreach (FileInfo file in buildRequests)
                            {
                                BuildRequestlistBox.Items.Add(file);
                            }
                            length1 = length;
                        }

                    });
                }
            }
            catch (System.Threading.Tasks.TaskCanceledException)
            {


            }

        }

         //function to handle (x)button on UI
        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            ClientUtil.Quit_ButtonHandler();
            Thread.Sleep(6000);
            Process.GetCurrentProcess().Kill();
        }
        //startup handler function to specify number of child builders to be spawned at startup
        public void StartupHandler(int numChldBldr)
        {
            FileNameTextBox.Text = numChldBldr.ToString();
        }

        //function to handlee get build logs button on ui
        private void GetBuildLogstButton_Click(object sender, RoutedEventArgs e)
        {
            
            string[] buildLogs = System.IO.Directory.GetFiles(buildLogsPath);
            BuildLogsListBox.Items.Clear();
            foreach (string buildLog in buildLogs)
            {
                BuildLogsListBox.Items.Add(System.IO.Path.GetFileName(buildLog));
            }

        }

        //function to handler get test logs button on ui
        private void GetTestLogsBtn_Click(object sender, RoutedEventArgs e)
        {

            string[] testLogs = System.IO.Directory.GetFiles(testLogsPath);
            TestLogsListBox.Items.Clear();
            foreach (string testLog in testLogs)
            {
                TestLogsListBox.Items.Add(System.IO.Path.GetFileName(testLog));
            }
        }

        //function to handle double click event on the build logs list box
        private void BuildLogsListBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string fileName = BuildLogsListBox.SelectedValue as string;
            try
            {
                string path = System.IO.Path.Combine("../../../Repository/BuildLogs", fileName);
                string contents = File.ReadAllText(path);
                Popup popup = new Popup();
                popup.codeView.Text = contents;
                popup.Show();
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }
        
        //function to handle test logs list box mouse double click on ui
        private void TestLogslistBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string fileName = TestLogsListBox.SelectedValue as string;
            try
            {
                string path = System.IO.Path.Combine("../../../Repository/TestLogs", fileName);
                string contents = File.ReadAllText(path);
                Popup popup = new Popup();
                popup.codeView.Text = contents;
                popup.Show();
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }

        }

        //function to handle add tests button on ui
        private void AddTestsBtn_Click(object sender, RoutedEventArgs e)
        {
            List<string> testedFiles = new List<string>();
            string testDriver = null;
            string testRequest = "";

            if (TestDriverslistBox.SelectedItem == null || TestFileslistBox.SelectedItems.Count<1 ||BuildRequestlistBox.SelectedItem==null)
            {
                MessageBox.Show("Please select test files and test drivers along with a build request to add tests");


            }

            foreach(var item in TestFileslistBox.SelectedItems)
            {
                if (item != null)
                {
                    testedFiles.Add(item.ToString());
                }
            }
            if(TestDriverslistBox.SelectedItem!=null)
            {
                testDriver = TestDriverslistBox.SelectedItem.ToString();
            }
            if (BuildRequestlistBox.SelectedItem != null)
            {
                testRequest = BuildRequestlistBox.SelectedItem.ToString();
            }
            addTestHandler(testDriver, testedFiles, testRequest);
            
        }

        //function to add multiple tests to one test request
        public static void addTestHandler(String testDriver, List<String> testedFiles, String testRequestList)
        {
            try
            {
                TestRequestUtil tr = new TestRequestUtil();
                string testRequestPath = "../../../Repository/BuildRequests";
                loadXml(System.IO.Path.Combine(testRequestPath, testRequestList));

                XElement root = doc.Element("testRequest");
                IEnumerable<XElement> rows = root.Descendants("test");
                XElement firstRow = rows.First();
                firstRow.AddBeforeSelf(
                   new XElement("test",
                   new XElement("testDriver", testDriver),
                   testedFiles.Select(i => new XElement("tested", i))
                   ));
                saveXml(testRequestPath, testRequestList);
                MessageBox.Show("Added test to " + testRequestList);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        //function to loadXml
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

        /*--<functionality to save the xml document when adding new test to build req-->*/
        public static bool saveXml(string path, string fileName)
        {

            string savePath = System.IO.Path.Combine(path, fileName);
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                doc.Save(System.IO.Path.Combine(path, fileName));
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--\n", ex.Message);
                return false;
            }

        }
        //function to handle double click of double click of build request list box 
        //Will do nothing
        public void BuildRequestlistBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Console.Write("Invoke buildRequestlistbox");  
        }
        
        //function to handle change of list box
        public void FileNameTextBox_TextChanged(object sender, RoutedEventArgs  e )
        {
            var h = FileNameTextBox.Text;

        }


        //function to view generated build request
        private void ViewBuildRequest_Click(object sender, RoutedEventArgs e)
        {
            int index = BuildRequestlistBox.SelectedIndex;
            var fileName = BuildRequestlistBox.Items[index];
            try
            {
                string path = System.IO.Path.Combine("../../../Repository/BuildRequests", fileName.ToString());
                string contents = File.ReadAllText(path);
                Popup popup = new Popup();
                popup.codeView.Text = contents;
                popup.Show();
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }

            BuildRequestlistBox.SelectedItems.Clear();
        }
        
    }

}