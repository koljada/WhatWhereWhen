using SimpleEchoBot.ErrorHandler;
using System.Web;
using System.Web.Mvc;

namespace SimpleEchoBot
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new AiHandleErrorAttribute());
           // filters.Add(new ExceptionFilter());
        }
    }
}
