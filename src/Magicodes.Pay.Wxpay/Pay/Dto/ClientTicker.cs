using System;
using System.Collections.Generic;
using System.Text;

namespace Magicodes.Pay.Wxpay.Pay.Dto
{
    public static class ClientTicker
    {
        static ClientTicker()
        {
            Token = new CToken();
            Ticket = new CTicket();
        }
        public static CToken Token { get; set; } 
        public static CTicket Ticket { get; set; }
    }

    public class CToken
    {
        public string access_token { get; set; }
        public DateTime TokenStart { get; set; }
        public int expires_in { get; set; } = 0;
    }

    public class CTicket
    {
        public int errcode { get; set; }
        public string errmsg { get; set; }
        public string ticket { get; set; }
        public DateTime TicketStart { get; set; }
        public int expires_in { get; set; } = 0;
    }
}
