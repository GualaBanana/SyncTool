﻿namespace SyncTool
{
    class Sync
    {
        readonly Cloud _cloud;
        readonly Tracker _tracker;

        public List<string> AlreadyTracked => _tracker.TrackList;
        static int NewSyncEntries { get; set; } = 0;


        public Sync()
        {
            // Dictates the order of components' initialization.
            Directory.CreateDirectory(Config.InstallationPath);
            _cloud = new();
            _tracker = new();
        }


        public void StartTracking(string directory)
        {
            UsagePolicy.MustBeOnRightDrive(directory);
            UsagePolicy.MustExist(directory);
            UsagePolicy.MustBeNotTracked(directory, _tracker);

            _tracker.Add(directory);
        }
        public void StopTracking(string directory)
        {
            UsagePolicy.MustBeTracked(directory, _tracker);

            _tracker.Remove(directory);
        }
        // Make event that is raised when synchronization is completed. Fosho!
        public void Synchronize()
        {
            NewSyncEntries = 0;
            SynchronizeDirectories();
            SynchronizeFiles(source: _tracker, destination: _cloud);

            if (NewSyncEntries > 0) OnSyncCompletion?.Invoke("Synchronization completed.", NewSyncEntries);
        }
        public void SynchronizeWithCloud() => SynchronizeFiles(_cloud, _tracker);
        void SynchronizeDirectories()
        {
            var newDirectories = _tracker.NewDirectories;

            if (!newDirectories.Any()) return;
            NewSyncEntries += newDirectories.Count;

            newDirectories.ForEach(directory => StartTracking(directory));

            foreach (var directory in _tracker.TrackList.Select(directory => _tracker.RelativeName(directory)))
                Directory.CreateDirectory(_cloud.FullPathFromRelative(directory));
        }
        static void SynchronizeFiles(IRelativePathManager source, IRelativePathManager destination)
        {
            var newFiles = source.RelativeFileNames.Except(destination.RelativeFileNames);

            if (!newFiles.Any()) return;
            NewSyncEntries += newFiles.Count();

            foreach (string file in newFiles)
            {
                string? fileLocation;
                if ((fileLocation = Path.GetDirectoryName(file)) != null)
                {
                    string destinationDirectory = destination.FullPathFromRelative(fileLocation);
                    Directory.CreateDirectory(destinationDirectory);
                }

                if (!File.Exists(file))
                {
                    string from = source.FullPathFromRelative(file);
                    string to = destination.FullPathFromRelative(file);
                    File.Copy(from, to);
                }
            }
        }


        public event EventHandler<int>? OnSyncCompletion;
    }
}
