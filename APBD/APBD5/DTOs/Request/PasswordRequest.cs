using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APBD5.DTOs.RequestModels
{
    public class PasswordRequest
    {
        public string OldPass { set; get; }
        public string NewPass { set; get; }


    }
}