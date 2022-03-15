using System;
using System.Collections.Generic;
using System.Text;

namespace Magicodes.Pay.Wxpay.Pay.Models
{
    public class JsApiSign
    {
        public string noncestr { get; set; }
        public string timestamp { get; set; }
        public string url { get; set; }
        public string sign { get; set; }
    }
}
