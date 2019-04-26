using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;

namespace SimpleWAWS.Models
{
    public class InUseResourceEntity:TableEntity 
    {
        public string ResourceGroup { get; set; }
    }
}