using JsonQuickStart;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestJsonQuickStart
{
    [TestClass]
    public class TestJsonFileSystemManager
    {
        [TestMethod]
        public void TestSimpleStringSave()
        {
            string testString = "test";
            var dataSource = "C:\\Ner\\Data\\ByoNlp\\";

            var jsonFileSystemManager = new JsonFileSystemManager();
            Assert.IsTrue(jsonFileSystemManager.InsertItem(dataSource, testString, "string", typeof(string), testString, testString));
        }
    }
}
