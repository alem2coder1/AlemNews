using Microsoft.AspNetCore.Mvc;
using MODEL;

namespace COMMON;

public class MessageHelper
{
    public static IActionResult RedirectAjax(string message, string status, string backUrl, object data)
    {
        return new JsonResult(new AjaxMsgModel()
        {
            Message = message,
            Status = status,
            BackUrl = backUrl,
            Data = data
        });
    }
}