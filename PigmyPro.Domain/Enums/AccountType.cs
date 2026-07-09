namespace PigmyPro.Domain.Enums
{
    public enum AccountType
    {
        Pigmy = 1,
        Loan = 2,
        Recurring = 3
    }

    public static class AccountTypeExtensions
    {
        public static string GetDisplayName(int code1)
        {
            return code1 switch
            {
                1 => "Pigmy",
                2 => "Loan",
                3 => "Recurring",
                _ => "Other"
            };
        }
    }
}
