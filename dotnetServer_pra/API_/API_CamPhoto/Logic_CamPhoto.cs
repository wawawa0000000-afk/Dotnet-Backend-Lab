using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;

public static class Logic_CamPhoto
{
    public static IResult GetPhotos(string photoPath)
    {
        // フォルダが存在しない場合のエラー回避
        if (!Directory.Exists(photoPath)) 
        {
            return Results.NotFound("写真フォルダが見つかりません。");
        }

        var files = Directory.GetFiles(photoPath, "*.*", SearchOption.AllDirectories)
                             .Select(file => new {
                                 Name = Path.GetFileName(file),
                                 Url = $"/photos/{Path.GetRelativePath(photoPath, file).Replace("\\", "/")}"
                             });
                             
        return Results.Ok(files);
    }
}