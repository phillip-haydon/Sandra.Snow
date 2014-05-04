namespace Snow.StaticFileProcessors
{
    using Enums;
    using Extensions;
    using Models;
    using Nancy.Testing;
    using System;
    using System.IO;
    using System.Linq;

    public class PostsProcessor : BaseProcessor
    {
        public override string ProcessorName
        {
            get { return "posts"; }
        }

        protected override void Impl(SnowyData snowyData, SnowSettings settings)
        {
            var filteredPosts = snowyData.Files.Where(ShouldProcess).ToList();

            var pageSize = settings.PageSize;
            var skip = 0;
            var iteration = 1;
            var currentIteration = filteredPosts.Skip(skip).Take(pageSize).ToList();
            var totalPages = (int)Math.Ceiling((double)filteredPosts.Count / pageSize);

            TestModule.TotalPages = totalPages;

            while (currentIteration.Any())
            {
                var folder = "page" + iteration;
                var fileName = "index.html";

                if (skip <= 1)
                {
                    //first iteration
                    folder = "";
                    fileName = DestinationName;
                }

                TestModule.PostsPaged = currentIteration.ToList();
                TestModule.PageNumber = iteration;
                TestModule.HasNextPage = iteration < totalPages;
                TestModule.HasPreviousPage = iteration > 1 && totalPages > 1;
                TestModule.GeneratedUrl = (settings.SiteUrl + "/" + Destination + "/" + folder).TrimEnd('/') + "/";

                var result = snowyData.Browser.Post("/static");

                result.ThrowIfNotSuccessful(snowyData.File.File);

                var outputFolder = Path.Combine(snowyData.Settings.Output, Destination, folder);

                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                File.WriteAllText(Path.Combine(outputFolder, fileName), result.Body.AsString());

                skip += pageSize;
                iteration++;
                currentIteration = filteredPosts.Skip(skip).Take(pageSize).ToList();
            }
        }

        private bool ShouldProcess(Post post)
        {
            return post.Published == Published.True;
        }

        protected override void ParseDirectories(SnowyData snowyData)
        {
            var source = snowyData.File.File;

            var sourceFile = source;
            var destinationDirectory = Path.Combine(snowyData.Settings.Output, source.Substring(0, snowyData.File.File.IndexOf('.')));
            var destinationName = source.Substring(0, snowyData.File.File.IndexOf('.'));

            if (source.Contains(" => "))
            {
                var directorySplit = source.Split(new[] { " => " }, StringSplitOptions.RemoveEmptyEntries);

                sourceFile = directorySplit[0];

                var indexOfDirectory = directorySplit[1].LastIndexOfAny(new []{'/', '\\'});

                if (indexOfDirectory > 0)
                {
                    destinationDirectory = directorySplit[1].Substring(0, indexOfDirectory);
                    destinationName = directorySplit[1].Substring(indexOfDirectory, directorySplit[1].Length - indexOfDirectory);
                }
                else
                {
                    destinationDirectory = "/";
                    destinationName = directorySplit[1];
                }
            }

            SourceFile = sourceFile;
            Destination = destinationDirectory.Trim(new[] { '/', '\\' });
            DestinationName = destinationName.Trim(new[] { '/', '\\' });
        }
    }
}