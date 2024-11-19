using NUnit.Framework;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTest
{
    [TestFixture]
    public class FolderSyncServiceTests
    {
        private string testSourceFolder;
        private string testReplicaFolder;
        private string logFilePath;

        [SetUp]
        public void Setup()
        {
            testSourceFolder = Path.Combine(Path.GetTempPath(), "TestSource");
            testReplicaFolder = Path.Combine(Path.GetTempPath(), "TestReplica");
            logFilePath = Path.Combine(Path.GetTempPath(), "sync.log");

            // Create test directories
            Directory.CreateDirectory(testSourceFolder);
            Directory.CreateDirectory(testReplicaFolder);
        }

        [TearDown]
        public void Cleanup()
        {
            // Clean up test directories
            if (Directory.Exists(testSourceFolder))
                Directory.Delete(testSourceFolder, true);
            if (Directory.Exists(testReplicaFolder))
                Directory.Delete(testReplicaFolder, true);
            if (File.Exists(logFilePath))
                File.Delete(logFilePath);
        }

        [Test]
        public async Task SyncFoldersAsync_CopiesNewFile()
        {
            // Arrange
            string sourceFile = Path.Combine(testSourceFolder, "test.txt");
            File.WriteAllText(sourceFile, "Hello, World!");

            // Act
            await FolderSyncService.SyncFoldersAsync(testSourceFolder, testReplicaFolder, logFilePath, CancellationToken.None);

            // Assert
            Assert.IsTrue(File.Exists(Path.Combine(testReplicaFolder, "test.txt")));
        }

        [Test]
        public async Task SyncFoldersAsync_RemovesDeletedFile()
        {
            // Arrange
            string sourceFile = Path.Combine(testSourceFolder, "test.txt");
            File.WriteAllText(sourceFile, "Hello, World!");
            await FolderSyncService.SyncFoldersAsync(testSourceFolder, testReplicaFolder, logFilePath, CancellationToken.None);

            // Act
            File.Delete(sourceFile);
            await FolderSyncService.SyncFoldersAsync(testSourceFolder, testReplicaFolder, logFilePath, CancellationToken.None);

            // Assert
            Assert.IsFalse(File.Exists(Path.Combine(testReplicaFolder, "test.txt")));
        }
    }
}