using AnotherDriveLibraryExplorer.Model;

namespace AnotherDriveLibraryExplorer.ViewModel
{
    public class FolderViewModel : TreeViewItemViewModel
    {
        readonly protected FolderModel _folder;

        public FolderViewModel(FolderModel folder, FolderViewModel parentFolder=null) : base(parentFolder, true)
        {
            _folder = folder;
        }

        public string Name
        {
            get { return _folder.Name; }
        }

        public string FullName
        {
            get { return _folder.FullName; }
        }

        protected override void LoadChildren()
        {
            foreach (FolderModel d in _folder.GetDirectories())
            {
                base.Children.Add(new FolderViewModel(d, this));
            }

        }
    }
}
