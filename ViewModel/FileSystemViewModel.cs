using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using AnotherDriveLibraryExplorer.Model;
using System.Windows.Input;
using AnotherDriveLibraryExplorer.Command;
using System.ComponentModel;

namespace AnotherDriveLibraryExplorer.ViewModel
{
    /// <summary>
    /// The ViewModel for the AnotherDriveLibraryExplorer.
    /// This exposes a read-only collection of file system items.
    /// </summary>
    class FileSystemViewModel : INotifyPropertyChanged
    {
        private string _result;
        private ICommand _saveCommand;

        readonly ReadOnlyCollection<DriveViewModel> _drives;
        


        public FileSystemViewModel()
        {
            List<DriveViewModel> list = new List<DriveViewModel>();
            string[] driveNames = System.Environment.GetLogicalDrives();
            foreach (string dr in driveNames)
            {
                System.IO.DriveInfo di = new System.IO.DriveInfo(dr);
                // Here we skip the drive if it is not ready to be read. This
                // is not necessarily the appropriate action in all scenarios.
                if (!di.IsReady)
                {
                    Console.WriteLine("The drive {0} could not be read", di.Name);
                    continue;
                }
                FolderModel root = new FolderModel(di.RootDirectory);
                list.Add(new DriveViewModel(root));
            }

            _drives = new ReadOnlyCollection<DriveViewModel>(list);
        }

        public ReadOnlyCollection<DriveViewModel> Drives
        {
            get { return _drives; }
        }

        public string Statistic
        {
            get { return _result; }
            set
            {
                _result = value;
                this.OnPropertyChanged("Statistic");
            }
        }

        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand == null)
                {
                    _saveCommand = new RelayCommand(
                        param => this.SaveObject(),
                        param => this.CanSave()
                    );
                }
                return _saveCommand;
            }
        }

        
        private bool CanSave()
        {
            // Verify command can be executed here
            if (TreeViewItemViewModel.SelectedItem == null)
            {
                return false;
            }
            return true;
        }

        private void SaveObject()
        {
            // Save command execution logic
            FolderViewModel f = (FolderViewModel)TreeViewItemViewModel.SelectedItem;
            DatabaseModel db = new DatabaseModel(f.FullName);
            Statistic = db.GetStatistic();
            FolderViewModel.ResetSelectedItem();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
