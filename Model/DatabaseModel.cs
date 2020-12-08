using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Security.Cryptography;

namespace AnotherDriveLibraryExplorer.Model
{
    public class DatabaseModel
    {
        const int USERID = 1;   //Intend first user get access to own files 
        string db = @"URI=file:db.sqlite3";
        int folderCounter;
        int fileCounter;
        public DatabaseModel(string path)
        {
            Folder root = new Folder(path, null);
            AddRootFolder(root);
            TraverseTree(root);
        }

        public void TraverseTree(Folder root)
        {
            // Data structure to hold names of subfolders to be
            // examined for files.
            Stack<Folder> dirs = new Stack<Folder>(20);

            if (!System.IO.Directory.Exists(root.original))
            {
                throw new ArgumentException();
            }

            dirs.Push(root);

            while (dirs.Count > 0)
            {
                Folder currentDir = dirs.Pop();

                string[] subDirs;
                try
                {
                    subDirs = System.IO.Directory.GetDirectories(currentDir.original);
                }
                // An UnauthorizedAccessException exception will be thrown if we do not have
                // discovery permission on a folder or file. It may or may not be acceptable
                // to ignore the exception and continue enumerating the remaining files and
                // folders. It is also possible (but unlikely) that a DirectoryNotFound exception
                // will be raised. This will happen if currentDir has been deleted by
                // another application or thread after our call to Directory.Exists. The
                // choice of which exceptions to catch depends entirely on the specific task
                // you are intending to perform and also on how much you know with certainty
                // about the systems on which this code will run.
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (System.IO.DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }

                string[] files = null;
                try
                {
                    files = System.IO.Directory.GetFiles(currentDir.original);
                    foreach (string file in files)
                    {
                        File newFile = new File(file, currentDir);
                        AddFile(newFile);
                    }

                }
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (System.IO.DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }


                // Push the subdirectories onto the stack for traversal.
                // This could also be done before handing the files.
                foreach (string dir in subDirs)
                {
                    Folder newFolder = new Folder(dir, currentDir);
                    AddFolder(newFolder);
                    dirs.Push(newFolder);
                }
            }


            void AddFile(File file)
            {
                using (SQLiteConnection connection = new SQLiteConnection(db))
                {
                    var command = connection.CreateCommand();
                    // Perform the required action on each file here.
                    // Modify this block to perform your required task.
                    try
                    {
                        connection.Open();
                        command.CommandText =
                        @"
                            INSERT INTO filestorage_source
                            (id, item, img)
                            VALUES
                            ($id, $item, $img)
                        ";
                        command.Parameters.AddWithValue("$id", file.id);
                        command.Parameters.AddWithValue("$item", file.original);
                        command.Parameters.AddWithValue("$img", file.GetThumbnail());
                        command.ExecuteNonQuery();

                        command.CommandText =
                        @"
                            INSERT INTO filestorage_file
                            (id, name, path, contributor_id, original_id)
                            VALUES
                            ($id, $name, $path, $contributor_id, $original_id)
                        ";
                        command.Parameters.AddWithValue("$id", file.id);
                        command.Parameters.AddWithValue("$name", file.name);
                        command.Parameters.AddWithValue("$path", file.path);
                        command.Parameters.AddWithValue("$contributor_id", USERID);
                        command.Parameters.AddWithValue("$original_id", file.id);
                        command.ExecuteNonQuery();

                        command.CommandText =
                        @"
                            INSERT INTO filestorage_folder_files
                            (folder_id, file_id)
                            VALUES
                            ($folder_id, $file_id)
                        ";
                        command.Parameters.AddWithValue("$file_id", file.id);
                        command.Parameters.AddWithValue("$folder_id", file.parent.id);
                        command.ExecuteNonQuery();

                        connection.Close();

                        fileCounter++;
                    }
                    catch (System.IO.FileNotFoundException e)
                    {
                        // If file was deleted by a separate application
                        //  or thread since the call to TraverseTree()
                        // then just continue.
                        Console.WriteLine(e.Message);

                    }
                }

            }

        }

        void AddRootFolder(Folder folder)
        {
            AddFolder(folder, true);
        }
    
        void AddFolder(Folder folder, bool isRoot=false)
        {
            using (SQLiteConnection connection = new SQLiteConnection(db))
            {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText =
                @"
                    INSERT INTO filestorage_folder
                    (id, name, path, owner_id, parent_id)
                    VALUES
                    ($id, $name, $path, $owner_id, $parent_id)
                ";
                command.Parameters.AddWithValue("$id", folder.id);
                command.Parameters.AddWithValue("$name", folder.name);
                command.Parameters.AddWithValue("$path", folder.path);
                command.Parameters.AddWithValue("$owner_id", USERID);
                if (isRoot)
                {
                    command.Parameters.AddWithValue("$parent_id", GetRootId());
                } 
                else
                {
                    command.Parameters.AddWithValue("$parent_id", folder.parent.id);
                }

                command.ExecuteNonQuery();

                connection.Close();
                folderCounter++;
            }
        }

        private string GetRootId()
        {
            string root = null;
            using (SQLiteConnection connection = new SQLiteConnection(db))
            {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText =
                @"
                    SELECT id
                    FROM filestorage_folder
                    WHERE owner_id = $user_id AND path = '' AND name = ''
                ";
                command.Parameters.AddWithValue("$user_id", USERID);

                
                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        reader.Read();
                        root = reader.GetString(0);
                    }
                }
                catch (System.InvalidOperationException e)
                {
                    Console.WriteLine("Root folder is absent.");
                    System.Windows.Application.Current.Shutdown();
                }
                return root;

            }
        }

        public string GetStatistic()
        {
            return $"Added {fileCounter} files {folderCounter} folders.";
        }

  

    }



    public class Folder : Item
    {
        public Folder(string name, Folder parent) : base(name, parent, 16)
        {
        }

    }

    public class File : Item
    {
        public File(string name, Folder parent) : base(name, parent, 8)
        {
        }

        /// <summary>
        /// Make thumbnail and return location 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetThumbnail()
        {
            //TODO: Process a image
            return name;
        }

        /// <summary>
        /// Relative path to resource directory
        /// </summary>
        /// <param name="relativeTo"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public string GetRelativePath(string relativeTo, string path)
        {
            var uri = new Uri(relativeTo);
            var rel = Uri.UnescapeDataString(uri.MakeRelativeUri(new Uri(path)).ToString()).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (rel.Contains(Path.DirectorySeparatorChar.ToString()) == false)
            {
                rel = $".{ Path.DirectorySeparatorChar }{ rel }";
            }
            return rel;
        }
    }

    public class Item
    {
        readonly public string name;
        readonly public string path;
        readonly public string id;
        readonly public Item parent;
        readonly public string original;

        public Item(string fullName, Item parentFolder, int lengthKey)
        {
            original = fullName;
            parent = parentFolder;
            name = GetName();
            path = GetPath();
            id = GenerateId(lengthKey);
        }

        private string GetName()
        {
            return Path.GetFileName(original);
        }

        /// <summary>
        /// Unix style path is relative to selected directory.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetPath()
        {
            if (parent == null)
            {
                return "/";
            }
            string path = Path.GetDirectoryName(parent.original);
            string s = original.Replace(path, null);
            s = s.Replace('\\', '/');
            return s;
        }

        /// <summary>
        /// Thread safe secure token generator.
        /// </summary>
        /// <param name="keyLength"></param>
        /// <returns></returns>
        private string GenerateId(int keyLength)
        {
            RNGCryptoServiceProvider rngCryptoServiceProvider = new RNGCryptoServiceProvider();
            byte[] randomBytes = new byte[keyLength];
            rngCryptoServiceProvider.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

    }


}





