using System;
using Microsoft.AspNetCore.Mvc;

namespace DFC.ServiceTaxonomy.ApiFunction.Exceptions
{
    public class ApiFunctionException : Exception
    {
        public ActionResult ActionResult { get; }
        
        // just 1 ctor accepting ActionResult?
        public ApiFunctionException(StatusCodeResult statusCodeResult, string logMessage)
        : base(logMessage)
        {
            ActionResult = statusCodeResult;
        }

        public ApiFunctionException(ObjectResult objectResult, string logMessage)
        : base(logMessage)
        {
            ActionResult = objectResult;
        }

        public static ApiFunctionException BadRequest(string message)
        {
            return new ApiFunctionException(new BadRequestObjectResult(message), message);
        }
    }
}