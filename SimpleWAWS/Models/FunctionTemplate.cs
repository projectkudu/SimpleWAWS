namespace SimpleWAWS.Models
{
    public class FunctionTemplate : BaseTemplate
    {
        public static BaseTemplate DefaultFunctionTemplate(string templateName)
        {
            return new FunctionTemplate() { Name = templateName, AppService = AppService.Function }; 
        }
    }
}