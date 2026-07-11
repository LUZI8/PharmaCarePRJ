
namespace PharmaCare.Helpers.FileHelper
{
    public class FileHelper : IFileHelper
    {
        IWebHostEnvironment env;

        public FileHelper(IWebHostEnvironment env)
        {
            this.env = env;
        }

        public string SaveImage(IFormFile file, string oldImageName, string folderName)
        {
            string imageName = "";
            var allowedExt = new List<string> {".jpeg", ".jpg", ".png" };
            if (file != null)
            {
                var folderPath = Path.Combine(env.WebRootPath, folderName);
                if (!Directory.Exists(folderPath)) { 
                    Directory.CreateDirectory(folderPath);
                }
                FileInfo fileInfo = new FileInfo(file.FileName);
                var ext = fileInfo.Extension;
                if (allowedExt.Contains(ext))
                {
                    var imageTempName = "Image" + Guid.NewGuid() + "_" + file.FileName;

                    var fullPath = Path.Combine(folderPath, imageTempName);
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }
                    imageName = "/"+ folderName + "/" + imageTempName;
                }
                else
                {
                    imageName = "Error";
                }
                
            }
            else
            {
                imageName = oldImageName;
            }
            return imageName;
        }
    }
}
