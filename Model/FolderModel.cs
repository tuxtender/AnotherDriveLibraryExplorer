using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AnotherDriveLibraryExplorer.Model
{
    public class FolderModel 
    {
        DirectoryInfo _folder;
        public FolderModel(DirectoryInfo folder)
        {
            this._folder = folder;
        }

        public string Name 
        { 
            get {return _folder.Name; }
        }

        public string FullName
        {
            get { return _folder.FullName; }
        }
        public FolderModel[] GetDirectories()
        {
            try
            {
               FolderModel[] array = (
               from d in _folder.GetDirectories()
               select new FolderModel(d)
               ).ToArray();
                return array;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("UnAuthorizedAccessException: Unable to access directory. ");
                return Array.Empty<FolderModel>();
            }
          
        }
    }
}
