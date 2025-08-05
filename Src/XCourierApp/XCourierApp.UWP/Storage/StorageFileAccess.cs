using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using XCourierApp;
using XCourierApp.Storage;
using XCourierApp.Storage.UWP;

[assembly: Xamarin.Forms.Dependency(typeof(StorageFileAccess))]
namespace XCourierApp.Storage.UWP
{
    public class StorageFileAccess : IStorageFileAccess
    {
        public string GetStorageFileLocation()
        {
            // Get the local app data folder
            var localFolder = ApplicationData.Current.LocalFolder;
            string dbPath = Path.Combine(localFolder.Path, Constants.OFFLINE_DATABASE_NAME);

            // Ensure the directory exists
            var directory = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create the file if it doesn't exist
            if (!File.Exists(dbPath))
            {
                File.Create(dbPath).Dispose();
            }

            return dbPath;
        }
    } // class
} // namespace