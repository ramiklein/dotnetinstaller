using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using dotNetUnitTestsRunner;
using System.Windows.Automation;
using System.Diagnostics;
using System.Threading;
using White.Core;
using White.Core.Factory;
using White.Core.UIItems;
using White.Core.UIItems.WindowItems;
using White.Core.UIItems.WindowStripControls;
using White.Core.UIItems.MenuItems;
using White.Core.UIItems.TreeItems;
using White.Core.UIItems.Finders;
using White.Core.UIItems.Custom;
using White.Core.UIItems.TableItems;
using White.Core.WindowsAPI;
using System.IO;
using InstallerLib;
using System.Web;
using System.Net;

namespace InstallerEditorUnitTests
{
    [TestFixture]
    public class UITemplateUnitTests
    {
        [Test]
        public void TestAddSetupConfigurationLang()
        {
            using (Application installerEditor = Application.Launch(InstallerEditorExeUtils.Executable))
            {
                Window mainWindow = installerEditor.GetWindow("Installer Editor", InitializeOption.NoCache);
                UIAutomation.Find<MenuBar>(mainWindow, "Application").MenuItem("File", "New").Click();
                Tree configurationTree = UIAutomation.Find<Tree>(mainWindow, "configurationTree");
                TreeNode configurationNode = configurationTree.SelectedNode;
                string prevousCancelCaptionValue = string.Empty;
                string[] languages = { "English", "Deutsch", "Fran�ais", "Italiano" };
                foreach (string language in languages)
                {
                    configurationNode.Select();
                    UIAutomation.Find<MenuBar>(mainWindow, "Application").MenuItem("Tools", "Template For New Item", language).Click();
                    UIAutomation.Find<MenuBar>(mainWindow, "Application").MenuItem("Edit", "Add", "Configurations", "Setup Configuration").Click();
                    Panel propertyGrid = UIAutomation.Find<Panel>(mainWindow, "propertyGrid");
                    TableHeader cancelCaptionItem = UIAutomation.Find<TableHeader>(propertyGrid, "cancel_caption");
                    ValuePattern cancelCaptionValuePattern = (ValuePattern)cancelCaptionItem.AutomationElement.GetCurrentPattern(ValuePattern.Pattern);
                    string currentCancelCaptionValue = cancelCaptionValuePattern.Current.Value;
                    Console.WriteLine(" {0}: {1}", language, currentCancelCaptionValue);
                    Assert.AreNotEqual(currentCancelCaptionValue, prevousCancelCaptionValue);
                    prevousCancelCaptionValue = currentCancelCaptionValue;
                }
                // note: the selection is not saved since the application is killed
            }
        }

        [Test]
        public void TestCustomizeTemplates()
        {
            using (Application installerEditor = Application.Launch(InstallerEditorExeUtils.Executable))
            {
                Window mainWindow = installerEditor.GetWindow("Installer Editor", InitializeOption.NoCache);
                Menu templateForNewItem = UIAutomation.Find<MenuBar>(mainWindow, "Application").MenuItem("Tools", "Template For New Item");
                templateForNewItem.Click();
                int countBefore = templateForNewItem.ChildMenus.Count;
                UIAutomation.Find<Menu>(templateForNewItem, "English").Click();
                // add an item
                UIAutomation.Find<MenuBar>(mainWindow, "Application").MenuItem("Tools", "Customize Templates").Click();
                Window customizeTemplatesWindow = mainWindow.ModalWindow("Customize Templates");
                Panel gridList = UIAutomation.Find<Panel>(customizeTemplatesWindow, "gridList");
                gridList.DoubleClick();
                gridList.Keyboard.Enter(string.Format(@"{0}\english_template.xml", InstallerLibUtils.TemplatesLocation));
                gridList.KeyIn(KeyboardInput.SpecialKeys.RETURN);
                UIAutomation.Find<Button>(customizeTemplatesWindow, "OK").Click();
                // check whether the item was added
                templateForNewItem = UIAutomation.Find<MenuBar>(mainWindow, "Application").MenuItem("Tools", "Template For New Item");
                templateForNewItem.Click();
                int countAfter = templateForNewItem.ChildMenus.Count;
                UIAutomation.Find<Menu>(templateForNewItem, "English").Click();
                Assert.AreEqual(countBefore + 1, countAfter);
                // note: the updated list is not saved since the application is killed
            }
        }
    }
}
