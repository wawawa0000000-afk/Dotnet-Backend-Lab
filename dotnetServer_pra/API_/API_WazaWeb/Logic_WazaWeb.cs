using Microsoft.AspNetCore.Http;
using System;

public static class Logic_WazaWeb
{
    public static IResult GetInfo(string webPath)
    {
        return Results.Ok(new {
            Project = "和座製作所 ポータルシステム",
            WebDataPath = webPath,
            Status = "Online",
            Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        });
    }
}