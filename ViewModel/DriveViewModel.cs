using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using AnotherDriveLibraryExplorer.Model;

namespace AnotherDriveLibraryExplorer.ViewModel
{
    public class DriveViewModel : FolderViewModel
    {
        public DriveViewModel(FolderModel drive) : base(drive)
        {
        }
       
    }
}
