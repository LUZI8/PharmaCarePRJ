namespace PharmaCare.Helpers.FileHelper
{
    public interface IFileHelper
    {
        String SaveImage(IFormFile file, string oldImageName, string folderName);
    }
}
