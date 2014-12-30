using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Authentication
{
    public enum TokenResults
    {
        DoesntExist,
        ExistAndWrong,
        ExistsAndCorrect
    }
}