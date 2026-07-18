using System;
using System.Collections.Generic;
using System.Text;

namespace PigmyPro.Domain.Entities
{
    public class Bank
    {
        public int BankID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? ContactNo { get; set; }
        public string? ContactPerson { get; set; }
        public string? EmailID { get; set; }
        public bool ActiveYN { get; set; } = true;
        public DateTime EntryDateTime { get; set; }
        public long CollectionGLCode { get; set; }
        public char hasCBS { get; set; } = 'N';
        public char? RecieptPrinting { get; set; } = 'N';
        public int? No_of_Holidays { get; set; }
        public byte[]? Logo { get; set; }
        public string? LogoFileName { get; set; }
        public string? AppLoginPrefix { get; set; }
    }
}
