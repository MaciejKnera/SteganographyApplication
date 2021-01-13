using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MVCUI.Models
{
    public class InputForm
    {
        public string Message { get; set; }
        public byte[] CarrierFile { get; set; }
        public byte[] FileToHide { get; set; }
    }
}
