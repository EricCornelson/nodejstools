﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using System.IO;
using System.Windows.Automation;
using EnvDTE;
using Microsoft.NodejsTools;
using Microsoft.TC.TestHostAdapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudioTools;
using TestUtilities;
using TestUtilities.UI;

namespace Microsoft.Nodejs.Tests.UI {
    [TestClass]
    public class NodejsBasicProjectTests : NodejsProjectTest {
        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void AddNewTypeScriptItem() {
            using (var solution = Project("AddNewTypeScriptItem", Compile("server")).Generate().ToVs()) {
                var project = solution.WaitForItem("AddNewTypeScriptItem", "server.js");
                AutomationWrapper.Select(project);

                var dialog = solution.App.OpenDialogWithDteExecuteCommand("Project.AddNewItem");
                var newItem = new NewItemDialog(AutomationElement.FromHandle(dialog));
                newItem.FileName = "NewTSFile.ts";
                newItem.ClickOK();

                solution.App.ExecuteCommand("Build.BuildSolution");
                solution.App.WaitForOutputWindowText("Build", "tsc.exe");
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void TestDebuggerPort() {
            var filename = Path.Combine(TestData.GetTempPath(), Path.GetRandomFileName());
            Console.WriteLine("Temp file is: {0}", filename);
            var code = String.Format(@"
require('fs').writeFileSync('{0}', process.debugPort);
while(true) {{
}}", filename.Replace("\\", "\\\\"));

            var project = Project("DebuggerPort", 
                Compile("server", code), 
                Property(NodejsConstants.DebuggerPort, "1234"),
                Property(CommonConstants.StartupFile, "server.js")
            );

            using (var solution = project.Generate().ToVs()) {
                solution.App.Dte.ExecuteCommand("Debug.Start");
                solution.App.WaitForMode(dbgDebugMode.dbgRunMode);

                for (int i = 0; i < 10 && !File.Exists(filename); i++) {
                    System.Threading.Thread.Sleep(1000);
                }
                Assert.IsTrue(File.Exists(filename), "debugger port not written out");
                solution.App.Dte.ExecuteCommand("Debug.StopDebugging");

                Assert.AreEqual(
                    File.ReadAllText(filename),
                    "1234"
                );
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void TestEnvironmentVariables() {
            var filename = Path.Combine(TestData.GetTempPath(), Path.GetRandomFileName());
            Console.WriteLine("Temp file is: {0}", filename);
            var code = String.Format(@"
require('fs').writeFileSync('{0}', process.env.fob);
while(true) {{
}}", filename.Replace("\\", "\\\\"));

            var project = Project("EnvironmentVariables",
                Compile("server", code),
                Property(NodejsConstants.EnvironmentVariables, "fob=100"),
                Property(CommonConstants.StartupFile, "server.js")
            );

            using (var solution = project.Generate().ToVs()) {
                solution.App.Dte.ExecuteCommand("Debug.Start");
                solution.App.WaitForMode(dbgDebugMode.dbgRunMode);

                for (int i = 0; i < 10 && !File.Exists(filename); i++) {
                    System.Threading.Thread.Sleep(1000);
                }
                Assert.IsTrue(File.Exists(filename), "debugger port not written out");
                solution.App.Dte.ExecuteCommand("Debug.StopDebugging");

                Assert.AreEqual(
                    File.ReadAllText(filename),
                    "100"
                );
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void TestEnvironmentVariablesNoDebugging() {
            var filename = Path.Combine(TestData.GetTempPath(), Path.GetRandomFileName());
            Console.WriteLine("Temp file is: {0}", filename);
            var code = String.Format(@"
require('fs').writeFileSync('{0}', process.env.fob);
", filename.Replace("\\", "\\\\"));

            var project = Project("EnvironmentVariables",
                Compile("server", code),
                Property(NodejsConstants.EnvironmentVariables, "fob=123"),
                Property(CommonConstants.StartupFile, "server.js")
            );

            using (var solution = project.Generate().ToVs()) {
                solution.App.Dte.ExecuteCommand("Debug.StartWithoutDebugging");

                for (int i = 0; i < 10 && !File.Exists(filename); i++) {
                    System.Threading.Thread.Sleep(1000);
                }
                Assert.IsTrue(File.Exists(filename), "debugger port not written out");

                Assert.AreEqual(
                    File.ReadAllText(filename),
                    "123"
                );
            }
        }

        [TestMethod, Priority(0), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void TestProjectProperties() {
            var filename = Path.Combine(TestData.GetTempPath(), Path.GetRandomFileName());

            var project = Project("ProjectProperties",
                Compile("server"),
                Property(NodejsConstants.EnvironmentVariables, "fob=123"),
                Property(NodejsConstants.DebuggerPort, "1234"),
                Property(CommonConstants.StartupFile, "server.js")
            );

            using (var solution = project.Generate().ToVs()) {
                var projectNode = solution.WaitForItem("ProjectProperties");
                AutomationWrapper.Select(projectNode);

                solution.App.Dte.ExecuteCommand("ClassViewContextMenus.ClassViewMultiselectProjectReferencesItems.Properties");
                AutomationElement doc = null;
                for (int i = 0; i < 10; i++) {
                    doc = solution.App.GetDocumentTab("ProjectProperties");
                    if (doc != null) {
                        break;
                    }
                    System.Threading.Thread.Sleep(1000);
                }
                Assert.IsNotNull(doc, "Failed to find project properties tab");

                var debuggerPort = 
                    new TextBox(
                        new AutomationWrapper(doc).FindByAutomationId("_debuggerPort")
                    );
                var envVars = new TextBox(
                    new AutomationWrapper(doc).FindByAutomationId("_envVars")
                );

                Assert.AreEqual(debuggerPort.Value, "1234");
                Assert.AreEqual(envVars.Value, "fob=123");

                debuggerPort.Value = "2468";
                envVars.Value = "baz=246";

                solution.App.Dte.ExecuteCommand("File.SaveAll");

                var projFile = File.ReadAllText(solution.Project.FullName);
                Assert.AreNotEqual(-1, projFile.IndexOf("<DebuggerPort>2468</DebuggerPort>"));
                Assert.AreNotEqual(-1, projFile.IndexOf("<EnvironmentVariables>baz=246</EnvironmentVariables>"));
            }
        }
    }
}