namespace KSEB.Security
{
    public static class OtpHelper
    {
        public static string GenerateOtp()
        {
            return new Random().Next(100000, 999999).ToString();
        }
    }
}
