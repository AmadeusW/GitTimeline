using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloaderApp
{
    class Program
    {
        private static string _token;
        private static string _repo;
        private static string _targetPath;

        static void Main(string[] args)
        {
            GetRepoData();
            GetSaveLocation();
            DownloadData();
        }

        private static void DownloadData()
        {
            
        }

        private static void GetSaveLocation()
        {
            bool inputIsCorrect;
            do
            {
                var docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                Console.WriteLine("Target filename: ");
                var fileName = Console.ReadLine();
                Console.WriteLine("Press [Y] if this target path is correct:");
                _targetPath = Path.Combine(docsPath, fileName);
                Console.WriteLine($"{_targetPath}");
                var key = Console.ReadKey().KeyChar.ToString();
                Console.WriteLine();
                if (key.Equals("Y", StringComparison.InvariantCultureIgnoreCase))
                {
                    inputIsCorrect = true;
                }
                else
                {
                    inputIsCorrect = false;
                }
            } while (!inputIsCorrect);
        }

        private static void GetRepoData()
        {
            bool inputIsCorrect;
            do
            {
                Console.WriteLine("GitHub token: ");
                _token = Console.ReadLine();
                Console.WriteLine("GitHub repo: ");
                _repo = Console.ReadLine();
                Console.WriteLine("Press [Y] if this information is correct:");
                Console.WriteLine($"{_token}, {_repo}");
                var key = Console.ReadKey().KeyChar.ToString();
                Console.WriteLine();
                if (key.Equals("Y", StringComparison.InvariantCultureIgnoreCase))
                {
                    inputIsCorrect = true;
                }
                else
                {
                    inputIsCorrect = false;
                }
            } while (!inputIsCorrect);
        }
    }
}
