﻿/***************************************
//   Copyright 2014 - Svetoslav Vasilev

//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at

//     http://www.apache.org/licenses/LICENSE-2.0

//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
*****************************************/

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.IO;
using System.IO.Pipes;
using System.ServiceModel;
using System.ServiceModel.Channels;

using TransMock.TestUtils;
using TransMock.Wcf.Adapter;
using TransMock.Integration.BizUnit;

using BizUnit;
using BizUnit.Core;
using Moq;
using BizUnit.Core.TestBuilder;

namespace TransMock.Integration.BizUnit.Tests
{
    /// <summary>
    /// Tests the MockReceiveStep class
    /// </summary>
    [TestClass]
    public class TestMockRequestResponseStep
    {
        public TestMockRequestResponseStep()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        MockAdapterConnectionUri connectionUri;
        MockAdapter adapter;
        MockAdapterOutboundHandler outboundHandler;

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            //Setting up the inbound handler with all the references
            connectionUri = new MockAdapterConnectionUri(new Uri("mock://localhost/2WayTestEndpoint"));
            adapter = new MockAdapter();
            adapter.Encoding = "UTF-8";
            MockAdapterConnectionFactory connectionFactory = new MockAdapterConnectionFactory(
                connectionUri, null, adapter);
            MockAdapterConnection connection = new MockAdapterConnection(connectionFactory);
            outboundHandler = new MockAdapterOutboundHandler(connection, null);
        }
        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup() 
        {
            //give some time for the pipe to clean
            System.Threading.Thread.Sleep(100);
        }
        
        #endregion

        [TestMethod]
        public void TestValidateMethod_ValidStep()
        {
            var loggerMock = TestMockReceiveStep.CreateLoggerMock();

            Context context = new Context(loggerMock.Object);
            MockRequestResponseStep step = new MockRequestResponseStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.Encoding = "UTF-8";
            step.ResponsePath = "TestResponse.xml";
            //Calling Validate in order to start the 
            step.Validate(context);
            // Cleaning up the step
            step.Cleanup();

            step = null;
        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestResponse.xml")]
        public void TestReceiveSmallMessage_XML()
        {            
            //Setting up the ILogger moq
            var loggerMock = TestMockReceiveStep.CreateLoggerMock();
            
            Context context = new Context(loggerMock.Object);

            MockRequestResponseStep step = new MockRequestResponseStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.Encoding = "UTF-8";
            step.ResponsePath = "TestResponse.xml";
            step.Timeout = 30;
            //Calling Validate in order to start the server
            step.Validate(context);
            //Setting up a manual reset event
            System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);
            //here we queue up the step.Execute method in a separate thread as the execution model would actually be
            System.Threading.ThreadPool.QueueUserWorkItem((state) =>
            {
                step.Execute(context);
                manualEvent.Set();
            });
                        
            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(xml, Encoding.UTF8);
            msg.Properties.Add("http://schemas.microsoft.com/BizTalk/2003/system-properties#IsSolicitResponse", true);

            Message responseMsg = outboundHandler.Execute(msg, TimeSpan.FromSeconds(10));
            
            //Waiting for the manual event to be set
            manualEvent.WaitOne(1000);

            //string expected = ReadResponseFileContent("TestResponse.xml").Trim().Replace("\r\n", string.Empty).Replace("\t", string.Empty);
            //string actual = GeneralTestHelper.GetBodyAsString(responseMsg, Encoding.UTF8).Trim().Replace("\r\n", string.Empty).Replace("\t", string.Empty);
            
            //Assert.AreEqual(expected, actual, 
            //    "Response message is not matching the expected content");

            loggerMock.Verify(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s)),
                It.Is<string>(s => !string.IsNullOrEmpty(s))), 
                Times.Exactly(2), 
                "The LogData method was not called");
            
        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestResponse.xml")]
        public void TestReceiveSmallMessages_Debatch3_XML()
        {
            //Setting up the ILogger moq
            var loggerMock = TestMockReceiveStep.CreateLoggerMock();

            Context context = new Context(loggerMock.Object);

            MockRequestResponseStep step = new MockRequestResponseStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.Encoding = "UTF-8";
            step.ResponsePath = "TestResponse.xml";
            step.Timeout = 30;
            step.DebatchedMessageCount = 3;
            //Calling Validate in order to start the 
            step.Validate(context);
            //Setting up a manual reset event
            System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);
            //here we queue up the step.Execute method in a separate thread as the execution model would actually be
            System.Threading.ThreadPool.QueueUserWorkItem((state) =>
            {
                step.Execute(context);
                manualEvent.Set();
            });

            var responseMessageList = new List<Message>(3);

            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            for (int i = 0; i < 3; i++)
            {
                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(xml, Encoding.UTF8);
                msg.Properties.Add("http://schemas.microsoft.com/BizTalk/2003/system-properties#IsSolicitResponse", true);

                var responseMsg = outboundHandler.Execute(msg, TimeSpan.FromSeconds(10));
                responseMessageList.Add(responseMsg);
            }            

            //Waiting for the manual event to be set
            manualEvent.WaitOne(1000);
            
            //string expected = ReadResponseFileContent("TestResponse.xml").Trim().Replace("\r\n", string.Empty).Replace("\t", string.Empty);
            //string actual = GeneralTestHelper.GetBodyAsString(responseMsg, Encoding.UTF8).Trim().Replace("\r\n", string.Empty).Replace("\t", string.Empty);

            //Assert.AreEqual(expected, actual, 
            //    "Response message is not matching the expected content");
            Assert.AreEqual(3, responseMessageList.Count, "The number of response messages is incorrect.");

            loggerMock.Verify(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s)),
                It.Is<string>(s => !string.IsNullOrEmpty(s))), 
                Times.Exactly(6), 
                "The LogData method was not called");

        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestResponse.xml")]
        public void TestReceiveSmallMessages_Debatch50_XML()
        {
            //Setting up the ILogger moq
            var loggerMock = TestMockReceiveStep.CreateLoggerMock();

            Context context = new Context(loggerMock.Object);

            MockRequestResponseStep step = new MockRequestResponseStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.Encoding = "UTF-8";
            step.ResponsePath = "TestResponse.xml";
            step.Timeout = 30;
            step.DebatchedMessageCount = 50;
            //Calling Validate in order to start the 
            step.Validate(context);
            //Setting up a manual reset event
            System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);
            //here we queue up the step.Execute method in a separate thread as the execution model would actually be
            System.Threading.ThreadPool.QueueUserWorkItem((state) =>
            {
                step.Execute(context);
                manualEvent.Set();
            });

            var responseMessageList = new List<Message>(3);

            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            for (int i = 0; i < 50; i++)
            {
                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(xml, Encoding.UTF8);
                msg.Properties.Add("http://schemas.microsoft.com/BizTalk/2003/system-properties#IsSolicitResponse", true);

                var responseMsg = outboundHandler.Execute(msg, TimeSpan.FromSeconds(10));
                responseMessageList.Add(responseMsg);
            }

            //Waiting for the manual event to be set
            manualEvent.WaitOne(3000);

            //string expected = ReadResponseFileContent("TestResponse.xml").Trim().Replace("\r\n", string.Empty).Replace("\t", string.Empty);
            //string actual = GeneralTestHelper.GetBodyAsString(responseMsg, Encoding.UTF8).Trim().Replace("\r\n", string.Empty).Replace("\t", string.Empty);

            //Assert.AreEqual(expected, actual, 
            //    "Response message is not matching the expected content");
            Assert.AreEqual(50, responseMessageList.Count, "The number of response messages is incorrect.");

            loggerMock.Verify(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s)),
                It.Is<string>(s => !string.IsNullOrEmpty(s))),
                Times.Exactly(100),
                "The LogData method was not called");

        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestResponse.xml")]
        public void TestReceiveSmallMessages_Debatch3_SerialValidationSingleStep_XML()
        {
            //Setting up the ILogger moq
            var loggerMock = TestMockReceiveStep.CreateLoggerMock();

            Context context = new Context(loggerMock.Object);

            MockRequestResponseStep step = new MockRequestResponseStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.Encoding = "UTF-8";
            step.ResponsePath = "TestResponse.xml";
            step.Timeout = 30;
            step.DebatchedMessageCount = 3;
            
            // Setting up a validation step mock
            var validationStepMock = TestMockReceiveStep.CreateSubStepMock();
            step.SubSteps.Add(validationStepMock.Object);

            //Calling Validate in order to start the 
            step.Validate(context);
            //Setting up a manual reset event
            System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);
            //here we queue up the step.Execute method in a separate thread as the execution model would actually be
            System.Threading.ThreadPool.QueueUserWorkItem((state) =>
            {
                step.Execute(context);
                manualEvent.Set();
            });

            var responseMessageList = new List<Message>(3);

            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            for (int i = 0; i < 3; i++)
            {
                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(xml, Encoding.UTF8);
                msg.Properties.Add("http://schemas.microsoft.com/BizTalk/2003/system-properties#IsSolicitResponse", true);

                var responseMsg = outboundHandler.Execute(msg, TimeSpan.FromSeconds(10));
                responseMessageList.Add(responseMsg);
            }

            //Waiting for the manual event to be set
            manualEvent.WaitOne(1000);

            //string expected = ReadResponseFileContent("TestResponse.xml").Trim().Replace("\r\n", string.Empty).Replace("\t", string.Empty);
            //string actual = GeneralTestHelper.GetBodyAsString(responseMsg, Encoding.UTF8).Trim().Replace("\r\n", string.Empty).Replace("\t", string.Empty);

            //Assert.AreEqual(expected, actual, 
            //    "Response message is not matching the expected content");
            Assert.AreEqual(3, responseMessageList.Count, "The number of response messages is incorrect.");

            loggerMock.Verify(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s)),
                It.Is<string>(s => !string.IsNullOrEmpty(s))),
                Times.Exactly(6),
                "The LogData method was not called");

            validationStepMock.Verify(vs => vs.Execute(
                It.Is<Stream>(s => s != null),
                It.Is<Context>(c => c != null)),
                Times.Exactly(3),
                "The SubStep mock was not called the expected number of times");
        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestResponse.xml")]
        public void TestReceiveSmallMessages_Debatch3_CascadingValidationSingleStep_XML()
        {
            //Setting up the ILogger moq
            var loggerMock = TestMockReceiveStep.CreateLoggerMock();

            Context context = new Context(loggerMock.Object);

            MockRequestResponseStep step = new MockRequestResponseStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.Encoding = "UTF-8";
            step.ResponsePath = "TestResponse.xml";
            step.Timeout = 30;
            step.DebatchedMessageCount = 3;
            step.ValidationMode = MultiMessageValidationMode.Cascading;

            // Setting up a validation step mock list
            var validationStepMockList = new List<Mock<SubStepBase>>(3);
            for (int i = 0; i < 3; i++)
            {
                var validationStepMock = TestMockReceiveStep.CreateSubStepMock();
                var subStepCollection = new System.Collections.ObjectModel.Collection<SubStepBase>();
                subStepCollection.Add(validationStepMock.Object);
                
                step.CascadingSubSteps.Add(i, subStepCollection);

                validationStepMockList.Add(validationStepMock);
            }
            

            //Calling Validate in order to start the 
            step.Validate(context);
            //Setting up a manual reset event
            System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);
            //here we queue up the step.Execute method in a separate thread as the execution model would actually be
            System.Threading.ThreadPool.QueueUserWorkItem((state) =>
            {
                step.Execute(context);
                manualEvent.Set();
            });

            var responseMessageList = new List<Message>(3);

            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            for (int i = 0; i < 3; i++)
            {
                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(xml, Encoding.UTF8);
                msg.Properties.Add("http://schemas.microsoft.com/BizTalk/2003/system-properties#IsSolicitResponse", true);

                var responseMsg = outboundHandler.Execute(msg, TimeSpan.FromSeconds(10));
                responseMessageList.Add(responseMsg);
            }

            //Waiting for the manual event to be set
            manualEvent.WaitOne(1000);

            //string expected = ReadResponseFileContent("TestResponse.xml").Trim().Replace("\r\n", string.Empty).Replace("\t", string.Empty);
            //string actual = GeneralTestHelper.GetBodyAsString(responseMsg, Encoding.UTF8).Trim().Replace("\r\n", string.Empty).Replace("\t", string.Empty);

            //Assert.AreEqual(expected, actual, 
            //    "Response message is not matching the expected content");
            Assert.AreEqual(3, responseMessageList.Count, "The number of response messages is incorrect.");

            loggerMock.Verify(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s)),
                It.Is<string>(s => !string.IsNullOrEmpty(s))),
                Times.Exactly(6),
                "The LogData method was not called");

            for (int i = 0; i < validationStepMockList.Count; i++)
            {
                var validationStepMock = validationStepMockList[i];

                // For cascading validation mode each sub step should be called only once
                validationStepMock.Verify(vs => vs.Execute(
                    It.Is<Stream>(s => s != null),
                    It.Is<Context>(c => c != null)),
                    Times.Exactly(1),
                    "The SubStep mock was not called the expected number of times");
            }
            
        }

        internal static string ReadResponseFileContent(string path)
        {
            return System.IO.File.ReadAllText(path, Encoding.UTF8);
        }
    }
}
